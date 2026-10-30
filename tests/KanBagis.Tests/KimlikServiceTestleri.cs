using KanBagis.Business.Guvenlik;
using KanBagis.Business.Kimlik;
using KanBagis.DataAccess.Repositories;
using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;

namespace KanBagis.Tests;

public class KimlikServiceTestleri : IDisposable
{
    private readonly TestVeritabani _db = new();
    private readonly AyarlanabilirZaman _zaman = new(new DateTimeOffset(2026, 10, 22, 9, 0, 0, TimeSpan.Zero));
    private readonly KimlikService _kimlik;

    public KimlikServiceTestleri()
    {
        _kimlik = new KimlikService(
            new KullaniciRepository(_db.Context),
            new BagisciRepository(_db.Context),
            new HastaneRepository(_db.Context),
            new EfRepository<KanGrubu>(_db.Context),
            new Pbkdf2ParolaHasher(iterasyon: 1_000),
            _zaman);
    }

    public void Dispose() => _db.Dispose();

    private static readonly string DenizTc = TestVerisi.GecerliTc(50);

    private static BagisciKayitIstegi Istek(
        string eposta = "Deniz@Ornek.com", string parola = "Gizli1234", string? tc = null,
        DateOnly? dogumTarihi = null, string il = "Bingöl", int kilo = 60, int kanGrubuId = 1) =>
        new(eposta, parola, tc ?? DenizTc, "Deniz", "Yıldız", "05550000050",
            dogumTarihi ?? new DateOnly(1996, 5, 20), Cinsiyet.Kadin, kilo, il, kanGrubuId);

    // ---------------- KAYIT ----------------

    [Fact]
    public async Task Bagisci_kaydi_hesap_ve_bagisciyi_birlikte_olusturur()
    {
        var sonuc = await _kimlik.BagisciKaydetAsync(Istek());
        _db.TakibiTemizle();

        Assert.True(sonuc.Basarili);
        var kullanici = await _db.Context.Kullanicilar.Include(k => k.Bagisci).SingleAsync();
        Assert.Equal("deniz@ornek.com", kullanici.Eposta);       // küçük harfe çevrildi
        Assert.Equal(KullaniciRolu.Bagisci, kullanici.Rol);
        Assert.Equal(DenizTc, kullanici.Bagisci!.TcKimlikNo);
        Assert.Equal(60, kullanici.Bagisci.Kilo);
        Assert.NotEqual("Gizli1234", kullanici.ParolaHash);       // düz metin saklanmadı
        Assert.DoesNotContain("Gizli1234", kullanici.ParolaHash);
    }

    [Fact]
    public async Task Ayni_eposta_ile_ikinci_kayit_reddedilir()
    {
        await _kimlik.BagisciKaydetAsync(Istek());

        var sonuc = await _kimlik.BagisciKaydetAsync(Istek(eposta: "  DENIZ@ornek.com ", tc: TestVerisi.GecerliTc(51)));

        Assert.False(sonuc.Basarili);
        Assert.Contains("e-posta", sonuc.Hata);
    }

    [Fact]
    public async Task Kayitli_TC_ile_ikinci_kayit_reddedilir()
    {
        await _kimlik.BagisciKaydetAsync(Istek());

        var sonuc = await _kimlik.BagisciKaydetAsync(Istek(eposta: "baska@ornek.com"));

        Assert.False(sonuc.Basarili);
        Assert.Equal("Bu TC kimlik numarasıyla kayıtlı bir bağışçı var.", sonuc.Hata);
    }

    [Theory]
    [InlineData("12345678901")]   // kontrol haneleri tutmuyor
    [InlineData("01234567890")]   // 0 ile başlıyor
    [InlineData("99900000001")]   // örnek verideki sentetik numara (bilerek geçersiz)
    [InlineData("1234567890")]    // 10 hane
    public async Task Gecersiz_TC_ile_kayit_reddedilir(string tc)
    {
        var sonuc = await _kimlik.BagisciKaydetAsync(Istek(tc: tc));

        Assert.Equal("TC kimlik numarası geçersiz.", sonuc.Hata);
    }

    [Fact]
    public async Task On_sekiz_yas_altinda_kayit_reddedilir()
    {
        // Test tarihi 22.10.2026 → 23.10.2008 doğumlu kişi henüz 17 yaşında.
        var sonuc = await _kimlik.BagisciKaydetAsync(Istek(dogumTarihi: new DateOnly(2008, 10, 23)));
        Assert.False(sonuc.Basarili);
        Assert.Contains("18", sonuc.Hata);

        // Tam 18. doğum günü: kabul.
        Assert.True((await _kimlik.BagisciKaydetAsync(Istek(dogumTarihi: new DateOnly(2008, 10, 22)))).Basarili);
    }

