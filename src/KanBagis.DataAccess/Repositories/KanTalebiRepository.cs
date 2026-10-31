using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;

namespace KanBagis.DataAccess.Repositories;

/// <summary>Talep listesinde her talep için bildirim sayıları (tek sorguda hesaplanır).</summary>
public record TalepOzeti(KanTalebi Talep, int BildirimSayisi, int KabulSayisi);

public interface IKanTalebiRepository : IRepository<KanTalebi>
{
    Task<List<TalepOzeti>> HastaneninTalepleriAsync(int hastaneId);
    Task<KanTalebi?> AcikTalebiGetirAsync(int hastaneId, int kanGrubuId);

    /// <summary>Talep + hastane + kan grubu + bildirimler (bağışçılarıyla). Salt okunur.</summary>
    Task<KanTalebi?> DetayGetirAsync(int talepId);
}

public class KanTalebiRepository(KanBagisDbContext context) : EfRepository<KanTalebi>(context), IKanTalebiRepository
{
    public async Task<List<TalepOzeti>> HastaneninTalepleriAsync(int hastaneId)
    {
        var satirlar = await Tablo.AsNoTracking()
            .Where(t => t.HastaneId == hastaneId)
            .OrderByDescending(t => t.OlusturmaTarihi)
            .Select(t => new
            {
                Talep = t,
                t.KanGrubu,   // projeksiyonda Include yok sayıldığı için kan grubu açıkça seçilir
                BildirimSayisi = t.Bildirimler.Count,
                KabulSayisi = t.Bildirimler.Count(b => b.Yanit == BildirimYaniti.Kabul),
            })
            .ToListAsync();

        foreach (var s in satirlar) s.Talep.KanGrubu = s.KanGrubu;
        return satirlar.Select(s => new TalepOzeti(s.Talep, s.BildirimSayisi, s.KabulSayisi)).ToList();
    }

    public Task<KanTalebi?> AcikTalebiGetirAsync(int hastaneId, int kanGrubuId) =>
        Tablo.AsNoTracking()
             .FirstOrDefaultAsync(t => t.HastaneId == hastaneId && t.KanGrubuId == kanGrubuId && t.Durum == TalepDurumu.Acik);

    public Task<KanTalebi?> DetayGetirAsync(int talepId) =>
        Tablo.AsNoTracking()
             .Include(t => t.Hastane)
             .Include(t => t.KanGrubu)
             .Include(t => t.Bildirimler).ThenInclude(b => b.Bagisci).ThenInclude(b => b.KanGrubu)
             .AsSplitQuery()
             .FirstOrDefaultAsync(t => t.Id == talepId);
}
