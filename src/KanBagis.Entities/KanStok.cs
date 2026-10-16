namespace KanBagis.Entities;

public class KanStok : IEntity
{
    public int Id { get; set; }
    public int UniteSayisi { get; set; }
    public DateTime GuncellemeTarihi { get; set; }

    public int KanGrubuId { get; set; }
    public KanGrubu KanGrubu { get; set; } = null!;
}
