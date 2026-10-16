namespace KanBagis.Entities;

/// <summary>Bagisci ile KanTalebi arasındaki N-N ilişkinin ara tablosu.</summary>
public class Bildirim : IEntity
{
    public int Id { get; set; }
    public string Mesaj { get; set; } = string.Empty;
    public DateTime GonderimTarihi { get; set; }
    public bool Okundu { get; set; }
    public BildirimYaniti Yanit { get; set; } = BildirimYaniti.Beklemede;

    public int BagisciId { get; set; }
    public Bagisci Bagisci { get; set; } = null!;

    public int KanTalebiId { get; set; }
    public KanTalebi KanTalebi { get; set; } = null!;
}
