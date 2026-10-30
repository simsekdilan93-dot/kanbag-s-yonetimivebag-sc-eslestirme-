using System.Data.Common;
using KanBagis.DataAccess.Repositories;
using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;

namespace KanBagis.Tests;

/// <summary>Veritabanı kısıtlarının hatalı veriyi gerçekten reddettiğini doğrular.</summary>
public class KisitTestleri : IDisposable
{
    private readonly TestVeritabani _db = new();

    public void Dispose() => _db.Dispose();

    private static Bagisci Bagisci(string tc, int kanGrubuId = 1) => new()
    {
        TcKimlikNo = tc, Ad = "Deneme", Soyad = "Kişi", Telefon = "05550000099",
        DogumTarihi = new DateOnly(1999, 1, 1), Cinsiyet = Cinsiyet.Erkek, Kilo = 70,
        Sehir = "Bingöl", KanGrubuId = kanGrubuId,
    };

    [Fact]
    public async Task Ayni_TC_iki_kez_eklenemez_UNIQUE()
    {
        _db.Context.Bagiscilar.Add(Bagisci("99900000001")); // örnek veride zaten var

        await Assert.ThrowsAsync<DbUpdateException>(() => _db.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task TC_11_haneden_farkli_olamaz_CHECK()
    {
        _db.Context.Bagiscilar.Add(Bagisci("12345"));

        await Assert.ThrowsAsync<DbUpdateException>(() => _db.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task Olmayan_kan_grubuna_bagisci_eklenemez_FOREIGN_KEY()
    {
        _db.Context.Bagiscilar.Add(Bagisci("99900000098", kanGrubuId: 99));

        await Assert.ThrowsAsync<DbUpdateException>(() => _db.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task Kilo_mantikli_aralikta_olmali_CHECK()
    {
        var bagisci = Bagisci("99900000097");
        bagisci.Kilo = 0;
        _db.Context.Bagiscilar.Add(bagisci);

        await Assert.ThrowsAsync<DbUpdateException>(() => _db.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task Ayni_ABO_ve_Rh_ikinci_kez_eklenemez_UNIQUE()
    {
        _db.Context.KanGruplari.Add(new KanGrubu { Id = 99, Ad = "Sıfır negatif", AboGrubu = AboGrubu.O, RhPozitif = false });

        await Assert.ThrowsAsync<DbUpdateException>(() => _db.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task Stok_negatif_olamaz_CHECK()
    {
        var stok = await _db.Context.KanStoklari.FirstAsync();
        stok.UniteSayisi = -1;

        await Assert.ThrowsAsync<DbUpdateException>(() => _db.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task Ayni_talep_ayni_bagisciya_iki_kez_bildirilemez_UNIQUE()
    {
        // Örnek veride (BagisciId=2, KanTalebiId=1) bildirimi zaten var.
        _db.Context.Bildirimler.Add(new Bildirim
        {
            BagisciId = 2, KanTalebiId = 1, Mesaj = "Tekrar", GonderimTarihi = DateTime.UtcNow,
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => _db.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task Bagiscisi_olan_kan_grubu_silinemez_RESTRICT()
    {
        await Assert.ThrowsAnyAsync<DbException>(() =>
            _db.Context.KanGruplari.Where(k => k.Id == 8).ExecuteDeleteAsync());
    }

    [Fact]
    public async Task Talep_silinince_bildirimleri_de_silinir_CASCADE()
    {
        Assert.Equal(3, await _db.Context.Bildirimler.CountAsync(b => b.KanTalebiId == 1));

        await _db.Context.KanTalepleri.Where(t => t.Id == 1).ExecuteDeleteAsync();

        Assert.Equal(0, await _db.Context.Bildirimler.CountAsync(b => b.KanTalebiId == 1));
    }

    [Fact]
    public async Task Bagis_kaydedilince_stok_artar_TRANSACTION()
    {
        var repo = new BagisRepository(_db.Context);

        // Bağışçı 10 → 0 Rh- (KanGrubuId 8), başlangıç stoğu 2.
        await repo.BagisKaydetVeStokArtirAsync(new Bagis
        {
            BagisciId = 10, HastaneId = 1, UniteSayisi = 1, BagisTarihi = DateTime.UtcNow,
        });
        _db.TakibiTemizle();

        Assert.Equal(3, (await _db.Context.KanStoklari.SingleAsync(s => s.KanGrubuId == 8)).UniteSayisi);
        Assert.Equal(6, await _db.Context.Bagislar.CountAsync());
    }

    [Fact]
    public async Task Hatali_bagista_hicbir_sey_kaydedilmez_ROLLBACK()
    {
        var repo = new BagisRepository(_db.Context);

        // HastaneId 99 yok → FK hatası; transaction geri alınır.
        await Assert.ThrowsAsync<DbUpdateException>(() => repo.BagisKaydetVeStokArtirAsync(new Bagis
        {
            BagisciId = 10, HastaneId = 99, UniteSayisi = 1, BagisTarihi = DateTime.UtcNow,
        }));
        _db.TakibiTemizle();

        Assert.Equal(2, (await _db.Context.KanStoklari.SingleAsync(s => s.KanGrubuId == 8)).UniteSayisi);
        Assert.Equal(5, await _db.Context.Bagislar.CountAsync());
    }
}
