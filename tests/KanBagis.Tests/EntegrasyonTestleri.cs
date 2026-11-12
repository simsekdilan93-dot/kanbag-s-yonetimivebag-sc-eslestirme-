using KanBagis.Business.Bagiscilar;
using KanBagis.Business.Talepler;
using KanBagis.DataAccess.Repositories;
using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;

namespace KanBagis.Tests;

/// <summary>
/// Hafta 8 (VİZE): modüllerin BİRLİKTE çalıştığını sınayan entegrasyon testleri.
/// Talep (Modül B) → kabul → bağış kaydı → stok (Hafta 4) → bağışçının uygunluğu (Modül A).
/// Örnek veri: talep #1 = Hastane 1, 0 Rh-, 4 ünite; bildirim #1 = Mehmet (Kabul), #3 = Burak (Beklemede).
/// </summary>
public class EntegrasyonTestleri : IDisposable
{
    private static readonly DateTimeOffset Simdi = new(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);
    private readonly TestVeritabani _db = new();
    private readonly TalepService _talepler;
    private readonly BagisciService _bagiscilar;

    public EntegrasyonTestleri()
    {
        var zaman = new AyarlanabilirZaman(Simdi);
        var kural = new BagisUygunlukKurali(zaman);
        _bagiscilar = new BagisciService(new BagisciRepository(_db.Context), new EfRepository<KanGrubu>(_db.Context), kural);
        _talepler = new TalepService(
            new KanTalebiRepository(_db.Context), new HastaneRepository(_db.Context), new EfRepository<KanGrubu>(_db.Context),
            new BildirimRepository(_db.Context), new BagisRepository(_db.Context),
            new EslestirmeService(new BagisciRepository(_db.Context), new EfRepository<KanGrubu>(_db.Context), kural),
            zaman);
    }

    public void Dispose() => _db.Dispose();

    private async Task<int> StokAsync(int kanGrubuId)
    {
        _db.TakibiTemizle();
        return (await _db.Context.KanStoklari.SingleAsync(s => s.KanGrubuId == kanGrubuId)).UniteSayisi;
    }

    [Fact]
    public async Task Kabul_eden_bagiscinin_bagisi_kaydedilir_stok_artar_ve_bagisci_uygunlugu_degisir()
    {
        Assert.True((await _bagiscilar.ProfilGetirAsync(2))!.Uygunluk.Uygun);   // Mehmet: bağıştan önce uygun
        var stokOnce = await StokAsync(8);                                      // 0 Rh-

        var sonuc = await _talepler.BagisKaydetAsync(hastaneId: 1, talepId: 1, bildirimId: 1);

        Assert.True(sonuc.Basarili, sonuc.Hata);
        Assert.False(sonuc.TalepKapandi);                                       // 4 ünitenin 1'i alındı
        Assert.Equal(stokOnce + 1, await StokAsync(8));

        var bagis = await _db.Context.Bagislar.SingleAsync(b => b.KanTalebiId == 1);
        Assert.Equal((2, 1), (bagis.BagisciId, bagis.HastaneId));

        // Modül A artık yeni bağışı görüyor: Mehmet (erkek) 90 gün boyunca uygun değil.
        var profil = (await _bagiscilar.ProfilGetirAsync(2))!;
        Assert.False(profil.Uygunluk.Uygun);
        Assert.Equal(new DateOnly(2027, 2, 10), profil.Uygunluk.SonrakiUygunTarih);

        // Talep detayı da güncel: 1 bağış alındı, Mehmet "Alındı" olarak işaretli.
        var detay = (await _talepler.DetayGetirAsync(1, 1))!;
        Assert.Equal(1, detay.AlinanBagisSayisi);
        Assert.True(detay.KabulEdenler.Single().BagisAlindi);
    }

