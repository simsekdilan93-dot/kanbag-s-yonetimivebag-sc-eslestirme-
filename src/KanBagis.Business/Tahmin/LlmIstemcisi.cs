using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Beta.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KanBagis.Business.Tahmin;

/// <summary>
/// Yapay zekâ ayarları. API anahtarı GİZLİDİR: appsettings'e yazılmaz, ortam değişkeninden okunur:
///   ANTHROPIC_API_KEY  (ya da DisServisler__YapayZeka__ApiAnahtari)
/// Anahtar yoksa bileşen kendiliğinden kapalıdır ve istatistiksel tahmin kullanılır; uygulama yine çalışır.
/// </summary>
public sealed class YapayZekaAyarlari
{
    public const string Bolum = "DisServisler:YapayZeka";

    public bool Etkin { get; set; } = true;
    public string Model { get; set; } = "claude-opus-5-5";
    public int MaksimumToken { get; set; } = 8000;
    public int ZamanAsimiSaniye { get; set; } = 90;

    /// <summary>GİZLİ. Boşsa ANTHROPIC_API_KEY ortam değişkenine bakılır.</summary>
    public string? ApiAnahtari { get; set; }

    public string? EtkinAnahtar =>
        !string.IsNullOrWhiteSpace(ApiAnahtari) ? ApiAnahtari : Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");

    public bool Kullanilabilir => Etkin && !string.IsNullOrWhiteSpace(EtkinAnahtar);
}

/// <summary>Dil modeli soyutlaması: testlerde sahte istemci kullanılır, gerçek API çağrılmaz.</summary>
public interface ILlmIstemcisi
{
    bool Kullanilabilir { get; }
    string Model { get; }

    /// <summary>Asla istisna fırlatmaz: hata, zaman aşımı ve ret <see cref="LlmYaniti"/> içinde döner.</summary>
    Task<LlmYaniti> JsonIsteAsync(string sistem, string kullanici, Dictionary<string, JsonElement> sema, CancellationToken iptal = default);
}

/// <summary>
/// Anthropic Claude API istemcisi (resmî C# SDK'sı "Anthropic").
///   • Yapılandırılmış çıktı (OutputConfig.Format = JSON şeması): model şemaya uymayan metin üretemez.
///   • Ret yedeği (fallbacks "default", beta server-side-fallback-2026-07-01): model isteği güvenlik
///     nedeniyle reddederse sunucu, Anthropic'in önerdiği başka bir modelle aynı istekte yeniden dener.
///   • stop_reason "refusal" ya da "max_tokens" ise çıktı KULLANILMAZ.
/// </summary>
public sealed class AnthropicLlmIstemcisi(IOptions<YapayZekaAyarlari> ayarlar, ILogger<AnthropicLlmIstemcisi> log) : ILlmIstemcisi
{
    private readonly YapayZekaAyarlari _a = ayarlar.Value;

    public bool Kullanilabilir => _a.Kullanilabilir;
    public string Model => _a.Model;

    public async Task<LlmYaniti> JsonIsteAsync(string sistem, string kullanici, Dictionary<string, JsonElement> sema, CancellationToken iptal = default)
    {
        if (!Kullanilabilir) return new LlmYaniti(null, Model, Hata: "Yapay zekâ kapalı ya da API anahtarı tanımlı değil.");

        using var zamanAsimi = CancellationTokenSource.CreateLinkedTokenSource(iptal);
        zamanAsimi.CancelAfter(TimeSpan.FromSeconds(_a.ZamanAsimiSaniye));

        try
        {
            AnthropicClient istemci = new() { ApiKey = _a.EtkinAnahtar! };
            var yanit = await istemci.Beta.Messages.Create(new MessageCreateParams
            {
                Model = _a.Model,
                MaxTokens = _a.MaksimumToken,
                Betas = ["server-side-fallback-2026-07-01"],
                Fallbacks = new Default(),   // "default": yedek modeli Anthropic seçer
                System = sistem,
                OutputConfig = new BetaOutputConfig
                {
                    Effort = Effort.Medium,
                    Format = new BetaJsonOutputFormat { Schema = sema },
                },
                Messages = [new BetaUserMessageParam { Content = kullanici }],
            }, zamanAsimi.Token);

            var model = yanit.Model.ToString() ?? Model;
            if (yanit.StopReason == "refusal")
            {
                log.LogWarning("Yapay zekâ isteği reddetti (yedek model de dahil).");
                return new LlmYaniti(null, model, Reddedildi: true, Hata: "Model isteği reddetti.");
            }
            if (yanit.StopReason == "max_tokens")
                return new LlmYaniti(null, model, Hata: "Çıktı token sınırında kesildi (eksik JSON).");

            var metin = string.Concat(yanit.Content.Select(b => b.TryPickText(out var t) ? t.Text : ""));
            return new LlmYaniti(metin, model);
        }
        catch (OperationCanceledException) when (!iptal.IsCancellationRequested)
        {
            return new LlmYaniti(null, Model, Hata: $"Zaman aşımı ({_a.ZamanAsimiSaniye} sn).");
        }
        catch (AnthropicRateLimitException)
        {
            return new LlmYaniti(null, Model, Hata: "Hız sınırı aşıldı (429), daha sonra tekrar deneyin.");
        }
        catch (Anthropic5xxException e)
        {
            return new LlmYaniti(null, Model, Hata: $"Sunucu hatası ({(int)e.StatusCode}).");
        }
        catch (AnthropicApiException e)
        {
            // 401 (anahtar hatalı), 400 (istek hatalı) vb. Ayrıntı loglanır, anahtar asla loglanmaz.
            log.LogWarning("Yapay zekâ API hatası: {Durum}", e.StatusCode);
            return new LlmYaniti(null, Model, Hata: $"API hatası ({(int)e.StatusCode}).");
        }
        catch (HttpRequestException)
        {
            return new LlmYaniti(null, Model, Hata: "Ağ hatası: API'ye ulaşılamadı.");
        }
    }
}
