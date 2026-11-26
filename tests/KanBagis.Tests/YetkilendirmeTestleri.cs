using System.Net;
using System.Text.RegularExpressions;
using KanBagis.Business.Bagiscilar;
using KanBagis.Business.Guvenlik;
using KanBagis.Business.Kimlik;
using KanBagis.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
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

    // ---------------- İstatistik panosu (Hafta 12) ----------------

    [Fact]
    public async Task Istatistik_panosu_yalnizca_kuruma_acik_grafikler_gorunur()
    {
        var bagisci = await GirisYapmisIstemciAsync(await BagisciOlusturAsync());
        var engel = await bagisci.GetAsync("/Istatistik");
        Assert.Contains("/Hesap/ErisimEngellendi", engel.Headers.Location!.ToString());
        Assert.Contains("/Hesap/ErisimEngellendi", (await bagisci.GetAsync("/Istatistik/Csv")).Headers.Location!.ToString());

        var kurum = await GirisYapmisIstemciAsync(await KurumOlusturAsync());
        var html = await kurum.GetStringAsync("/Istatistik?gun=365");
        Assert.Contains("<svg", html);
        Assert.Contains("Bildirim özeti", html);
        Assert.Contains("Karşılanma oranı", html);
        Assert.DoesNotContain("99900000001", html);   // KVKK: bağışçı TC'si yok
    }

    [Fact]
    public async Task Istatistik_csv_indirilir()
    {
        var kurum = await GirisYapmisIstemciAsync(await KurumOlusturAsync());
        var yanit = await kurum.GetAsync("/Istatistik/Csv?gun=30");

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        Assert.Equal("text/csv", yanit.Content.Headers.ContentType!.MediaType);
        Assert.Contains("30gun.csv", yanit.Content.Headers.ContentDisposition!.ToString());
        Assert.Contains("Metrik;Değer", await yanit.Content.ReadAsStringAsync());
    }

    // ---------------- Hafta 13: hata yolları (mutlu yol dışı) ----------------

    [Fact]
    public async Task Stok_hatali_hareket_formu_hata_mesajiyla_yeniden_gosterir_stok_degismez()
    {
        var kurum = await GirisYapmisIstemciAsync(await KurumOlusturAsync());
        var stokOnce = Stok(3);

        var yanit = await kurum.PostAsync("/Stok/Hareket", Form(await TokenAlAsync(kurum, "/Stok"),
            ("KanGrubuId", "3"), ("Neden", "Imha"), ("Miktar", "1"), ("Aciklama", "")));

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);   // yönlendirme yok: form hatayla yeniden
        Assert.Contains("açıklama zorunlu", await yanit.Content.ReadAsStringAsync());
        Assert.Equal(stokOnce, Stok(3));
    }

    [Fact]
    public async Task Stok_kritik_cikis_once_onay_ister_onayla_kaydedilir_ve_uyari_acilir()
    {
        var kurum = await GirisYapmisIstemciAsync(await KurumOlusturAsync());
        var (stok, kritik) = StokVeKritik(5);                 // AB Rh+ (8 / kritik 3)
        var miktar = (stok - kritik).ToString();               // tam kritik eşiğe indirir

        var onaysiz = await kurum.PostAsync("/Stok/Hareket", Form(await TokenAlAsync(kurum, "/Stok"),
            ("KanGrubuId", "5"), ("Neden", "Transfuzyon"), ("Miktar", miktar)));
        Assert.Equal(HttpStatusCode.OK, onaysiz.StatusCode);
        Assert.Contains("onaylıyorum", await onaysiz.Content.ReadAsStringAsync());
        Assert.Equal(stok, Stok(5));

        var onayli = await kurum.PostAsync("/Stok/Hareket", Form(await TokenAlAsync(kurum, "/Stok"),
            ("KanGrubuId", "5"), ("Neden", "Transfuzyon"), ("Miktar", miktar), ("KritikOnayi", "true")));
        Assert.Equal(HttpStatusCode.Redirect, onayli.StatusCode);
        Assert.Equal(kritik, Stok(5));
        using var scope = _uygulama.Services.CreateScope();
        var uyari = scope.ServiceProvider.GetRequiredService<KanBagis.DataAccess.KanBagisDbContext>()
            .StokUyarilari.Single(u => u.KanGrubuId == 5 && u.KapanisTarihi == null);
        Assert.Equal(StokDurumu.Kritik, uyari.Seviye);
    }

    [Fact]
    public async Task Gecersiz_esik_reddedilir_mesaj_gosterilir()
    {
        var kurum = await GirisYapmisIstemciAsync(await KurumOlusturAsync());
        var yanit = await kurum.PostAsync("/Stok/Esik", Form(await TokenAlAsync(kurum, "/Stok"),
            ("kanGrubuId", "2"), ("kritikEsik", "9"), ("uyariEsigi", "4")));
        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        Assert.Contains("Uyarı eşiği kritik eşikten büyük olmalı", await kurum.GetStringAsync("/Stok"));
    }

    [Theory]
    [InlineData("/KurumTalep/Detay/999999")]
    [InlineData("/Tahmin/Kayit/999999")]
    public async Task Olmayan_kayit_404_doner(string adres)
    {
        var kurum = await GirisYapmisIstemciAsync(await KurumOlusturAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await kurum.GetAsync(adres)).StatusCode);
    }

    [Theory]
    [InlineData("0")]     // alt sınırın altı
    [InlineData("11")]    // üst sınırın üstü (en fazla 10)
    public async Task Talep_formu_unite_sinir_disinda_reddedilir(string unite)
    {
        var kurum = await GirisYapmisIstemciAsync(await KurumOlusturAsync(il: "Hakkari"));
        var yanit = await kurum.PostAsync("/KurumTalep/Yeni", Form(await TokenAlAsync(kurum, "/KurumTalep/Yeni"),
            ("KanGrubuId", "4"), ("UniteSayisi", unite), ("Aciliyet", "Normal")));

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        using var scope = _uygulama.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KanBagis.DataAccess.KanBagisDbContext>();
        Assert.DoesNotContain(db.KanTalepleri.Include(t => t.Hastane), t => t.Hastane.Sehir == "Hakkari");
    }

    [Fact]
    public async Task Hareket_ve_uyari_listeleri_suzgecle_acilir()
    {
        var kurum = await GirisYapmisIstemciAsync(await KurumOlusturAsync());
        Assert.Equal(HttpStatusCode.OK, (await kurum.GetAsync("/Stok/Hareketler?kanGrubuId=7&tur=Giris&yalnizcaBenim=true")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await kurum.GetAsync("/Stok/Uyarilar")).StatusCode);
    }

    private int Stok(int kanGrubuId) => StokVeKritik(kanGrubuId).Stok;

    private (int Stok, int Kritik) StokVeKritik(int kanGrubuId)
    {
        using var scope = _uygulama.Services.CreateScope();
        var s = scope.ServiceProvider.GetRequiredService<KanBagis.DataAccess.KanBagisDbContext>().KanStoklari.Single(k => k.KanGrubuId == kanGrubuId);
        return (s.UniteSayisi, s.KritikEsik);
    }

    private static FormUrlEncodedContent Form(string token, params (string Ad, string Deger)[] alanlar) =>
        new(alanlar.Select(a => new KeyValuePair<string, string>(a.Ad, a.Deger))
                   .Append(new("__RequestVerificationToken", token)));

    // ---------------- Stok Takip ve Uyarı Modülü (Hafta 11) ----------------

    [Fact]
    public async Task Stok_sayfasi_yalnizca_kuruma_acik()
    {
        var bagisci = await GirisYapmisIstemciAsync(await BagisciOlusturAsync());
        var yanit = await bagisci.GetAsync("/Stok");
        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        Assert.Contains("/Hesap/ErisimEngellendi", yanit.Headers.Location!.ToString());

        var kurum = await GirisYapmisIstemciAsync(await KurumOlusturAsync());
        var html = await kurum.GetStringAsync("/Stok");
        Assert.Contains("Kan grubu stokları", html);
        Assert.Contains("açık stok uyarısı", html);
    }

    [Fact]
    public async Task Stok_hareketi_oturumdaki_kurum_adina_kaydedilir_formdaki_kimlik_yok_sayilir()
    {
        var kurum = await GirisYapmisIstemciAsync(await KurumOlusturAsync(il: "Edirne"));
        var token = await TokenAlAsync(kurum, "/Stok");

        var yanit = await kurum.PostAsync("/Stok/Hareket", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["KanGrubuId"] = "7",
            ["Neden"] = "TransferGiris",
            ["Miktar"] = "2",
            ["Aciklama"] = "Test transferi",
            ["HastaneId"] = "1",        // saldırgan alan: dikkate alınmamalı
            ["KullaniciId"] = "1",
            ["__RequestVerificationToken"] = token,
        }));
        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);

        using var scope = _uygulama.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KanBagis.DataAccess.KanBagisDbContext>();
        var h = db.StokHareketleri.Include(x => x.Hastane).Where(x => x.Aciklama == "Test transferi").Single();
        Assert.Equal("Edirne", h.Hastane!.Sehir);
        Assert.NotEqual(1, h.HastaneId);
    }

    [Fact]
    public async Task Stok_hareketi_csrf_belirteci_olmadan_reddedilir()
    {
        var kurum = await GirisYapmisIstemciAsync(await KurumOlusturAsync());
        var yanit = await kurum.PostAsync("/Stok/Hareket", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["KanGrubuId"] = "1", ["Neden"] = "Transfuzyon", ["Miktar"] = "1",
        }));
        Assert.Equal(HttpStatusCode.BadRequest, yanit.StatusCode);
    }

    [Fact]
    public async Task Kurum_panelinde_stok_uyarisi_ve_menude_uyari_sayisi_gorunur()
    {
        var kurum = await GirisYapmisIstemciAsync(await KurumOlusturAsync());
        var html = await kurum.GetStringAsync("/KurumPanel");

        Assert.Contains("Stok uyarısı:", html);
        Assert.Contains("Açık stok uyarısı", html);   // menüdeki rozet
    }

    // ---------------- Yapay zekâ tahmini (Hafta 10) ----------------

    [Fact]
    public async Task Tahmin_sayfasi_yalnizca_kuruma_acik()
    {
        var bagisci = await GirisYapmisIstemciAsync(await BagisciOlusturAsync());
        var yanit = await bagisci.GetAsync("/Tahmin");
        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        Assert.Contains("/Hesap/ErisimEngellendi", yanit.Headers.Location!.ToString());

        var kurum = await GirisYapmisIstemciAsync(await KurumOlusturAsync());
        Assert.Equal(HttpStatusCode.OK, (await kurum.GetAsync("/Tahmin/Gunluk")).StatusCode);
    }

    [Fact]
    public async Task Kurum_tahmini_calistirir_seffaflik_notu_ve_gunluk_kaydi_gorunur()
    {
        var kurum = await GirisYapmisIstemciAsync(await KurumOlusturAsync());
        var token = await TokenAlAsync(kurum, "/Tahmin");

        var yanit = await kurum.PostAsync("/Tahmin/Calistir", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));
        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);

        var html = await kurum.GetStringAsync("/Tahmin");
        Assert.Contains("Şeffaflık:", html);
        Assert.Contains("İstatistiksel tahmin (yapay zekâ kullanılmadı)", html);
        Assert.Contains("0 Rh-", html);

        var gunluk = await kurum.GetStringAsync("/Tahmin/Gunluk");
        Assert.Contains("/Tahmin/Kayit/", gunluk);
    }

    [Fact]
    public async Task Tahmin_calistirma_csrf_belirteci_olmadan_reddedilir()
    {
        var kurum = await GirisYapmisIstemciAsync(await KurumOlusturAsync());
        var yanit = await kurum.PostAsync("/Tahmin/Calistir", new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.BadRequest, yanit.StatusCode);
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

        // 4) VİZE entegrasyonu: bağışçı hastaneye gelir, kurum "Bağış alındı" der.
        int StokAPozitif()
        {
            using var s = _uygulama.Services.CreateScope();
            return s.ServiceProvider.GetRequiredService<KanBagis.DataAccess.KanBagisDbContext>()
                    .KanStoklari.Single(k => k.KanGrubuId == 1).UniteSayisi;
        }
        var stokOnce = StokAPozitif();
        var kabulBildirimId = Regex.Match(detay, "name=\"bildirimId\" value=\"(\\d+)\"").Groups[1].Value;
        var bagis = await kurum.PostAsync(detayAdresi.Replace("/Detay/", "/BagisKaydet/"), new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["bildirimId"] = kabulBildirimId,
            ["__RequestVerificationToken"] = await TokenAlAsync(kurum, detayAdresi),
        }));
        Assert.Equal(HttpStatusCode.Redirect, bagis.StatusCode);

        var sonDetay = await kurum.GetStringAsync(detayAdresi);
        Assert.Contains("talep karşılandı olarak kapatıldı", sonDetay);   // 1 ünitelik talep tamamlandı
        Assert.Contains("✓ Alındı", sonDetay);
        Assert.Equal(stokOnce + 1, StokAPozitif());                       // stok arttı

        // 5) Bağışçının paneli: son bağış bugün, artık bağış için uygun değil (90/120 gün kuralı).
        var sonPanel = await bagisci.GetStringAsync("/BagisciPanel");
        Assert.Contains("Şu an bağış yapamazsınız", sonPanel);
        Assert.Contains("1 bağış", sonPanel);

        // 6) Hafta 11 entegrasyonu: bağış stok hareket geçmişinde "Bağış" nedenli giriş olarak görünür.
        var hareketler = await kurum.GetStringAsync("/Stok/Hareketler?kanGrubuId=1&yalnizcaBenim=true");
        Assert.Contains("Bağış", hareketler);
        Assert.Contains("için bağış", hareketler);   // "Talep #N için bağış"

        // 7) Hafta 12 entegrasyonu: istatistik panosu (yalnızca bu hastane, son 7 gün) talebi ve bağışı sayar.
        var pano = await kurum.GetStringAsync("/Istatistik?gun=7&hastanem=true");
        Assert.Contains("1 bağış · talebe bağlı 1", pano);
        Assert.Contains("1 / 1 sonuçlanan talep", pano);
    }

    [Fact]
    public async Task Alt_bilgide_surum_numarasi_gorunur()
    {
        var html = await YeniIstemci().GetStringAsync("/");

        // Sürüm Directory.Build.props'tan gelir; semantic versioning biçiminde olmalı (MAJOR.MINOR.PATCH).
        Assert.Matches(@"^\d+\.\d+\.\d+$", KanBagis.Web.SurumBilgisi.Surum);
        Assert.Contains("v" + KanBagis.Web.SurumBilgisi.Surum, html);
    }

    [Fact]
    public async Task Sablondan_kalan_privacy_sayfasi_kaldirildi()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await YeniIstemci().GetAsync("/Home/Privacy")).StatusCode);
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
    public async Task H13_06_Giris_formu_Turkce_I_ile_yazilan_epostayi_engellemez()
    {
        // İstemci tarafı: girişte yalnızca-ASCII e-posta doğrulaması olmamalı; telefon ilk harfi büyütmemeli.
        var html = await YeniIstemci().GetStringAsync("/Hesap/Giris");
        var girdi = Regex.Match(html, "<input[^>]*name=\"Eposta\"[^>]*>").Value;
        Assert.DoesNotContain("data-val-email", girdi);
        Assert.DoesNotContain("type=\"email\"", girdi);
        Assert.Contains("autocapitalize=\"none\"", girdi);

        // Sunucu tarafı: küçük harfle kayıtlı hesaba "İ" ile giriş.
        using (var scope = _uygulama.Services.CreateScope())
        {
            var kayit = await scope.ServiceProvider.GetRequiredService<IKimlikService>().KurumKaydetAsync(
                new KurumKayitIstegi("ilkerweb@ornek.com", Parola, "İlker Test Hastanesi", "Bingöl", "04260000000"));
            Assert.True(kayit.Basarili, kayit.Hata);
        }
        var yanit = await GirisFormuGonderAsync(YeniIstemci(), "İlkerweb@ornek.com", Parola);
        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        Assert.Equal("/KurumPanel", yanit.Headers.Location!.ToString());
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
            // Testler internete çıkmaz: yol mesafesi kuş uçuşu, anlık bildirim kapalı.
            builder.UseSetting("DisServisler:Mesafe:Etkin", "false");
            builder.UseSetting("DisServisler:Ntfy:Etkin", "false");
            builder.UseSetting("DisServisler:YapayZeka:Etkin", "false");   // ANTHROPIC_API_KEY tanımlı olsa bile API çağrılmaz
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
