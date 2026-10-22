using System.ComponentModel.DataAnnotations;

namespace KanBagis.Entities;

public enum Cinsiyet
{
    [Display(Name = "Kadın")] Kadin,
    [Display(Name = "Erkek")] Erkek,
}

public enum Aciliyet { Normal, Acil }

public enum TalepDurumu { Acik, Karsilandi, Iptal }

public enum BildirimYaniti { Beklemede, Kabul, Red }
