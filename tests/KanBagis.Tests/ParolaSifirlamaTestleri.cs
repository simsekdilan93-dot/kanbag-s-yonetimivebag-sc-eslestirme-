using System.Text.RegularExpressions;
using KanBagis.Business.Eposta;
using KanBagis.Business.Guvenlik;
using KanBagis.Business.Kimlik;
using KanBagis.DataAccess.Repositories;
using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace KanBagis.Tests;

/// <summary>Gönderilen e-postaları yakalayan sahte gönderici (gerçek e-posta gönderilmez).</summary>
public sealed class SahteEposta : IEpostaGonderici
{
    public List<(string Alici, string Konu, string Metin)> Gonderilenler { get; } = [];
    public bool Basarisiz { get; set; }

    public Task<bool> GonderAsync(string alici, string konu, string metin, CancellationToken iptal = default)
    {
        lock (Gonderilenler) Gonderilenler.Add((alici, konu, metin));
        return Task.FromResult(!Basarisiz);
    }

    public string? SonKod(string alici)
    {
        lock (Gonderilenler)
            return Gonderilenler.LastOrDefault(g => g.Alici == alici) is { Metin: { } m } ? Regex.Match(m, @"\b(\d{6})\b").Groups[1].Value : null;
    }
}

/// <summary>v1.1.0 — "Parolamı unuttum": e-postayla gelen kodla parola yenileme.</summary>
public class ParolaSifirlamaTestleri : IDisposable
{
    private const string Eposta = "unutkan@ornek.com";
    private const string EskiParola = "Eski12345";
    private readonly TestVeritabani _db = new();
    private readonly AyarlanabilirZaman _zaman = new(new DateTimeOffset(2026, 11, 28, 10, 0, 0, TimeSpan.Zero));
    private readonly SahteEposta _eposta = new();
    private readonly Pbkdf2ParolaHasher _hasher = new(iterasyon: 1_000);
    private readonly KimlikService _kimlik;
    private readonly ParolaSifirlamaService _servis;

    public ParolaSifirlamaTestleri()
    {
        var kullanicilar = new KullaniciRepository(_db.Context);
        _kimlik = new KimlikService(kullanicilar, new BagisciRepository(_db.Context), new HastaneRepository(_db.Context),
            new EfRepository<KanGrubu>(_db.Context), _hasher, _zaman);
        _servis = new ParolaSifirlamaService(kullanicilar, new ParolaSifirlamaRepository(_db.Context), _hasher, _eposta, _zaman);
        Assert.True(_kimlik.KurumKaydetAsync(new KurumKayitIstegi(Eposta, EskiParola, "Unutkan Hastanesi", "Bingöl", "04260000000")).Result.Basarili);
    }

    public void Dispose() => _db.Dispose();

    private async Task<string> KodIste()
    {
        await _servis.KodGonderAsync(Eposta);
        return _eposta.SonKod(Eposta) ?? throw new InvalidOperationException("kod gönderilmedi");
    }

    private Task<GirisSonucu> Giris(string parola) => _kimlik.GirisYapAsync(Eposta, parola);

    [Fact]
    public async Task Kayitli_epostaya_6_haneli_kod_gonderilir_veritabaninda_yalnizca_hash_saklanir()
    {
        var kod = await KodIste();

        Assert.Matches(@"^\d{6}$", kod);
        var (alici, konu, metin) = Assert.Single(_eposta.Gonderilenler);
        Assert.Equal(Eposta, alici);
        Assert.Contains("parola yenileme kodu", konu);
        Assert.Contains("15 dakika", metin);
        var kayit = Assert.Single(_db.Context.ParolaSifirlamaKodlari);
        Assert.DoesNotContain(kod, kayit.KodHash);                          // kod düz metin saklanmaz
        Assert.StartsWith("PBKDF2-SHA256$", kayit.KodHash);
    }

    [Fact]
    public async Task Kayitli_olmayan_epostaya_kod_gonderilmez_ama_hata_da_verilmez()
    {
        await _servis.KodGonderAsync("boyle@biri.yok");     // istisna yok, sonuç yok: hesap varlığı sızmaz
        Assert.Empty(_eposta.Gonderilenler);
        Assert.Empty(_db.Context.ParolaSifirlamaKodlari);
    }

