using System.Net;
using System.Text;
using System.Text.Json;
using KanBagis.Business.Bagiscilar;
using KanBagis.Business.DisServisler;
using KanBagis.DataAccess.Repositories;
using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace KanBagis.Tests;

/// <summary>
/// Gerçek internete çıkmadan HTTP yanıtı üreten sahte sunucu. Her isteği kaydeder;
/// sıradaki yanıtı ya da istisnayı (zaman aşımı, ağ hatası) döner.
/// </summary>
public sealed class SahteHttp : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> _yanitlar = new();
    public List<(HttpRequestMessage Istek, string? Govde)> Istekler { get; } = [];

    public SahteHttp Yanit(HttpStatusCode kod, string govde = "", Action<HttpResponseMessage>? ayarla = null)
    {
        _yanitlar.Enqueue((_, _) =>
        {
            var y = new HttpResponseMessage(kod) { Content = new StringContent(govde, Encoding.UTF8, "application/json") };
            ayarla?.Invoke(y);
            return Task.FromResult(y);
        });
        return this;
    }

    public SahteHttp HicYanitVerme()
    {
        _yanitlar.Enqueue(async (_, ct) => { await Task.Delay(Timeout.Infinite, ct); return null!; });
        return this;
    }

    public SahteHttp AgHatasi()
    {
        _yanitlar.Enqueue((_, _) => throw new HttpRequestException("Ağa ulaşılamadı"));
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken ct)
    {
        Istekler.Add((istek, istek.Content is null ? null : await istek.Content.ReadAsStringAsync(ct)));
        return await _yanitlar.Dequeue()(istek, ct);
    }

    public HttpClient Istemci() => new(this);
}

// =====================================================================
// Harita / yol mesafesi API'si (OSRM)
// =====================================================================
public class OsrmMesafeTestleri
{
    private static readonly AyarlanabilirZaman Zaman = new(new DateTimeOffset(2026, 11, 16, 9, 0, 0, TimeSpan.Zero));

    private static (OsrmMesafeServisi Servis, SahteHttp Http) Kur(MesafeApiAyarlari? ayar = null, MesafeOnbellegi? onbellek = null)
    {
        var http = new SahteHttp();
        var servis = new OsrmMesafeServisi(http.Istemci(), onbellek ?? new MesafeOnbellegi(),
            Options.Create(ayar ?? new MesafeApiAyarlari { IstekArasiEnAzMilisaniye = 0, ZamanAsimiSaniye = 1 }),
            Zaman, NullLogger<OsrmMesafeServisi>.Instance);
        return (servis, http);
    }

    private const string BasariliYanit = """{"code":"Ok","distances":[[0,144236.4,139371]],"durations":[[0,6420.3,6000]]}""";

    [Fact]
    public async Task Yanit_ayristirilir_metre_km_saniye_dakikaya_cevrilir()
    {
        var (servis, http) = Kur();
        http.Yanit(HttpStatusCode.OK, BasariliYanit);

        var sonuc = await servis.HesaplaAsync("Bingöl", ["Elazığ", "Diyarbakır"]);

        Assert.Equal(new YolBilgisi(144.2, 107, MesafeKaynagi.YolApi), sonuc["Elazığ"]);
        Assert.Equal(new YolBilgisi(139.4, 100, MesafeKaynagi.YolApi), sonuc["Diyarbakır"]);

        // REST isteği: GET, boylam,enlem sırası, merkez 0. kaynak.
        var istek = Assert.Single(http.Istekler).Istek;
        Assert.Equal(HttpMethod.Get, istek.Method);
        Assert.Contains("/table/v1/driving/40.5,38.88;39.22,38.67;40.24,37.91", istek.RequestUri!.ToString());
        Assert.Contains("sources=0", istek.RequestUri.Query);
    }

    [Fact]
    public async Task Ayni_il_icin_api_cagrilmaz()
    {
        var (servis, http) = Kur();

        var sonuc = await servis.HesaplaAsync("Bingöl", ["Bingöl"]);

        Assert.Equal(MesafeKaynagi.AyniIl, sonuc["Bingöl"].Kaynak);
        Assert.Empty(http.Istekler);
    }

