using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;

namespace KanBagis.DataAccess.Repositories;

public interface IKanStokRepository : IRepository<KanStok>
{
    Task<List<KanStok>> KanGrubuylaTumuAsync();
}

public class KanStokRepository(KanBagisDbContext context) : EfRepository<KanStok>(context), IKanStokRepository
{
    public Task<List<KanStok>> KanGrubuylaTumuAsync() =>
        Tablo.AsNoTracking()
             .Include(s => s.KanGrubu)
             .OrderBy(s => s.KanGrubuId)
             .ToListAsync();
}
