namespace KanBagis.Entities;

public class Bagis : IEntity
{
    public int Id { get; set; }
    public DateTime BagisTarihi { get; set; }
    public int UniteSayisi { get; set; } = 1;

    public int BagisciId { get; set; }
    public Bagisci Bagisci { get; set; } = null!;

    public int HastaneId { get; set; }
    public Hastane Hastane { get; set; } = null!;

    /// <summary>Bağış bir kan talebine yanıt olarak yapıldıysa o talep; gönüllü bağışta boş.</summary>
    public int? KanTalebiId { get; set; }
    public KanTalebi? KanTalebi { get; set; }
}
