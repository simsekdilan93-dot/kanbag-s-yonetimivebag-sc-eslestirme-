using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;

namespace KanBagis.DataAccess.Repositories;

public interface IBagisRepository : IRepository<Bagis>
{
    Task<Bagis> BagisKaydetVeStokArtirAsync(Bagis bagis);
}

public class BagisRepository(KanBagisDbContext context) : EfRepository<Bagis>(context), IBagisRepository
{
    /// <summary>
    /// Bağışı kaydeder ve bağışçının kan grubunun stoğunu artırır.
    /// İki işlem tek bir TRANSACTION içinde yapılır: biri başarısız olursa
    /// ikisi de geri alınır (ROLLBACK). Böylece "bağış kaydedildi ama stok artmadı"
    /// gibi tutarsız bir durum oluşmaz.
    /// </summary>
    public async Task<Bagis> BagisKaydetVeStokArtirAsync(Bagis bagis)
    {
        await using var transaction = await Context.Database.BeginTransactionAsync();

        var kanGrubuId = await Context.Bagiscilar
            .Where(b => b.Id == bagis.BagisciId)
            .Select(b => (int?)b.KanGrubuId)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException($"Bağışçı bulunamadı: {bagis.BagisciId}");

        Context.Bagislar.Add(bagis);
        await Context.SaveChangesAsync();

        var guncellenen = await Context.KanStoklari
            .Where(s => s.KanGrubuId == kanGrubuId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.UniteSayisi, x => x.UniteSayisi + bagis.UniteSayisi)
                .SetProperty(x => x.GuncellemeTarihi, DateTime.UtcNow));

        if (guncellenen == 0)
            throw new InvalidOperationException($"Kan grubu için stok kaydı yok: {kanGrubuId}");

        await transaction.CommitAsync();
        return bagis;
    }
}
