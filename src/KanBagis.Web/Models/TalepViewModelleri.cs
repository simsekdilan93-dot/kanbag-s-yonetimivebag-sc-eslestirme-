using System.ComponentModel.DataAnnotations;
using KanBagis.Business.Talepler;
using KanBagis.Entities;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KanBagis.Web.Models;

/// <summary>Talep formu. Hastane formdan değil oturumdan belirlenir (sahiplik).</summary>
public class TalepOlusturViewModel
{
    [Required(ErrorMessage = "Kan grubu seçin.")]
    [Display(Name = "Kan grubu")]
    public int? KanGrubuId { get; set; }

    [Required(ErrorMessage = "Ünite sayısı zorunludur.")]
    [Range(AciliyetKurallari.MinimumUnite, AciliyetKurallari.MaksimumUnite, ErrorMessage = "Ünite sayısı {1}–{2} arasında olmalıdır.")]
    [Display(Name = "Ünite sayısı")]
    public int? UniteSayisi { get; set; } = 1;

    [Required(ErrorMessage = "Aciliyet seçin.")]
    public Aciliyet? Aciliyet { get; set; } = Entities.Aciliyet.Normal;

    [StringLength(TalepService.MaksimumAciklamaUzunlugu, ErrorMessage = "Açıklama en fazla {1} karakter olabilir.")]
    [Display(Name = "Açıklama (isteğe bağlı)")]
    public string? Aciklama { get; set; }

    [ValidateNever] public List<SelectListItem> KanGruplari { get; set; } = [];
}
