using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;

namespace KanBagis.DataAccess.Repositories;

public interface IBagisciRepository : IRepository<Bagisci>
{
    Task<Bagisci?> TcIleGetirAsync(string tcKimlikNo);
    Task<List<Bagisci>> UygunBagiscilariGetirAsync(int kanGrubuId, string sehir, DateTime sonBagisSiniri);
    Task<DateTime?> SonBagisTarihiAsync(int bagisciId);
    Task<Bagisci?> KanGrubuylaGetirAsync(int id);
    Task<List<Bildirim>> BildirimleriGetirAsync(int bagisciId);
}

public class BagisciRepository(KanBagisDbContext context) : EfRepository<Bagisci>(context), IBagisciRepository
{
    public Task<Bagisci?> TcIleGetirAsync(string tcKimlikNo) =>
        Tablo.Include(b => b.KanGrubu)
             .FirstOrDefaultAsync(b => b.TcKimlikNo == tcKimlikNo);

    /// <summary>
    /// Eşleştirme sorgusu: aynı kan grubundan, aynı şehirde olan ve
    /// <paramref name="sonBagisSiniri"/> tarihinden sonra bağış yapmamış bağışçılar.
    /// (KanGrubuId, Sehir) bileşik indeksini kullanır.
    /// </summary>
    public Task<List<Bagisci>> UygunBagiscilariGetirAsync(int kanGrubuId, string sehir, DateTime sonBagisSiniri) =>
        Tablo.AsNoTracking()
             .Where(b => b.KanGrubuId == kanGrubuId && b.Sehir == sehir)
             .Where(b => !b.Bagislar.Any(x => x.BagisTarihi > sonBagisSiniri))
             .OrderBy(b => b.Ad)
             .ToListAsync();

    public Task<DateTime?> SonBagisTarihiAsync(int bagisciId) =>
        Context.Bagislar
               .Where(b => b.BagisciId == bagisciId)
               .MaxAsync(b => (DateTime?)b.BagisTarihi);

    public Task<Bagisci?> KanGrubuylaGetirAsync(int id) =>
        Tablo.AsNoTracking()
             .Include(b => b.KanGrubu)
             .FirstOrDefaultAsync(b => b.Id == id);

    public Task<List<Bildirim>> BildirimleriGetirAsync(int bagisciId) =>
        Context.Bildirimler.AsNoTracking()
               .Include(b => b.KanTalebi).ThenInclude(t => t.Hastane)
               .Include(b => b.KanTalebi).ThenInclude(t => t.KanGrubu)
               .Where(b => b.BagisciId == bagisciId)
               .OrderByDescending(b => b.GonderimTarihi)
               .ToListAsync();
}
