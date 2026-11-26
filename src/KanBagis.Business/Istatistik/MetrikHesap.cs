using KanBagis.DataAccess.Repositories;
using KanBagis.Entities;

namespace KanBagis.Business.Istatistik;

/// <summary>
/// Metriklerin tanımları (veritabanından bağımsız, saf fonksiyonlar → doğrudan test edilir).
/// Tanımlar docs/moduller/Istatistik-Panosu.md'de de yazılıdır.
/// </summary>
public static class MetrikHesap
{
    /// <summary>
    /// Karşılanma oranı = karşılanan / (karşılanan + iptal). AÇIK talepler paydaya girmez: henüz sonuçlanmadılar;
    /// girselerdi yeni açılan her talep oranı haksız yere düşürürdü.
    /// </summary>
    public static TalepMetrikleri Talep(IReadOnlyList<TalepGrubu> g, IReadOnlyList<KarsilanmaSuresi> sureler)
    {
        int Say(TalepDurumu d) => g.Where(x => x.Durum == d).Sum(x => x.TalepSayisi);
        var toplam = g.Sum(x => x.TalepSayisi);
        var karsilandi = Say(TalepDurumu.Karsilandi);
        var iptal = Say(TalepDurumu.Iptal);
        var acil = g.Where(x => x.Aciliyet == Aciliyet.Acil).Sum(x => x.TalepSayisi);

        // Bozuk kayıt (kapanış < oluşturma) süre ortalamasına katılmaz.
        var saatler = sureler.Select(s => (s.Kapanis - s.Olusturma).TotalHours).Where(h => h >= 0).ToList();

        return new TalepMetrikleri(
            toplam, g.Sum(x => x.Unite), Say(TalepDurumu.Acik), karsilandi, iptal, acil,
            new Oran(karsilandi, karsilandi + iptal), new Oran(acil, toplam),
            saatler.Count == 0 ? null : Math.Round(saatler.Average(), 1),
            Medyan(saatler));
    }

    public static BagisMetrikleri Bagis(IReadOnlyList<BagisGrubu> g) => new(
        g.Sum(x => x.BagisSayisi), g.Sum(x => x.Unite),
        g.Where(x => x.TalebeBagli).Sum(x => x.BagisSayisi), g.Where(x => !x.TalebeBagli).Sum(x => x.BagisSayisi));

    /// <summary>
    /// Yanıt oranı = (kabul + red) / gönderilen. Kabul oranı = kabul / (kabul + red) — bekleyenler hariç.
    /// Telefon başarı oranı = gönderildi / (gönderildi + başarısız); telefonu kapalı ("Yok") ve kuyrukta olanlar hariç.
    /// </summary>
    public static BildirimOzeti Bildirim(IReadOnlyList<BildirimGrubu> g)
    {
        int Yanit(BildirimYaniti y) => g.Where(x => x.Yanit == y).Sum(x => x.Sayi);
        var kanal = Enum.GetValues<DisKanalDurumu>().ToDictionary(d => d, d => g.Where(x => x.DisKanal == d).Sum(x => x.Sayi));
        var toplam = g.Sum(x => x.Sayi);
        var kabul = Yanit(BildirimYaniti.Kabul);
        var red = Yanit(BildirimYaniti.Red);
        var gonderildi = kanal[DisKanalDurumu.Gonderildi];

        return new BildirimOzeti(toplam, kabul, red, Yanit(BildirimYaniti.Beklemede),
            new Oran(kabul + red, toplam), new Oran(kabul, kabul + red),
            kanal, new Oran(gonderildi, gonderildi + kanal[DisKanalDurumu.Basarisiz]));
    }

    /// <summary>
    /// [bitis - 7·n, bitis) aralığını n haftaya böler; talep ve bağış ünitelerini haftalara dağıtır.
    /// Hiç kaydı olmayan hafta 0 olarak YER ALIR (atlanırsa grafik eğilimi yanlış gösterir).
    /// </summary>
    public static List<HaftaSatiri> Haftalik(DateTime bitis, int haftaSayisi, IEnumerable<TarihliMiktar> talepler, IEnumerable<TarihliMiktar> bagislar)
    {
        var baslangic = bitis.AddDays(-7 * haftaSayisi);
        var talep = new int[haftaSayisi];
        var bagis = new int[haftaSayisi];

        void Dagit(IEnumerable<TarihliMiktar> kayitlar, int[] dizi)
        {
            foreach (var k in kayitlar)
            {
                if (k.Tarih < baslangic || k.Tarih >= bitis) continue;
                dizi[(int)((k.Tarih - baslangic).TotalDays / 7)] += k.Unite;
            }
        }
        Dagit(talepler, talep);
        Dagit(bagislar, bagis);

        return Enumerable.Range(0, haftaSayisi)
            .Select(i => new HaftaSatiri(baslangic.AddDays(7 * i), talep[i], bagis[i]))
            .ToList();
    }

    public static double? Medyan(IReadOnlyList<double> degerler)
    {
        if (degerler.Count == 0) return null;
        var s = degerler.Order().ToArray();
        var o = s.Length / 2;
        return Math.Round(s.Length % 2 == 1 ? s[o] : (s[o - 1] + s[o]) / 2, 1);
    }

    /// <summary>Gün sayısına göre grafikteki hafta sayısı (en az 1, en fazla 13; 365 gün için de son 13 hafta).</summary>
    public static int HaftaSayisi(int gun) => Math.Clamp((int)Math.Ceiling(gun / 7.0), 1, 13);
}