    [Fact]
    public async Task Basarili_sonuc_onbellege_alinir_ikinci_istek_gitmez()
    {
        var (servis, http) = Kur();
        http.Yanit(HttpStatusCode.OK, BasariliYanit);

        await servis.HesaplaAsync("Bingöl", ["Elazığ", "Diyarbakır"]);
        var ikinci = await servis.HesaplaAsync("Bingöl", ["Elazığ"]);

        Assert.Single(http.Istekler);
        Assert.Equal(MesafeKaynagi.YolApi, ikinci["Elazığ"].Kaynak);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, "")]
    [InlineData(HttpStatusCode.BadRequest, """{"code":"InvalidQuery"}""")]
    [InlineData(HttpStatusCode.OK, "bu JSON değil")]
    [InlineData(HttpStatusCode.OK, """{"code":"NoTable","message":"yol bulunamadı"}""")]
    public async Task Hata_ya_da_bozuk_yanitta_kus_ucusuna_doner(HttpStatusCode kod, string govde)
    {
        var (servis, http) = Kur();
        http.Yanit(kod, govde);

        var sonuc = await servis.HesaplaAsync("Bingöl", ["Elazığ"]);

        Assert.Equal(MesafeKaynagi.KusUcusu, sonuc["Elazığ"].Kaynak);
        Assert.InRange(sonuc["Elazığ"].Km, 100, 130);
    }

    [Fact]
    public async Task Zaman_asiminda_kus_ucusuna_doner()
    {
        var (servis, http) = Kur();
        http.HicYanitVerme();   // sunucu hiç yanıt vermiyor; 1 sn sonra vazgeçilir

        var sonuc = await servis.HesaplaAsync("Bingöl", ["Elazığ"]);

        Assert.Equal(MesafeKaynagi.KusUcusu, sonuc["Elazığ"].Kaynak);
    }

    [Fact]
    public async Task Ag_hatasinda_kus_ucusuna_doner()
    {
        var (servis, http) = Kur();
        http.AgHatasi();

        Assert.Equal(MesafeKaynagi.KusUcusu, (await servis.HesaplaAsync("Bingöl", ["Elazığ"]))["Elazığ"].Kaynak);
    }

    [Fact]
    public async Task Hiz_sinirinda_429_sunucunun_dedigi_sure_boyunca_api_cagrilmaz()
    {
        var (servis, http) = Kur();
        http.Yanit(HttpStatusCode.TooManyRequests, "", y => y.Headers.RetryAfter = new(TimeSpan.FromSeconds(120)));

        var ilk = await servis.HesaplaAsync("Bingöl", ["Elazığ"]);
        var ikinci = await servis.HesaplaAsync("Bingöl", ["Muş"]);   // başka il, ama bekleme süresi dolmadı

        Assert.Equal(MesafeKaynagi.KusUcusu, ilk["Elazığ"].Kaynak);
        Assert.Equal(MesafeKaynagi.KusUcusu, ikinci["Muş"].Kaynak);
        Assert.Single(http.Istekler);   // ikinci çağrıda sunucuya gidilmedi
    }

    [Fact]
    public async Task Yol_bulunamayan_hedef_kus_ucusuna_digerleri_api_ile()
    {
        var (servis, http) = Kur();
        http.Yanit(HttpStatusCode.OK, """{"code":"Ok","distances":[[0,null,139371]],"durations":[[0,null,6000]]}""");

        var sonuc = await servis.HesaplaAsync("Bingöl", ["Elazığ", "Diyarbakır"]);

        Assert.Equal(MesafeKaynagi.KusUcusu, sonuc["Elazığ"].Kaynak);
        Assert.Equal(MesafeKaynagi.YolApi, sonuc["Diyarbakır"].Kaynak);
    }

    [Fact]
    public async Task Kapaliyken_api_cagrilmaz()
    {
        var (servis, http) = Kur(new MesafeApiAyarlari { Etkin = false });

        var sonuc = await servis.HesaplaAsync("Bingöl", ["Elazığ"]);

        Assert.Empty(http.Istekler);
        Assert.Equal(MesafeKaynagi.KusUcusu, sonuc["Elazığ"].Kaynak);
    }
}

