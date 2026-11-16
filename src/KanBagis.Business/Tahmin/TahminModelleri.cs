using KanBagis.Entities;

namespace KanBagis.Business.Tahmin;

/// <summary>Sıralama önemli: büyük değer = daha ciddi uyarı. Doğrulamada "uyarıyı düşürme" kontrolü buna dayanır.</summary>
public enum RiskSeviyesi
{
    Dusuk,
    Orta,
    Yuksek,
    Kritik,
}

/// <summary>Bir kan grubunun son 12 haftalık talep geçmişi ve güncel stoku (toplu sayılar, kişisel veri yok).</summary>
public sealed record GrupGecmisi(int KanGrubuId, string KanGrubu, IReadOnlyList<int> HaftalikUnite, int Stok);

public sealed record GrupTahmini(
    int KanGrubuId,
    string KanGrubu,
    int Stok,
    double OrtalamaHaftalik,
    double HaftalikEgilim,
    int GelecekHaftaTahmini,
    double? KarsilamaHaftasi,
    RiskSeviyesi Risk,
    bool VeriYetersiz,
    string Gerekce);

public sealed record TahminSonucu(
    int KayitId,
    DateTime Tarih,
    TahminKaynagi Kaynak,
    string? Model,
    IReadOnlyList<GrupTahmini> Gruplar,
    string? GenelYorum,
    IReadOnlyList<string> Notlar);

/// <summary>Dil modelinden dönen yanıt. Metin null ise <see cref="Hata"/> ya da <see cref="Reddedildi"/> nedenini söyler.</summary>
public sealed record LlmYaniti(string? Metin, string Model, bool Reddedildi = false, string? Hata = null);

public static class RiskMetinleri
{
    public static string Ad(RiskSeviyesi r) => r switch
    {
        RiskSeviyesi.Kritik => "Kritik",
        RiskSeviyesi.Yuksek => "Yüksek",
        RiskSeviyesi.Orta => "Orta",
        _ => "Düşük",
    };

    public static string Kaynak(TahminKaynagi k) => k switch
    {
        TahminKaynagi.YapayZeka => "Yapay zekâ (doğrulandı)",
        TahminKaynagi.YapayZekaDuzeltildi => "Yapay zekâ (doğrulamada düzeltildi)",
        _ => "İstatistiksel tahmin (yapay zekâ kullanılmadı)",
    };
}
