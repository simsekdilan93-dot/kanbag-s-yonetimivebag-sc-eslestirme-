using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;

namespace KanBagis.DataAccess.Repositories;

public interface IKanTalebiRepository : IRepository<KanTalebi>
{
    Task<List<KanTalebi>> HastaneninTalepleriAsync(int hastaneId);
}

public class KanTalebiRepository(KanBagisDbContext context) : EfRepository<KanTalebi>(context), IKanTalebiRepository
{
    public Task<List<KanTalebi>> HastaneninTalepleriAsync(int hastaneId) =>
        Tablo.AsNoTracking()
             .Include(t => t.KanGrubu)
             .Where(t => t.HastaneId == hastaneId)
             .OrderByDescending(t => t.OlusturmaTarihi)
             .ToListAsync();
}
