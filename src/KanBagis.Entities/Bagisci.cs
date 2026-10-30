namespace KanBagis.Entities;

public class Bagisci : IEntity
{
    public int Id { get; set; }
    public string TcKimlikNo { get; set; } = string.Empty;
    public string Ad { get; set; } = string.Empty;
    public string Soyad { get; set; } = string.Empty;
    public string Telefon { get; set; } = string.Empty;
    public string? Eposta { get; set; }
    public DateOnly DogumTarihi { get; set; }
    public Cinsiyet Cinsiyet { get; set; }
    public int Kilo { get; set; }

    /// <summary>İl adı; 81 il listesinden seçilir (eşleştirme bu alana göre yapılır).</summary>
    public string Sehir { get; set; } = string.Empty;

    public int KanGrubuId { get; set; }
    public KanGrubu KanGrubu { get; set; } = null!;

    public ICollection<Bagis> Bagislar { get; set; } = new List<Bagis>();
    public ICollection<Bildirim> Bildirimler { get; set; } = new List<Bildirim>();
}