// =====================================================================
// Bildirim kanalları: ntfy (push) ve Twilio (SMS)
// =====================================================================
public class BildirimKanaliTestleri
{
    private static readonly Bagisci Deniz = new()
    {
        Ad = "Deniz", Soyad = "Aksoy", Telefon = "05550001122", AnlikBildirimKanali = "kanbagis-abc123", SmsIzni = true,
    };
    private static readonly DisMesaj AcilMesaj = new("ACİL kan ihtiyacı", "Bingöl: A Rh+ kan ihtiyacı (2 ünite).", true);

    [Fact]
    public async Task Ntfy_JSON_ile_dogru_kanala_yuksek_oncelikle_gonderir()
    {
        var http = new SahteHttp().Yanit(HttpStatusCode.OK, """{"id":"x1"}""");
        var kanal = new NtfyKanali(http.Istemci(), Options.Create(new NtfyAyarlari()));

        var sonuc = await kanal.GonderAsync(Deniz, AcilMesaj, default);

        Assert.Equal(KanalSonucTuru.Basarili, sonuc.Tur);
        var (istek, govde) = Assert.Single(http.Istekler);
        Assert.Equal(HttpMethod.Post, istek.Method);
        Assert.Equal("https://ntfy.sh/", istek.RequestUri!.ToString());
        Assert.Null(istek.Headers.Authorization);                 // belirteç yoksa başlık da yok
        using var json = JsonDocument.Parse(govde!);
        Assert.Equal("kanbagis-abc123", json.RootElement.GetProperty("topic").GetString());
        Assert.Equal(5, json.RootElement.GetProperty("priority").GetInt32());   // acil = en yüksek
    }

    [Fact]
    public async Task Ntfy_belirtec_tanimliysa_Bearer_basligi_eklenir()
    {
        var http = new SahteHttp().Yanit(HttpStatusCode.OK, "{}");
        var kanal = new NtfyKanali(http.Istemci(), Options.Create(new NtfyAyarlari { Token = "tk_gizli" }));

        await kanal.GonderAsync(Deniz, AcilMesaj, default);

        var yetki = http.Istekler[0].Istek.Headers.Authorization!;
        Assert.Equal(("Bearer", "tk_gizli"), (yetki.Scheme, yetki.Parameter));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, KanalSonucTuru.KaliciHata)]
    [InlineData(HttpStatusCode.BadRequest, KanalSonucTuru.KaliciHata)]
    [InlineData(HttpStatusCode.ServiceUnavailable, KanalSonucTuru.GeciciHata)]
    [InlineData(HttpStatusCode.TooManyRequests, KanalSonucTuru.HizSiniri)]
    public async Task Http_kodu_dogru_hata_turune_cevrilir(HttpStatusCode kod, KanalSonucTuru beklenen)
    {
        var http = new SahteHttp().Yanit(kod, """{"error":"sebep"}""", y => y.Headers.RetryAfter = new(TimeSpan.FromSeconds(30)));
        var kanal = new NtfyKanali(http.Istemci(), Options.Create(new NtfyAyarlari()));

        var sonuc = await kanal.GonderAsync(Deniz, AcilMesaj, default);

        Assert.Equal(beklenen, sonuc.Tur);
        Assert.Contains("sebep", sonuc.Hata);                       // hata yanıtı ayrıştırıldı
        if (beklenen == KanalSonucTuru.HizSiniri) Assert.Equal(TimeSpan.FromSeconds(30), sonuc.SonraTekrarDene);
    }

    [Fact]
    public void Twilio_anahtarlar_yoksa_kapalidir()
    {
        var kanal = new TwilioSmsKanali(new HttpClient(), Options.Create(new TwilioAyarlari()));

        Assert.False(kanal.Etkin);
    }

    [Fact]
    public async Task Twilio_Basic_kimlik_dogrulama_ve_form_ile_gonderir()
    {
        var http = new SahteHttp().Yanit(HttpStatusCode.Created, """{"sid":"SM1","status":"queued"}""");
        var kanal = new TwilioSmsKanali(http.Istemci(), Options.Create(new TwilioAyarlari
        {
            AccountSid = "AC123", AuthToken = "gizli", Gonderen = "+15005550006",
        }));

        var sonuc = await kanal.GonderAsync(Deniz, AcilMesaj, default);

        Assert.Equal(KanalSonucTuru.Basarili, sonuc.Tur);
        var (istek, govde) = Assert.Single(http.Istekler);
        Assert.EndsWith("/2010-04-01/Accounts/AC123/Messages.json", istek.RequestUri!.ToString());
        Assert.Equal("Basic", istek.Headers.Authorization!.Scheme);
        Assert.Equal("AC123:gizli", Encoding.ASCII.GetString(Convert.FromBase64String(istek.Headers.Authorization.Parameter!)));
        Assert.Contains("To=%2B905550001122", govde);               // 0555… → +90555…
    }

    [Fact]
    public async Task Twilio_hata_mesaji_ayristirilir()
    {
        var http = new SahteHttp().Yanit(HttpStatusCode.BadRequest, """{"code":21211,"message":"The 'To' number is not valid.","status":400}""");
        var kanal = new TwilioSmsKanali(http.Istemci(), Options.Create(new TwilioAyarlari
        {
            AccountSid = "AC123", AuthToken = "gizli", Gonderen = "+15005550006",
        }));

        var sonuc = await kanal.GonderAsync(Deniz, AcilMesaj, default);

        Assert.Equal(KanalSonucTuru.KaliciHata, sonuc.Tur);
        Assert.Contains("The 'To' number is not valid.", sonuc.Hata);
    }

    [Theory]
    [InlineData("05550001122", "+905550001122")]
    [InlineData("5550001122", null)]
    [InlineData("0555abc1122", null)]
    public void Telefon_uluslararasi_bicime_cevrilir(string telefon, string? beklenen)
    {
        Assert.Equal(beklenen, TwilioSmsKanali.UluslararasiNumara(telefon));
    }

    [Fact]
    public void Kanal_yalnizca_bagisci_izin_verdiyse_aciktir()
    {
        var izinsiz = new Bagisci { Telefon = "05550001122", SmsIzni = false, AnlikBildirimKanali = null };

        Assert.False(new NtfyKanali(new HttpClient(), Options.Create(new NtfyAyarlari())).BagisciIcinAcikMi(izinsiz));
        Assert.False(new TwilioSmsKanali(new HttpClient(), Options.Create(new TwilioAyarlari())).BagisciIcinAcikMi(izinsiz));
    }
}

