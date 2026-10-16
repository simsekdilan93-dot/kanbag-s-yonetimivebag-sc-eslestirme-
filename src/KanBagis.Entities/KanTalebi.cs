namespace KanBagis.Entities;

public class KanTalebi : IEntity
{
    public int Id { get; set; }
    public int UniteSayisi { get; set; }
    public Aciliyet Aciliyet { get; set; }
    public TalepDurumu Durum { get; set; } = TalepDurumu.Acik;
    public DateTime OlusturmaTarihi { get; set; }

    public int HastaneId { get; set; }
    public Hastane Hastane { get; set; } = null!;

    public int KanGrubuId { get; set; }
    public KanGrubu KanGrubu { get; set; } = null!;

    public ICollection<Bildirim> Bildirimler { get; set; } = new List<Bildirim>();
}
