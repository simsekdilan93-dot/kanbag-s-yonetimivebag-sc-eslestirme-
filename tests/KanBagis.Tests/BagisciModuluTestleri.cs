using KanBagis.Business.Bagiscilar;
using KanBagis.Business.Tanimlar;
using KanBagis.DataAccess.Repositories;
using KanBagis.DataAccess.Seed;
using KanBagis.Entities;

namespace KanBagis.Tests;

// =====================================================================
// Kan grubu uyumluluğu
// =====================================================================
public class KanGrubuUyumlulukTestleri
{
    private static readonly KanGrubu[] Gruplar = OrnekVeri.KanGruplari;
    private static KanGrubu G(string ad) => Gruplar.Single(g => g.Ad == ad);

    // Tıbbi uyumluluk tablosu: verici → kan verebileceği alıcılar
    [Theory]
    [InlineData("A Rh+",  "A Rh+,AB Rh+")]
    [InlineData("A Rh-",  "A Rh+,A Rh-,AB Rh+,AB Rh-")]
    [InlineData("B Rh+",  "B Rh+,AB Rh+")]
    [InlineData("B Rh-",  "B Rh+,B Rh-,AB Rh+,AB Rh-")]
    [InlineData("AB Rh+", "AB Rh+")]
    [InlineData("AB Rh-", "AB Rh+,AB Rh-")]
    [InlineData("0 Rh+",  "A Rh+,B Rh+,AB Rh+,0 Rh+")]
    [InlineData("0 Rh-",  "A Rh+,A Rh-,B Rh+,B Rh-,AB Rh+,AB Rh-,0 Rh+,0 Rh-")]
    public void Uyumluluk_tablosu_dogru(string verici, string beklenenAlicilar)
    {
        var alicilar = KanGrubuUyumlulugu.AlabilecekGruplar(G(verici), Gruplar).Select(g => g.Ad);

        Assert.Equal(beklenenAlicilar.Split(','), alicilar);
    }

    [Fact]
    public void Genel_verici_0_Rh_negatif_genel_alici_AB_Rh_pozitif()
    {
        Assert.All(Gruplar, alici => Assert.True(KanGrubuUyumlulugu.VerebilirMi(G("0 Rh-"), alici)));
        Assert.All(Gruplar, verici => Assert.True(KanGrubuUyumlulugu.VerebilirMi(verici, G("AB Rh+"))));
    }

    [Fact]
    public void Rh_pozitif_kan_Rh_negatif_aliciya_verilemez()
    {
        Assert.False(KanGrubuUyumlulugu.VerebilirMi(G("0 Rh+"), G("0 Rh-")));
        Assert.True(KanGrubuUyumlulugu.VerebilirMi(G("0 Rh-"), G("0 Rh+")));
    }
}

// =====================================================================
// Bağış uygunluk kuralı
// =====================================================================
public class BagisUygunlukKuraliTestleri
{
    private static readonly DateTime Bugun = new(2026, 10, 22, 9, 0, 0, DateTimeKind.Utc);
    private readonly BagisUygunlukKurali _kural = new(new AyarlanabilirZaman(new DateTimeOffset(Bugun)));

    private static Bagisci Kisi(Cinsiyet cinsiyet = Cinsiyet.Erkek, int yas = 30, int kilo = 70) => new()
    {
        Cinsiyet = cinsiyet,
        Kilo = kilo,
        DogumTarihi = DateOnly.FromDateTime(Bugun).AddYears(-yas),
    };

    [Fact]
    public void Hic_bagis_yapmamis_yetiskin_uygundur()
    {
        var sonuc = _kural.Degerlendir(Kisi(), sonBagisTarihi: null);

        Assert.True(sonuc.Uygun);
        Assert.Empty(sonuc.Nedenler);
    }

    [Theory]
    [InlineData(Cinsiyet.Erkek, 89, false)]
    [InlineData(Cinsiyet.Erkek, 90, true)]
    [InlineData(Cinsiyet.Kadin, 119, false)]
    [InlineData(Cinsiyet.Kadin, 120, true)]
    public void Bagis_araligi_cinsiyete_gore_degisir(Cinsiyet cinsiyet, int kacGunOnce, bool beklenen)
    {
        var sonuc = _kural.Degerlendir(Kisi(cinsiyet), Bugun.AddDays(-kacGunOnce));

        Assert.Equal(beklenen, sonuc.Uygun);
    }

    [Fact]
    public void Erken_bagista_sonraki_uygun_tarih_hesaplanir()
    {
        var sonuc = _kural.Degerlendir(Kisi(Cinsiyet.Kadin), new DateTime(2026, 8, 20));

        Assert.False(sonuc.Uygun);
        Assert.Equal(new DateOnly(2026, 12, 18), sonuc.SonrakiUygunTarih);   // 20.08 + 120 gün
    }

