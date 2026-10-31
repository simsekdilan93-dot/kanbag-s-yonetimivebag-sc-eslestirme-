using System.Net;
using System.Text.RegularExpressions;
using KanBagis.Business.Bagiscilar;
using KanBagis.Business.Guvenlik;
using KanBagis.Business.Kimlik;
using KanBagis.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace KanBagis.Tests;

/// <summary>
/// Uygulamayı gerçekten başlatıp HTTP istekleri gönderir. Yetki kontrolünün yalnızca
/// arayüzde değil SUNUCUDA yapıldığını kanıtlar: adres elle yazılsa bile erişim engellenir.
/// </summary>
public class YetkilendirmeTestleri : IClassFixture<YetkilendirmeTestleri.TestUygulamasi>
{
    private const string Parola = "Gizli1234";
    private readonly TestUygulamasi _uygulama;

    public YetkilendirmeTestleri(TestUygulamasi uygulama) => _uygulama = uygulama;

    // ---------------- Giriş yapmamış kullanıcı ----------------

    [Theory]
    [InlineData("/BagisciPanel")]
    [InlineData("/KurumPanel")]
    public async Task Giris_yapmamis_kullanici_giris_sayfasina_yonlendirilir(string adres)
    {
        var yanit = await YeniIstemci().GetAsync(adres);

        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        Assert.StartsWith("http://localhost/Hesap/Giris?ReturnUrl=", yanit.Headers.Location!.ToString());
    }

    // ---------------- Rol kontrolü ----------------

    [Fact]
    public async Task Bagisci_kendi_paneline_girer_kurum_paneline_giremez()
    {
        var istemci = await GirisYapmisIstemciAsync(await BagisciOlusturAsync());

        Assert.Equal(HttpStatusCode.OK, (await istemci.GetAsync("/BagisciPanel")).StatusCode);

        var yanit = await istemci.GetAsync("/KurumPanel");
        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        Assert.Contains("/Hesap/ErisimEngellendi", yanit.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Kurum_kendi_paneline_girer_bagisci_paneline_giremez()
    {
        var istemci = await GirisYapmisIstemciAsync(await KurumOlusturAsync());

        Assert.Equal(HttpStatusCode.OK, (await istemci.GetAsync("/KurumPanel")).StatusCode);

        var yanit = await istemci.GetAsync("/BagisciPanel");
        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        Assert.Contains("/Hesap/ErisimEngellendi", yanit.Headers.Location!.ToString());
    }

    // ---------------- Oturum ----------------

    [Fact]
    public async Task Oturum_cerezi_guvenli_ayarlarla_olusur()
    {
        var eposta = await BagisciOlusturAsync();
        var istemci = YeniIstemci();

        var yanit = await GirisFormuGonderAsync(istemci, eposta, Parola);

        var cerez = yanit.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("KanBagis.Oturum="));
        Assert.Contains("httponly", cerez, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cerez, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cikis_yapinca_oturum_kapanir()
    {
        var istemci = await GirisYapmisIstemciAsync(await BagisciOlusturAsync());
        Assert.Equal(HttpStatusCode.OK, (await istemci.GetAsync("/BagisciPanel")).StatusCode);

        var token = await TokenAlAsync(istemci, "/BagisciPanel");
        await istemci.PostAsync("/Hesap/Cikis", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));

        var yanit = await istemci.GetAsync("/BagisciPanel");
        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        Assert.Contains("/Hesap/Giris", yanit.Headers.Location!.ToString());
    }

    // ---------------- Profil (sahiplik) ----------------

    [Fact]
    public async Task Bagisci_kendi_profilini_gorur_TC_maskelenir()
    {
        var istemci = await GirisYapmisIstemciAsync(await BagisciOlusturAsync());

        var html = await istemci.GetStringAsync("/BagisciPanel/Profil");

        Assert.Contains("Profilim", html);
        Assert.Contains("*****", html);
    }

    [Fact]
    public async Task Profil_formuna_baskasinin_Id_si_eklense_bile_yalnizca_kendi_kaydi_guncellenir()
    {
        var istemci = await GirisYapmisIstemciAsync(await BagisciOlusturAsync());
        var token = await TokenAlAsync(istemci, "/BagisciPanel/Profil");

        // Saldırgan formu elle değiştirip örnek verideki 1 numaralı bağışçının (Ayşe) Id'sini ekliyor.
        var yanit = await istemci.PostAsync("/BagisciPanel/Profil", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Id"] = "1",
            ["BagisciId"] = "1",
            ["Telefon"] = "05009999999",
            ["Il"] = "Edirne",   // Van'dan uzak: uçtan uca talep testiyle (Van) çakışmasın
            ["Kilo"] = "65",
            ["KanGrubuId"] = "1",
            ["__RequestVerificationToken"] = token,
        }));

        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        Assert.Contains("Edirne", await istemci.GetStringAsync("/BagisciPanel"));   // kendi kaydı değişti

        using var scope = _uygulama.Services.CreateScope();
        var ayse = await scope.ServiceProvider.GetRequiredService<IBagisciService>().ProfilGetirAsync(1);
        Assert.Equal("05550000001", ayse!.Bagisci.Telefon);                      // Ayşe'ninki değişmedi
        Assert.Equal("Bingöl", ayse.Bagisci.Sehir);
    }

