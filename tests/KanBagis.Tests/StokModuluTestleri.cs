using KanBagis.Business.Stok;
using KanBagis.DataAccess.Repositories;
using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;

namespace KanBagis.Tests;

/// <summary>
/// Hafta 11 — Stok Takip ve Uyarı Modülü. Örnek veride başlangıç durumu:
///   A Rh+ 24 (kritik 8 / düşük 15) Normal · A Rh- 6 (3/6) Düşük · B Rh- 3 (2/4) Düşük
///   AB Rh- 1 (1/2) Kritik · 0 Rh- 2 (4/8) Kritik  → migration 4 açık uyarı üretir.
/// </summary>
public class StokModuluTestleri : IDisposable
{
    private const int ARhPoz = 1, ARhNeg = 2, ABRhNeg = 6, SifirRhNeg = 8;
    private static readonly DateTimeOffset Simdi = new(2026, 11, 26, 10, 0, 0, TimeSpan.Zero);

    private readonly TestVeritabani _db = new();
    private readonly StokService _servis;
    private readonly IslemYapan _yapan;

    public StokModuluTestleri()
    {
        _servis = new StokService(new KanStokRepository(_db.Context), new AyarlanabilirZaman(Simdi));
        var kullanici = new Kullanici
        {
            Eposta = "stok@ornekhastane.com", ParolaHash = "x", Rol = KullaniciRolu.Kurum, HastaneId = 1, OlusturmaTarihi = Simdi.UtcDateTime,
        };
        _db.Context.Kullanicilar.Add(kullanici);
        _db.Context.SaveChanges();
        _yapan = new IslemYapan(kullanici.Id, 1);
        _db.TakibiTemizle();
    }

    public void Dispose() => _db.Dispose();

    private int Stok(int kanGrubuId)
    {
        _db.TakibiTemizle();
        return _db.Context.KanStoklari.Single(s => s.KanGrubuId == kanGrubuId).UniteSayisi;
    }

    private List<StokUyarisi> AcikUyarilar(int kanGrubuId)
    {
        _db.TakibiTemizle();
        return _db.Context.StokUyarilari.Where(u => u.KanGrubuId == kanGrubuId && u.KapanisTarihi == null).ToList();
    }

    // ---------------- Stok modeli ve kritik eşik ----------------

    [Theory]
    [InlineData(0, StokDurumu.Kritik)]
    [InlineData(4, StokDurumu.Kritik)]    // eşiğe eşit → kritik
    [InlineData(5, StokDurumu.Dusuk)]
    [InlineData(8, StokDurumu.Dusuk)]
    [InlineData(9, StokDurumu.Normal)]
    public void Durum_esiklere_gore_hesaplanir(int stok, StokDurumu beklenen) =>
        Assert.Equal(beklenen, KanStok.DurumHesapla(stok, kritikEsik: 4, uyariEsigi: 8));

    [Fact]
    public void Migration_mevcut_stoga_gore_baslangic_uyarilarini_acar()
    {
        var acik = _db.Context.StokUyarilari.Where(u => u.KapanisTarihi == null).OrderBy(u => u.KanGrubuId).ToList();

        Assert.Equal([ARhNeg, 4, ABRhNeg, SifirRhNeg], acik.Select(u => u.KanGrubuId));
        Assert.Equal(StokDurumu.Kritik, acik.Single(u => u.KanGrubuId == SifirRhNeg).Seviye);
        Assert.Equal(StokDurumu.Dusuk, acik.Single(u => u.KanGrubuId == ARhNeg).Seviye);
    }

    // ---------------- Giriş / çıkış kaydı ----------------

    [Fact]
    public async Task Cikis_stogu_azaltir_ve_oncesi_sonrasi_ile_kaydedilir()
    {
        var cevap = await _servis.HareketGirAsync(new StokHareketIstegi(ARhPoz, HareketNedeni.Transfuzyon, 3, null), _yapan);

        Assert.True(cevap.Basarili, string.Join(" ", cevap.Hatalar));
        Assert.Equal(21, Stok(ARhPoz));
        var h = _db.Context.StokHareketleri.Single();
        Assert.Equal((HareketTuru.Cikis, 24, 21, 3), (h.Tur, h.OncekiStok, h.SonrakiStok, h.Miktar));
        Assert.Equal((_yapan.KullaniciId, 1), (h.KullaniciId!.Value, h.HastaneId!.Value));
        Assert.Equal(Simdi.UtcDateTime, h.Tarih);
    }

