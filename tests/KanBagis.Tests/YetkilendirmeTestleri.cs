using System.Net;
using System.Text.RegularExpressions;
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
            ["TcKimlikNo"] = "99920000001",
            ["Ad"] = "Form",
            ["Soyad"] = "Kullanıcı",
            ["Telefon"] = "05550000077",
            ["DogumTarihi"] = "1998-04-15",
            ["Cinsiyet"] = "1",
            ["Sehir"] = "Bingöl",
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
            ["Sehir"] = "Elazığ",
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
            ["Sehir"] = "Bingöl",
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

    private async Task<string> BagisciOlusturAsync()
    {
        var no = Interlocked.Increment(ref _sayac);
        var eposta = $"bagisci{no}@ornek.com";
        using var scope = _uygulama.Services.CreateScope();
        var sonuc = await scope.ServiceProvider.GetRequiredService<IKimlikService>().BagisciKaydetAsync(new BagisciKayitIstegi(
            eposta, Parola, $"999100{no:00000}", "Test", "Bağışçı", "05550000000",
            new DateOnly(1995, 1, 1), Cinsiyet.Kadin, "Bingöl", 1));
        Assert.True(sonuc.Basarili, sonuc.Hata);
        return eposta;
    }

    private async Task<string> KurumOlusturAsync()
    {
        var no = Interlocked.Increment(ref _sayac);
        var eposta = $"kurum{no}@ornek.com";
        using var scope = _uygulama.Services.CreateScope();
        var sonuc = await scope.ServiceProvider.GetRequiredService<IKimlikService>().KurumKaydetAsync(
            new KurumKayitIstegi(eposta, Parola, $"Test Hastanesi {no}", "Bingöl", "04260000000"));
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