    [Fact]
    public void On_sekiz_yas_altinda_uygun_degil_ve_18_yas_gunu_gosterilir()
    {
        var kisi = Kisi(yas: 17);

        var sonuc = _kural.Degerlendir(kisi, null);

        Assert.False(sonuc.Uygun);
        Assert.Equal(kisi.DogumTarihi.AddYears(18), sonuc.SonrakiUygunTarih);
    }

    [Theory]
    [InlineData(65, true)]
    [InlineData(66, false)]
    public void Yas_ust_siniri_65(int yas, bool beklenen)
    {
        var sonuc = _kural.Degerlendir(Kisi(yas: yas), null);

        Assert.Equal(beklenen, sonuc.Uygun);
        if (!beklenen) Assert.Null(sonuc.SonrakiUygunTarih);   // kalıcı engel: tarih verilmez
    }

    [Theory]
    [InlineData(49, false)]
    [InlineData(50, true)]
    public void Kilo_en_az_50(int kilo, bool beklenen)
    {
        Assert.Equal(beklenen, _kural.Degerlendir(Kisi(kilo: kilo), null).Uygun);
    }

    [Fact]
    public void Birden_fazla_neden_varsa_hepsi_listelenir()
    {
        var sonuc = _kural.Degerlendir(Kisi(kilo: 45), Bugun.AddDays(-10));

        Assert.Equal(2, sonuc.Nedenler.Count);
        Assert.Null(sonuc.SonrakiUygunTarih);   // kilo kalıcı engel olduğu için tarih yok
    }
}

// =====================================================================
// Doğrulama kuralları
// =====================================================================
public class BagisciDogrulamaTestleri
{
    [Theory]
    [InlineData("10000000146", true)]    // algoritmaya uyan bilinen örnek numara
    [InlineData("10000000147", false)]   // son hane yanlış
    [InlineData("10000000156", false)]   // 10. hane yanlış
    [InlineData("00000000000", false)]   // 0 ile başlıyor
    [InlineData("1000000014", false)]    // 10 hane
    [InlineData("1000000014a", false)]   // harf
    [InlineData(null, false)]
    public void TC_kimlik_algoritmasi(string? tc, bool beklenen)
    {
        Assert.Equal(beklenen, BagisciDogrulama.TcKimlikNoGecerliMi(tc));
    }

    [Fact]
    public void Test_uretecinin_urettigi_numaralar_gecerli()
    {
        Assert.All(Enumerable.Range(1, 200), i => Assert.True(BagisciDogrulama.TcKimlikNoGecerliMi(TestVerisi.GecerliTc(i))));
    }

    [Fact]
    public void Ornek_verideki_TC_numaralari_bilerek_gecersiz_KVKK()
    {
        // Sentetik numaralar algoritmaya uymaz; böylece hiçbir zaman gerçek bir kişiye ait olamaz.
        Assert.All(OrnekVeri.Bagiscilar, b => Assert.False(BagisciDogrulama.TcKimlikNoGecerliMi(b.TcKimlikNo)));
    }

    [Fact]
    public void Il_listesi_81_il_icerir_ve_ornek_veriyle_uyumlu()
    {
        Assert.Equal(81, Iller.Tumu.Count);
        Assert.Equal(81, Iller.Tumu.Distinct().Count());
        Assert.All(OrnekVeri.Bagiscilar, b => Assert.True(Iller.GecerliMi(b.Sehir)));
        Assert.All(OrnekVeri.Hastaneler, h => Assert.True(Iller.GecerliMi(h.Sehir)));
    }

    [Fact]
    public void Il_listesi_Turkce_alfabeye_gore_siralanir()
    {
        var sirali = Iller.AlfabetikSirali;
        Assert.True(sirali.ToList().IndexOf("Çorum") < sirali.ToList().IndexOf("Denizli"));   // Ç, C'den sonra D'den önce
        Assert.True(sirali.ToList().IndexOf("Iğdır") < sirali.ToList().IndexOf("İstanbul"));  // I, İ'den önce
    }

    [Theory]
    [InlineData(2000, 10, 22, 26)]   // bugün doğum günü
    [InlineData(2000, 10, 23, 25)]   // doğum günü yarın
    [InlineData(2000, 2, 29, 26)]    // artık yıl
    public void Yas_hesabi(int yil, int ay, int gun, int beklenen)
    {
        Assert.Equal(beklenen, BagisciDogrulama.YasHesapla(new DateOnly(yil, ay, gun), new DateOnly(2026, 10, 22)));
    }
}

// =====================================================================
// Bağışçı servisi (profil, kan grubu kilidi). Uygun bağışçı bulma: EslestirmeTestleri.cs
// =====================================================================
public class BagisciServiceTestleri : IDisposable
{
    private readonly TestVeritabani _db = new();
    private readonly BagisciService _servis;

