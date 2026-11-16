using System.Text.Json;

namespace KanBagis.Business.Tahmin;

/// <summary>
/// İstem (prompt) tasarımı. Belgelenmiş hâli: docs/Yapay-Zeka-Bileseni.md.
/// İlkeler:
///   1. Rol ve görev açık: "kan merkezi stok planlama yardımcısı", tek iş: gelecek hafta talebi + stok riski.
///   2. Veri yalnızca TOPLU SAYILAR: kan grubu başına 12 haftalık ünite dizisi ve stok. Ad, TC, telefon yok (KVKK).
///   3. İstatistiksel taban tahmin de verilir; model ondan sapacaksa gerekçesini yazmak zorunda.
///   4. Risk tanımı sayısal olarak verilir (kaç hafta yeter), böylece model kendi ölçütünü uydurmaz.
///   5. Çıktı biçimi JSON şemasıyla zorlanır (structured output); yine de uygulama çıktıyı ayrıca doğrular.
/// </summary>
public static class TahminIstemi
{
    /// <summary>İstem sürümü: istem değiştikçe artırılır ve günlükte saklanır (hangi istemle ne çıktı alındı).</summary>
    public const string Surum = "istem-v1";

    public static readonly string[] KanGruplari = ["A Rh+", "A Rh-", "B Rh+", "B Rh-", "AB Rh+", "AB Rh-", "0 Rh+", "0 Rh-"];
    public static readonly string[] RiskDegerleri = ["Dusuk", "Orta", "Yuksek", "Kritik"];

    public const string Sistem = """
        Sen bir bölge kan merkezinin stok planlama yardımcısısın. Görevin, her kan grubu için GELECEK HAFTA kaç ünite
        kan talep edileceğini tahmin etmek ve mevcut stoka göre risk seviyesini belirlemek.

        Kurallar:
        - Yalnızca verilen sayılara dayan. Veride olmayan bir olayı (bayram, kaza, salgın) varsayma.
        - Her kan grubu için "istatistikselTaban" değeri hazır hesaplanmış doğrusal eğilim tahminidir. Ondan belirgin
          biçimde sapıyorsan nedenini gerekçede sayılarla açıkla (ör. "son 4 hafta ortalaması 9, taban 6'da kalıyor").
        - "veriYetersiz": true olan gruplarda eğilim çıkarma; ortalamaya yakın kal ve gerekçede verinin az olduğunu söyle.
        - Risk tanımı (karşılama = stok / gelecek hafta tahmini):
            Kritik: 1 haftadan az, Yuksek: 1-2 hafta, Orta: 2-4 hafta, Dusuk: 4 haftadan fazla ya da talep beklenmiyor.
        - Kan eksikliği hasta güvenliğini etkiler: emin değilsen riski DÜŞÜK değil, bir üst seviyede göster.
        - Gerekçe Türkçe, en fazla iki cümle, sayılarla.
        - genelYorum: kurum yöneticisi için 1-3 cümlelik Türkçe özet (hangi gruplar için bağış kampanyası gerekli).
        - Sekiz kan grubunun her biri çıktıda tam olarak bir kez yer almalı.
        """;

    /// <summary>Kullanıcı mesajı: veri JSON olarak verilir (modelin sayıları yanlış okuma ihtimalini azaltır).</summary>
    public static string KullaniciMesaji(IReadOnlyList<GrupGecmisi> gecmis, IReadOnlyList<GrupTahmini> taban)
    {
        var veri = gecmis.Select(g =>
        {
            var t = taban.First(x => x.KanGrubuId == g.KanGrubuId);
            return new
            {
                kanGrubu = g.KanGrubu,
                haftalikTalep = g.HaftalikUnite,
                mevcutStok = g.Stok,
                istatistikselTaban = t.GelecekHaftaTahmini,
                haftalikEgilim = t.HaftalikEgilim,
                veriYetersiz = t.VeriYetersiz,
            };
        });

        var json = JsonSerializer.Serialize(new { haftaSayisi = 12, birim = "ünite", kanGruplari = veri },
            new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

        return $"""
            Aşağıda son 12 haftanın (eskiden yeniye) kan grubu bazında haftalık talep edilen ünite sayıları ve
            güncel stok var. Her kan grubu için gelecek hafta tahminini, risk seviyesini ve gerekçesini ver.

            {json}
            """;
    }

    /// <summary>Çıktı JSON şeması (structured output). Model bu şemaya uymayan çıktı üretemez.</summary>
    public static Dictionary<string, JsonElement> Sema()
    {
        var grup = new
        {
            type = "object",
            properties = new
            {
                kanGrubu = new { type = "string", @enum = KanGruplari },
                gelecekHaftaTahmini = new { type = "integer" },
                risk = new { type = "string", @enum = RiskDegerleri },
                gerekce = new { type = "string" },
            },
            required = new[] { "kanGrubu", "gelecekHaftaTahmini", "risk", "gerekce" },
            additionalProperties = false,
        };

        return new Dictionary<string, JsonElement>
        {
            ["type"] = JsonSerializer.SerializeToElement("object"),
            ["properties"] = JsonSerializer.SerializeToElement(new
            {
                gruplar = new { type = "array", items = grup },
                genelYorum = new { type = "string" },
            }),
            ["required"] = JsonSerializer.SerializeToElement(new[] { "gruplar", "genelYorum" }),
            ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
        };
    }
}
