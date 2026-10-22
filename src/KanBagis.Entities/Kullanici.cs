namespace KanBagis.Entities;

/// <summary>
/// Sisteme giriş yapan hesap. Rolüne göre ya bir bağışçıya ya da bir hastaneye (kurum) bağlıdır.
/// Parola asla düz metin saklanmaz; yalnızca tuzlu hash'i tutulur.
/// </summary>
public class Kullanici : IEntity
{
    public int Id { get; set; }
    public string Eposta { get; set; } = string.Empty;
    public string ParolaHash { get; set; } = string.Empty;
    public KullaniciRolu Rol { get; set; }

    public int BasarisizGirisSayisi { get; set; }
    public DateTime? KilitBitisTarihi { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
    public DateTime? SonGirisTarihi { get; set; }

    // Rol = Bagisci ise dolu (1-1)
    public int? BagisciId { get; set; }
    public Bagisci? Bagisci { get; set; }

    // Rol = Kurum ise dolu (bir hastanenin birden çok personel hesabı olabilir: 1-N)
    public int? HastaneId { get; set; }
    public Hastane? Hastane { get; set; }
}
