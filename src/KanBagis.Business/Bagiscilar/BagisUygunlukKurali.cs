using KanBagis.Entities;

namespace KanBagis.Business.Bagiscilar;

public record UygunlukSonucu(bool Uygun, IReadOnlyList<string> Nedenler, DateOnly? SonrakiUygunTarih);

/// <summary>
/// Bir bağışçının bugün kan bağışı yapıp yapamayacağına karar verir.
/// Kurallar Türk Kızılay'ın tam kan bağışı ölçütlerine dayanır:
///   - Yaş 18–65 arası
///   - Kilo en az 50 kg
///   - İki bağış arası en az 90 gün (erkek) / 120 gün (kadın)
///     Kadınlarda süre daha uzundur, çünkü vücudun demir depoları daha geç yenilenir.
/// Uygun değilse TÜM nedenler listelenir; kullanıcı neyi beklemesi gerektiğini görür.
/// </summary>
public sealed class BagisUygunlukKurali(TimeProvider zaman)
{
    public const int MinimumYas = 18;
    public const int MaksimumYas = 65;
    public const int MinimumKilo = 50;
    public const int ErkekBagisAraligiGun = 90;
    public const int KadinBagisAraligiGun = 120;

    public UygunlukSonucu Degerlendir(Bagisci bagisci, DateTime? sonBagisTarihi)
    {
        var bugun = DateOnly.FromDateTime(zaman.GetUtcNow().UtcDateTime);
        var nedenler = new List<string>();
        DateOnly? sonrakiTarih = null;

        var yas = BagisciDogrulama.YasHesapla(bagisci.DogumTarihi, bugun);
        if (yas < MinimumYas)
        {
            nedenler.Add($"Bağış için en az {MinimumYas} yaşında olmalısınız.");
            sonrakiTarih = bagisci.DogumTarihi.AddYears(MinimumYas);
        }
        else if (yas > MaksimumYas)
        {
            nedenler.Add($"{MaksimumYas} yaşından sonra bağış kabul edilmemektedir.");
        }

        if (bagisci.Kilo < MinimumKilo)
            nedenler.Add($"Bağış için en az {MinimumKilo} kg olmalısınız.");

        if (sonBagisTarihi is { } son)
        {
            var aralik = BagisAraligiGun(bagisci.Cinsiyet);
            var uygunTarih = DateOnly.FromDateTime(son).AddDays(aralik);
            if (uygunTarih > bugun)
            {
                nedenler.Add($"Son bağışınızın üzerinden en az {aralik} gün geçmelidir.");
                sonrakiTarih = sonrakiTarih is null || uygunTarih > sonrakiTarih ? uygunTarih : sonrakiTarih;
            }
        }

        // Yaş üst sınırı ya da kilo gibi zamanla kendiliğinden düzelmeyen bir neden varsa tarih verilmez.
        var kaliciEngel = yas > MaksimumYas || bagisci.Kilo < MinimumKilo;
        return new UygunlukSonucu(nedenler.Count == 0, nedenler, kaliciEngel ? null : sonrakiTarih);
    }

    public static int BagisAraligiGun(Cinsiyet cinsiyet) =>
        cinsiyet == Cinsiyet.Kadin ? KadinBagisAraligiGun : ErkekBagisAraligiGun;
}
