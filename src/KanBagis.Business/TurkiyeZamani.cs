namespace KanBagis.Business;

/// <summary>
/// Veritabanında tüm zamanlar UTC saklanır; ama "bugün", "yaş", "90 gün geçti mi" gibi TAKVİM GÜNÜ soruları
/// kullanıcının yaşadığı yere göre (Türkiye, UTC+3) cevaplanmalıdır. Aksi hâlde her gece 00:00–03:00 arasında
/// tarih bir gün geride kalır (Hata H13-01, docs/test/Hata-Kayitlari.md).
/// </summary>
public static class TurkiyeZamani
{
    /// <summary>Türkiye 2016'dan beri yaz/kış saati uygulamaz, sürekli UTC+3. Saat dilimi veritabanı bulunamazsa bu kullanılır.</summary>
    private static readonly TimeSpan SabitFark = TimeSpan.FromHours(3);

    private static readonly TimeZoneInfo? Dilim = Bul();

    private static TimeZoneInfo? Bul()
    {
        foreach (var id in new[] { "Europe/Istanbul", "Turkey Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return null;
    }

    /// <summary>UTC anı Türkiye'deki takvim gününe çevirir.</summary>
    public static DateOnly Tarih(DateTime utc)
    {
        var u = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        var yerel = Dilim is null ? u + SabitFark : TimeZoneInfo.ConvertTimeFromUtc(u, Dilim);
        return DateOnly.FromDateTime(yerel);
    }

    public static DateOnly Bugun(TimeProvider zaman) => Tarih(zaman.GetUtcNow().UtcDateTime);
}
