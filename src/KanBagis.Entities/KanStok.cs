namespace KanBagis.Entities;

/// <summary>
/// Bir kan grubunun bölgedeki stoğu (Hafta 11: Stok Takip ve Uyarı Modülü).
/// Stok yalnızca <see cref="StokHareketi"/> ile değişir; her değişiklik giriş/çıkış olarak kaydedilir.
/// İki eşik vardır: stok UyariEsigi'ne inince "Düşük", KritikEsik'e inince "Kritik" uyarısı açılır.
/// </summary>
public class KanStok : IEntity
{
    public int Id { get; set; }
    public int UniteSayisi { get; set; }
    public DateTime GuncellemeTarihi { get; set; }

    /// <summary>Stok bu değere eşit ya da altındaysa KRİTİK.</summary>
    public int KritikEsik { get; set; }

    /// <summary>Stok bu değere eşit ya da altındaysa DÜŞÜK (KritikEsik ≤ UyariEsigi).</summary>
    public int UyariEsigi { get; set; }

    public int KanGrubuId { get; set; }
    public KanGrubu KanGrubu { get; set; } = null!;

    public StokDurumu Durum => DurumHesapla(UniteSayisi, KritikEsik, UyariEsigi);

    /// <summary>Tek yerde tanımlı kural: sayfalar, uyarılar ve testler aynı hesabı kullanır.</summary>
    public static StokDurumu DurumHesapla(int stok, int kritikEsik, int uyariEsigi) =>
        stok <= kritikEsik ? StokDurumu.Kritik
        : stok <= uyariEsigi ? StokDurumu.Dusuk
        : StokDurumu.Normal;
}

public enum StokDurumu
{
    Normal,
    Dusuk,
    Kritik,
}
