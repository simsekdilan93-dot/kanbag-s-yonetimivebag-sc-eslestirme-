using KanBagis.DataAccess.Seed;
using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;

namespace KanBagis.DataAccess.Repositories;

/// <summary>Pano sorgularının ortak süzgeci. Tarih aralığı [Baslangic, Bitis).</summary>
public sealed record IstatistikSuzgeci(DateTime Baslangic, DateTime Bitis, int? HastaneId = null, bool SentetikDahil = false);

// Toplulaştırma sonuçları (her satır bir GROUP BY grubu).
public sealed record TalepGrubu(int KanGrubuId, TalepDurumu Durum, Aciliyet Aciliyet, int TalepSayisi, int Unite);
public sealed record BagisGrubu(int KanGrubuId, bool TalebeBagli, int BagisSayisi, int Unite);
public sealed record BildirimGrubu(BildirimYaniti Yanit, DisKanalDurumu DisKanal, int Sayi);
public sealed record TarihliMiktar(DateTime Tarih, int Unite);
public sealed record KarsilanmaSuresi(DateTime Olusturma, DateTime Kapanis);

public interface IIstatistikRepository
{
    Task<List<TalepGrubu>> TalepleriTopla(IstatistikSuzgeci s);
    Task<List<BagisGrubu>> BagislariTopla(IstatistikSuzgeci s);
    Task<List<BildirimGrubu>> BildirimleriTopla(IstatistikSuzgeci s);
    Task<List<TarihliMiktar>> TalepTarihleri(IstatistikSuzgeci s);
    Task<List<TarihliMiktar>> BagisTarihleri(IstatistikSuzgeci s);
    Task<List<KarsilanmaSuresi>> KarsilanmaSureleri(IstatistikSuzgeci s);
}

/// <summary>
/// İstatistik panosunun veri kaynağı. Sayma/toplama işleri VERİTABANINDA yapılır (GROUP BY + COUNT/SUM);
/// belleğe yalnızca gruplanmış küçük sonuçlar gelir. Haftalık dağılım ve süre hesabı için yalnızca
/// gereken iki sütun çekilir (SQLite'ta tarih farkı/hafta fonksiyonları EF ile çevrilemiyor).
/// Sentetik talep geçmişi (Hafta 10 demosu) varsayılan olarak HARİÇ tutulur; gerçek istatistiği bozmasın.
/// </summary>
public class IstatistikRepository(KanBagisDbContext context) : IIstatistikRepository
{
    private IQueryable<KanTalebi> Talepler(IstatistikSuzgeci s)
    {
        var q = context.KanTalepleri.AsNoTracking()
            .Where(t => t.OlusturmaTarihi >= s.Baslangic && t.OlusturmaTarihi < s.Bitis);
        if (s.HastaneId is { } h) q = q.Where(t => t.HastaneId == h);
        if (!s.SentetikDahil) q = q.Where(t => t.Aciklama != SentetikGecmis.Etiket);
        return q;
    }

    private IQueryable<Bagis> Bagislar(IstatistikSuzgeci s)
    {
        var q = context.Bagislar.AsNoTracking()
            .Where(b => b.BagisTarihi >= s.Baslangic && b.BagisTarihi < s.Bitis);
        if (s.HastaneId is { } h) q = q.Where(b => b.HastaneId == h);
        return q;
    }

    // SELECT KanGrubuId, Durum, Aciliyet, COUNT(*), SUM(UniteSayisi) FROM KanTalepleri WHERE ... GROUP BY KanGrubuId, Durum, Aciliyet
    public Task<List<TalepGrubu>> TalepleriTopla(IstatistikSuzgeci s) =>
        Talepler(s)
            .GroupBy(t => new { t.KanGrubuId, t.Durum, t.Aciliyet })
            .Select(g => new TalepGrubu(g.Key.KanGrubuId, g.Key.Durum, g.Key.Aciliyet, g.Count(), g.Sum(t => t.UniteSayisi)))
            .ToListAsync();

    // Bağışlanan kan, bağışçının kan grubudur (talep edilen grup değil).
    public Task<List<BagisGrubu>> BagislariTopla(IstatistikSuzgeci s) =>
        Bagislar(s)
            .GroupBy(b => new { b.Bagisci.KanGrubuId, TalebeBagli = b.KanTalebiId != null })
            .Select(g => new BagisGrubu(g.Key.KanGrubuId, g.Key.TalebeBagli, g.Count(), g.Sum(b => b.UniteSayisi)))
            .ToListAsync();

    public Task<List<BildirimGrubu>> BildirimleriTopla(IstatistikSuzgeci s)
    {
        var q = context.Bildirimler.AsNoTracking()
            .Where(b => b.GonderimTarihi >= s.Baslangic && b.GonderimTarihi < s.Bitis);
        if (s.HastaneId is { } h) q = q.Where(b => b.KanTalebi.HastaneId == h);
        return q.GroupBy(b => new { b.Yanit, b.DisKanalDurumu })
            .Select(g => new BildirimGrubu(g.Key.Yanit, g.Key.DisKanalDurumu, g.Count()))
            .ToListAsync();
    }

    public Task<List<TarihliMiktar>> TalepTarihleri(IstatistikSuzgeci s) =>
        Talepler(s).Select(t => new TarihliMiktar(t.OlusturmaTarihi, t.UniteSayisi)).ToListAsync();

    public Task<List<TarihliMiktar>> BagisTarihleri(IstatistikSuzgeci s) =>
        Bagislar(s).Select(b => new TarihliMiktar(b.BagisTarihi, b.UniteSayisi)).ToListAsync();

    public Task<List<KarsilanmaSuresi>> KarsilanmaSureleri(IstatistikSuzgeci s) =>
        Talepler(s)
            .Where(t => t.Durum == TalepDurumu.Karsilandi && t.KapanisTarihi != null)
            .Select(t => new KarsilanmaSuresi(t.OlusturmaTarihi, t.KapanisTarihi!.Value))
            .ToListAsync();
}
