using KanBagis.Business.Bagiscilar;
using KanBagis.Business.DisServisler;
using KanBagis.Business.Guvenlik;
using KanBagis.Business.Istatistik;
using KanBagis.Business.Kimlik;
using KanBagis.Business.Paneller;
using KanBagis.Business.Stok;
using KanBagis.Business.Tahmin;
using KanBagis.Business.Talepler;
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
        services.AddScoped<IEslestirmeService, EslestirmeService>();
        services.AddScoped<ITalepService, TalepService>();
        services.AddScoped<IBildirimService, BildirimService>();
        services.AddScoped<IPanelService, PanelService>();
        services.AddScoped<IStokService, StokService>();
        services.AddScoped<IIstatistikService, IstatistikService>();   // Hafta 12: İstatistik Panosu   // Hafta 11: Stok Takip ve Uyarı Modülü

        // ---- Harici servisler (Hafta 9). Ayarlar Web katmanında appsettings + ortam değişkenlerinden bağlanır.
        // Harita / yol mesafesi: OSRM. Önbellek ve hız sınırı durumu tek örnek (singleton).
        services.AddSingleton<MesafeOnbellegi>();
        services.AddHttpClient<OsrmMesafeServisi>(c =>
            c.DefaultRequestHeaders.UserAgent.ParseAdd("KanBagis/0.2 (Bingol Universitesi ders projesi)"));
        services.AddTransient<IYolMesafesiServisi>(sp => sp.GetRequiredService<OsrmMesafeServisi>());

        // Telefon bildirim kanalları: ntfy (push) ve Twilio (SMS). Yapılandırılmayan kanal kendiliğinden kapalı kalır.
        services.AddHttpClient<NtfyKanali>();
        services.AddHttpClient<TwilioSmsKanali>();
        services.AddTransient<IBildirimKanali>(sp => sp.GetRequiredService<NtfyKanali>());
        services.AddTransient<IBildirimKanali>(sp => sp.GetRequiredService<TwilioSmsKanali>());

        // Gönderim kuyruğu ve arka plan servisi.
        services.AddSingleton<BildirimKuyrugu>();
        services.AddSingleton<IBildirimKuyrugu>(sp => sp.GetRequiredService<BildirimKuyrugu>());
        services.AddScoped<IDisBildirimGonderici, DisBildirimGonderici>();
        services.AddHostedService<BildirimGonderimArkaPlanServisi>();
        services.AddScoped<ITanimService, TanimService>();

        // ---- Yapay zekâ bileşeni (Hafta 10): talep ve stok tahmini. Anahtar yoksa istatistiksel tahmin kullanılır.
        services.AddSingleton<ILlmIstemcisi, AnthropicLlmIstemcisi>();
        services.AddScoped<ITahminService, TahminService>();

        return services;
    }

    public static Task VeritabaniniHazirlaAsync(IServiceProvider services) =>
        VeritabaniKurulum.MigrationlariUygulaAsync(services);

    public static Task<bool> VeritabaniErisilebilirMiAsync(IServiceProvider services, CancellationToken iptal = default) =>
        VeritabaniKurulum.ErisilebilirMiAsync(services, iptal);
}
