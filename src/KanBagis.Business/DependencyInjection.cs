using KanBagis.DataAccess;
using Microsoft.Extensions.DependencyInjection;

namespace KanBagis.Business;

public static class DependencyInjection
{
    /// <summary>
    /// İş katmanını ve onun kullandığı veri katmanını kaydeder.
    /// Web katmanı yalnızca bu metodu çağırır, veri katmanını doğrudan tanımaz.
    /// </summary>
    public static IServiceCollection AddBusiness(this IServiceCollection services, string connectionString)
    {
        services.AddDataAccess(connectionString);
        // İş servisleri (BagisciService vb.) sonraki haftalarda buraya eklenecek.
        return services;
    }

    public static Task VeritabaniniHazirlaAsync(IServiceProvider services) =>
        VeritabaniKurulum.MigrationlariUygulaAsync(services);
}
