using System.ComponentModel.DataAnnotations;

namespace KanBagis.Entities;

public enum Cinsiyet
{
    [Display(Name = "Kadın")] Kadin,
    [Display(Name = "Erkek")] Erkek,
}

/// <summary>ABO kan grubu. "O" harfi Türkçede "0 (sıfır)" grubu olarak bilinir.</summary>
public enum AboGrubu { A, B, AB, O }

public enum Aciliyet { Normal, Acil }

public enum TalepDurumu { Acik, Karsilandi, Iptal }

public enum BildirimYaniti { Beklemede, Kabul, Red }

/// <summary>Bildirimin telefona (anlık bildirim / SMS) gönderim durumu. Site içi bildirim her zaman vardır.</summary>
public enum DisKanalDurumu
{
    Yok,         // bağışçının telefon bildirimi kapalı ya da servis yapılandırılmamış
    Kuyrukta,    // arka planda gönderilmeyi bekliyor (ya da yeniden denenecek)
    Gonderildi,
    Basarisiz,   // tüm denemeler başarısız; bağışçı yine de site içinden görebilir
}
