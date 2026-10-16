using KanBagis.Entities;

namespace KanBagis.DataAccess.Repositories;

/// <summary>Tüm tablolar için ortak CRUD işlemleri.</summary>
public interface IRepository<T> where T : class, IEntity
{
    Task<List<T>> TumunuGetirAsync();                  // READ (liste)
    Task<T?> IdIleGetirAsync(int id);                  // READ (tek kayıt)
    Task<T> EkleAsync(T varlik);                       // CREATE
    Task GuncelleAsync(T varlik);                      // UPDATE
    Task<bool> SilAsync(int id);                       // DELETE
}
