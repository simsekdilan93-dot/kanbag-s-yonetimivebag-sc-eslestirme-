using KanBagis.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KanBagis.DataAccess;

public static class DependencyInjection
{
    /// <summary>Veri katmanının servislerini (DbContext + repository'ler) kaydeder.</summary>
    public static IServiceCollection AddDataAccess(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<KanBagisDbContext>(options => options.UseSqlite(connectionString));

        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IBagisciRepository, BagisciRepository>();
        services.AddScoped<IBagisRepository, BagisRepository>();
        services.AddScoped<IKullaniciRepository, KullaniciRepository>();
        services.AddScoped<IHastaneRepository, HastaneRepository>();
        services.AddScoped<IKanTalebiRepository, KanTalebiRepository>();
        services.AddScoped<IKanStokRepository, KanStokRepository>();

        return services;
    }
}