    [Fact]
    public async Task Kurum_bagisci_profiline_erisemez()
    {
        var istemci = await GirisYapmisIstemciAsync(await KurumOlusturAsync());

        var yanit = await istemci.GetAsync("/BagisciPanel/Profil");

        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        Assert.Contains("/Hesap/ErisimEngellendi", yanit.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Kurum_paneli_ildeki_uygun_bagisci_sayisini_gosterir()
    {
        var istemci = await GirisYapmisIstemciAsync(await KurumOlusturAsync());

        var html = await istemci.GetStringAsync("/KurumPanel");

        Assert.Contains("Bingöl'de uygun bağışçı", html);
        Assert.DoesNotContain("Mehmet", html);   // KVKK: kurum bağışçı adlarını görmez
    }

    [Fact]
    public async Task Kan_grubu_uyumluluk_sayfasi_herkese_acik()
    {
        var yanit = await YeniIstemci().GetAsync("/KanGrubu");

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        Assert.Contains("genel verici", await yanit.Content.ReadAsStringAsync());
    }

    // ---------------- Talep ve eşleştirme: uçtan uca ----------------

    [Fact]
    public async Task Talep_bildirim_kabul_akisi_uctan_uca()
    {
        // Van'da örnek veri yok: yalnızca bu testin bağışçısı eşleşir.
        var bagisciEposta = await BagisciOlusturAsync(il: "Van", kanGrubuId: 1);   // A Rh+
        var kurum = await GirisYapmisIstemciAsync(await KurumOlusturAsync(il: "Van"));

        // 1) Kurum formdan talep açar.
        var token = await TokenAlAsync(kurum, "/KurumTalep/Yeni");
        var yanit = await kurum.PostAsync("/KurumTalep/Yeni", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["KanGrubuId"] = "1",
            ["UniteSayisi"] = "1",
            ["Aciliyet"] = "Normal",
            ["__RequestVerificationToken"] = token,
        }));
        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        var detayAdresi = yanit.Headers.Location!.ToString();
        Assert.StartsWith("/KurumTalep/Detay/", detayAdresi);
        var ilkDetay = await kurum.GetStringAsync(detayAdresi);
        Assert.Contains("1 uygun bağışçıya bildirim gönderildi", ilkDetay);

        // 2) Bağışçı panelinde bildirimi görür ve kabul eder.
        var bagisci = await GirisYapmisIstemciAsync(bagisciEposta);
        var panel = await bagisci.GetStringAsync("/BagisciPanel");
        Assert.Contains("Gelebilirim", panel);
        var bildirimId = Regex.Match(panel, "name=\"bildirimId\" value=\"(\\d+)\"").Groups[1].Value;

        var kabul = await bagisci.PostAsync("/BagisciPanel/Yanitla", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["bildirimId"] = bildirimId,
            ["yanit"] = "Kabul",
            ["__RequestVerificationToken"] = await TokenAlAsync(bagisci, "/BagisciPanel"),
        }));
        Assert.Equal(HttpStatusCode.Redirect, kabul.StatusCode);
        Assert.Contains("Talebi kabul ettiniz", await bagisci.GetStringAsync("/BagisciPanel"));

        // 3) Kurum, kabul eden bağışçının telefonunu görür.
        var detay = await kurum.GetStringAsync(detayAdresi);
        Assert.Contains("05550000000", detay);
        Assert.Contains("Test Bağışçı", detay);
    }

    [Fact]
    public async Task Baska_kurumun_talep_detayi_404()
    {
        var sahip = await GirisYapmisIstemciAsync(await KurumOlusturAsync());
        var token = await TokenAlAsync(sahip, "/KurumTalep/Yeni");
        var yanit = await sahip.PostAsync("/KurumTalep/Yeni", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["KanGrubuId"] = "6", ["UniteSayisi"] = "1", ["Aciliyet"] = "Normal", ["__RequestVerificationToken"] = token,
        }));
        var detayAdresi = yanit.Headers.Location!.ToString();

        var baskaKurum = await GirisYapmisIstemciAsync(await KurumOlusturAsync());

        Assert.Equal(HttpStatusCode.NotFound, (await baskaKurum.GetAsync(detayAdresi)).StatusCode);
    }

    [Fact]
    public async Task Bagisci_talep_acamaz()
    {
        var bagisci = await GirisYapmisIstemciAsync(await BagisciOlusturAsync());

        var yanit = await bagisci.GetAsync("/KurumTalep/Yeni");

        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        Assert.Contains("/Hesap/ErisimEngellendi", yanit.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Bagisci_baskasinin_bildirimini_yanitlayamaz()
    {
        // Örnek veride bildirim #3 Burak'a ait ve beklemede. Test bağışçısı onu kabul etmeye çalışıyor.
        var saldirgan = await GirisYapmisIstemciAsync(await BagisciOlusturAsync());

        await saldirgan.PostAsync("/BagisciPanel/Yanitla", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["bildirimId"] = "3",
            ["yanit"] = "Kabul",
            ["__RequestVerificationToken"] = await TokenAlAsync(saldirgan, "/BagisciPanel"),
        }));

        Assert.Contains("Bildirim bulunamadı.", await saldirgan.GetStringAsync("/BagisciPanel"));
        using var scope = _uygulama.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KanBagis.DataAccess.KanBagisDbContext>();
        Assert.Equal(BildirimYaniti.Beklemede, db.Bildirimler.Single(b => b.Id == 3).Yanit);
    }

    // ---------------- Kayıt formları ----------------

    [Fact]
    public async Task Bagisci_kayit_formu_hesap_acar_ve_panele_goturur()
    {
        var istemci = YeniIstemci();
        var token = await TokenAlAsync(istemci, "/Hesap/KayitBagisci");

        var yanit = await istemci.PostAsync("/Hesap/KayitBagisci", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Eposta"] = "formdan@ornek.com",
            ["Parola"] = Parola,
            ["ParolaTekrar"] = Parola,
            ["TcKimlikNo"] = TestVerisi.GecerliTc(2001),
            ["Ad"] = "Form",
            ["Soyad"] = "Kullanıcı",
            ["Telefon"] = "05550000077",
            ["DogumTarihi"] = "1998-04-15",
            ["Cinsiyet"] = "1",
            ["Kilo"] = "72",
            ["Il"] = "Bingöl",
            ["KanGrubuId"] = "8",
            ["__RequestVerificationToken"] = token,
        }));

        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        Assert.Equal("/BagisciPanel", yanit.Headers.Location!.ToString());

        var panel = await istemci.GetStringAsync("/BagisciPanel");
        Assert.Contains("Merhaba, Form", panel);
        Assert.Contains("0 Rh-", panel);
    }

    [Fact]
    public async Task Kurum_kayit_formu_hesap_acar_ve_panele_goturur()
    {
        var istemci = YeniIstemci();
        var token = await TokenAlAsync(istemci, "/Hesap/KayitKurum");

        var yanit = await istemci.PostAsync("/Hesap/KayitKurum", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Eposta"] = "hastane-form@ornek.com",
            ["Parola"] = Parola,
            ["ParolaTekrar"] = Parola,
            ["HastaneAdi"] = "Form Hastanesi",
            ["Il"] = "Elazığ",
            ["Telefon"] = "04240000077",
            ["__RequestVerificationToken"] = token,
        }));

        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        Assert.Equal("/KurumPanel", yanit.Headers.Location!.ToString());
        Assert.Contains("Form Hastanesi", await istemci.GetStringAsync("/KurumPanel"));
    }

    [Fact]
    public async Task Zayif_parolayla_kayit_formu_reddedilir()
    {
        var istemci = YeniIstemci();
        var token = await TokenAlAsync(istemci, "/Hesap/KayitKurum");

        var yanit = await istemci.PostAsync("/Hesap/KayitKurum", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Eposta"] = "zayif@ornek.com",
            ["Parola"] = "sadeceharf",
            ["ParolaTekrar"] = "sadeceharf",
            ["HastaneAdi"] = "Zayıf Hastanesi",
            ["Il"] = "Bingöl",
            ["Telefon"] = "04260000077",
            ["__RequestVerificationToken"] = token,
        }));

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);   // yönlendirme yok, form tekrar gösterildi
        Assert.Contains("en az bir harf ve bir rakam", await yanit.Content.ReadAsStringAsync());
    }

    // ---------------- Giriş formu ----------------

    [Fact]
    public async Task Hatali_giriste_genel_hata_mesaji_gosterilir()
    {
        var eposta = await BagisciOlusturAsync();

        var yanit = await GirisFormuGonderAsync(YeniIstemci(), eposta, "Yanlis1234");
        var html = await yanit.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        Assert.Contains("E-posta veya parola hatalı.", html);
    }

    [Fact]
    public async Task CSRF_belirteci_olmayan_giris_istegi_reddedilir()
    {
        var yanit = await YeniIstemci().PostAsync("/Hesap/Giris", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["Eposta"] = "x@ornek.com", ["Parola"] = Parola }));

        Assert.Equal(HttpStatusCode.BadRequest, yanit.StatusCode);
    }

    [Fact]
    public async Task Disari_yonlendirme_yapilmaz_OPEN_REDIRECT()
    {
        var eposta = await BagisciOlusturAsync();

        var yanit = await GirisFormuGonderAsync(YeniIstemci(), eposta, Parola, returnUrl: "https://kotu-site.com");

        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        Assert.Equal("/BagisciPanel", yanit.Headers.Location!.ToString());
    }

    // ---------------- Yardımcılar ----------------

    private HttpClient YeniIstemci() =>
        _uygulama.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private async Task<string> BagisciOlusturAsync(string il = "Bingöl", int kanGrubuId = 1)
    {
        var no = Interlocked.Increment(ref _sayac);
        var eposta = $"bagisci{no}@ornek.com";
        using var scope = _uygulama.Services.CreateScope();
        var sonuc = await scope.ServiceProvider.GetRequiredService<IKimlikService>().BagisciKaydetAsync(new BagisciKayitIstegi(
            eposta, Parola, TestVerisi.GecerliTc(1000 + no), "Test", "Bağışçı", "05550000000",
            new DateOnly(1995, 1, 1), Cinsiyet.Kadin, 60, il, kanGrubuId));
        Assert.True(sonuc.Basarili, sonuc.Hata);
        return eposta;
    }

    private async Task<string> KurumOlusturAsync(string il = "Bingöl")
    {
        var no = Interlocked.Increment(ref _sayac);
        var eposta = $"kurum{no}@ornek.com";
        using var scope = _uygulama.Services.CreateScope();
        var sonuc = await scope.ServiceProvider.GetRequiredService<IKimlikService>().KurumKaydetAsync(
            new KurumKayitIstegi(eposta, Parola, $"Test Hastanesi {no}", il, "04260000000"));
        Assert.True(sonuc.Basarili, sonuc.Hata);
        return eposta;
    }

    private static int _sayac;

    private async Task<HttpClient> GirisYapmisIstemciAsync(string eposta)
    {
        var istemci = YeniIstemci();
        var yanit = await GirisFormuGonderAsync(istemci, eposta, Parola);
        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        return istemci;
    }

    private static async Task<HttpResponseMessage> GirisFormuGonderAsync(
        HttpClient istemci, string eposta, string parola, string? returnUrl = null)
    {
        var token = await TokenAlAsync(istemci, "/Hesap/Giris");
        var alanlar = new Dictionary<string, string>
        {
            ["Eposta"] = eposta,
            ["Parola"] = parola,
            ["__RequestVerificationToken"] = token,
        };
        if (returnUrl is not null) alanlar["ReturnUrl"] = returnUrl;

        return await istemci.PostAsync("/Hesap/Giris", new FormUrlEncodedContent(alanlar));
    }

    /// <summary>Sayfadaki formdan CSRF belirtecini okur (tarayıcının yaptığı gibi).</summary>
    private static async Task<string> TokenAlAsync(HttpClient istemci, string adres)
    {
        var html = await istemci.GetStringAsync(adres);
        var eslesme = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(eslesme.Success, $"{adres} sayfasında CSRF belirteci bulunamadı.");
        return eslesme.Groups[1].Value;
    }

    /// <summary>Uygulamayı geçici bir SQLite dosyasıyla başlatır.</summary>
    public sealed class TestUygulamasi : WebApplicationFactory<Program>
    {
        private readonly string _dbYolu = Path.Combine(Path.GetTempPath(), $"kanbagis-test-{Guid.NewGuid():N}.db");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:KanBagis", $"Data Source={_dbYolu};Pooling=False");
            builder.ConfigureTestServices(services =>
                services.AddSingleton<IParolaHasher>(new Pbkdf2ParolaHasher(iterasyon: 1_000)));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (File.Exists(_dbYolu)) File.Delete(_dbYolu);
        }
    }
}
