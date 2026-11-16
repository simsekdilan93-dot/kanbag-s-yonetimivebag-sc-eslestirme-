using System.Text.Json;

namespace KanBagis.Business.Tahmin;

public sealed record DogrulamaSonucu(
    bool Kullanilabilir,
    bool Duzeltildi,
    IReadOnlyList<GrupTahmini> Gruplar,
    string? GenelYorum,
    IReadOnlyList<string> Notlar);

/// <summary>
/// Yapay zekâ çıktısını KÖRLEMESİNE KULLANMAMAK için doğrulama katmanı.
/// Şema zorlaması (structured output) biçimi garanti eder ama İÇERİĞİ garanti etmez; bu yüzden:
///   Reddet (taban tahmine dön): boş çıktı, bozuk JSON, "gruplar" yok, aynı grup iki kez, bilinmeyen grup,
///                               grupların yarısından fazlası eksik.
///   Düzelt (not düşerek):       eksik grup → taban; negatif/uçuk tahmin → taban; geçersiz risk → taban;
///                               riskin stok/tahmin oranıyla çelişmesi → hesaplanan risk;
///                               taban kritik/yüksek derken yapay zekânın uyarıyı düşürmesi → taban risk (güvenlik kuralı);
///                               boş/uzun gerekçe → kısaltma ya da taban gerekçe.
/// </summary>
public static class CiktiDogrulayici
{
    public const int GerekceEnFazla = 300;
    public const int YorumEnFazla = 600;

    public static DogrulamaSonucu Dogrula(string? hamCikti, IReadOnlyList<GrupTahmini> taban)
    {
        var notlar = new List<string>();
        DogrulamaSonucu Reddet(string neden)
        {
            notlar.Add("REDDEDİLDİ: " + neden + " → istatistiksel tahmin kullanıldı.");
            return new DogrulamaSonucu(false, false, taban, null, notlar);
        }

        if (string.IsNullOrWhiteSpace(hamCikti)) return Reddet("çıktı boş");

        JsonDocument belge;
        try { belge = JsonDocument.Parse(hamCikti); }
        catch (JsonException) { return Reddet("çıktı geçerli JSON değil"); }

        using (belge)
        {
            var kok = belge.RootElement;
            if (kok.ValueKind != JsonValueKind.Object
                || !kok.TryGetProperty("gruplar", out var gruplar) || gruplar.ValueKind != JsonValueKind.Array)
                return Reddet("\"gruplar\" listesi yok");

            var tabanAdla = taban.ToDictionary(t => t.KanGrubu);
            var gelen = new Dictionary<string, JsonElement>();
            foreach (var g in gruplar.EnumerateArray())
            {
                var ad = g.ValueKind == JsonValueKind.Object && g.TryGetProperty("kanGrubu", out var a) && a.ValueKind == JsonValueKind.String
                    ? a.GetString()! : null;
                if (ad is null || !tabanAdla.ContainsKey(ad)) return Reddet($"bilinmeyen kan grubu ({ad ?? "boş"})");
                if (!gelen.TryAdd(ad, g)) return Reddet($"{ad} iki kez yazılmış");
            }

            var eksik = taban.Where(t => !gelen.ContainsKey(t.KanGrubu)).ToList();
            if (eksik.Count > taban.Count / 2) return Reddet($"{eksik.Count}/{taban.Count} kan grubu eksik");

            var sonuc = new List<GrupTahmini>();
            foreach (var t in taban)
            {
                if (!gelen.TryGetValue(t.KanGrubu, out var g))
                {
                    notlar.Add($"{t.KanGrubu}: çıktıda yok, istatistiksel tahmin kullanıldı.");
                    sonuc.Add(t);
                    continue;
                }
                sonuc.Add(GrubuDogrula(t, g, notlar));
            }

            string? yorum = null;
            if (kok.TryGetProperty("genelYorum", out var y) && y.ValueKind == JsonValueKind.String)
                yorum = Kisalt(y.GetString()!.Trim(), YorumEnFazla);
            if (string.IsNullOrWhiteSpace(yorum))
            {
                notlar.Add("Genel yorum boş geldi.");
                yorum = null;
            }

            return new DogrulamaSonucu(true, notlar.Count > 0, sonuc, yorum, notlar);
        }
    }