    public BagisciServiceTestleri()
    {
        var zaman = new AyarlanabilirZaman(new DateTimeOffset(2026, 10, 22, 9, 0, 0, TimeSpan.Zero));
        _servis = new BagisciService(
            new BagisciRepository(_db.Context),
            new EfRepository<KanGrubu>(_db.Context),
            new BagisUygunlukKurali(zaman));
    }

    public void Dispose() => _db.Dispose();

    // Örnek veri hatırlatma (22.10.2026 itibarıyla):
    //   1 Ayşe   A Rh+  Bingöl     K  son bağış 15.06 → 129 gün → uygun
    //   2 Mehmet 0 Rh-  Bingöl     E  son bağış 01.07 → 113 gün → uygun
    //   4 Ali    A Rh+  Bingöl     E  bağış yok        → uygun
    //   7 Fatma  0 Rh+  Bingöl     K  son bağış 20.08 →  63 gün → UYGUN DEĞİL (120 gün)
    //   8 Emre   A Rh-  Diyarbakır E  bağış yok        → uygun
    //   9 Merve  B Rh-  Bingöl     K  bağış yok, 52 kg → uygun
    //  10 Burak  0 Rh-  Bingöl     E  bağış yok        → uygun

    [Fact]
    public async Task Profil_uygunluk_ve_nedenleri_doner()
    {
        var fatma = await _servis.ProfilGetirAsync(7);

        Assert.NotNull(fatma);
        Assert.False(fatma.Uygunluk.Uygun);
        Assert.Equal(new DateOnly(2026, 12, 18), fatma.Uygunluk.SonrakiUygunTarih);
        Assert.True(fatma.KanGrubuKilitli);   // bağış kaydı var
    }

    [Fact]
    public async Task Profil_guncellenir()
    {
        var sonuc = await _servis.ProfilGuncelleAsync(10, new ProfilGuncellemeIstegi("05551112233", "Elazığ", 90, 8));
        _db.TakibiTemizle();

        Assert.True(sonuc.Basarili);
        var burak = (await _servis.ProfilGetirAsync(10))!.Bagisci;
        Assert.Equal(("05551112233", "Elazığ", 90), (burak.Telefon, burak.Sehir, burak.Kilo));
    }

    [Fact]
    public async Task Bagisi_olmayan_bagiscinin_kan_grubu_duzeltilebilir()
    {
        var sonuc = await _servis.ProfilGuncelleAsync(10, new ProfilGuncellemeIstegi("05550000010", "Bingöl", 88, 7));
        _db.TakibiTemizle();

        Assert.True(sonuc.Basarili);
        Assert.Equal("0 Rh+", (await _servis.ProfilGetirAsync(10))!.Bagisci.KanGrubu.Ad);
    }

    [Fact]
    public async Task Bagisi_olan_bagiscinin_kan_grubu_degistirilemez()
    {
        // Mehmet'in (2) bağış kaydı var; 0 Rh- → A Rh+ yapılmak isteniyor.
        var sonuc = await _servis.ProfilGuncelleAsync(2, new ProfilGuncellemeIstegi("05550000002", "Bingöl", 84, 1));
        _db.TakibiTemizle();

        Assert.False(sonuc.Basarili);
        Assert.Contains("değiştirilemez", sonuc.Hata);
        Assert.Equal(8, (await _servis.ProfilGetirAsync(2))!.Bagisci.KanGrubuId);

        // Kan grubu aynı kalırsa diğer alanlar güncellenebilir.
        Assert.True((await _servis.ProfilGuncelleAsync(2, new ProfilGuncellemeIstegi("05559998877", "Bingöl", 84, 8))).Basarili);
    }

    [Theory]
    [InlineData("5551112233", "Bingöl", 70, 1, "Telefon")]   // 0 ile başlamıyor
    [InlineData("05551112233", "bingöl", 70, 1, "il")]       // listede yok
    [InlineData("05551112233", "Bingöl", 20, 1, "Kilo")]     // aralık dışı
    [InlineData("05551112233", "Bingöl", 70, 99, "kan grubu")] // olmayan kan grubu
    public async Task Gecersiz_profil_bilgisi_reddedilir_ve_kayit_degismez(string telefon, string il, int kilo, int kanGrubuId, string hataIcerir)
    {
        var sonuc = await _servis.ProfilGuncelleAsync(10, new ProfilGuncellemeIstegi(telefon, il, kilo, kanGrubuId));
        _db.TakibiTemizle();

        Assert.False(sonuc.Basarili);
        Assert.Contains(hataIcerir, sonuc.Hata);
        Assert.Equal("05550000010", (await _servis.ProfilGetirAsync(10))!.Bagisci.Telefon);
    }
}
