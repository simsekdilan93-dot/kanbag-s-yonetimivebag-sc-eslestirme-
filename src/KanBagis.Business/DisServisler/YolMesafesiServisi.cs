using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text.Json;
using KanBagis.Business.Konum;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KanBagis.Business.DisServisler;

public enum MesafeKaynagi { AyniIl, YolApi, KusUcusu }

/// <summary>İki il arasındaki uzaklık. Dakika yalnızca yol API'si yanıt verdiyse bilinir.</summary>
public record YolBilgisi(double Km, double? Dakika, MesafeKaynagi Kaynak);

public interface IYolMesafesiServisi
{
    /// <summary>Merkez ilden her hedef ile uzaklık. Hiçbir zaman hata fırlatmaz: API çalışmazsa kuş uçuşuna döner.</summary>
    Task<IReadOnlyDictionary<string, YolBilgisi>> HesaplaAsync(string merkezIl, IEnumerable<string> hedefIller, CancellationToken ct = default);
}

/// <summary>Harici API kullanmadan kuş uçuşu (Haversine) mesafe. API kapalıyken ve testlerde kullanılır.</summary>
public sealed class KusUcusuMesafeServisi : IYolMesafesiServisi
{
    public Task<IReadOnlyDictionary<string, YolBilgisi>> HesaplaAsync(string merkezIl, IEnumerable<string> hedefIller, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyDictionary<string, YolBilgisi>>(
            hedefIller.Distinct().ToDictionary(il => il, il => KusUcusu(merkezIl, il)));

    public static YolBilgisi KusUcusu(string merkez, string hedef) =>
        merkez == hedef ? new YolBilgisi(0, 0, MesafeKaynagi.AyniIl)
                        : new YolBilgisi(IlKonumlari.MesafeKm(merkez, hedef), null, MesafeKaynagi.KusUcusu);
}

/// <summary>
/// Başarılı API yanıtlarının önbelleği (uygulama boyunca tek örnek). İl merkezleri arası yol değişmediği için
/// aynı iki il için API bir kez çağrılır; hız sınırına takılma olasılığı da azalır.
/// </summary>
public sealed class MesafeOnbellegi
{
    public ConcurrentDictionary<(string, string), YolBilgisi> Kayitlar { get; } = new();
    public SemaphoreSlim Kilit { get; } = new(1, 1);
    public DateTimeOffset SonIstek { get; set; } = DateTimeOffset.MinValue;