    [Fact]
    public async Task Istenen_unite_sayisina_ulasilinca_talep_kendiliginden_kapanir()
    {
        // Hastane 1, A Rh+, 1 ünite → bildirim gönderilir; ilk bağışçı kabul etmiş sayılır.
        var talepId = (await _talepler.OlusturAsync(1, new TalepOlusturmaIstegi(1, 1, Aciliyet.Normal, null))).TalepId!.Value;
        var bildirim = await _db.Context.Bildirimler.OrderBy(b => b.Id).FirstAsync(b => b.KanTalebiId == talepId);
        bildirim.Yanit = BildirimYaniti.Kabul;
        bildirim.YanitTarihi = Simdi.UtcDateTime;
        await _db.Context.SaveChangesAsync();

        var sonuc = await _talepler.BagisKaydetAsync(1, talepId, bildirim.Id);

        Assert.True(sonuc.TalepKapandi);
        _db.TakibiTemizle();
        var talep = await _db.Context.KanTalepleri.SingleAsync(t => t.Id == talepId);
        Assert.Equal(TalepDurumu.Karsilandi, talep.Durum);
        Assert.Equal(Simdi.UtcDateTime, talep.KapanisTarihi);

        // Kapanan talebe artık ne bağış ne bildirim eklenebilir.
        Assert.Contains("Kapanmış", (await _talepler.DahaFazlaBildirAsync(1, talepId)).Hata);
    }

    [Fact]
    public async Task Ayni_bagis_iki_kez_kaydedilemez()
    {
        await _talepler.BagisKaydetAsync(1, 1, 1);
        var stok = await StokAsync(8);

        var ikinci = await _talepler.BagisKaydetAsync(1, 1, 1);

        Assert.Contains("zaten kaydedildi", ikinci.Hata);
        Assert.Equal(stok, await StokAsync(8));
    }

    [Theory]
    [InlineData(1, 1, 3, "kabul eden")]       // Burak henüz kabul etmedi (Beklemede)
    [InlineData(1, 1, 4, "böyle bir bağışçı")] // bildirim #4 başka talebin (talep #2)
    [InlineData(2, 1, 1, "Talep bulunamadı")]  // hastane 2, hastane 1'in talebine bağış kaydedemez (sahiplik)
    public async Task Gecersiz_bagis_kaydi_reddedilir_ve_stok_degismez(int hastaneId, int talepId, int bildirimId, string hata)
    {
        var stok = await StokAsync(8);

        var sonuc = await _talepler.BagisKaydetAsync(hastaneId, talepId, bildirimId);

        Assert.False(sonuc.Basarili);
        Assert.Contains(hata, sonuc.Hata);
        Assert.Equal(stok, await StokAsync(8));
        Assert.Equal(5, await _db.Context.Bagislar.CountAsync());
    }

    [Fact]
    public async Task Ayni_talep_icin_ikinci_bagis_veritabaninda_da_engellenir_ve_islem_geri_alinir()
    {
        var repo = new BagisRepository(_db.Context);
        await repo.BagisKaydetVeStokArtirAsync(new Bagis { BagisciId = 2, HastaneId = 1, KanTalebiId = 1, BagisTarihi = Simdi.UtcDateTime });
        var stok = await StokAsync(8);

        // İş kuralı atlanıp doğrudan veri katmanına gidilse bile filtreli UNIQUE indeks engeller; transaction geri alınır.
        await Assert.ThrowsAsync<DbUpdateException>(() => repo.BagisKaydetVeStokArtirAsync(
            new Bagis { BagisciId = 2, HastaneId = 1, KanTalebiId = 1, BagisTarihi = Simdi.UtcDateTime }));

        Assert.Equal(stok, await StokAsync(8));
    }

    [Fact]
    public async Task Talepsiz_gonullu_bagislar_icin_kisit_uygulanmaz()
    {
        var repo = new BagisRepository(_db.Context);

        // Örnek veride Mehmet'in iki talepsiz bağışı zaten var; bir tane daha eklenebilir.
        await repo.BagisKaydetVeStokArtirAsync(new Bagis { BagisciId = 2, HastaneId = 1, BagisTarihi = Simdi.UtcDateTime });

        Assert.Equal(3, await _db.Context.Bagislar.CountAsync(b => b.BagisciId == 2 && b.KanTalebiId == null));
    }
}