    [Fact]
    public async Task Giris_stogu_artirir()
    {
        var cevap = await _servis.HareketGirAsync(new StokHareketIstegi(ARhPoz, HareketNedeni.TransferGiris, 5, "Elazığ'dan"), _yapan);

        Assert.True(cevap.Basarili);
        Assert.Equal(29, Stok(ARhPoz));
        Assert.Equal(HareketTuru.Giris, _db.Context.StokHareketleri.Single().Tur);
    }

    [Fact]
    public async Task Stoktan_fazla_cikis_yapilamaz_hicbir_sey_yazilmaz()
    {
        var cevap = await _servis.HareketGirAsync(new StokHareketIstegi(SifirRhNeg, HareketNedeni.Transfuzyon, 3, null, KritikOnayi: true), _yapan);

        Assert.False(cevap.Basarili);
        Assert.Contains("yalnızca 2 ünite", cevap.Hatalar.Single());
        Assert.Equal(2, Stok(SifirRhNeg));
        Assert.Empty(_db.Context.StokHareketleri);
    }

    [Fact]
    public async Task Depo_katmaninda_da_stok_eksiye_dusmez_atomik_guncelleme()
    {
        // Servisteki kontrol atlansa bile (ör. aynı anda iki çıkış) veritabanı güncellemesi koşullu olduğu için stok eksiye inmez.
        var depo = new KanStokRepository(_db.Context);
        var sonuc = await depo.HareketUygulaAsync(new StokHareketi
        {
            KanGrubuId = SifirRhNeg, Tur = HareketTuru.Cikis, Neden = HareketNedeni.Transfuzyon, Miktar = 5, Tarih = Simdi.UtcDateTime,
        });

        Assert.Equal(StokIslemDurumu.StokYetersiz, sonuc.Durum);
        Assert.Equal(2, sonuc.MevcutStok);
        Assert.Equal(2, Stok(SifirRhNeg));
        Assert.Empty(_db.Context.StokHareketleri);
    }

