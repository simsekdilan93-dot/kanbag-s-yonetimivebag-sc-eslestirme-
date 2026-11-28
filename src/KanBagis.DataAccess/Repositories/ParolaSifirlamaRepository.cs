using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;

namespace KanBagis.DataAccess.Repositories;

public interface IParolaSifirlamaRepository : IRepository<ParolaSifirlamaKodu>
{
    /// <summary>Kullanıcının açık (kapanmamış) kodu; izlenir, çünkü deneme sayısı güncellenir.</summary>
    Task<ParolaSifirlamaKodu?> AcikKoduGetirAsync(int kullaniciId);

    /// <summary>Kullanıcının en son istediği kodun zamanı (sık istek sınırı için).</summary>
    Task<DateTime?> SonIstekTarihiAsync(int kullaniciId);

    /// <summary>Eski açık kodu kapatıp yenisini ekler (tek SaveChanges, tek transaction).</summary>
    Task YeniKodEkleAsync(ParolaSifirlamaKodu yeni, DateTime simdi);

    /// <summary>Kodu kapatır ve kullanıcının parolasını değiştirir; ikisi birlikte kaydedilir.</summary>
    Task KoduKullanVeParolayiDegistirAsync(ParolaSifirlamaKodu kod, string yeniParolaHash, DateTime simdi);

    Task KaydetAsync();
}

public class ParolaSifirlamaRepository(KanBagisDbContext context) : EfRepository<ParolaSifirlamaKodu>(context), IParolaSifirlamaRepository
{
    public Task<ParolaSifirlamaKodu?> AcikKoduGetirAsync(int kullaniciId) =>
        Tablo.FirstOrDefaultAsync(k => k.KullaniciId == kullaniciId && k.KapanisTarihi == null);

    public async Task<DateTime?> SonIstekTarihiAsync(int kullaniciId) =>
        await Tablo.Where(k => k.KullaniciId == kullaniciId)
                   .OrderByDescending(k => k.OlusturmaTarihi)
                   .Select(k => (DateTime?)k.OlusturmaTarihi)
                   .FirstOrDefaultAsync();

    public async Task YeniKodEkleAsync(ParolaSifirlamaKodu yeni, DateTime simdi)
    {
        await using var islem = await Context.Database.BeginTransactionAsync();
        // Önce eski açık kod kapatılır ve kaydedilir; aksi hâlde "kullanıcı başına tek açık kod" indeksi ihlal edilir.
        await Tablo.Where(k => k.KullaniciId == yeni.KullaniciId && k.KapanisTarihi == null)
                   .ExecuteUpdateAsync(s => s.SetProperty(k => k.KapanisTarihi, (DateTime?)simdi));
        Tablo.Add(yeni);
        await Context.SaveChangesAsync();
        await islem.CommitAsync();
    }

    public async Task KoduKullanVeParolayiDegistirAsync(ParolaSifirlamaKodu kod, string yeniParolaHash, DateTime simdi)
    {
        var kullanici = await Context.Kullanicilar.FirstAsync(k => k.Id == kod.KullaniciId);
        kullanici.ParolaHash = yeniParolaHash;
        // Parolasını yenileyen kişi hesabın sahibi olduğunu kanıtladı: giriş kilidi de açılır.
        kullanici.BasarisizGirisSayisi = 0;
        kullanici.KilitBitisTarihi = null;
        kod.KapanisTarihi = simdi;
        await Context.SaveChangesAsync();
    }

    public Task KaydetAsync() => Context.SaveChangesAsync();
}
