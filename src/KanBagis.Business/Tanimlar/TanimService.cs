using KanBagis.DataAccess.Repositories;
using KanBagis.Entities;

namespace KanBagis.Business.Tanimlar;

/// <summary>Formlardaki seçim listeleri (kan grupları vb.) için sabit tanım verileri.</summary>
public interface ITanimService
{
    Task<List<KanGrubu>> KanGruplariAsync();
}

public sealed class TanimService(IRepository<KanGrubu> kanGruplari) : ITanimService
{
    public async Task<List<KanGrubu>> KanGruplariAsync() =>
        (await kanGruplari.TumunuGetirAsync()).OrderBy(k => k.Id).ToList();
}