    [Fact]
    public async Task Dogru_kodla_parola_yenilenir_eski_parola_artik_calismaz()
    {
        var kod = await KodIste();

        var sonuc = await _servis.ParolayiYenileAsync(Eposta, kod, "Yeni12345");

        Assert.Equal(SifirlamaDurumu.Basarili, sonuc.Durum);
        Assert.Equal(GirisDurumu.Basarili, (await Giris("Yeni12345")).Durum);
        Assert.Equal(GirisDurumu.HataliBilgi, (await Giris(EskiParola)).Durum);
    }

    [Fact]
    public async Task Kod_bosluklu_ve_buyuk_I_ile_yazilan_epostayla_da_kabul_edilir()
    {
        var kod = await KodIste();
        var sonuc = await _servis.ParolayiYenileAsync("UNUTKAN@ORNEK.COM", $" {kod[..3]} {kod[3..]} ", "Yeni12345");
        Assert.Equal(SifirlamaDurumu.Basarili, sonuc.Durum);
    }

    [Fact]
    public async Task Kod_bir_kez_kullanilabilir()
    {
        var kod = await KodIste();
        await _servis.ParolayiYenileAsync(Eposta, kod, "Yeni12345");

        var ikinci = await _servis.ParolayiYenileAsync(Eposta, kod, "Baska12345");

        Assert.Equal(SifirlamaDurumu.KodHatali, ikinci.Durum);
        Assert.Equal(GirisDurumu.Basarili, (await Giris("Yeni12345")).Durum);
    }

    [Fact]
    public async Task Suresi_dolan_kod_reddedilir()
    {
        var kod = await KodIste();
        _zaman.Ilerlet(ParolaSifirlamaService.KodGecerlilikSuresi + TimeSpan.FromSeconds(1));

        var sonuc = await _servis.ParolayiYenileAsync(Eposta, kod, "Yeni12345");

        Assert.Equal(SifirlamaDurumu.KodHatali, sonuc.Durum);
        Assert.Equal(GirisDurumu.Basarili, (await Giris(EskiParola)).Durum);
    }

    [Fact]
    public async Task Bes_yanlis_denemede_kod_iptal_olur_dogru_kod_da_artik_calismaz()
    {
        var kod = await KodIste();
        var yanlis = kod == "000000" ? "111111" : "000000";

        for (var i = 1; i < ParolaSifirlamaService.MaksimumDeneme; i++)
            Assert.Equal(SifirlamaDurumu.KodHatali, (await _servis.ParolayiYenileAsync(Eposta, yanlis, "Yeni12345")).Durum);
        Assert.Equal(SifirlamaDurumu.CokDenendi, (await _servis.ParolayiYenileAsync(Eposta, yanlis, "Yeni12345")).Durum);

        Assert.Equal(SifirlamaDurumu.KodHatali, (await _servis.ParolayiYenileAsync(Eposta, kod, "Yeni12345")).Durum);
        Assert.Equal(GirisDurumu.Basarili, (await Giris(EskiParola)).Durum);
    }

    [Fact]
    public async Task Yeni_kod_isteyince_eski_kod_gecersiz_olur()
    {
        var eski = await KodIste();
        _zaman.Ilerlet(ParolaSifirlamaService.IstekAraligi);
        var yeni = await KodIste();

        if (eski != yeni)
            Assert.Equal(SifirlamaDurumu.KodHatali, (await _servis.ParolayiYenileAsync(Eposta, eski, "Yeni12345")).Durum);
        Assert.Equal(SifirlamaDurumu.Basarili, (await _servis.ParolayiYenileAsync(Eposta, yeni, "Yeni12345")).Durum);
        Assert.Equal(2, _db.Context.ParolaSifirlamaKodlari.Count());
        Assert.Empty(_db.Context.ParolaSifirlamaKodlari.AsNoTracking().Where(k => k.KapanisTarihi == null));   // ikisi de kapandı
    }

