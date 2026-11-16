namespace KanBagis.Business.Tahmin;

/// <summary>
/// Yapay zekâdan BAĞIMSIZ taban tahmin. Hem yapay zekâ kapalıyken kullanılır hem de yapay zekâ çıktısının
/// karşılaştırıldığı "güvenlik ağı"dır.
///   • Ortalama: son 12 haftanın haftalık ortalaması.
///   • Eğilim: en küçük kareler doğrusu (haftalık değişim, ünite/hafta).
///   • Gelecek hafta = doğrunun 13. haftadaki değeri (negatifse 0).
///   • Karşılama süresi = stok / gelecek hafta tahmini (kaç hafta yeter).
/// Az veride (talepli hafta sayısı az) eğilim güvenilmez; o zaman yalnızca ortalama kullanılır ve "veri yetersiz" denir.
/// </summary>
public static class IstatistikselTahmin
{
    /// <summary>En az bu kadar haftada talep olmalı; yoksa eğilim hesaplanmaz.</summary>
    public const int EnAzTalepliHafta = 4;

    public static GrupTahmini Hesapla(GrupGecmisi g)
    {
        var y = g.HaftalikUnite;
        var n = y.Count;
        var ortalama = n == 0 ? 0 : y.Average();
        var talepliHafta = y.Count(v => v > 0);
        var veriYetersiz = talepliHafta < EnAzTalepliHafta;

        double egilim = 0;
        double tahminDegeri = ortalama;
        if (!veriYetersiz && n >= 2)
        {
            // x = 0..n-1, y = haftalık ünite → eğim = Σ(x-x̄)(y-ȳ) / Σ(x-x̄)²
            var xOrt = (n - 1) / 2.0;
            double pay = 0, payda = 0;
            for (var x = 0; x < n; x++)
            {
                pay += (x - xOrt) * (y[x] - ortalama);
                payda += (x - xOrt) * (x - xOrt);
            }
            egilim = pay / payda;
            tahminDegeri = ortalama + egilim * (n - xOrt);   // x = n (gelecek hafta)
        }

        var tahmin = (int)Math.Round(Math.Max(0, tahminDegeri), MidpointRounding.AwayFromZero);
        var karsilama = KarsilamaHaftasi(g.Stok, tahmin);
        var risk = RiskHesapla(karsilama);

        var gerekce = veriYetersiz
            ? $"Son 12 haftanın yalnızca {talepliHafta} haftasında talep var; eğilim hesaplanmadı, ortalama ({ortalama:0.#}) kullanıldı."
            : $"12 haftalık ortalama {ortalama:0.#} ünite, eğilim haftada {egilim:+0.##;-0.##;0} ünite; stok {KarsilamaMetni(karsilama)}.";

        return new GrupTahmini(g.KanGrubuId, g.KanGrubu, g.Stok, Math.Round(ortalama, 2), Math.Round(egilim, 2),
            tahmin, karsilama, risk, veriYetersiz, gerekce);
    }

    public static IReadOnlyList<GrupTahmini> Hesapla(IEnumerable<GrupGecmisi> gruplar) => gruplar.Select(Hesapla).ToList();

    /// <summary>Talep beklenmiyorsa (tahmin 0) stok "süresiz" yeter: null.</summary>
    public static double? KarsilamaHaftasi(int stok, int haftalikTahmin) =>
        haftalikTahmin <= 0 ? null : Math.Round((double)stok / haftalikTahmin, 1);

    /// <summary>Stok 1 haftadan az yetiyorsa kritik, 2'den az yüksek, 4'ten az orta.</summary>
    public static RiskSeviyesi RiskHesapla(double? karsilamaHaftasi) => karsilamaHaftasi switch
    {
        null => RiskSeviyesi.Dusuk,
        < 1 => RiskSeviyesi.Kritik,
        < 2 => RiskSeviyesi.Yuksek,
        < 4 => RiskSeviyesi.Orta,
        _ => RiskSeviyesi.Dusuk,
    };

    public static string KarsilamaMetni(double? karsilama) =>
        karsilama is null ? "talep beklenmediği için yeterli" : $"yaklaşık {karsilama:0.#} hafta yeter";
}
