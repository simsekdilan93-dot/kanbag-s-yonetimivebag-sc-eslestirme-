namespace KanBagis.Entities;

/// <summary>
/// "Parolamı unuttum" ile e-postaya gönderilen tek kullanımlık kod.
/// Kodun KENDİSİ saklanmaz, yalnızca tuzlu hash'i (parola gibi): veritabanı ele geçirilse bile kod okunamaz.
/// Kod kısa süre geçerlidir, sınırlı sayıda denenebilir ve bir kez kullanılabilir.
/// </summary>
public class ParolaSifirlamaKodu : IEntity
{
    public int Id { get; set; }
    public string KodHash { get; set; } = string.Empty;
    public DateTime OlusturmaTarihi { get; set; }
    public DateTime SonKullanmaTarihi { get; set; }
    public int DenemeSayisi { get; set; }

    /// <summary>Kod kullanıldığında ya da yeni kod istendiğinde/çok denendiğinde doldurulur; dolu kod artık geçersizdir.</summary>
    public DateTime? KapanisTarihi { get; set; }

    public int KullaniciId { get; set; }
    public Kullanici Kullanici { get; set; } = null!;
}
