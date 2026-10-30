using System.ComponentModel.DataAnnotations;
using KanBagis.Business.Bagiscilar;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KanBagis.Web.Models;

/// <summary>
/// Profil düzenleme formu. Bilerek bir "BagisciId" alanı YOK: hangi kaydın güncelleneceği
/// formdan değil, oturumdan belirlenir (sahiplik kontrolü).
/// </summary>
public class BagisciProfilViewModel
{
    [Required(ErrorMessage = "Telefon zorunludur.")]
    [RegularExpression(@"^0\d{10}$", ErrorMessage = "Telefon 0 ile başlayan 11 hane olmalıdır.")]
    public string Telefon { get; set; } = string.Empty;

    [Required(ErrorMessage = "İl seçin.")]
    [Display(Name = "İl")]
    public string Il { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kilo zorunludur.")]
    [Range(BagisciDogrulama.MinimumKilo, BagisciDogrulama.MaksimumKilo, ErrorMessage = "Kilo {1}–{2} arasında olmalıdır.")]
    [Display(Name = "Kilo (kg)")]
    public int? Kilo { get; set; }

    [Required(ErrorMessage = "Kan grubu seçin.")]
    [Display(Name = "Kan grubu")]
    public int? KanGrubuId { get; set; }

    // ---- Yalnızca gösterim (formdan geri okunmaz) ----
    [ValidateNever] public string AdSoyad { get; set; } = string.Empty;
    [ValidateNever] public string TcMaskeli { get; set; } = string.Empty;
    [ValidateNever] public DateOnly DogumTarihi { get; set; }
    [ValidateNever] public string Cinsiyet { get; set; } = string.Empty;
    [ValidateNever] public bool KanGrubuKilitli { get; set; }
    [ValidateNever] public List<SelectListItem> KanGruplari { get; set; } = [];
}
