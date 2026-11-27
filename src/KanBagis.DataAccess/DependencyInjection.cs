using KanBagis.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace KanBagis.DataAccess;

public static class DependencyInjection
{
    /// <summary>Veri katmanının servislerini (DbContext + repository'ler) kaydeder.</summary>
    public static IServiceCollection AddDataAccess(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<KanBagisDbContext>(options => options
            .UseSqlite(connectionString)
            // Temiz kurulumda (Hafta 14) ilk açılışta her migration için uyarı basılıyordu. İkisi de SQLite'ın
            // bilinen davranışı: ALTER TABLE desteği sınırlı olduğu için EF tabloyu yeniden kurar ve bunu yaparken
            // "PRAGMA foreign_keys = 0" komutunu transaction DIŞINDA çalıştırır. Risk yalnızca migration tam o anda
            // yarıda kesilirse vardır; bu yüzden dağıtımda migration öncesi yedek alınır (docs/Dagitim.md).
            .ConfigureWarnings(w => w.Ignore(
                RelationalEventId.NonTransactionalMigrationOperationWarning,
                Microsoft.EntityFrameworkCore.Diagnostics.SqliteEventId.TableRebuildPendingWarning)));

        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IBagisciRepository, BagisciRepository>();
        services.AddScoped<IBagisRepository, BagisRepository>();
        services.AddScoped<IKullaniciRepository, KullaniciRepository>();
        services.AddScoped<IHastaneRepository, HastaneRepository>();
        services.AddScoped<IKanTalebiRepository, KanTalebiRepository>();
        services.AddScoped<IKanStokRepository, KanStokRepository>();
        services.AddScoped<IBildirimRepository, BildirimRepository>();
        services.AddScoped<ITahminRepository, TahminRepository>();
        services.AddScoped<IIstatistikRepository, IstatistikRepository>();

        return services;
    }
}
