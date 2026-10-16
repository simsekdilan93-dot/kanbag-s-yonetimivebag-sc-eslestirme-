namespace KanBagis.Entities;

public class Hastane : IEntity
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string Sehir { get; set; } = string.Empty;
    public string Telefon { get; set; } = string.Empty;

    public ICollection<KanTalebi> Talepler { get; set; } = new List<KanTalebi>();
    public ICollection<Bagis> Bagislar { get; set; } = new List<Bagis>();
}
