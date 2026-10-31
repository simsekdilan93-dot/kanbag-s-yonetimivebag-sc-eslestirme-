using KanBagis.Business.Bagiscilar;
using KanBagis.Business.Konum;
using KanBagis.Business.Talepler;
using KanBagis.Business.Tanimlar;
using KanBagis.DataAccess.Repositories;
using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;

namespace KanBagis.Tests;

// Örnek veri hatırlatma (test tarihi 22.10.2026):
//   Bağışçılar                                     Hastaneler
//   1 Ayşe   A Rh+  Bingöl     son bağış 15.06    1 Örnek Devlet  Bingöl  (açık talep #1: 0 Rh-)
//   2 Mehmet 0 Rh-  Bingöl     son bağış 01.07    2 Örnek Şehir   Elazığ  (açık talep #2: A Rh+)
//   3 Zeynep B Rh+  Elazığ     son bağış 02.05    3 Örnek Üniv.   Diyarbakır
//   4 Ali    A Rh+  Bingöl     bağış yok
//   5 Elif   0 Rh-  Elazığ     bağış yok          Talep #1 bildirimleri: Mehmet (Kabul), Elif (Red), Burak (Beklemede)
//   7 Fatma  0 Rh+  Bingöl     20.08 → UYGUN DEĞİL (kadın, 120 gün)
//   8 Emre   A Rh-  Diyarbakır bağış yok
//   9 Merve  B Rh-  Bingöl     bağış yok
//  10 Burak  0 Rh-  Bingöl     bağış yok
//   Uzaklıklar: Bingöl–Elazığ ~113 km, Bingöl–Diyarbakır ~110 km, Bingöl–Muş ~88 km

// =====================================================================
// Konum ve mesafe
// =====================================================================
public class KonumTestleri
{
    [Theory]
    [InlineData("Bingöl", "Elazığ", 100, 130)]
    [InlineData("İstanbul", "Ankara", 330, 370)]
    [InlineData("Edirne", "Iğdır", 1400, 1550)]   // Türkiye'nin iki ucu (kuş uçuşu ~1480 km; yol mesafesi daha uzun)
    public void Iller_arasi_mesafe_makul(string il1, string il2, double enAz, double enCok)
    {
        var km = IlKonumlari.MesafeKm(il1, il2);

        Assert.InRange(km, enAz, enCok);
        Assert.Equal(km, IlKonumlari.MesafeKm(il2, il1), 6);   // simetrik
    }

    [Fact]
    public void Ayni_il_sifir_km()
    {
        Assert.Equal(0, IlKonumlari.MesafeKm("Bingöl", "Bingöl"));
    }

    [Fact]
    public void Tum_81_ilin_koordinati_tanimli()
    {
        Assert.Equal(Iller.Tumu.OrderBy(x => x), IlKonumlari.TanimliIller.OrderBy(x => x));
    }

    [Fact]
    public void Yaricap_icindeki_iller()
    {
        var yakin = IlKonumlari.YaricapIcindekiIller("Bingöl", 100);
        Assert.Contains("Bingöl", yakin.Keys);
        Assert.Contains("Muş", yakin.Keys);
        Assert.DoesNotContain("Elazığ", yakin.Keys);

        var genis = IlKonumlari.YaricapIcindekiIller("Bingöl", 300);
        Assert.Contains("Elazığ", genis.Keys);
        Assert.Contains("Diyarbakır", genis.Keys);
        Assert.DoesNotContain("İstanbul", genis.Keys);
    }
}

// =====================================================================
// Eşleştirme: uyumluluk + konum + uygunluk + sıralama
// =====================================================================
public class EslestirmeServiceTestleri : IDisposable
{
    private readonly TestVeritabani _db = new();
    private readonly EslestirmeService _eslestirme;

