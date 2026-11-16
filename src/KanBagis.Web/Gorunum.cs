using System.Globalization;
using KanBagis.Business.DisServisler;
using KanBagis.Business.Konum;
using KanBagis.Entities;

namespace KanBagis.Web;

/// <summary>Enum değerlerinin ekranda gösterilecek Türkçe karşılıkları ve rozet renkleri.</summary>
public static class Gorunum
{
    public static string Durum(TalepDurumu durum) => durum switch
    {
        TalepDurumu.Acik => "Açık",
        TalepDurumu.Karsilandi => "Karşılandı",
        TalepDurumu.Iptal => "İptal",
        _ => durum.ToString(),
    };

    public static string DurumRozeti(TalepDurumu durum) => durum switch
    {
        TalepDurumu.Acik => "text-bg-primary",
        TalepDurumu.Karsilandi => "text-bg-success",
        _ => "text-bg-secondary",
    };

    public static string Yanit(BildirimYaniti yanit) => yanit switch
    {
        BildirimYaniti.Beklemede => "Yanıt bekleniyor",
        BildirimYaniti.Kabul => "Kabul ettiniz",
        BildirimYaniti.Red => "Reddettiniz",
        _ => yanit.ToString(),
    };

    public static string Mesafe(double km) => km < 1 ? "Aynı il" : $"~{km:0} km";

    /// <summary>"144 km yol · ~1 sa 45 dk" ya da API yanıt vermediyse "~113 km (kuş uçuşu)".</summary>
    public static string Yol(double km, double? dakika, MesafeKaynagi kaynak) => kaynak switch
    {
        MesafeKaynagi.AyniIl => "Aynı il",
        MesafeKaynagi.YolApi => $"{km:0} km yol" + (dakika is { } d ? $" · ~{Sure(d)}" : ""),
        _ => $"~{km:0} km (kuş uçuşu)",
    };

    public static string Yol(YolBilgisi y) => Yol(y.Km, y.Dakika, y.Kaynak);

    private static string Sure(double dakika) =>
        dakika < 60 ? $"{dakika:0} dk" : $"{(int)(dakika / 60)} sa {dakika % 60:0} dk";

    /// <summary>OpenStreetMap yol tarifi bağlantısı (il merkezinden il merkezine; adres saklanmadığı için yaklaşık).</summary>
    public static string YolTarifi(string nereden, string nereye)
    {
        var a = IlKonumlari.Getir(nereden);
        var b = IlKonumlari.Getir(nereye);
        static string K(double x) => x.ToString("0.#####", CultureInfo.InvariantCulture);
        return $"https://www.openstreetmap.org/directions?engine=fossgis_osrm_car&route={K(a.Enlem)}%2C{K(a.Boylam)}%3B{K(b.Enlem)}%2C{K(b.Boylam)}";
    }

    public static string DisKanal(DisKanalDurumu d) => d switch
    {
        DisKanalDurumu.Gonderildi => "Telefona gönderildi",
        DisKanalDurumu.Kuyrukta => "Telefona gönderiliyor",
        DisKanalDurumu.Basarisiz => "Telefona ulaşmadı",
        _ => "",
    };
}
