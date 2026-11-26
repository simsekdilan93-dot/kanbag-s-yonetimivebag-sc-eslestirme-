using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Html;

namespace KanBagis.Web;

/// <summary>Grafikteki bir veri serisi (ör. "Talep" ya da "Bağış").</summary>
public sealed record Seri(string Ad, string Renk, IReadOnlyList<int> Degerler);

/// <summary>Yığılmış yüzde çubuğunun bir parçası.</summary>
public sealed record Parca(string Ad, string Renk, int Deger);

/// <summary>
/// Sunucuda SVG grafik üretir: harici JavaScript kütüphanesi gerekmez, sayfa çevrimdışı da çalışır, testte HTML olarak okunur.
/// Yanıltıcı görselden kaçınmak için:
///   • Değer ekseni HER ZAMAN 0'dan başlar (kesik eksen küçük farkı büyük gösterir).
///   • Eksen üst sınırı "yuvarlak" bir sayıdır (1-2-5 × 10ⁿ) ve tüm seriler aynı ölçeği kullanır.
///   • Her çubuğun değeri üstüne yazılır; renk tek başına anlam taşımaz (lejant + değer).
///   • Hiç veri yoksa boş eksen yerine "veri yok" yazılır.
/// Varsayılan genişlikler sayfadaki sütun genişliğine yakındır; SVG ölçeklenince yazılar okunaksız küçülmesin.
/// Tüm metinler HTML-kodlanır (XSS).
/// </summary>
public static class SvgGrafik
{
    private static readonly CultureInfo Tr = new("tr-TR");
    private static string K(string s) => WebUtility.HtmlEncode(s);
    private static string S(double d) => d.ToString("0.#", CultureInfo.InvariantCulture);

    /// <summary>0'dan başlayan eksen için yuvarlak üst sınır: 7 → 10, 23 → 25, 0 → 1.</summary>
    public static int EksenUstSiniri(int enBuyuk)
    {
        if (enBuyuk <= 0) return 1;
        var us = Math.Pow(10, Math.Floor(Math.Log10(enBuyuk)));
        foreach (var c in new[] { 1, 2, 2.5, 5, 10 })
            if (c * us >= enBuyuk) return (int)Math.Ceiling(c * us);
        return (int)(10 * us);
    }

