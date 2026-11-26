using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;

namespace KanBagis.DataAccess.Repositories;

public enum StokIslemDurumu
{
    Basarili,
    StokYetersiz,    // çıkış stoğu eksiye düşürecekti
    StokKaydiYok,    // kan grubu için stok satırı yok
}

/// <summary>Uyarı tarafında ne oldu: ekranda "uyarı açıldı / kapandı" mesajı için.</summary>
public enum UyariDegisimi
{
    Yok,
    Acildi,
    SeviyeDegisti,
    Kapandi,
}

public sealed record StokIslemSonucu(StokIslemDurumu Durum, StokHareketi? Hareket = null,
    StokDurumu YeniDurum = StokDurumu.Normal, UyariDegisimi Uyari = UyariDegisimi.Yok, int MevcutStok = 0);

public sealed record HareketFiltresi(int? KanGrubuId = null, int? HastaneId = null, HareketTuru? Tur = null, int Adet = 100);

public interface IKanStokRepository : IRepository<KanStok>
{
    Task<List<KanStok>> KanGrubuylaTumuAsync();
    Task<KanStok?> KanGrubuIleGetirAsync(int kanGrubuId);

    /// <summary>
    /// Stoğu hareket kadar değiştirir, hareketi ve uyarı değişikliğini kaydeder. Hepsi TEK transaction'dır.
    /// Çıkışta stok yetmiyorsa hiçbir şey yazılmaz. Dışarıda açık bir transaction varsa (bağış kaydı) ona katılır.
    /// hareket.OncekiStok / SonrakiStok bu metotta doldurulur.
    /// </summary>
    Task<StokIslemSonucu> HareketUygulaAsync(StokHareketi hareket);

    /// <summary>Eşikleri değiştirir ve uyarıyı yeni eşiğe göre yeniden değerlendirir.</summary>
    Task<StokIslemSonucu> EsikGuncelleAsync(int kanGrubuId, int kritikEsik, int uyariEsigi, DateTime tarih);

    Task<List<StokHareketi>> HareketleriGetirAsync(HareketFiltresi filtre);
    Task<List<StokUyarisi>> AcikUyarilarAsync();
    Task<int> AcikUyariSayisiAsync();
    Task<List<StokUyarisi>> UyariGecmisiAsync(int adet);
}

public class KanStokRepository(KanBagisDbContext context) : EfRepository<KanStok>(context), IKanStokRepository
{
    public Task<List<KanStok>> KanGrubuylaTumuAsync() =>
        Tablo.AsNoTracking()
             .Include(s => s.KanGrubu)
             .OrderBy(s => s.KanGrubuId)
             .ToListAsync();

    public Task<KanStok?> KanGrubuIleGetirAsync(int kanGrubuId) =>
        Tablo.AsNoTracking().Include(s => s.KanGrubu).FirstOrDefaultAsync(s => s.KanGrubuId == kanGrubuId);

