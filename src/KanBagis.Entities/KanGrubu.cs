namespace KanBagis.Entities;

/// <summary>
/// Kan grubu iki bağımsız bilgiden oluşur: ABO grubu (A, B, AB, 0) ve Rh faktörü (+/-).
/// İkisi ayrı alanlarda tutulur; böylece uyumluluk ("kim kime kan verebilir")
/// metin karşılaştırmasıyla değil, kuralla hesaplanır. "Ad" yalnızca ekranda gösterim içindir.
/// </summary>
public class KanGrubu : IEntity
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public AboGrubu AboGrubu { get; set; }
    public bool RhPozitif { get; set; }

    public KanStok? Stok { get; set; }
    public ICollection<Bagisci> Bagiscilar { get; set; } = new List<Bagisci>();
    public ICollection<KanTalebi> Talepler { get; set; } = new List<KanTalebi>();
}