    [Theory]
    [InlineData("bingol")]
    [InlineData("Bingol")]
    [InlineData("")]
    [InlineData("Atlantis")]
    public async Task Listede_olmayan_il_reddedilir(string il)
    {
        var sonuc = await _kimlik.BagisciKaydetAsync(Istek(il: il));

        Assert.Equal("Geçerli bir il seçin.", sonuc.Hata);
    }

    [Fact]
    public async Task Olmayan_kan_grubu_reddedilir()
    {
        var sonuc = await _kimlik.BagisciKaydetAsync(Istek(kanGrubuId: 99));

        Assert.Equal("Geçerli bir kan grubu seçin.", sonuc.Hata);
    }

    [Theory]
    [InlineData("kisa1")]          // 8 karakterden kısa
    [InlineData("sadeceharf")]     // rakam yok
    [InlineData("12345678")]       // harf yok
    public async Task Zayif_parola_reddedilir(string parola)
    {
        var sonuc = await _kimlik.BagisciKaydetAsync(Istek(parola: parola));

        Assert.False(sonuc.Basarili);
        Assert.Equal(0, await _db.Context.Kullanicilar.CountAsync());
    }

    [Fact]
    public async Task Kurum_kaydi_hastaneyi_de_olusturur()
    {
        var sonuc = await _kimlik.KurumKaydetAsync(
            new KurumKayitIstegi("kurum@ornek.com", "Gizli1234", "Deneme Hastanesi", "Bingöl", "04260000009"));

        Assert.True(sonuc.Basarili);
        Assert.Equal(KullaniciRolu.Kurum, sonuc.Kullanici!.Rol);
        Assert.Equal(4, await _db.Context.Hastaneler.CountAsync()); // 3 örnek + 1 yeni
    }

    // ---------------- GİRİŞ ----------------

    [Fact]
    public async Task Dogru_bilgilerle_giris_basarili()
    {
        await _kimlik.BagisciKaydetAsync(Istek());

        var sonuc = await _kimlik.GirisYapAsync("deniz@ornek.com", "Gizli1234");

        Assert.Equal(GirisDurumu.Basarili, sonuc.Durum);
        Assert.Equal(_zaman.GetUtcNow().UtcDateTime, sonuc.Kullanici!.SonGirisTarihi);
    }

    [Fact]
    public async Task Yanlis_parola_ve_olmayan_eposta_ayni_yaniti_alir()
    {
        await _kimlik.BagisciKaydetAsync(Istek());

        var yanlisParola = await _kimlik.GirisYapAsync("deniz@ornek.com", "Yanlis1234");
        var olmayanHesap = await _kimlik.GirisYapAsync("yok@ornek.com", "Gizli1234");

        Assert.Equal(GirisDurumu.HataliBilgi, yanlisParola.Durum);
        Assert.Equal(GirisDurumu.HataliBilgi, olmayanHesap.Durum);
        Assert.Null(yanlisParola.Kullanici);
    }

    [Fact]
    public async Task Bes_hatali_denemeden_sonra_hesap_15_dakika_kilitlenir()
    {
        await _kimlik.BagisciKaydetAsync(Istek());

        for (var i = 1; i < KimlikService.MaksimumHataliDeneme; i++)
            Assert.Equal(GirisDurumu.HataliBilgi, (await _kimlik.GirisYapAsync("deniz@ornek.com", "Yanlis1234")).Durum);

        var besinci = await _kimlik.GirisYapAsync("deniz@ornek.com", "Yanlis1234");
        Assert.Equal(GirisDurumu.HesapKilitli, besinci.Durum);

        // Kilitliyken DOĞRU parola da kabul edilmez.
        _zaman.Ilerlet(TimeSpan.FromMinutes(14));
        Assert.Equal(GirisDurumu.HesapKilitli, (await _kimlik.GirisYapAsync("deniz@ornek.com", "Gizli1234")).Durum);

        // Süre dolunca giriş yapılabilir.
        _zaman.Ilerlet(TimeSpan.FromMinutes(2));
        Assert.Equal(GirisDurumu.Basarili, (await _kimlik.GirisYapAsync("deniz@ornek.com", "Gizli1234")).Durum);
    }

    [Fact]
    public async Task Basarili_giris_hatali_deneme_sayacini_sifirlar()
    {
        await _kimlik.BagisciKaydetAsync(Istek());
        for (var i = 0; i < 4; i++)
            await _kimlik.GirisYapAsync("deniz@ornek.com", "Yanlis1234");

        await _kimlik.GirisYapAsync("deniz@ornek.com", "Gizli1234");

        Assert.Equal(0, (await _db.Context.Kullanicilar.SingleAsync()).BasarisizGirisSayisi);
    }
}

/// <summary>Testte zamanı ileri sarabilmek için (kilit süresi denemesi).</summary>
public sealed class AyarlanabilirZaman(DateTimeOffset baslangic) : TimeProvider
{
    private DateTimeOffset _simdi = baslangic;
    public override DateTimeOffset GetUtcNow() => _simdi;
    public void Ilerlet(TimeSpan sure) => _simdi += sure;
}
