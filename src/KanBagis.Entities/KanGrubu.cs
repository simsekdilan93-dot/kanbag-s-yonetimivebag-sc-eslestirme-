namespace KanBagis.Entities;

public class KanGrubu : IEntity
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;

    public KanStok? Stok { get; set; }
    public ICollection<Bagisci> Bagiscilar { get; set; } = new List<Bagisci>();
    public ICollection<KanTalebi> Talepler { get; set; } = new List<KanTalebi>();
}
