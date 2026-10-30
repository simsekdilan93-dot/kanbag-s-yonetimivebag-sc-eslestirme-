using KanBagis.Business.Bagiscilar;
using KanBagis.Business.Guvenlik;
using KanBagis.Business.Kimlik;
using KanBagis.Business.Paneller;
using KanBagis.Business.Tanimlar;
using KanBagis.DataAccess;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IParolaHasher>(new Pbkdf2ParolaHasher());
        services.AddScoped<IKimlikService, KimlikService>();
        services.AddSingleton<BagisUygunlukKurali>();
        services.AddScoped<IBagisciService, BagisciService>();
        services.AddScoped<IPanelService, PanelService>();
        services.AddScoped<ITanimService, TanimService>();

        return services;
    }

    public static Task VeritabaniniHazirlaAsync(IServiceProvider services) =>
        VeritabaniKurulum.MigrationlariUygulaAsync(services);
}