    public async Task<StokIslemSonucu> HareketUygulaAsync(StokHareketi hareket)
    {
        var fark = hareket.Tur == HareketTuru.Giris ? hareket.Miktar : -hareket.Miktar;

        // Dışarıda transaction yoksa (elle giriş/çıkış) burada açılır; varsa (bağış kaydı) ona katılır.
        await using var kendiIslemim = Context.Database.CurrentTransaction is null
            ? await Context.Database.BeginTransactionAsync()
            : null;

        // ATOMİK güncelleme: "UPDATE ... SET UniteSayisi = UniteSayisi + @fark WHERE ... AND UniteSayisi + @fark >= 0".
        // Önce okuyup sonra yazsaydık, aynı anda yapılan iki çıkış aynı eski değeri görüp stoğu eksiye düşürebilirdi.
        var guncellenen = await Tablo
            .Where(s => s.KanGrubuId == hareket.KanGrubuId && s.UniteSayisi + fark >= 0)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.UniteSayisi, x => x.UniteSayisi + fark)
                .SetProperty(x => x.GuncellemeTarihi, hareket.Tarih));

        var stok = await Tablo.AsNoTracking().FirstOrDefaultAsync(s => s.KanGrubuId == hareket.KanGrubuId);
        if (stok is null) return new StokIslemSonucu(StokIslemDurumu.StokKaydiYok);
        if (guncellenen == 0) return new StokIslemSonucu(StokIslemDurumu.StokYetersiz, MevcutStok: stok.UniteSayisi);

        hareket.SonrakiStok = stok.UniteSayisi;
        hareket.OncekiStok = stok.UniteSayisi - fark;
        Context.StokHareketleri.Add(hareket);

        var uyari = await UyariyiGuncelle(stok, hareket.Tarih);
        await Context.SaveChangesAsync();
        if (kendiIslemim is not null) await kendiIslemim.CommitAsync();

        return new StokIslemSonucu(StokIslemDurumu.Basarili, hareket, stok.Durum, uyari, stok.UniteSayisi);
    }

    public async Task<StokIslemSonucu> EsikGuncelleAsync(int kanGrubuId, int kritikEsik, int uyariEsigi, DateTime tarih)
    {
        await using var islem = await Context.Database.BeginTransactionAsync();

        var guncellenen = await Tablo.Where(s => s.KanGrubuId == kanGrubuId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.KritikEsik, kritikEsik)
                .SetProperty(x => x.UyariEsigi, uyariEsigi));
        if (guncellenen == 0) return new StokIslemSonucu(StokIslemDurumu.StokKaydiYok);

        var stok = await Tablo.AsNoTracking().FirstAsync(s => s.KanGrubuId == kanGrubuId);
        var uyari = await UyariyiGuncelle(stok, tarih);
        await Context.SaveChangesAsync();
        await islem.CommitAsync();
        return new StokIslemSonucu(StokIslemDurumu.Basarili, null, stok.Durum, uyari, stok.UniteSayisi);
    }

    /// <summary>
    /// Uyarı kuralı: stok normal → açık uyarı kapanır; düşük/kritik → açık uyarı yoksa açılır,
    /// varsa seviyesi ve stok değeri güncellenir. Böylece bir kan grubunun en fazla bir açık uyarısı olur.
    /// </summary>
    private async Task<UyariDegisimi> UyariyiGuncelle(KanStok stok, DateTime tarih)
    {
        var acik = await Context.StokUyarilari.FirstOrDefaultAsync(u => u.KanGrubuId == stok.KanGrubuId && u.KapanisTarihi == null);
        var durum = stok.Durum;

        if (durum == StokDurumu.Normal)
        {
            if (acik is null) return UyariDegisimi.Yok;
            acik.KapanisTarihi = tarih;
            acik.GuncellemeTarihi = tarih;
            acik.StokDegeri = stok.UniteSayisi;
            return UyariDegisimi.Kapandi;
        }

        var esik = durum == StokDurumu.Kritik ? stok.KritikEsik : stok.UyariEsigi;
        if (acik is null)
        {
            Context.StokUyarilari.Add(new StokUyarisi
            {
                KanGrubuId = stok.KanGrubuId, Seviye = durum, StokDegeri = stok.UniteSayisi, Esik = esik,
                AcilisTarihi = tarih, GuncellemeTarihi = tarih,
            });
            return UyariDegisimi.Acildi;
        }

        var seviyeDegisti = acik.Seviye != durum;
        acik.Seviye = durum;
        acik.Esik = esik;
        acik.StokDegeri = stok.UniteSayisi;
        acik.GuncellemeTarihi = tarih;
        return seviyeDegisti ? UyariDegisimi.SeviyeDegisti : UyariDegisimi.Yok;
    }

    public Task<List<StokHareketi>> HareketleriGetirAsync(HareketFiltresi f)
    {
        var sorgu = Context.StokHareketleri.AsNoTracking()
            .Include(h => h.KanGrubu).Include(h => h.Hastane).Include(h => h.Kullanici)
            .AsQueryable();
        if (f.KanGrubuId is { } g) sorgu = sorgu.Where(h => h.KanGrubuId == g);
        if (f.HastaneId is { } hs) sorgu = sorgu.Where(h => h.HastaneId == hs);
        if (f.Tur is { } t) sorgu = sorgu.Where(h => h.Tur == t);
        return sorgu.OrderByDescending(h => h.Tarih).ThenByDescending(h => h.Id).Take(f.Adet).ToListAsync();
    }

    public Task<List<StokUyarisi>> AcikUyarilarAsync() =>
        Context.StokUyarilari.AsNoTracking().Include(u => u.KanGrubu)
            .Where(u => u.KapanisTarihi == null)
            .OrderByDescending(u => u.Seviye == StokDurumu.Kritik).ThenBy(u => u.KanGrubuId)
            .ToListAsync();

    public Task<int> AcikUyariSayisiAsync() => Context.StokUyarilari.CountAsync(u => u.KapanisTarihi == null);

    public Task<List<StokUyarisi>> UyariGecmisiAsync(int adet) =>
        Context.StokUyarilari.AsNoTracking().Include(u => u.KanGrubu)
            .OrderByDescending(u => u.GuncellemeTarihi).ThenByDescending(u => u.Id).Take(adet)
            .ToListAsync();
}