    public EslestirmeServiceTestleri()
    {
        var zaman = new AyarlanabilirZaman(new DateTimeOffset(2026, 10, 22, 9, 0, 0, TimeSpan.Zero));
        _eslestirme = new EslestirmeService(
            new BagisciRepository(_db.Context),
            new EfRepository<KanGrubu>(_db.Context),
            new BagisUygunlukKurali(zaman));
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Uyumlu_gruplar_aranir_ve_tam_grup_once_gelir()
    {
        // A Rh+ talebi → A Rh+, A Rh-, 0 Rh+, 0 Rh- verebilir. Fatma (0 Rh+) 120 gün kuralına takılır, Merve (B Rh-) uyumsuz.
        var adaylar = await _eslestirme.AdaylariBulAsync(1, "Bingöl", Aciliyet.Normal);

        // Önce tam grup (A Rh+): Ali (hiç bağış yok) → Ayşe; sonra 0 Rh-: Burak (hiç bağış yok) → Mehmet.
        Assert.Equal(["Ali", "Ayşe", "Burak", "Mehmet"], adaylar.Select(a => a.Bagisci.Ad));
        Assert.All(adaylar.Take(2), a => Assert.True(a.TamGrupEslesmesi));
        Assert.All(adaylar.Skip(2), a => Assert.False(a.TamGrupEslesmesi));
    }

    [Fact]
    public async Task Acil_talepte_arama_yaricapi_genisler_ve_yakin_once_gelir()
    {
        // 0 Rh- talebi, merkez Bingöl. Elif Elazığ'da (~113 km).
        var normal = await _eslestirme.AdaylariBulAsync(8, "Bingöl", Aciliyet.Normal);   // 100 km
        var acil = await _eslestirme.AdaylariBulAsync(8, "Bingöl", Aciliyet.Acil);       // 300 km

        Assert.Equal(["Burak", "Mehmet"], normal.Select(a => a.Bagisci.Ad));
        Assert.Equal(["Burak", "Mehmet", "Elif"], acil.Select(a => a.Bagisci.Ad));       // Elif en uzak → en sonda
        Assert.Equal(0, acil[0].MesafeKm);
        Assert.InRange(acil[2].MesafeKm, 100, 130);
    }

    [Fact]
    public async Task Haric_tutulanlar_listeye_girmez()
    {
        var adaylar = await _eslestirme.AdaylariBulAsync(8, "Bingöl", Aciliyet.Acil, new HashSet<int> { 10 });

        Assert.DoesNotContain(adaylar, a => a.Bagisci.Id == 10);
    }

    [Fact]
    public async Task Ilde_arama_esitlikte_en_uzun_suredir_bagis_yapmayan_once()
    {
        // AB Rh+ herkesten alabilir; Bingöl'de AB Rh+ bağışçı yok → hiçbiri tam eşleşme değil.
        // Hiç bağış yapmamışlar (Id sırasıyla): Ali(4), Merve(9), Burak(10); sonra Ayşe (15.06), Mehmet (01.07).
        var adaylar = await _eslestirme.IldekiAdaylariBulAsync(5, "Bingöl");

        Assert.Equal(["Ali", "Merve", "Burak", "Ayşe", "Mehmet"], adaylar.Select(a => a.Bagisci.Ad));
    }

    [Fact]
    public async Task Ilde_arama_baska_ili_kapsamaz()
    {
        // A Rh- alıcı, Diyarbakır: A Rh- ve 0 Rh- verebilir → yalnızca Emre (Bingöl 110 km ötede, dahil değil).
        var adaylar = await _eslestirme.IldekiAdaylariBulAsync(2, "Diyarbakır");

        Assert.Equal(["Emre"], adaylar.Select(a => a.Bagisci.Ad));
    }
}

// =====================================================================
// Talep servisi: oluşturma, doğrulama, bildirim, sahiplik, kapatma
// =====================================================================
public class TalepServiceTestleri : IDisposable
{
    private readonly TestVeritabani _db = new();
    private readonly TalepService _talepler;

    public TalepServiceTestleri()
    {
        var zaman = new AyarlanabilirZaman(new DateTimeOffset(2026, 10, 22, 9, 0, 0, TimeSpan.Zero));
        var eslestirme = new EslestirmeService(
            new BagisciRepository(_db.Context),
            new EfRepository<KanGrubu>(_db.Context),
            new BagisUygunlukKurali(zaman));
        _talepler = new TalepService(
            new KanTalebiRepository(_db.Context),
            new HastaneRepository(_db.Context),
            new EfRepository<KanGrubu>(_db.Context),
            new BildirimRepository(_db.Context),
            eslestirme,
            zaman);
    }

