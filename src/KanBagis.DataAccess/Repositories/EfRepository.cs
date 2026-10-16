using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;

namespace KanBagis.DataAccess.Repositories;

public class EfRepository<T>(KanBagisDbContext context) : IRepository<T> where T : class, IEntity
{
    protected readonly KanBagisDbContext Context = context;
    protected DbSet<T> Tablo => Context.Set<T>();

    // SELECT * FROM Tablo
    public Task<List<T>> TumunuGetirAsync() =>
        Tablo.AsNoTracking().ToListAsync();

    // SELECT * FROM Tablo WHERE Id = @id
    public Task<T?> IdIleGetirAsync(int id) =>
        Tablo.FirstOrDefaultAsync(x => x.Id == id);

    // INSERT INTO Tablo (...) VALUES (...)
    public async Task<T> EkleAsync(T varlik)
    {
        Tablo.Add(varlik);
        await Context.SaveChangesAsync();
        return varlik;
    }

    // UPDATE Tablo SET ... WHERE Id = @id
    public async Task GuncelleAsync(T varlik)
    {
        Tablo.Update(varlik);
        await Context.SaveChangesAsync();
    }

    // DELETE FROM Tablo WHERE Id = @id
    public async Task<bool> SilAsync(int id)
    {
        var silinen = await Tablo.Where(x => x.Id == id).ExecuteDeleteAsync();
        return silinen > 0;
    }
}
