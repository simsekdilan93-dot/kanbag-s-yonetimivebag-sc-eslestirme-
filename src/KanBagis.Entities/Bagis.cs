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
}
