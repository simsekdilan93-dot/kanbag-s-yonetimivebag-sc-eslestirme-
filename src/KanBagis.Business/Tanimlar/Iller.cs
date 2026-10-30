namespace KanBagis.Business.Tanimlar;

/// <summary>
/// Türkiye'nin 81 ili (plaka sırasıyla). Konum serbest metin olarak alınmaz:
/// "bingol" ile "Bingöl" farklı yazılırsa eşleştirme aynı ildeki bağışçıyı kaçırır.
/// </summary>
public static class Iller
{
    public static readonly IReadOnlyList<string> Tumu =
    [
        "Adana", "Adıyaman", "Afyonkarahisar", "Ağrı", "Amasya", "Ankara", "Antalya", "Artvin", "Aydın",
        "Balıkesir", "Bilecik", "Bingöl", "Bitlis", "Bolu", "Burdur", "Bursa", "Çanakkale", "Çankırı",
        "Çorum", "Denizli", "Diyarbakır", "Edirne", "Elazığ", "Erzincan", "Erzurum", "Eskişehir",
        "Gaziantep", "Giresun", "Gümüşhane", "Hakkari", "Hatay", "Isparta", "Mersin", "İstanbul", "İzmir",
        "Kars", "Kastamonu", "Kayseri", "Kırklareli", "Kırşehir", "Kocaeli", "Konya", "Kütahya", "Malatya",
        "Manisa", "Kahramanmaraş", "Mardin", "Muğla", "Muş", "Nevşehir", "Niğde", "Ordu", "Rize", "Sakarya",
        "Samsun", "Siirt", "Sinop", "Sivas", "Tekirdağ", "Tokat", "Trabzon", "Tunceli", "Şanlıurfa", "Uşak",
        "Van", "Yozgat", "Zonguldak", "Aksaray", "Bayburt", "Karaman", "Kırıkkale", "Batman", "Şırnak",
        "Bartın", "Ardahan", "Iğdır", "Yalova", "Karabük", "Kilis", "Osmaniye", "Düzce",
    ];

    private static readonly HashSet<string> Kume = new(Tumu, StringComparer.Ordinal);

    public static bool GecerliMi(string? il) => il is not null && Kume.Contains(il);

    /// <summary>Alfabetik sıralı liste (Türkçe kurallarına göre: Ç, Ğ, İ, Ö, Ş, Ü doğru yerde).</summary>
    public static IReadOnlyList<string> AlfabetikSirali { get; } =
        Tumu.Order(StringComparer.Create(new System.Globalization.CultureInfo("tr-TR"), false)).ToList();
}
