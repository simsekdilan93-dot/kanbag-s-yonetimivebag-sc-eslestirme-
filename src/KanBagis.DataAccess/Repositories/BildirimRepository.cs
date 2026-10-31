using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;

namespace KanBagis.DataAccess.Repositories;

public interface IBildirimRepository : IRepository<Bildirim>
{
    Task<HashSet<int>> BildirilmisBagisciIdleriAsync(int talepId);
    Task TopluEkleAsync(IEnumerable<Bildirim> bildirimler);

    /// <summary>
    /// Bildirimi YALNIZCA sahibi olan bağışçı için getirir (talebiyle birlikte, değişiklik takibi açık).
    /// Başkasının bildirimiyse null döner; böylece bildirimin var olduğu bile anlaşılmaz.
    /// </summary>
    Task<Bildirim?> BagiscininBildiriminiGetirAsync(int bildirimId, int bagisciId);

    /// <summary>Bağışçının, hâlâ açık olan başka bir talepte "Kabul" yanıtı var mı?</summary>
    Task<bool> AcikTalepteKabulVarMiAsync(int bagisciId, int haricBildirimId);

    Task OkunduIsaretleAsync(int bagisciId);
}

public class BildirimRepository(KanBagisDbContext context) : EfRepository<Bildirim>(context), IBildirimRepository
{
    public async Task<HashSet<int>> BildirilmisBagisciIdleriAsync(int talepId) =>
        (await Tablo.Where(b => b.KanTalebiId == talepId).Select(b => b.BagisciId).ToListAsync()).ToHashSet();

    public async Task TopluEkleAsync(IEnumerable<Bildirim> bildirimler)
    {
        Tablo.AddRange(bildirimler);
        await Context.SaveChangesAsync();
    }

    public Task<Bildirim?> BagiscininBildiriminiGetirAsync(int bildirimId, int bagisciId) =>
        Tablo.Include(b => b.KanTalebi)
             .FirstOrDefaultAsync(b => b.Id == bildirimId && b.BagisciId == bagisciId);

    public Task<bool> AcikTalepteKabulVarMiAsync(int bagisciId, int haricBildirimId) =>
        Tablo.AnyAsync(b => b.BagisciId == bagisciId
                         && b.Id != haricBildirimId
                         && b.Yanit == BildirimYaniti.Kabul
                         && b.KanTalebi.Durum == TalepDurumu.Acik);

    public Task OkunduIsaretleAsync(int bagisciId) =>
        Tablo.Where(b => b.BagisciId == bagisciId && !b.Okundu)
             .ExecuteUpdateAsync(s => s.SetProperty(b => b.Okundu, true));
}