    private static GrupTahmini GrubuDogrula(GrupTahmini t, JsonElement g, List<string> notlar)
    {
        // 1) Tahmin: tam sayı, 0 ile makul üst sınır arasında. Üst sınır: geçmişin en yükseğinin 3 katı (en az 10).
        var ustSinir = Math.Max(10, (int)Math.Ceiling(Math.Max(t.OrtalamaHaftalik, t.GelecekHaftaTahmini) * 3));
        int tahmin;
        if (g.TryGetProperty("gelecekHaftaTahmini", out var th) && th.ValueKind == JsonValueKind.Number
            && th.TryGetInt32(out var deger) && deger >= 0 && deger <= ustSinir)
        {
            tahmin = deger;
        }
        else
        {
            notlar.Add($"{t.KanGrubu}: tahmin geçersiz ya da makul aralık (0-{ustSinir}) dışında ({Ham(g, "gelecekHaftaTahmini")}), taban {t.GelecekHaftaTahmini} kullanıldı.");
            tahmin = t.GelecekHaftaTahmini;
        }

        var karsilama = IstatistikselTahmin.KarsilamaHaftasi(t.Stok, tahmin);
        var hesaplananRisk = IstatistikselTahmin.RiskHesapla(karsilama);

        // 2) Risk: geçerli değer mi?
        RiskSeviyesi risk;
        if (g.TryGetProperty("risk", out var r) && r.ValueKind == JsonValueKind.String
            && Enum.TryParse<RiskSeviyesi>(r.GetString(), ignoreCase: false, out var okunan) && Enum.IsDefined(okunan))
        {
            risk = okunan;
        }
        else
        {
            notlar.Add($"{t.KanGrubu}: risk değeri geçersiz ({Ham(g, "risk")}), hesaplanan risk kullanıldı.");
            risk = hesaplananRisk;
        }

        // 3) Tutarlılık: risk, modelin KENDİ tahminine göre hesaplanan riskten düşük olamaz.
        if (risk < hesaplananRisk)
        {
            notlar.Add($"{t.KanGrubu}: risk '{RiskMetinleri.Ad(risk)}' denmiş ama stok {t.Stok} / tahmin {tahmin} → '{RiskMetinleri.Ad(hesaplananRisk)}'; yükseltildi.");
            risk = hesaplananRisk;
        }

        // 4) Güvenlik kuralı: taban kritik/yüksek uyarı veriyorsa yapay zekâ uyarıyı DÜŞÜREMEZ.
        if (t.Risk >= RiskSeviyesi.Yuksek && risk < t.Risk)
        {
            notlar.Add($"{t.KanGrubu}: istatistiksel risk '{RiskMetinleri.Ad(t.Risk)}' iken yapay zekâ '{RiskMetinleri.Ad(risk)}' dedi; güvenlik için '{RiskMetinleri.Ad(t.Risk)}' korundu.");
            risk = t.Risk;
        }

        // 5) Gerekçe: boş olamaz, çok uzun olamaz.
        var gerekce = g.TryGetProperty("gerekce", out var ge) && ge.ValueKind == JsonValueKind.String ? ge.GetString()!.Trim() : "";
        if (gerekce.Length == 0)
        {
            notlar.Add($"{t.KanGrubu}: gerekçe boş, istatistiksel gerekçe kullanıldı.");
            gerekce = t.Gerekce;
        }
        else if (gerekce.Length > GerekceEnFazla)
        {
            notlar.Add($"{t.KanGrubu}: gerekçe {GerekceEnFazla} karaktere kısaltıldı.");
            gerekce = Kisalt(gerekce, GerekceEnFazla);
        }

        return t with { GelecekHaftaTahmini = tahmin, KarsilamaHaftasi = karsilama, Risk = risk, Gerekce = gerekce };
    }

    private static string Ham(JsonElement g, string alan) =>
        g.TryGetProperty(alan, out var v) ? Kisalt(v.GetRawText(), 30) : "yok";

    private static string Kisalt(string s, int en) => s.Length <= en ? s : s[..(en - 1)] + "…";
}
