using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KanBagis.DataAccess;

public static class VeritabaniKurulum
{
    /// <summary>Bekleyen migration'ları veritabanına uygular (yoksa veritabanını oluşturur).</summary>
    public static async Task MigrationlariUygulaAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<KanBagisDbContext>();
        await context.Database.MigrateAsync();
    }

    /// <summary>Sağlık kontrolü: veritabanına bağlanıp basit bir sorgu çalıştırılabiliyor mu?</summary>
    public static async Task<bool> ErisilebilirMiAsync(IServiceProvider services, CancellationToken iptal = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<KanBagisDbContext>();
        return await context.Database.CanConnectAsync(iptal) && await context.KanGruplari.AnyAsync(iptal);
    }
}