    /// <summary>Dikey gruplu sütun grafiği (x: kategoriler, her kategoride her seri için bir sütun).</summary>
    public static IHtmlContent Sutun(string baslik, IReadOnlyList<string> etiketler, IReadOnlyList<Seri> seriler, string birim = "ünite", int G = 660)
    {
        const int Y = 300, Sol = 44, Sag = 10, Ust = 34, Alt = 46;
        var enBuyuk = seriler.SelectMany(s => s.Degerler).DefaultIfEmpty(0).Max();
        var ust = EksenUstSiniri(enBuyuk);
        var cizimY = Y - Ust - Alt;
        var grupG = (double)(G - Sol - Sag) / Math.Max(1, etiketler.Count);
        var sutunG = Math.Min(28, grupG * 0.8 / seriler.Count);

        var sb = Baslat(G, Y, baslik);
        Lejant(sb, seriler.Select(s => (s.Ad, s.Renk)), Sol, 16);

        // Yatay kılavuz çizgileri ve eksen değerleri (0, ¼, ½, ¾, üst).
        for (var i = 0; i <= 4; i++)
        {
            var deger = ust * i / 4.0;
            var y = Ust + cizimY - cizimY * i / 4.0;
            sb.Append($"<line x1=\"{Sol}\" x2=\"{G - Sag}\" y1=\"{S(y)}\" y2=\"{S(y)}\" stroke=\"{(i == 0 ? "#555" : "#ddd")}\" />");
            sb.Append($"<text x=\"{Sol - 6}\" y=\"{S(y + 4)}\" text-anchor=\"end\" font-size=\"11\" fill=\"#555\">{deger.ToString("0.#", Tr)}</text>");
        }

        if (enBuyuk == 0) VeriYok(sb, G / 2, Ust + cizimY / 2);

        for (var k = 0; k < etiketler.Count; k++)
        {
            var grupX = Sol + grupG * k + (grupG - sutunG * seriler.Count) / 2;
            for (var s = 0; s < seriler.Count; s++)
            {
                var v = seriler[s].Degerler[k];
                var h = cizimY * v / (double)ust;
                var x = grupX + sutunG * s;
                var y = Ust + cizimY - h;
                sb.Append($"<rect x=\"{S(x)}\" y=\"{S(y)}\" width=\"{S(sutunG - 2)}\" height=\"{S(h)}\" fill=\"{seriler[s].Renk}\">")
                  .Append($"<title>{K(etiketler[k])} · {K(seriler[s].Ad)}: {v} {K(birim)}</title></rect>");
                if (v > 0)
                    sb.Append($"<text x=\"{S(x + (sutunG - 2) / 2)}\" y=\"{S(y - 3)}\" text-anchor=\"middle\" font-size=\"10\" fill=\"#222\">{v}</text>");
            }
            sb.Append($"<text x=\"{S(Sol + grupG * k + grupG / 2)}\" y=\"{Y - Alt + 16}\" text-anchor=\"middle\" font-size=\"11\" fill=\"#333\">{K(etiketler[k])}</text>");
        }
        sb.Append($"<text x=\"{Sol}\" y=\"{Y - 6}\" font-size=\"11\" fill=\"#666\">Dikey eksen: {K(birim)} (0'dan başlar)</text>");
        return Bitir(sb);
    }

    /// <summary>Yatay gruplu çubuk grafiği (y: kategoriler). Uzun etiketli az sayıda kategori için.</summary>
    public static IHtmlContent Yatay(string baslik, IReadOnlyList<string> etiketler, IReadOnlyList<Seri> seriler, string birim = "ünite", int G = 460)
    {
        const int Sol = 70, Sag = 40, Ust = 34;
        var satirY = 12 * seriler.Count + 10;
        var Y = Ust + satirY * etiketler.Count + 30;
        var enBuyuk = seriler.SelectMany(s => s.Degerler).DefaultIfEmpty(0).Max();
        var ust = EksenUstSiniri(enBuyuk);
        var cizimG = G - Sol - Sag;

        var sb = Baslat(G, Y, baslik);
        Lejant(sb, seriler.Select(s => (s.Ad, s.Renk)), Sol, 16);
        for (var i = 0; i <= 4; i++)
        {
            var x = Sol + cizimG * i / 4.0;
            sb.Append($"<line x1=\"{S(x)}\" x2=\"{S(x)}\" y1=\"{Ust}\" y2=\"{Y - 24}\" stroke=\"{(i == 0 ? "#555" : "#ddd")}\" />");
            sb.Append($"<text x=\"{S(x)}\" y=\"{Y - 10}\" text-anchor=\"middle\" font-size=\"11\" fill=\"#555\">{(ust * i / 4.0).ToString("0.#", Tr)}</text>");
        }
        if (enBuyuk == 0) VeriYok(sb, G / 2, Ust + satirY * etiketler.Count / 2);

        for (var k = 0; k < etiketler.Count; k++)
        {
            var y0 = Ust + satirY * k + 4;
            sb.Append($"<text x=\"{Sol - 8}\" y=\"{y0 + 6 * seriler.Count + 2}\" text-anchor=\"end\" font-size=\"12\" fill=\"#222\">{K(etiketler[k])}</text>");
            for (var s = 0; s < seriler.Count; s++)
            {
                var v = seriler[s].Degerler[k];
                var w = cizimG * v / (double)ust;
                var y = y0 + 12 * s;
                sb.Append($"<rect x=\"{Sol}\" y=\"{y}\" width=\"{S(w)}\" height=\"10\" fill=\"{seriler[s].Renk}\">")
                  .Append($"<title>{K(etiketler[k])} · {K(seriler[s].Ad)}: {v} {K(birim)}</title></rect>");
                sb.Append($"<text x=\"{S(Sol + w + 4)}\" y=\"{y + 9}\" font-size=\"10\" fill=\"#222\">{v}</text>");
            }
        }
        return Bitir(sb);
    }

    /// <summary>%100 yığılmış yatay çubuk (bir bütünün parçaları, ör. bildirim yanıtları). Sayı ve yüzde birlikte yazılır.</summary>
    public static IHtmlContent Yigilmis(string baslik, IReadOnlyList<Parca> parcalar, int G = 560)
    {
        const int Y = 90, Sol = 10, Sag = 10, Ust = 30;
        var toplam = parcalar.Sum(p => p.Deger);
        var sb = Baslat(G, Y, baslik);
        if (toplam == 0)
        {
            VeriYok(sb, G / 2, Ust + 18);
            return Bitir(sb);
        }

        double x = Sol;
        var cizimG = G - Sol - Sag;
        foreach (var p in parcalar.Where(p => p.Deger > 0))
        {
            var w = cizimG * p.Deger / (double)toplam;
            var yuzde = (100.0 * p.Deger / toplam).ToString("0", Tr);
            sb.Append($"<rect x=\"{S(x)}\" y=\"{Ust}\" width=\"{S(w)}\" height=\"26\" fill=\"{p.Renk}\"><title>{K(p.Ad)}: {p.Deger} (%{yuzde})</title></rect>");
            if (w > 40)
                sb.Append($"<text x=\"{S(x + w / 2)}\" y=\"{Ust + 17}\" text-anchor=\"middle\" font-size=\"11\" fill=\"#fff\">{p.Deger}</text>");
            x += w;
        }
        Lejant(sb, parcalar.Select(p => ($"{p.Ad}: {p.Deger} (%{(100.0 * p.Deger / toplam).ToString("0", Tr)})", p.Renk)), Sol, Ust + 48);
        return Bitir(sb);
    }

    private static StringBuilder Baslat(int g, int y, string baslik) =>
        new StringBuilder($"<svg viewBox=\"0 0 {g} {y}\" width=\"100%\" role=\"img\" aria-label=\"{K(baslik)}\" style=\"max-width:{g}px;font-family:system-ui,sans-serif\">")
            .Append($"<title>{K(baslik)}</title>");

    private static IHtmlContent Bitir(StringBuilder sb) => new HtmlString(sb.Append("</svg>").ToString());

    private static void Lejant(StringBuilder sb, IEnumerable<(string Ad, string Renk)> ogeler, int x, int y)
    {
        double cx = x;
        foreach (var (ad, renk) in ogeler)
        {
            sb.Append($"<rect x=\"{S(cx)}\" y=\"{y - 9}\" width=\"10\" height=\"10\" fill=\"{renk}\" />")
              .Append($"<text x=\"{S(cx + 14)}\" y=\"{y}\" font-size=\"12\" fill=\"#222\">{K(ad)}</text>");
            cx += 24 + ad.Length * 6.5;
        }
    }

    private static void VeriYok(StringBuilder sb, double x, double y) =>
        sb.Append($"<text x=\"{S(x)}\" y=\"{S(y)}\" text-anchor=\"middle\" font-size=\"14\" fill=\"#888\">Bu dönemde veri yok</text>");
}
