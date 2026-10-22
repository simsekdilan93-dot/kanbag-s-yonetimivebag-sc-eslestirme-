using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;

namespace KanBagis.DataAccess.Repositories;

public interface IKullaniciRepository : IRepository<Kullanici>
{
    /// <summary>E-posta ile hesabı getirir (değişiklik takibi açık: giriş sayacı güncellenebilsin diye).</summary>
    Task<Kullanici?> EpostaIleGetirAsync(string eposta);
    Task<bool> EpostaKayitliMiAsync(string eposta);
}

public class KullaniciRepository(KanBagisDbContext context) : EfRepository<Kullanici>(context), IKullaniciRepository
{
    public Task<Kullanici?> EpostaIleGetirAsync(string eposta) =>
        Tablo.FirstOrDefaultAsync(k => k.Eposta == eposta);

    public Task<bool> EpostaKayitliMiAsync(string eposta) =>
        Tablo.AnyAsync(k => k.Eposta == eposta);
}
