using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KanBagis.Web;

/// <summary>
/// /saglik uç noktası: uygulama ayakta VE veritabanına erişebiliyorsa "Healthy" (200), değilse "Unhealthy" (503).
/// Ayrıntı (bağlantı dizesi, hata metni) dışarıya verilmez; yalnızca loglanır.
/// </summary>
public sealed class VeritabaniSaglikKontrolu(IServiceProvider servisler, ILogger<VeritabaniSaglikKontrolu> log) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken iptal = default)
    {
        try
        {
            return await KanBagis.Business.DependencyInjection.VeritabaniErisilebilirMiAsync(servisler, iptal)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Veritabanı boş ya da erişilemiyor.");
        }
        catch (Exception e)
        {
            log.LogError(e, "Sağlık kontrolü: veritabanına erişilemedi.");
            return HealthCheckResult.Unhealthy("Veritabanına erişilemedi.");
        }
    }
}
