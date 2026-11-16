using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;

namespace KanBagis.DataAccess.Repositories;

/// <summary>Bir kan grubunun bir haftadaki toplam talep edilen ünitesi.</summary>
public record HaftalikTalep(int KanGrubuId, DateTime HaftaBaslangici, int Unite);

public interface ITahminRepository : IRepository<TahminKaydi>
{
    /// <summary>[baslangic, bitis) aralığındaki talepler (iptaller hariç), kan grubu ve hafta bazında toplanmış.</summary>
    Task<List<HaftalikTalep>> HaftalikTalepleriGetirAsync(DateTime baslangic, DateTime bitis);

    Task<TahminKaydi?> SonKaydiGetirAsync();
    Task<List<TahminKaydi>> SonKayitlariGetirAsync(int adet);
}

public class TahminRepository(KanBagisDbContext context) : EfRepository<TahminKaydi>(context), ITahminRepository
{
    public async Task<List<HaftalikTalep>> HaftalikTalepleriGetirAsync(DateTime baslangic, DateTime bitis)
    {
        // Yalnızca gereken üç alan çekilir; hafta gruplaması bellekte yapılır (SQLite'ta tarih fonksiyonları sınırlı).
        var satirlar = await Context.KanTalepleri.AsNoTracking()
            .Where(t => t.OlusturmaTarihi >= baslangic && t.OlusturmaTarihi < bitis && t.Durum != TalepDurumu.Iptal)
            .Select(t => new { t.KanGrubuId, t.OlusturmaTarihi, t.UniteSayisi })
            .ToListAsync();

        return satirlar
            .GroupBy(s => (s.KanGrubuId, Hafta: baslangic.AddDays(7 * (int)((s.OlusturmaTarihi - baslangic).TotalDays / 7))))
            .Select(g => new HaftalikTalep(g.Key.KanGrubuId, g.Key.Hafta, g.Sum(s => s.UniteSayisi)))
            .ToList();
    }

    public Task<TahminKaydi?> SonKaydiGetirAsync() =>
        Tablo.AsNoTracking().OrderByDescending(k => k.Tarih).FirstOrDefaultAsync();

    public Task<List<TahminKaydi>> SonKayitlariGetirAsync(int adet) =>
        Tablo.AsNoTracking().OrderByDescending(k => k.Tarih).Take(adet).ToListAsync();
}
