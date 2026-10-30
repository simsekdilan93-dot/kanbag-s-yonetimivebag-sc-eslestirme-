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
