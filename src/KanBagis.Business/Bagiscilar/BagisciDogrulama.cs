using System.Text.RegularExpressions;
using KanBagis.Business.Tanimlar;

namespace KanBagis.Business.Bagiscilar;

/// <summary>
/// Bağışçı verisi için doğrulama kuralları. Kayıt ve profil güncelleme aynı kuralları kullanır.
/// Formdaki doğrulama kullanıcıya kolaylık içindir; asıl denetim burada, sunucuda yapılır.
/// </summary>
public static partial class BagisciDogrulama
{
    public const int KayitIcinMinimumYas = 18;
    public const int MinimumKilo = 30;   // veri girişi için mantıklı sınır (bağış için 50 kg gerekir)
    public const int MaksimumKilo = 250;

    [GeneratedRegex(@"^0\d{10}$")]
    private static partial Regex TelefonDeseni();

    /// <summary>
    /// TC kimlik numarası algoritması:
    /// 1) 11 hane, hepsi rakam, ilk hane 0 olamaz.
    /// 2) 10. hane = ((1,3,5,7,9. hanelerin toplamı) × 7 − (2,4,6,8. hanelerin toplamı)) mod 10
    /// 3) 11. hane = (ilk 10 hanenin toplamı) mod 10
    /// Bu kontrol yazım hatalarını yakalar; kişinin gerçekten var olduğunu kanıtlamaz.
    /// </summary>
    public static bool TcKimlikNoGecerliMi(string? tc)
    {
        if (tc is null || tc.Length != 11 || !tc.All(char.IsAsciiDigit) || tc[0] == '0')
            return false;

        var d = tc.Select(c => c - '0').ToArray();
        var tekler = d[0] + d[2] + d[4] + d[6] + d[8];
        var ciftler = d[1] + d[3] + d[5] + d[7];

        var onuncu = ((tekler * 7 - ciftler) % 10 + 10) % 10;   // negatif sonuca karşı +10
        var onbirinci = d.Take(10).Sum() % 10;

        return d[9] == onuncu && d[10] == onbirinci;
    }

    public static bool TelefonGecerliMi(string? telefon) =>
        telefon is not null && TelefonDeseni().IsMatch(telefon);

    public static bool KiloGecerliMi(int kilo) => kilo is >= MinimumKilo and <= MaksimumKilo;

    public static int YasHesapla(DateOnly dogumTarihi, DateOnly bugun)
    {
        var yas = bugun.Year - dogumTarihi.Year;
        if (dogumTarihi > bugun.AddYears(-yas)) yas--;   // bu yıl doğum günü henüz gelmediyse
        return yas;
    }

    /// <summary>Kayıt ve güncellemede ortak alanların ilk hatasını döndürür; hata yoksa null.</summary>
    public static string? OrtakAlanHatasi(string telefon, string il, int kilo)
    {
        if (!TelefonGecerliMi(telefon)) return "Telefon 0 ile başlayan 11 hane olmalıdır.";
        if (!Iller.GecerliMi(il)) return "Geçerli bir il seçin.";
        if (!KiloGecerliMi(kilo)) return $"Kilo {MinimumKilo}–{MaksimumKilo} arasında olmalıdır.";
        return null;
    }
}
