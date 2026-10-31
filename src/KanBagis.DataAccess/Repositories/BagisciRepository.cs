using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;

namespace KanBagis.DataAccess.Repositories;

/// <summary>Eşleştirme adayı: bağışçı ve son bağış tarihi (uygunluk kuralı iş katmanında uygulanır).</summary>
public record BagisciAdayi(Bagisci Bagisci, DateTime? SonBagisTarihi);

public interface IBagisciRepository : IRepository<Bagisci>
{
    Task<Bagisci?> TcIleGetirAsync(string tcKimlikNo);
    Task<List<BagisciAdayi>> AdaylariGetirAsync(IReadOnlyCollection<int> kanGrubuIdleri, IReadOnlyCollection<string> iller);
    Task<DateTime?> SonBagisTarihiAsync(int bagisciId);
    Task<int> BagisSayisiAsync(int bagisciId);
    Task<Bagisci?> KanGrubuylaGetirAsync(int id);
    Task<List<Bildirim>> BildirimleriGetirAsync(int bagisciId);
}

public class BagisciRepository(KanBagisDbContext context) : EfRepository<Bagisci>(context), IBagisciRepository
{
    public Task<Bagisci?> TcIleGetirAsync(string tcKimlikNo) =>
        Tablo.Include(b => b.KanGrubu)
             .FirstOrDefaultAsync(b => b.TcKimlikNo == tcKimlikNo);

    /// <summary>
    /// Eşleştirme adayları: verilen kan gruplarından birine sahip ve verilen illerden birinde yaşayan
    /// bağışçılar, son bağış tarihleriyle birlikte. (KanGrubuId, Sehir) bileşik indeksini kullanır.
    /// Tek sorguda gelir: son bağış tarihi alt sorgu (MAX) olarak hesaplanır.
    /// </summary>
    public Task<List<BagisciAdayi>> AdaylariGetirAsync(IReadOnlyCollection<int> kanGrubuIdleri, IReadOnlyCollection<string> iller) =>
        Tablo.AsNoTracking()
             .Where(b => kanGrubuIdleri.Contains(b.KanGrubuId) && iller.Contains(b.Sehir))
             .OrderBy(b => b.Ad)
             .Select(b => new BagisciAdayi(b, b.Bagislar.Max(x => (DateTime?)x.BagisTarihi)))
             .ToListAsync();

    public Task<DateTime?> SonBagisTarihiAsync(int bagisciId) =>
        Context.Bagislar
               .Where(b => b.BagisciId == bagisciId)
               .MaxAsync(b => (DateTime?)b.BagisTarihi);

    public Task<int> BagisSayisiAsync(int bagisciId) =>
        Context.Bagislar.CountAsync(b => b.BagisciId == bagisciId);

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
