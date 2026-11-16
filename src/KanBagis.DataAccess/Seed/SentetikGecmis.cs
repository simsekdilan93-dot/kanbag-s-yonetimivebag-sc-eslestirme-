using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;

namespace KanBagis.DataAccess.Seed;

/// <summary>
/// Talep/stok tahmini demosu için SON 12 HAFTAYA ait SENTETİK talep geçmişi üretir (Hafta 10).
/// Gerçek veri değildir: açıklamaları "[Sentetik geçmiş]" ile işaretlenir, bağışçı ya da kişisel veri içermez.
/// Aynı tohumla (seed) üretildiği için her çalıştırmada aynı desen çıkar (tekrarlanabilir demo).
/// Desen bilerek çeşitlidir: sabit, artan, azalan ve çok seyrek (az veri) kan grupları vardır.
/// </summary>
public static class SentetikGecmis
{
    public const string Etiket = "[Sentetik geçmiş]";

    // Kan grubu Id → (başlangıç haftalık ünite, haftalık değişim). 12 hafta boyunca uygulanır.
    private static readonly Dictionary<int, (double Baslangic, double Egilim)> Desen = new()
    {
        [1] = (5.0, 0.0),    // A Rh+  : sabit
        [2] = (1.5, 0.05),   // A Rh-  : hafif artış
        [3] = (4.0, -0.2),   // B Rh+  : azalış
        [4] = (1.0, 0.0),    // B Rh-  : düşük, sabit
        [5] = (1.5, 0.1),    // AB Rh+ : hafif artış
        [6] = (0.2, 0.0),    // AB Rh- : çok seyrek → "veri yetersiz" örneği
        [7] = (4.0, 0.35),   // 0 Rh+  : belirgin artış
        [8] = (2.0, 0.3),    // 0 Rh-  : artış + düşük stok → kritik örneği
    };

    /// <summary>Sentetik geçmiş yoksa üretir. Üretilen talep sayısını döner (zaten varsa 0).</summary>
    public static async Task<int> GerekirseUretAsync(KanBagisDbContext db, DateTime simdi)
    {
        var baslangic = simdi.Date.AddDays(-7 * 12);
        if (await db.KanTalepleri.AnyAsync(t => t.Aciklama == Etiket && t.OlusturmaTarihi >= baslangic))
            return 0;

        var rastgele = new Random(2026);
        var hastaneIdleri = await db.Hastaneler.OrderBy(h => h.Id).Select(h => h.Id).Take(3).ToListAsync();
        if (hastaneIdleri.Count == 0) return 0;

        var talepler = new List<KanTalebi>();
        for (var hafta = 0; hafta < 12; hafta++)
        {
            foreach (var (grupId, (bas, egilim)) in Desen)
            {
                var beklenen = Math.Max(0, bas + egilim * hafta);
                var unite = (int)Math.Round(beklenen + (rastgele.NextDouble() - 0.5) * Math.Max(1, beklenen * 0.6));
                while (unite > 0)
                {
                    var parca = Math.Min(unite, rastgele.Next(1, 4));
                    var tarih = baslangic.AddDays(7 * hafta + rastgele.Next(0, 7)).AddHours(rastgele.Next(8, 18));
                    talepler.Add(new KanTalebi
                    {
                        HastaneId = hastaneIdleri[rastgele.Next(hastaneIdleri.Count)],
                        KanGrubuId = grupId,
                        UniteSayisi = parca,
                        Aciliyet = rastgele.NextDouble() < 0.2 ? Aciliyet.Acil : Aciliyet.Normal,
                        Durum = TalepDurumu.Karsilandi,
                        Aciklama = Etiket,
                        OlusturmaTarihi = tarih,
                        KapanisTarihi = tarih.AddDays(1),
                    });
                    unite -= parca;
                }
            }
        }

        db.KanTalepleri.AddRange(talepler);
        await db.SaveChangesAsync();
        return talepler.Count;
    }
}