    public void Dispose() => _db.Dispose();

    private Task<List<string>> BildirilenlerAsync(int talepId) =>
        _db.Context.Bildirimler.Where(b => b.KanTalebiId == talepId)
           .OrderBy(b => b.Id).Select(b => b.Bagisci.Ad).ToListAsync();

    [Fact]
    public async Task Talep_olusturulunca_kota_kadar_en_oncelikli_bagisciya_bildirim_gider()
    {
        // Hastane 1 (Bingöl), A Rh+, 1 ünite, Normal → kota 1 × 3 = 3.
        var sonuc = await _talepler.OlusturAsync(1, new TalepOlusturmaIstegi(1, 1, Aciliyet.Normal, null));

        Assert.True(sonuc.Basarili, sonuc.Hata);
        Assert.Equal(3, sonuc.BildirimSayisi);
        Assert.Equal(["Ali", "Ayşe", "Burak"], await BildirilenlerAsync(sonuc.TalepId!.Value));

        var talep = await _db.Context.KanTalepleri.SingleAsync(t => t.Id == sonuc.TalepId);
        Assert.Equal(TalepDurumu.Acik, talep.Durum);
    }

    [Fact]
    public async Task Acil_talepte_mesaj_ACIL_ile_baslar_ve_kota_buyuktur()
    {
        // Hastane 2 (Elazığ), 0 Rh-, Acil → 300 km: Elif (Elazığ), Burak ve Mehmet (Bingöl).
        var sonuc = await _talepler.OlusturAsync(2, new TalepOlusturmaIstegi(8, 1, Aciliyet.Acil, null));

        Assert.Equal(["Elif", "Burak", "Mehmet"], await BildirilenlerAsync(sonuc.TalepId!.Value));
        Assert.All(await _db.Context.Bildirimler.Where(b => b.KanTalebiId == sonuc.TalepId).ToListAsync(),
            b => Assert.StartsWith("ACİL:", b.Mesaj));
    }

    [Fact]
    public async Task Ayni_kan_grubu_icin_ikinci_acik_talep_acilamaz()
    {
        // Hastane 1'in 0 Rh- için açık talebi (#1) var.
        var sonuc = await _talepler.OlusturAsync(1, new TalepOlusturmaIstegi(8, 2, Aciliyet.Acil, null));

        Assert.False(sonuc.Basarili);
        Assert.Equal(1, sonuc.TalepId);   // kullanıcı açık talebe yönlendirilebilsin
        Assert.Contains("zaten açık", sonuc.Hata);
    }

    [Theory]
    [InlineData(1, 0, "Ünite")]
    [InlineData(1, 11, "Ünite")]
    [InlineData(99, 1, "kan grubu")]
    public async Task Gecersiz_talep_reddedilir(int kanGrubuId, int unite, string hataIcerir)
    {
        var sonuc = await _talepler.OlusturAsync(1, new TalepOlusturmaIstegi(kanGrubuId, unite, Aciliyet.Normal, null));

        Assert.False(sonuc.Basarili);
        Assert.Contains(hataIcerir, sonuc.Hata);
        Assert.Equal(3, await _db.Context.KanTalepleri.CountAsync());   // yeni talep oluşmadı
    }

    [Fact]
    public async Task Cok_uzun_aciklama_reddedilir()
    {
        var sonuc = await _talepler.OlusturAsync(1, new TalepOlusturmaIstegi(1, 1, Aciliyet.Normal, new string('x', 301)));

        Assert.Contains("Açıklama", sonuc.Hata);
    }

    [Fact]
    public async Task Daha_fazla_bildir_ayni_kisiye_tekrar_gondermez()
    {
        var talepId = (await _talepler.OlusturAsync(1, new TalepOlusturmaIstegi(1, 1, Aciliyet.Normal, null))).TalepId!.Value;

        var ikinci = await _talepler.DahaFazlaBildirAsync(1, talepId);
        Assert.True(ikinci.Basarili);
        Assert.Equal(["Ali", "Ayşe", "Burak", "Mehmet"], await BildirilenlerAsync(talepId));

        var ucuncu = await _talepler.DahaFazlaBildirAsync(1, talepId);
        Assert.False(ucuncu.Basarili);
        Assert.Contains("başka uygun bağışçı yok", ucuncu.Hata);
    }