    /// <summary>Sunucu 429 (çok fazla istek) dediyse bu zamana kadar API çağrılmaz.</summary>
    public DateTimeOffset HizSiniriBitis { get; set; } = DateTimeOffset.MinValue;
}

/// <summary>
/// OSRM "table" API'si ile il merkezleri arasındaki YOL mesafesi ve süresi.
/// REST isteği:  GET {TemelAdres}/table/v1/driving/{boylam,enlem;boylam,enlem;…}?sources=0&amp;annotations=distance,duration
/// Örnek yanıt:  { "code": "Ok", "distances": [[0, 144236.4]], "durations": [[0, 6420.3]] }   (metre, saniye)
///
/// HATA YÖNETİMİ: zaman aşımı, ağ hatası, 4xx/5xx, 429 (hız sınırı), bozuk JSON, "code" ≠ "Ok" ve
/// eksik (null) değerlerin HEPSİNDE kuş uçuşu mesafeye dönülür. Eşleştirme hiçbir zaman API yüzünden durmaz.
/// </summary>
public sealed class OsrmMesafeServisi(
    HttpClient http,
    MesafeOnbellegi onbellek,
    IOptions<MesafeApiAyarlari> ayarlar,
    TimeProvider zaman,
    ILogger<OsrmMesafeServisi> log) : IYolMesafesiServisi
{
    private readonly MesafeApiAyarlari _a = ayarlar.Value;

    public async Task<IReadOnlyDictionary<string, YolBilgisi>> HesaplaAsync(string merkezIl, IEnumerable<string> hedefIller, CancellationToken ct = default)
    {
        var sonuc = new Dictionary<string, YolBilgisi>();
        var eksik = new List<string>();

        foreach (var il in hedefIller.Distinct())
        {
            if (il == merkezIl) sonuc[il] = new YolBilgisi(0, 0, MesafeKaynagi.AyniIl);
            else if (onbellek.Kayitlar.TryGetValue((merkezIl, il), out var kayit)) sonuc[il] = kayit;
            else eksik.Add(il);
        }

        if (eksik.Count > 0)
        {
            var apiSonucu = _a.Etkin ? await ApiyeSorAsync(merkezIl, eksik, ct) : null;
            foreach (var il in eksik)
            {
                if (apiSonucu?.GetValueOrDefault(il) is { } yol)
                {
                    sonuc[il] = yol;
                    onbellek.Kayitlar[(merkezIl, il)] = yol;
                }
                else
                {
                    sonuc[il] = KusUcusuMesafeServisi.KusUcusu(merkezIl, il);   // yedek
                }
            }
        }
        return sonuc;
    }

    private async Task<Dictionary<string, YolBilgisi>?> ApiyeSorAsync(string merkezIl, List<string> hedefler, CancellationToken ct)
    {
        if (zaman.GetUtcNow() < onbellek.HizSiniriBitis)
        {
            log.LogInformation("Mesafe API hız sınırında; {Bitis} saatine kadar kuş uçuşu kullanılıyor.", onbellek.HizSiniriBitis);
            return null;
        }

        await onbellek.Kilit.WaitAsync(ct);
        try
        {
            // Hız sınırı (istemci tarafı): iki istek arasında en az IstekArasiEnAzMilisaniye beklenir.
            var bekle = onbellek.SonIstek + TimeSpan.FromMilliseconds(_a.IstekArasiEnAzMilisaniye) - zaman.GetUtcNow();
            if (bekle > TimeSpan.Zero) await Task.Delay(bekle, ct);
            onbellek.SonIstek = zaman.GetUtcNow();

            var koordinatlar = new[] { merkezIl }.Concat(hedefler)
                .Select(IlKonumlari.Getir)
                .Select(k => string.Create(CultureInfo.InvariantCulture, $"{k.Boylam:0.#####},{k.Enlem:0.#####}"));
            var adres = $"{_a.TemelAdres.TrimEnd('/')}/table/v1/driving/{string.Join(';', koordinatlar)}?sources=0&annotations=distance,duration";

            using var zamanAsimi = CancellationTokenSource.CreateLinkedTokenSource(ct);
            zamanAsimi.CancelAfter(TimeSpan.FromSeconds(_a.ZamanAsimiSaniye));

            using var yanit = await http.GetAsync(adres, zamanAsimi.Token);

            if (yanit.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var sure = yanit.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(_a.HizSiniriVarsayilanBeklemeSaniye);
                onbellek.HizSiniriBitis = zaman.GetUtcNow() + sure;
                log.LogWarning("Mesafe API hız sınırı (429). {Sure} sn beklenecek, şimdilik kuş uçuşu kullanılıyor.", sure.TotalSeconds);
                return null;
            }
            if (!yanit.IsSuccessStatusCode)
            {
                log.LogWarning("Mesafe API hata döndü: {Kod}. Kuş uçuşu kullanılıyor.", (int)yanit.StatusCode);
                return null;
            }

            await using var akis = await yanit.Content.ReadAsStreamAsync(zamanAsimi.Token);
            return Ayristir(await JsonDocument.ParseAsync(akis, cancellationToken: zamanAsimi.Token), hedefler);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            log.LogWarning("Mesafe API {Sn} sn içinde yanıt vermedi (zaman aşımı). Kuş uçuşu kullanılıyor.", _a.ZamanAsimiSaniye);
            return null;
        }
        catch (HttpRequestException ex)
        {
            log.LogWarning("Mesafe API'ye ulaşılamadı: {Hata}. Kuş uçuşu kullanılıyor.", ex.Message);
            return null;
        }
        catch (JsonException)
        {
            log.LogWarning("Mesafe API yanıtı JSON değil. Kuş uçuşu kullanılıyor.");
            return null;
        }
        finally
        {
            onbellek.Kilit.Release();
        }
    }

    /// <summary>
    /// Yanıtı ayrıştırır. distances[0][i] ve durations[0][i], i. hedefe (merkez 0. sırada) metre ve saniyedir.
    /// Yol bulunamayan hedefte değer null gelir; o hedef sonuca eklenmez (kuş uçuşuna düşer).
    /// </summary>
    internal static Dictionary<string, YolBilgisi>? Ayristir(JsonDocument belge, IReadOnlyList<string> hedefler)
    {
        using (belge)
        {
            var kok = belge.RootElement;
            if (!kok.TryGetProperty("code", out var kod) || kod.GetString() != "Ok") return null;
            if (!kok.TryGetProperty("distances", out var mesafeler) || mesafeler.GetArrayLength() == 0) return null;

            var satirM = mesafeler[0];
            var satirS = kok.TryGetProperty("durations", out var sureler) && sureler.GetArrayLength() > 0 ? sureler[0] : default;

            var sonuc = new Dictionary<string, YolBilgisi>();
            for (var i = 0; i < hedefler.Count; i++)
            {
                var j = i + 1;   // 0. sütun merkezin kendisi
                if (j >= satirM.GetArrayLength() || satirM[j].ValueKind != JsonValueKind.Number) continue;

                double? dakika = satirS.ValueKind == JsonValueKind.Array && j < satirS.GetArrayLength() && satirS[j].ValueKind == JsonValueKind.Number
                    ? Math.Round(satirS[j].GetDouble() / 60, 0)
                    : null;
                sonuc[hedefler[i]] = new YolBilgisi(Math.Round(satirM[j].GetDouble() / 1000, 1), dakika, MesafeKaynagi.YolApi);
            }
            return sonuc;
        }
    }
}
