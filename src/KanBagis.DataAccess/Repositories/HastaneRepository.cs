using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;

namespace KanBagis.DataAccess.Repositories;

public interface IHastaneRepository : IRepository<Hastane>
{
    Task<bool> AdVeSehirIleVarMiAsync(string ad, string sehir);
}

public class HastaneRepository(KanBagisDbContext context) : EfRepository<Hastane>(context), IHastaneRepository
{
    public Task<bool> AdVeSehirIleVarMiAsync(string ad, string sehir) =>
        Tablo.AnyAsync(h => h.Ad == ad && h.Sehir == sehir);
}
