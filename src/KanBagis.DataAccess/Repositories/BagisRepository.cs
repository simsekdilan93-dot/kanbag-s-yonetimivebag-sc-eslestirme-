using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;

namespace KanBagis.DataAccess.Repositories;

public interface IBagisRepository : IRepository<Bagis>
{
    /// <summary>
    /// Bağışı kaydeder, bağışçının kan grubunun stoğunu artırır ve istenirse talebi
    /// "Karşılandı" olarak kapatır. Hepsi tek transaction içinde yapılır.
    /// </summary>
    Task<Bagis> BagisKaydetVeStokArtirAsync(Bagis bagis, int? kapatilacakTalepId = null);

    Task<bool> TalepIcinBagisVarMiAsync(int bagisciId, int talepId);
    Task<int> TalebinBagisSayisiAsync(int talepId);
}

public class BagisRepository(KanBagisDbContext context) : EfRepository<Bagis>(context), IBagisRepository
{
    /// <summary>
    /// İşlemler tek bir TRANSACTION içinde yapılır: biri başarısız olursa hepsi geri alınır
    /// (ROLLBACK). Böylece "bağış kaydedildi ama stok artmadı" ya da "talep kapandı ama
    /// bağış kaydedilmedi" gibi tutarsız bir durum oluşmaz.
    /// </summary>
    public async Task<Bagis> BagisKaydetVeStokArtirAsync(Bagis bagis, int? kapatilacakTalepId = null)
    {
        await using var transaction = await Context.Database.BeginTransactionAsync();

        var kanGrubuId = await Context.Bagiscilar
            .Where(b => b.Id == bagis.BagisciId)
            .Select(b => (int?)b.KanGrubuId)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException($"Bağışçı bulunamadı: {bagis.BagisciId}");

        Context.Bagislar.Add(bagis);
        await Context.SaveChangesAsync();

        // Stoğa bağışçının kendi kan grubu eklenir (talep edilen grup değil: bağışlanan kan bağışçınınkidir).
        var guncellenen = await Context.KanStoklari
            .Where(s => s.KanGrubuId == kanGrubuId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.UniteSayisi, x => x.UniteSayisi + bagis.UniteSayisi)
                .SetProperty(x => x.GuncellemeTarihi, bagis.BagisTarihi));

        if (guncellenen == 0)
            throw new InvalidOperationException($"Kan grubu için stok kaydı yok: {kanGrubuId}");

        if (kapatilacakTalepId is { } talepId)
        {
            await Context.KanTalepleri
                .Where(t => t.Id == talepId && t.Durum == TalepDurumu.Acik)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.Durum, TalepDurumu.Karsilandi)
                    .SetProperty(t => t.KapanisTarihi, (DateTime?)bagis.BagisTarihi));
        }

        await transaction.CommitAsync();
        return bagis;
    }

    public Task<bool> TalepIcinBagisVarMiAsync(int bagisciId, int talepId) =>
        Tablo.AnyAsync(b => b.BagisciId == bagisciId && b.KanTalebiId == talepId);

    public Task<int> TalebinBagisSayisiAsync(int talepId) =>
        Tablo.CountAsync(b => b.KanTalebiId == talepId);
}