    [Fact]
    public async Task Detayda_yalnizca_kabul_edenlerin_iletisim_bilgisi_var()
    {
        var detay = await _talepler.DetayGetirAsync(1, 1);

        Assert.NotNull(detay);
        Assert.Equal((1, 1, 1), (detay.KabulSayisi, detay.BekleyenSayisi, detay.RedSayisi));
        var kabul = Assert.Single(detay.KabulEdenler);
        Assert.Equal(("Mehmet Kaya", "05550000002"), (kabul.AdSoyad, kabul.Telefon));
        Assert.Equal(0, detay.BildirilmemisUygunAdaySayisi);   // Bingöl'deki iki 0 Rh- bağışçıya zaten bildirildi
    }

    [Fact]
    public async Task Baska_hastanenin_talebine_erisilemez_SAHIPLIK()
    {
        // Talep #1 hastane 1'in; hastane 2 deniyor.
        Assert.Null(await _talepler.DetayGetirAsync(2, 1));
        Assert.False((await _talepler.DahaFazlaBildirAsync(2, 1)).Basarili);
        Assert.False((await _talepler.KapatAsync(2, 1, TalepDurumu.Iptal)).Basarili);

        _db.TakibiTemizle();
        Assert.Equal(TalepDurumu.Acik, (await _db.Context.KanTalepleri.SingleAsync(t => t.Id == 1)).Durum);
    }

    [Fact]
    public async Task Talep_kapatilir_ve_tekrar_kapatilamaz()
    {
        Assert.True((await _talepler.KapatAsync(1, 1, TalepDurumu.Karsilandi)).Basarili);
        _db.TakibiTemizle();

        var talep = await _db.Context.KanTalepleri.SingleAsync(t => t.Id == 1);
        Assert.Equal(TalepDurumu.Karsilandi, talep.Durum);
        Assert.NotNull(talep.KapanisTarihi);

        Assert.Contains("zaten kapatılmış", (await _talepler.KapatAsync(1, 1, TalepDurumu.Iptal)).Hata);
        Assert.Contains("Kapanmış", (await _talepler.DahaFazlaBildirAsync(1, 1)).Hata);
    }

    [Fact]
    public async Task Talep_Acik_durumuna_kapatilamaz()
    {
        Assert.False((await _talepler.KapatAsync(1, 1, TalepDurumu.Acik)).Basarili);
    }

    [Fact]
    public async Task Kapanis_tarihi_kurali_veritabaninda_da_korunur_CHECK()
    {
        var talep = await _db.Context.KanTalepleri.SingleAsync(t => t.Id == 1);
        talep.Durum = TalepDurumu.Iptal;   // KapanisTarihi boş bırakıldı

        await Assert.ThrowsAsync<DbUpdateException>(() => _db.Context.SaveChangesAsync());
    }
}

// =====================================================================
// Bildirim yanıtı: sahiplik, kapalı talep, tek aktif kabul, uygunluk
// =====================================================================
public class BildirimServiceTestleri : IDisposable
{
    private readonly TestVeritabani _db = new();
    private readonly BildirimService _bildirimler;
    private readonly TalepService _talepler;

    public BildirimServiceTestleri()
    {
        var zaman = new AyarlanabilirZaman(new DateTimeOffset(2026, 10, 22, 9, 0, 0, TimeSpan.Zero));
        var kural = new BagisUygunlukKurali(zaman);
        var bagisciServisi = new BagisciService(new BagisciRepository(_db.Context), new EfRepository<KanGrubu>(_db.Context), kural);
        _bildirimler = new BildirimService(new BildirimRepository(_db.Context), bagisciServisi, zaman);
        _talepler = new TalepService(
            new KanTalebiRepository(_db.Context), new HastaneRepository(_db.Context), new EfRepository<KanGrubu>(_db.Context),
            new BildirimRepository(_db.Context),
            new EslestirmeService(new BagisciRepository(_db.Context), new EfRepository<KanGrubu>(_db.Context), kural),
            zaman);
    }