    [Fact]
    public async Task Ayni_hesaba_60_saniyede_birden_fazla_kod_gonderilmez()
    {
        await _servis.KodGonderAsync(Eposta);
        _zaman.Ilerlet(TimeSpan.FromSeconds(30));
        await _servis.KodGonderAsync(Eposta);
        Assert.Single(_eposta.Gonderilenler);

        _zaman.Ilerlet(TimeSpan.FromSeconds(31));
        await _servis.KodGonderAsync(Eposta);
        Assert.Equal(2, _eposta.Gonderilenler.Count);
    }

    [Theory]
    [InlineData("kisa1")]
    [InlineData("sadeceharf")]
    [InlineData("12345678")]
    public async Task Yeni_parola_kurallara_uymali_kod_harcanmaz(string zayif)
    {
        var kod = await KodIste();

        var sonuc = await _servis.ParolayiYenileAsync(Eposta, kod, zayif);

        Assert.Equal(SifirlamaDurumu.ParolaGecersiz, sonuc.Durum);
        Assert.Equal(SifirlamaDurumu.Basarili, (await _servis.ParolayiYenileAsync(Eposta, kod, "Gecerli123")).Durum);
    }

    [Fact]
    public async Task Parola_yenilenince_giris_kilidi_acilir()
    {
        for (var i = 0; i < KimlikService.MaksimumHataliDeneme; i++) await Giris("Yanlis999");
        Assert.Equal(GirisDurumu.HesapKilitli, (await Giris(EskiParola)).Durum);

        var kod = await KodIste();
        await _servis.ParolayiYenileAsync(Eposta, kod, "Yeni12345");

        Assert.Equal(GirisDurumu.Basarili, (await Giris("Yeni12345")).Durum);
    }

    [Fact]
    public void Kullanici_basina_tek_acik_kod_veritabaninda_korunur()
    {
        var kullaniciId = _db.Context.Kullanicilar.Single(k => k.Eposta == Eposta).Id;
        ParolaSifirlamaKodu Kod() => new()
        {
            KullaniciId = kullaniciId, KodHash = "x", OlusturmaTarihi = _zaman.GetUtcNow().UtcDateTime,
            SonKullanmaTarihi = _zaman.GetUtcNow().UtcDateTime.AddMinutes(15),
        };
        _db.Context.ParolaSifirlamaKodlari.Add(Kod());
        _db.Context.SaveChanges();
        _db.Context.ParolaSifirlamaKodlari.Add(Kod());
        Assert.Throws<DbUpdateException>(() => _db.Context.SaveChanges());
    }

    [Fact]
    public void Kodlar_rastgele_ve_6_haneli()
    {
        var kodlar = Enumerable.Range(0, 200).Select(_ => ParolaSifirlamaService.KodUret()).ToList();
        Assert.All(kodlar, k => Assert.Matches(@"^\d{6}$", k));
        Assert.True(kodlar.Distinct().Count() > 190);   // 200 kodda tekrar neredeyse olmaz
    }

    [Fact]
    public async Task SMTP_yoksa_eposta_klasore_eml_olarak_yazilir_Turkce_karakterler_korunur()
    {
        var klasor = Path.Combine(Path.GetTempPath(), $"kanbagis-eposta-{Guid.NewGuid():N}");
        try
        {
            var gonderici = new SmtpEpostaGonderici(Options.Create(new EpostaAyarlari { Klasor = klasor }), NullLogger<SmtpEpostaGonderici>.Instance);

            var basarili = await gonderici.GonderAsync("alici@ornek.com", "Kan Bağışı — parola yenileme kodu", ParolaSifirlamaService.EpostaMetni("482913"));

            Assert.True(basarili);
            var dosya = Assert.Single(Directory.GetFiles(klasor, "*.eml"));
            var icerik = File.ReadAllText(dosya);
            Assert.Contains("To: alici@ornek.com", icerik);
            Assert.Contains("charset=utf-8", icerik, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(klasor)) Directory.Delete(klasor, recursive: true);
        }
    }
}
