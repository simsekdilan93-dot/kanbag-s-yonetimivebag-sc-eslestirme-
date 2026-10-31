using KanBagis.Business.Tanimlar;

namespace KanBagis.Business.Konum;

public readonly record struct Koordinat(double Enlem, double Boylam);

/// <summary>
/// 81 il merkezinin YAKLAŞIK koordinatları (derece). Eşleştirmede iller arası mesafeyi
/// hesaplamak için kullanılır. Bağışçının tam adresi tutulmaz (KVKK: veri minimizasyonu);
/// konum il düzeyindedir, bu yüzden aynı ildeki herkesin mesafesi 0 km kabul edilir.
/// </summary>
public static class IlKonumlari
{
    private static readonly Dictionary<string, Koordinat> Konumlar = new(StringComparer.Ordinal)
    {
        ["Adana"] = new(37.00, 35.32),          ["Adıyaman"] = new(37.76, 38.28),
        ["Afyonkarahisar"] = new(38.76, 30.54), ["Ağrı"] = new(39.72, 43.05),
        ["Amasya"] = new(40.65, 35.83),         ["Ankara"] = new(39.93, 32.86),
        ["Antalya"] = new(36.89, 30.71),        ["Artvin"] = new(41.18, 41.82),
        ["Aydın"] = new(37.85, 27.85),          ["Balıkesir"] = new(39.65, 27.88),
        ["Bilecik"] = new(40.14, 29.98),        ["Bingöl"] = new(38.88, 40.50),
        ["Bitlis"] = new(38.40, 42.11),         ["Bolu"] = new(40.74, 31.61),
        ["Burdur"] = new(37.72, 30.29),         ["Bursa"] = new(40.19, 29.06),
        ["Çanakkale"] = new(40.15, 26.41),      ["Çankırı"] = new(40.60, 33.62),
        ["Çorum"] = new(40.55, 34.95),          ["Denizli"] = new(37.78, 29.09),
        ["Diyarbakır"] = new(37.91, 40.24),     ["Edirne"] = new(41.68, 26.56),
        ["Elazığ"] = new(38.67, 39.22),         ["Erzincan"] = new(39.75, 39.49),
        ["Erzurum"] = new(39.90, 41.27),        ["Eskişehir"] = new(39.78, 30.52),
        ["Gaziantep"] = new(37.07, 37.38),      ["Giresun"] = new(40.91, 38.39),
        ["Gümüşhane"] = new(40.46, 39.48),      ["Hakkari"] = new(37.58, 43.74),
        ["Hatay"] = new(36.20, 36.16),          ["Isparta"] = new(37.76, 30.55),
        ["Mersin"] = new(36.81, 34.64),         ["İstanbul"] = new(41.01, 28.98),
        ["İzmir"] = new(38.42, 27.14),          ["Kars"] = new(40.60, 43.10),
        ["Kastamonu"] = new(41.38, 33.78),      ["Kayseri"] = new(38.73, 35.48),
        ["Kırklareli"] = new(41.73, 27.22),     ["Kırşehir"] = new(39.15, 34.16),
        ["Kocaeli"] = new(40.77, 29.92),        ["Konya"] = new(37.87, 32.48),
        ["Kütahya"] = new(39.42, 29.98),        ["Malatya"] = new(38.35, 38.31),
        ["Manisa"] = new(38.61, 27.43),         ["Kahramanmaraş"] = new(37.58, 36.94),
        ["Mardin"] = new(37.31, 40.74),         ["Muğla"] = new(37.22, 28.36),
        ["Muş"] = new(38.75, 41.51),            ["Nevşehir"] = new(38.62, 34.71),
        ["Niğde"] = new(37.97, 34.68),          ["Ordu"] = new(40.98, 37.88),
        ["Rize"] = new(41.02, 40.52),           ["Sakarya"] = new(40.78, 30.40),
        ["Samsun"] = new(41.29, 36.33),         ["Siirt"] = new(37.93, 41.94),
        ["Sinop"] = new(42.03, 35.15),          ["Sivas"] = new(39.75, 37.02),
        ["Tekirdağ"] = new(40.98, 27.51),       ["Tokat"] = new(40.31, 36.55),
        ["Trabzon"] = new(41.00, 39.72),        ["Tunceli"] = new(39.11, 39.55),
        ["Şanlıurfa"] = new(37.16, 38.80),      ["Uşak"] = new(38.68, 29.41),
        ["Van"] = new(38.49, 43.38),            ["Yozgat"] = new(39.82, 34.81),
        ["Zonguldak"] = new(41.45, 31.79),      ["Aksaray"] = new(38.37, 34.03),
        ["Bayburt"] = new(40.26, 40.23),        ["Karaman"] = new(37.18, 33.22),
        ["Kırıkkale"] = new(39.85, 33.51),      ["Batman"] = new(37.89, 41.13),
        ["Şırnak"] = new(37.52, 42.46),         ["Bartın"] = new(41.63, 32.34),
        ["Ardahan"] = new(41.11, 42.70),        ["Iğdır"] = new(39.92, 44.04),
        ["Yalova"] = new(40.66, 29.27),         ["Karabük"] = new(41.20, 32.62),
        ["Kilis"] = new(36.72, 37.12),          ["Osmaniye"] = new(37.07, 36.25),
        ["Düzce"] = new(40.84, 31.16),
    };

    public static Koordinat Getir(string il) =>
        Konumlar.TryGetValue(il, out var k) ? k : throw new ArgumentException($"Bilinmeyen il: {il}", nameof(il));

    /// <summary>İki il merkezi arasındaki kuş uçuşu mesafe (km). Aynı il → 0.</summary>
    public static double MesafeKm(string il1, string il2) =>
        il1 == il2 ? 0 : Mesafe.HaversineKm(Getir(il1), Getir(il2));

    /// <summary>Verilen ile <paramref name="yaricapKm"/> içindeki iller ve uzaklıkları (kendisi 0 km).</summary>
    public static Dictionary<string, double> YaricapIcindekiIller(string merkezIl, double yaricapKm) =>
        Iller.Tumu
            .Select(il => (il, km: MesafeKm(merkezIl, il)))
            .Where(x => x.km <= yaricapKm)
            .ToDictionary(x => x.il, x => x.km);

    /// <summary>Tutarlılık kontrolü için: koordinatı tanımlı iller.</summary>
    public static IReadOnlyCollection<string> TanimliIller => Konumlar.Keys;
}

public static class Mesafe
{
    private const double DunyaYaricapiKm = 6371;

    /// <summary>
    /// Haversine formülü: küre üzerinde iki nokta arasındaki en kısa (büyük daire) mesafe.
    /// Düz harita üzerinde Pisagor kullanılsaydı, boylam derecesinin kuzeye gidildikçe
    /// kısalması hesaba katılmazdı.
    /// </summary>
    public static double HaversineKm(Koordinat a, Koordinat b)
    {
        static double Radyan(double derece) => derece * Math.PI / 180;

        var dEnlem = Radyan(b.Enlem - a.Enlem);
        var dBoylam = Radyan(b.Boylam - a.Boylam);

        var h = Math.Sin(dEnlem / 2) * Math.Sin(dEnlem / 2) +
                Math.Cos(Radyan(a.Enlem)) * Math.Cos(Radyan(b.Enlem)) *
                Math.Sin(dBoylam / 2) * Math.Sin(dBoylam / 2);

        return 2 * DunyaYaricapiKm * Math.Asin(Math.Sqrt(h));
    }
}