    [Fact]
    public async Task Hareketler_kayit_tutarli_olmazsa_veritabani_reddeder()
    {
        // 10 - 3 = 7 olmalı; 8 yazılırsa CHECK kısıtı reddeder.
        _db.Context.StokHareketleri.Add(new StokHareketi
        {
            KanGrubuId = ARhPoz, Tur = HareketTuru.Cikis, Neden = HareketNedeni.Imha, Miktar = 3, OncekiStok = 10, SonrakiStok = 8, Tarih = Simdi.UtcDateTime,
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => _db.Context.SaveChangesAsync());
        _db.TakibiTemizle();

        // Giriş nedeni çıkış türüyle kullanılamaz.
        _db.Context.StokHareketleri.Add(new StokHareketi
        {
            KanGrubuId = ARhPoz, Tur = HareketTuru.Cikis, Neden = HareketNedeni.TransferGiris, Miktar = 1, OncekiStok = 10, SonrakiStok = 9, Tarih = Simdi.UtcDateTime,
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => _db.Context.SaveChangesAsync());
    }

    // ---------------- İş kuralları / doğrulama ----------------

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    [InlineData(51)]
    public async Task Miktar_1_ile_50_arasinda_olmali(int miktar)
    {
        var cevap = await _servis.HareketGirAsync(new StokHareketIstegi(ARhPoz, HareketNedeni.TransferGiris, miktar, null), _yapan);

        Assert.False(cevap.Basarili);
        Assert.Contains(cevap.Hatalar, h => h.Contains("1 ile 50"));
    }

    [Fact]
    public async Task Bagis_girisi_elle_yapilamaz()
    {
        var cevap = await _servis.HareketGirAsync(new StokHareketIstegi(ARhPoz, HareketNedeni.Bagis, 1, null), _yapan);

        Assert.False(cevap.Basarili);
        Assert.Contains(cevap.Hatalar, h => h.Contains("Bağış girişi elle yapılamaz"));
        Assert.Equal(24, Stok(ARhPoz));
    }

    [Theory]
    [InlineData(HareketNedeni.Imha, null)]
    [InlineData(HareketNedeni.SayimEksigi, "  ")]
    [InlineData(HareketNedeni.SayimFazlasi, "yok")]
    public async Task Imha_ve_sayim_duzeltmesinde_aciklama_zorunlu(HareketNedeni neden, string? aciklama)
    {
        var cevap = await _servis.HareketGirAsync(new StokHareketIstegi(ARhPoz, neden, 1, aciklama), _yapan);

        Assert.False(cevap.Basarili);
        Assert.Contains(cevap.Hatalar, h => h.Contains("açıklama zorunlu"));
    }

    [Fact]
    public async Task Bilinmeyen_kan_grubu_reddedilir()
    {
        var cevap = await _servis.HareketGirAsync(new StokHareketIstegi(99, HareketNedeni.TransferGiris, 1, null), _yapan);
        Assert.Contains("Kan grubu bulunamadı.", cevap.Hatalar);
    }

    [Fact]
    public async Task Stogu_kritik_seviyeye_dusuren_cikis_onay_ister()
    {
        // A Rh+ 24, kritik eşik 8: 16 ünite çıkış → 8 (kritik).
        var istek = new StokHareketIstegi(ARhPoz, HareketNedeni.TransferCikis, 16, "Diyarbakır'a");

        var onaysiz = await _servis.HareketGirAsync(istek, _yapan);
        Assert.False(onaysiz.Basarili);
        Assert.True(onaysiz.OnayGerekli);
        Assert.Equal(24, Stok(ARhPoz));

        var onayli = await _servis.HareketGirAsync(istek with { KritikOnayi = true }, _yapan);
        Assert.True(onayli.Basarili);
        Assert.Equal(UyariDegisimi.Acildi, onayli.Uyari);
        Assert.Equal(8, Stok(ARhPoz));
    }

    // ---------------- Kritik eşik uyarısı ----------------

    [Fact]
    public async Task Esik_altina_inince_uyari_acilir_ustune_cikinca_kapanir()
    {
        var cikis = await _servis.HareketGirAsync(new StokHareketIstegi(ARhPoz, HareketNedeni.Transfuzyon, 10, null), _yapan);   // 24 → 14 (düşük)
        Assert.Equal(UyariDegisimi.Acildi, cikis.Uyari);
        var uyari = Assert.Single(AcikUyarilar(ARhPoz));
        Assert.Equal((StokDurumu.Dusuk, 14, 15), (uyari.Seviye, uyari.StokDegeri, uyari.Esik));

        var giris = await _servis.HareketGirAsync(new StokHareketIstegi(ARhPoz, HareketNedeni.TransferGiris, 5, null), _yapan);     // 14 → 19 (normal)
        Assert.Equal(UyariDegisimi.Kapandi, giris.Uyari);
        Assert.Empty(AcikUyarilar(ARhPoz));
        Assert.Equal(Simdi.UtcDateTime, _db.Context.StokUyarilari.Single(u => u.Id == uyari.Id).KapanisTarihi);
    }

    [Fact]
    public async Task Seviye_degisince_ayni_uyari_guncellenir_yeni_uyari_acilmaz()
    {
        // 0 Rh-: 2 (kritik) → +3 = 5 (düşük)
        var cevap = await _servis.HareketGirAsync(new StokHareketIstegi(SifirRhNeg, HareketNedeni.TransferGiris, 3, null), _yapan);

        Assert.Equal(UyariDegisimi.SeviyeDegisti, cevap.Uyari);
        var uyari = Assert.Single(AcikUyarilar(SifirRhNeg));
        Assert.Equal((StokDurumu.Dusuk, 5, 8), (uyari.Seviye, uyari.StokDegeri, uyari.Esik));
    }

    [Fact]
    public async Task Bir_kan_grubunun_iki_acik_uyarisi_olamaz()
    {
        _db.Context.StokUyarilari.Add(new StokUyarisi
        {
            KanGrubuId = SifirRhNeg, Seviye = StokDurumu.Kritik, StokDegeri = 2, Esik = 4, AcilisTarihi = Simdi.UtcDateTime, GuncellemeTarihi = Simdi.UtcDateTime,
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => _db.Context.SaveChangesAsync());
    }

    [Theory]
    [InlineData(5, 5)]
    [InlineData(6, 4)]
    [InlineData(-1, 3)]
    [InlineData(10, 501)]
    public async Task Gecersiz_esikler_reddedilir(int kritik, int uyari)
    {
        var cevap = await _servis.EsikGuncelleAsync(ARhPoz, kritik, uyari);
        Assert.False(cevap.Basarili);
    }

    [Fact]
    public async Task Esik_degisince_uyari_yeniden_degerlendirilir()
    {
        var yukselt = await _servis.EsikGuncelleAsync(ARhPoz, 25, 30);   // stok 24 → artık kritik
        Assert.True(yukselt.Basarili);
        Assert.Equal(UyariDegisimi.Acildi, yukselt.Uyari);
        Assert.Equal(StokDurumu.Kritik, Assert.Single(AcikUyarilar(ARhPoz)).Seviye);

        var dusur = await _servis.EsikGuncelleAsync(ARhPoz, 8, 15);      // geri al → uyarı kapanır
        Assert.Equal(UyariDegisimi.Kapandi, dusur.Uyari);
        Assert.Empty(AcikUyarilar(ARhPoz));
    }

    // ---------------- Önceki modüllerle entegrasyon ----------------

    [Fact]
    public async Task Bagis_kaydi_giris_hareketi_olusturur_ve_uyariyi_gunceller()
    {
        // Örnek bağışçı 2 (Mehmet Kaya) 0 Rh-. Stok 2 (kritik). 3 ünite bağış → 5 (düşük), sonra 4 ünite → 9 (normal).
        var bagislar = new BagisRepository(_db.Context);
        var ilk = await bagislar.BagisKaydetVeStokArtirAsync(new Bagis { BagisciId = 2, HastaneId = 1, UniteSayisi = 3, BagisTarihi = Simdi.UtcDateTime });

        Assert.Equal(5, Stok(SifirRhNeg));
        var h = _db.Context.StokHareketleri.Single();
        Assert.Equal((HareketTuru.Giris, HareketNedeni.Bagis, ilk.Id, 2, 5), (h.Tur, h.Neden, h.BagisId!.Value, h.OncekiStok, h.SonrakiStok));
        Assert.Equal(StokDurumu.Dusuk, Assert.Single(AcikUyarilar(SifirRhNeg)).Seviye);

        await bagislar.BagisKaydetVeStokArtirAsync(new Bagis { BagisciId = 2, HastaneId = 1, UniteSayisi = 4, BagisTarihi = Simdi.UtcDateTime });
        Assert.Equal(9, Stok(SifirRhNeg));
        Assert.Empty(AcikUyarilar(SifirRhNeg));
    }

    [Fact]
    public async Task Hareket_gecmisi_hastaneye_ve_ture_gore_suzulur()
    {
        await _servis.HareketGirAsync(new StokHareketIstegi(ARhPoz, HareketNedeni.Transfuzyon, 1, null), _yapan);
        await _servis.HareketGirAsync(new StokHareketIstegi(ARhPoz, HareketNedeni.TransferGiris, 2, null), _yapan with { HastaneId = 2 });

        Assert.Equal(2, (await _servis.HareketlerAsync(new HareketFiltresi())).Count);
        Assert.Equal(HareketNedeni.Transfuzyon, Assert.Single(await _servis.HareketlerAsync(new HareketFiltresi(HastaneId: 1))).Neden);
        Assert.Equal(2, Assert.Single(await _servis.HareketlerAsync(new HareketFiltresi(Tur: HareketTuru.Giris))).Miktar);
    }
}
