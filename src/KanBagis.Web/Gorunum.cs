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
}
