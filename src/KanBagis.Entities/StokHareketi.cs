namespace KanBagis.Entities;

public enum HareketTuru
{
    Giris,
    Cikis,
}

/// <summary>Her neden yalnızca bir türle kullanılabilir (bkz. <see cref="StokHareketKurallari.Tur"/>).</summary>
public enum HareketNedeni
{
    // Giriş
    Bagis,          // yalnızca bağış kaydıyla oluşur, elle girilemez
    TransferGiris,  // başka merkezden gelen kan
    SayimFazlasi,   // sayımda fazla çıktı (düzeltme)

    // Çıkış
    Transfuzyon,    // hastaya kullanıldı
    TransferCikis,  // başka merkeze gönderildi
    Imha,           // son kullanma tarihi geçti / bozuldu
    SayimEksigi,    // sayımda eksik çıktı (düzeltme)
}

public static class StokHareketKurallari
{
    public static HareketTuru Tur(HareketNedeni neden) => neden switch
    {
        HareketNedeni.Bagis or HareketNedeni.TransferGiris or HareketNedeni.SayimFazlasi => HareketTuru.Giris,
        _ => HareketTuru.Cikis,
    };
}

/// <summary>
/// Stok giriş/çıkış kaydı. Kayıt DEĞİŞTİRİLMEZ ve SİLİNMEZ (denetim izi): hata varsa ters yönde
/// bir düzeltme hareketi girilir. Önceki ve sonraki stok değeri de saklanır, böylece geçmiş
/// herhangi bir anın stoğu hareketlerden yeniden hesaplanabilir.
/// </summary>
public class StokHareketi : IEntity
{
    public int Id { get; set; }
    public DateTime Tarih { get; set; }
    public HareketTuru Tur { get; set; }
    public HareketNedeni Neden { get; set; }
    public int Miktar { get; set; }
    public int OncekiStok { get; set; }
    public int SonrakiStok { get; set; }
    public string? Aciklama { get; set; }

    public int KanGrubuId { get; set; }
    public KanGrubu KanGrubu { get; set; } = null!;

    /// <summary>Hareketi yapan kurum (oturumdan alınır, formdan değil). Bağış hareketinde bağışın hastanesi.</summary>
    public int? HastaneId { get; set; }
    public Hastane? Hastane { get; set; }

    /// <summary>Hareketi giren kullanıcı. Bağış hareketinde boş olabilir.</summary>
    public int? KullaniciId { get; set; }
    public Kullanici? Kullanici { get; set; }

    /// <summary>Neden = Bagis ise o bağış (1-1), değilse boş.</summary>
    public int? BagisId { get; set; }
    public Bagis? Bagis { get; set; }
}

/// <summary>
/// Kritik eşik uyarısı. Bir kan grubunun aynı anda en fazla BİR açık uyarısı olur.
/// Stok eşiğin altına inince açılır, seviye değişirse güncellenir, stok eşiğin üstüne çıkınca kendiliğinden kapanır.
/// </summary>
public class StokUyarisi : IEntity
{
    public int Id { get; set; }
    public StokDurumu Seviye { get; set; }
    public int StokDegeri { get; set; }
    public int Esik { get; set; }
    public DateTime AcilisTarihi { get; set; }
    public DateTime GuncellemeTarihi { get; set; }
    public DateTime? KapanisTarihi { get; set; }

    public int KanGrubuId { get; set; }
    public KanGrubu KanGrubu { get; set; } = null!;

    public bool Acik => KapanisTarihi is null;
}