// =====================================================================
// Gönderim: yeniden deneme, kalıcı hata, hız sınırı, durum kaydı, KVKK
// =====================================================================
public class DisBildirimGondericiTestleri : IDisposable
{
    private readonly TestVeritabani _db = new();

    public void Dispose() => _db.Dispose();

    /// <summary>Sonuçları sırayla dönen sahte kanal.</summary>
    private sealed class SahteKanal(params Func<KanalSonucu>[] sonuclar) : IBildirimKanali
    {
        private int _i;
        public List<DisMesaj> Mesajlar { get; } = [];
        public string Ad => "Sahte";
        public bool Etkin => true;
        public bool BagisciIcinAcikMi(Bagisci b) => b.AnlikBildirimKanali is not null;
        public Task<KanalSonucu> GonderAsync(Bagisci b, DisMesaj m, CancellationToken ct)
        {
            Mesajlar.Add(m);
            return Task.FromResult(sonuclar[Math.Min(_i++, sonuclar.Length - 1)]());
        }
    }

    private static KanalSonucu Gecici() => new(KanalSonucTuru.GeciciHata, "HTTP 503");

    private async Task<int> KuyruktaBildirimAsync(bool kanalAcik = true)
    {
        // Örnek veri: Burak (10) talep #1 için beklemede, bildirim #3.
        var burak = await _db.Context.Bagiscilar.SingleAsync(b => b.Id == 10);
        burak.AnlikBildirimKanali = kanalAcik ? "kanbagis-test" : null;
        var bildirim = await _db.Context.Bildirimler.SingleAsync(b => b.Id == 3);
        bildirim.DisKanalDurumu = DisKanalDurumu.Kuyrukta;
        await _db.Context.SaveChangesAsync();
        _db.TakibiTemizle();
        return 3;
    }

    private DisBildirimGonderici Gonderici(params IBildirimKanali[] kanallar) => new(
        new BildirimRepository(_db.Context), kanallar,
        Options.Create(new BildirimGonderimAyarlari { MaksimumDeneme = 3, IlkBeklemeMilisaniye = 1 }),
        new AyarlanabilirZaman(new DateTimeOffset(2026, 11, 16, 9, 0, 0, TimeSpan.Zero)),
        NullLogger<DisBildirimGonderici>.Instance);

