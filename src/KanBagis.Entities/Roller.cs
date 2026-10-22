namespace KanBagis.Entities;

public enum KullaniciRolu { Bagisci, Kurum }

/// <summary>[Authorize(Roles = ...)] içinde kullanılacak rol adları (enum adlarıyla aynı).</summary>
public static class Roller
{
    public const string Bagisci = nameof(KullaniciRolu.Bagisci);
    public const string Kurum = nameof(KullaniciRolu.Kurum);
}
