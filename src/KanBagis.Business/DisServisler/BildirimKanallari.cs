using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using KanBagis.Entities;
using Microsoft.Extensions.Options;

namespace KanBagis.Business.DisServisler;

/// <summary>Telefona giden mesaj. KVKK: kişisel veri (ad, telefon, TC) İÇERMEZ; ayrıntı için siteye girilir.</summary>
public record DisMesaj(string Baslik, string Metin, bool Acil);

public enum KanalSonucTuru
{
    Basarili,
    GeciciHata,   // zaman aşımı, ağ hatası, 5xx → yeniden denenir
    KaliciHata,   // 400/401/403: yanlış anahtar ya da geçersiz istek → yeniden denemek işe yaramaz
    HizSiniri,    // 429 → bir süre sonra kuyruğa geri konur
}

public record KanalSonucu(KanalSonucTuru Tur, string? Hata = null, TimeSpan? SonraTekrarDene = null)
{
    public static readonly KanalSonucu Tamam = new(KanalSonucTuru.Basarili);
}

public interface IBildirimKanali
{
    string Ad { get; }
    bool Etkin { get; }
    bool BagisciIcinAcikMi(Bagisci bagisci);
    Task<KanalSonucu> GonderAsync(Bagisci bagisci, DisMesaj mesaj, CancellationToken ct);
}

/// <summary>HTTP yanıt kodunu kanal sonucuna çevirir (iki kanal da aynı kuralı kullanır).</summary>
internal static class HttpSonucu
{
    public static async Task<KanalSonucu> CevirAsync(HttpResponseMessage yanit, Func<string, string?> hataMesajiCikar, CancellationToken ct)
    {
        if (yanit.IsSuccessStatusCode) return KanalSonucu.Tamam;

        var govde = await yanit.Content.ReadAsStringAsync(ct);
        var mesaj = $"HTTP {(int)yanit.StatusCode}" + (hataMesajiCikar(govde) is { } m ? $": {m}" : "");

        return yanit.StatusCode switch
        {
            HttpStatusCode.TooManyRequests => new KanalSonucu(KanalSonucTuru.HizSiniri, mesaj,
                yanit.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(60)),
            >= HttpStatusCode.InternalServerError or HttpStatusCode.RequestTimeout => new KanalSonucu(KanalSonucTuru.GeciciHata, mesaj),
            _ => new KanalSonucu(KanalSonucTuru.KaliciHata, mesaj),
        };
    }

    public static string? JsonAlani(string govde, string alan)
    {
        try
        {
            using var b = JsonDocument.Parse(govde);
            return b.RootElement.TryGetProperty(alan, out var d) ? d.ToString() : null;
        }
        catch (JsonException) { return null; }
    }
}

/// <summary>
/// ntfy (https://ntfy.sh): telefona anlık bildirim (push). Bağışçı kendi rastgele kanalına abone olur.
/// REST isteği:  POST {TemelAdres}   gövde (JSON): { "topic", "title", "message", "priority", "tags" }
/// Kimlik doğrulama: isteğe bağlı "Authorization: Bearer {Token}" (Token yalnızca ortam değişkeninde).
/// </summary>
public sealed class NtfyKanali(HttpClient http, IOptions<NtfyAyarlari> ayarlar) : IBildirimKanali
{
    private readonly NtfyAyarlari _a = ayarlar.Value;

    public string Ad => "Anlık bildirim (ntfy)";
    public bool Etkin => _a.Etkin;
    public bool BagisciIcinAcikMi(Bagisci b) => !string.IsNullOrEmpty(b.AnlikBildirimKanali);

    public async Task<KanalSonucu> GonderAsync(Bagisci b, DisMesaj m, CancellationToken ct)
    {
        using var istek = new HttpRequestMessage(HttpMethod.Post, _a.TemelAdres.TrimEnd('/'))
        {
            Content = JsonContent.Create(new
            {
                topic = b.AnlikBildirimKanali,
                title = m.Baslik,
                message = m.Metin,
                priority = m.Acil ? 5 : 3,          // 5 = en yüksek (telefonda sesli ve öne çıkan)
                tags = new[] { "drop_of_blood" },
            }),
        };
        if (!string.IsNullOrWhiteSpace(_a.Token))
            istek.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _a.Token);

        using var yanit = await http.SendAsync(istek, ct);
        return await HttpSonucu.CevirAsync(yanit, g => HttpSonucu.JsonAlani(g, "error"), ct);
    }
}

/// <summary>
/// Twilio SMS. Yalnızca hesap bilgileri ortam değişkenlerinde tanımlıysa ve bağışçı SMS izni verdiyse çalışır.
/// REST isteği:  POST {TemelAdres}/2010-04-01/Accounts/{AccountSid}/Messages.json   (form: To, From, Body)
/// Kimlik doğrulama: HTTP Basic — "AccountSid:AuthToken" Base64 ile kodlanıp Authorization başlığına konur.
/// Hata yanıtı (JSON): { "code": 21211, "message": "The 'To' number is not a valid phone number.", "status": 400 }
/// </summary>
public sealed class TwilioSmsKanali(HttpClient http, IOptions<TwilioAyarlari> ayarlar) : IBildirimKanali
{
    private readonly TwilioAyarlari _a = ayarlar.Value;

    public string Ad => "SMS (Twilio)";
    public bool Etkin => _a.Etkin;
    public bool BagisciIcinAcikMi(Bagisci b) => b.SmsIzni && UluslararasiNumara(b.Telefon) is not null;

    public async Task<KanalSonucu> GonderAsync(Bagisci b, DisMesaj m, CancellationToken ct)
    {
        var adres = $"{_a.TemelAdres.TrimEnd('/')}/2010-04-01/Accounts/{_a.AccountSid}/Messages.json";
        using var istek = new HttpRequestMessage(HttpMethod.Post, adres)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["To"] = UluslararasiNumara(b.Telefon)!,
                ["From"] = _a.Gonderen!,
                ["Body"] = $"{m.Baslik}: {m.Metin}",
            }),
        };
        var kimlik = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_a.AccountSid}:{_a.AuthToken}"));
        istek.Headers.Authorization = new AuthenticationHeaderValue("Basic", kimlik);

        using var yanit = await http.SendAsync(istek, ct);
        return await HttpSonucu.CevirAsync(yanit, g => HttpSonucu.JsonAlani(g, "message"), ct);
    }

    /// <summary>05xx xxx xx xx → +905xxxxxxxxx (Twilio uluslararası biçim ister).</summary>
    public static string? UluslararasiNumara(string telefon) =>
        telefon.Length == 11 && telefon.StartsWith('0') && telefon.All(char.IsAsciiDigit) ? "+90" + telefon[1..] : null;
}