    public void Dispose() => _db.Dispose();

    private async Task<Bildirim> BildirimAsync(int id)
    {
        _db.TakibiTemizle();
        return await _db.Context.Bildirimler.SingleAsync(b => b.Id == id);
    }

    [Fact]
    public async Task Bagisci_kendi_bildirimini_kabul_eder()
    {
        // Bildirim #3: Burak, talep #1, Beklemede.
        var sonuc = await _bildirimler.YanitlaAsync(10, 3, BildirimYaniti.Kabul);

        Assert.True(sonuc.Basarili, sonuc.Hata);
        var b = await BildirimAsync(3);
        Assert.Equal(BildirimYaniti.Kabul, b.Yanit);
        Assert.NotNull(b.YanitTarihi);
    }

    [Fact]
    public async Task Baskasinin_bildirimi_yanitlanamaz_SAHIPLIK()
    {
        // Burak (10), Ayşe'nin bildirimini (#4) yanıtlamaya çalışıyor.
        var sonuc = await _bildirimler.YanitlaAsync(10, 4, BildirimYaniti.Red);

        Assert.Equal("Bildirim bulunamadı.", sonuc.Hata);
        Assert.Equal(BildirimYaniti.Beklemede, (await BildirimAsync(4)).Yanit);
    }

    [Fact]
    public async Task Kapanmis_talebe_yanit_verilemez()
    {
        await _talepler.KapatAsync(1, 1, TalepDurumu.Iptal);
        _db.TakibiTemizle();

        var sonuc = await _bildirimler.YanitlaAsync(10, 3, BildirimYaniti.Kabul);

        Assert.Contains("kapanmış", sonuc.Hata);
    }

    [Fact]
    public async Task Ayni_anda_iki_acik_talep_kabul_edilemez()
    {
        // Mehmet (2) talep #1'i zaten kabul etmiş. Hastane 1 A Rh+ için 2 ünitelik talep açıyor → Mehmet'e de bildirim gider.
        var talepId = (await _talepler.OlusturAsync(1, new TalepOlusturmaIstegi(1, 2, Aciliyet.Normal, null))).TalepId!.Value;
        var mehmetinBildirimi = await _db.Context.Bildirimler.SingleAsync(b => b.KanTalebiId == talepId && b.BagisciId == 2);

        var kabul = await _bildirimler.YanitlaAsync(2, mehmetinBildirimi.Id, BildirimYaniti.Kabul);
        Assert.Contains("başka bir açık talep", kabul.Hata);

        // Red her zaman verilebilir.
        Assert.True((await _bildirimler.YanitlaAsync(2, mehmetinBildirimi.Id, BildirimYaniti.Red)).Basarili);

        // İlk talep kapanınca yenisini kabul edebilir.
        await _talepler.KapatAsync(1, 1, TalepDurumu.Karsilandi);
        _db.TakibiTemizle();
        Assert.True((await _bildirimler.YanitlaAsync(2, mehmetinBildirimi.Id, BildirimYaniti.Kabul)).Basarili);
    }

    [Fact]
    public async Task Bagisa_uygun_olmayan_kabul_edemez()
    {
        // Fatma (7) son bağışından bu yana 120 gün geçmedi. Elle bildirim oluşturuluyor (talep #2, A Rh+).
        _db.Context.Bildirimler.Add(new Bildirim { BagisciId = 7, KanTalebiId = 2, Mesaj = "Test", GonderimTarihi = DateTime.UtcNow });
        await _db.Context.SaveChangesAsync();
        var id = (await _db.Context.Bildirimler.SingleAsync(b => b.BagisciId == 7)).Id;

        var sonuc = await _bildirimler.YanitlaAsync(7, id, BildirimYaniti.Kabul);

        Assert.Contains("uygun değilsiniz", sonuc.Hata);
        Assert.Contains("120 gün", sonuc.Hata);
    }

    [Fact]
    public async Task Beklemede_gecerli_bir_yanit_degildir()
    {
        Assert.False((await _bildirimler.YanitlaAsync(10, 3, BildirimYaniti.Beklemede)).Basarili);
    }
}
