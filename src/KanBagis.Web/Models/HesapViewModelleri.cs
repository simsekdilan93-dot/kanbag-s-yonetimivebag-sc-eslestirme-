using System.ComponentModel.DataAnnotations;
using KanBagis.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KanBagis.Web.Models;

public class GirisViewModel
{
    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    [Display(Name = "E-posta")]
    public string Eposta { get; set; } = string.Empty;

    [Required(ErrorMessage = "Parola zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Parola")]
    public string Parola { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public abstract class KayitViewModelTemeli
{
    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    [StringLength(100)]
    [Display(Name = "E-posta")]
    public string Eposta { get; set; } = string.Empty;

    [Required(ErrorMessage = "Parola zorunludur.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Parola en az 8 karakter olmalıdır.")]
    [DataType(DataType.Password)]
    [Display(Name = "Parola")]
    public string Parola { get; set; } = string.Empty;

    [Required(ErrorMessage = "Parola tekrarı zorunludur.")]
    [Compare(nameof(Parola), ErrorMessage = "Parolalar eşleşmiyor.")]
    [DataType(DataType.Password)]
    [Display(Name = "Parola (tekrar)")]
    public string ParolaTekrar { get; set; } = string.Empty;
}

public class BagisciKayitViewModel : KayitViewModelTemeli
{
    [Required(ErrorMessage = "TC kimlik no zorunludur.")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "TC kimlik no 11 haneli olmalıdır.")]
    [Display(Name = "TC kimlik no")]
    public string TcKimlikNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ad zorunludur."), StringLength(50)]
    public string Ad { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad zorunludur."), StringLength(50)]
    public string Soyad { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefon zorunludur.")]
    [RegularExpression(@"^0\d{10}$", ErrorMessage = "Telefon 0 ile başlayan 11 hane olmalıdır.")]
    public string Telefon { get; set; } = string.Empty;

    [Required(ErrorMessage = "Doğum tarihi zorunludur.")]
    [DataType(DataType.Date)]
    [Display(Name = "Doğum tarihi")]
    public DateOnly? DogumTarihi { get; set; }

    [Required(ErrorMessage = "Cinsiyet seçin.")]
    public Cinsiyet? Cinsiyet { get; set; }

    [Required(ErrorMessage = "Şehir zorunludur."), StringLength(30)]
    [Display(Name = "Şehir")]
    public string Sehir { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kan grubu seçin.")]
    [Display(Name = "Kan grubu")]
    public int? KanGrubuId { get; set; }

    public List<SelectListItem> KanGruplari { get; set; } = [];
}

public class KurumKayitViewModel : KayitViewModelTemeli
{
    [Required(ErrorMessage = "Hastane adı zorunludur."), StringLength(100)]
    [Display(Name = "Hastane adı")]
    public string HastaneAdi { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şehir zorunludur."), StringLength(30)]
    [Display(Name = "Şehir")]
    public string Sehir { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefon zorunludur.")]
    [RegularExpression(@"^0\d{10}$", ErrorMessage = "Telefon 0 ile başlayan 11 hane olmalıdır.")]
    public string Telefon { get; set; } = string.Empty;
}