    private async Task<Bildirim> OkuAsync(int id)
    {
        _db.TakibiTemizle();
        return await _db.Context.Bildirimler.SingleAsync(b => b.Id == id);
    }

    [Fact]
    public async Task Gecici_hatada_yeniden_denenir_ve_basarili_olur()
    {
        var id = await KuyruktaBildirimAsync();
        var kanal = new SahteKanal(Gecici, Gecici, () => KanalSonucu.Tamam);

        await Gonderici(kanal).GonderAsync(id);

        var b = await OkuAsync(id);
        Assert.Equal((DisKanalDurumu.Gonderildi, 3, (string?)null), (b.DisKanalDurumu, b.DisKanalDeneme, b.DisKanalHata));
        Assert.NotNull(b.DisKanalTarihi);
    }

    [Fact]
    public async Task Tum_denemeler_basarisizsa_basarisiz_kaydedilir_ve_hata_saklanir()
    {
        var id = await KuyruktaBildirimAsync();

        await Gonderici(new SahteKanal(Gecici)).GonderAsync(id);

        var b = await OkuAsync(id);
        Assert.Equal(DisKanalDurumu.Basarisiz, b.DisKanalDurumu);
        Assert.Equal(3, b.DisKanalDeneme);                         // MaksimumDeneme
        Assert.Contains("HTTP 503", b.DisKanalHata);
    }

    [Fact]
    public async Task Kalici_hatada_yeniden_denenmez()
    {
        var id = await KuyruktaBildirimAsync();
        var kanal = new SahteKanal(() => new KanalSonucu(KanalSonucTuru.KaliciHata, "HTTP 401: unauthorized"));

        await Gonderici(kanal).GonderAsync(id);

        var b = await OkuAsync(id);
        Assert.Equal((DisKanalDurumu.Basarisiz, 1), (b.DisKanalDurumu, b.DisKanalDeneme));
        Assert.Single(kanal.Mesajlar);
    }

    [Fact]
    public async Task Hiz_sinirinda_kuyrukta_kalir_ve_ne_zaman_tekrar_denenecegi_doner()
    {
        var id = await KuyruktaBildirimAsync();
        var kanal = new SahteKanal(() => new KanalSonucu(KanalSonucTuru.HizSiniri, "HTTP 429", TimeSpan.FromSeconds(45)));

        var sonra = await Gonderici(kanal).GonderAsync(id);

        Assert.Equal(TimeSpan.FromSeconds(45), sonra);
        Assert.Equal(DisKanalDurumu.Kuyrukta, (await OkuAsync(id)).DisKanalDurumu);
    }

    [Fact]
    public async Task Bagiscinin_telefon_bildirimi_kapaliysa_hic_gonderilmez()
    {
        var id = await KuyruktaBildirimAsync(kanalAcik: false);
        var kanal = new SahteKanal(() => KanalSonucu.Tamam);

        await Gonderici(kanal).GonderAsync(id);

        Assert.Empty(kanal.Mesajlar);
        Assert.Equal(DisKanalDurumu.Yok, (await OkuAsync(id)).DisKanalDurumu);
    }

    [Fact]
    public async Task Telefona_giden_mesajda_kisisel_veri_yok_KVKK()
    {
        var id = await KuyruktaBildirimAsync();
        var kanal = new SahteKanal(() => KanalSonucu.Tamam);

        await Gonderici(kanal).GonderAsync(id);

        var m = Assert.Single(kanal.Mesajlar);
        var metin = m.Baslik + " " + m.Metin;
        Assert.Contains("0 Rh-", metin);
        Assert.Contains("Bingöl", metin);
        foreach (var kisisel in new[] { "Burak", "Öztürk", "05550000010", "99900000010" })
            Assert.DoesNotContain(kisisel, metin);
    }

    [Fact]
    public async Task Kanal_istisna_firlatirsa_gecici_hata_sayilir()
    {
        var id = await KuyruktaBildirimAsync();
        var kanal = new SahteKanal(() => throw new HttpRequestException("DNS çözümlenemedi"), () => KanalSonucu.Tamam);

        await Gonderici(kanal).GonderAsync(id);

        Assert.Equal(DisKanalDurumu.Gonderildi, (await OkuAsync(id)).DisKanalDurumu);
    }

    [Fact]
    public async Task Zaten_gonderilmis_bildirim_tekrar_gonderilmez()
    {
        var id = await KuyruktaBildirimAsync();
        var kanal = new SahteKanal(() => KanalSonucu.Tamam);
        await Gonderici(kanal).GonderAsync(id);

        await Gonderici(kanal).GonderAsync(id);

        Assert.Single(kanal.Mesajlar);
    }
}

// =====================================================================
// Anahtarların güvenliği ve bağışçı tercihleri
// =====================================================================
public class AnahtarVeTercihTestleri : IDisposable
{
    private readonly TestVeritabani _db = new();
    public void Dispose() => _db.Dispose();

    [Fact]
    public void Depodaki_appsettings_dosyasinda_anahtar_yok()
    {
        var yol = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "KanBagis.Web", "appsettings.json");
        using var ayar = JsonDocument.Parse(File.ReadAllText(yol));
        var dis = ayar.RootElement.GetProperty("DisServisler");

        Assert.False(dis.GetProperty("Twilio").TryGetProperty("AccountSid", out _));
        Assert.False(dis.GetProperty("Twilio").TryGetProperty("AuthToken", out _));
        Assert.False(dis.GetProperty("Ntfy").TryGetProperty("Token", out _));
    }

    [Fact]
    public async Task Anlik_bildirim_acilinca_rastgele_kanal_uretilir_kapaninca_silinir()
    {
        var servis = new BagisciService(new BagisciRepository(_db.Context), new EfRepository<KanGrubu>(_db.Context),
            new BagisUygunlukKurali(TimeProvider.System), Options.Create(new NtfyAyarlari { KanalOneki = "kanbagis" }));

        await servis.AnlikBildirimAyarlaAsync(10, ac: true);
        _db.TakibiTemizle();
        var ilk = (await _db.Context.Bagiscilar.SingleAsync(b => b.Id == 10)).AnlikBildirimKanali;

        await servis.AnlikBildirimAyarlaAsync(10, ac: true);
        _db.TakibiTemizle();
        var ikinci = (await _db.Context.Bagiscilar.SingleAsync(b => b.Id == 10)).AnlikBildirimKanali;

        Assert.Matches("^kanbagis-[a-z2-9]{20}$", ilk);
        Assert.NotEqual(ilk, ikinci);                             // her açılışta yeni, tahmin edilemez ad

        await servis.AnlikBildirimAyarlaAsync(10, ac: false);
        _db.TakibiTemizle();
        Assert.Null((await _db.Context.Bagiscilar.SingleAsync(b => b.Id == 10)).AnlikBildirimKanali);
    }

    [Fact]
    public async Task Yeni_talepteki_bildirimler_telefon_kuyruguna_girer()
    {
        var kuyruk = new KayitliKuyruk();
        var zaman = new AyarlanabilirZaman(new DateTimeOffset(2026, 11, 16, 9, 0, 0, TimeSpan.Zero));
        var kural = new BagisUygunlukKurali(zaman);
        var talepler = new KanBagis.Business.Talepler.TalepService(
            new KanTalebiRepository(_db.Context), new HastaneRepository(_db.Context), new EfRepository<KanGrubu>(_db.Context),
            new BildirimRepository(_db.Context), new BagisRepository(_db.Context),
            new KanBagis.Business.Talepler.EslestirmeService(new BagisciRepository(_db.Context), new EfRepository<KanGrubu>(_db.Context), kural, new KusUcusuMesafeServisi()),
            new KusUcusuMesafeServisi(), kuyruk, zaman);

        var sonuc = await talepler.OlusturAsync(1, new(1, 1, Aciliyet.Normal, null));

        var bildirimler = await _db.Context.Bildirimler.Where(b => b.KanTalebiId == sonuc.TalepId).ToListAsync();
        Assert.Equal(bildirimler.Select(b => b.Id).Order(), kuyruk.Idler.Order());
        Assert.All(bildirimler, b => Assert.Equal(DisKanalDurumu.Kuyrukta, b.DisKanalDurumu));
    }

    private sealed class KayitliKuyruk : IBildirimKuyrugu
    {
        public List<int> Idler { get; } = [];
        public void Ekle(int bildirimId) => Idler.Add(bildirimId);
    }
}
