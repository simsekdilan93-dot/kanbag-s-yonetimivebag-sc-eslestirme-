using KanBagis.Business.Talepler;
using KanBagis.Business.Tanimlar;
using KanBagis.Entities;
using KanBagis.Web.Guvenlik;
using KanBagis.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KanBagis.Web.Controllers;

/// <summary>
/// Kurumun kan talebi işlemleri. Yalnızca "Kurum" rolü erişebilir; hastane kimliği
/// oturumdan okunur ve iş katmanı her işlemde talebin bu hastaneye ait olduğunu kontrol eder.
/// </summary>
[Authorize(Roles = Roller.Kurum)]
public class KurumTalepController(ITalepService talepler, ITanimService tanimlar) : Controller
{
    private int? OturumdakiHastaneId => User.SayiGetir(KullaniciClaimleri.HastaneId);

    [HttpGet]
    public async Task<IActionResult> Yeni() =>
        OturumdakiHastaneId is null ? Forbid() : View(await ListeleriDoldurAsync(new TalepOlusturViewModel()));

    [HttpPost]
    public async Task<IActionResult> Yeni(TalepOlusturViewModel model)
    {
        if (OturumdakiHastaneId is not { } hastaneId) return Forbid();
        if (!ModelState.IsValid) return View(await ListeleriDoldurAsync(model));

        var sonuc = await talepler.OlusturAsync(hastaneId, new TalepOlusturmaIstegi(
            model.KanGrubuId!.Value, model.UniteSayisi!.Value, model.Aciliyet!.Value, model.Aciklama));

        if (!sonuc.Basarili)
        {
            ModelState.AddModelError(string.Empty, sonuc.Hata!);
            ViewData["AcikTalepId"] = sonuc.TalepId;
            return View(await ListeleriDoldurAsync(model));
        }

        TempData["Mesaj"] = sonuc.BildirimSayisi > 0
            ? $"Talep oluşturuldu. {sonuc.BildirimSayisi} uygun bağışçıya bildirim gönderildi."
            : "Talep oluşturuldu, ancak arama alanında şu an uygun bağışçı bulunamadı.";
        return RedirectToAction(nameof(Detay), new { id = sonuc.TalepId });
    }

    [HttpGet]
    public async Task<IActionResult> Detay(int id)
    {
        if (OturumdakiHastaneId is not { } hastaneId) return Forbid();

        var detay = await talepler.DetayGetirAsync(hastaneId, id);
        // Başka hastanenin talebi de "bulunamadı" döner: talebin var olduğu bile belli edilmez.
        return detay is null ? NotFound() : View(detay);
    }

    [HttpPost]
    public async Task<IActionResult> DahaFazlaBildir(int id)
    {
        if (OturumdakiHastaneId is not { } hastaneId) return Forbid();

        var sonuc = await talepler.DahaFazlaBildirAsync(hastaneId, id);
        TempData[sonuc.Basarili ? "Mesaj" : "Hata"] = sonuc.Basarili
            ? $"{sonuc.BildirimSayisi} bağışçıya daha bildirim gönderildi."
            : sonuc.Hata;
        return RedirectToAction(nameof(Detay), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Kapat(int id, TalepDurumu durum)
    {
        if (OturumdakiHastaneId is not { } hastaneId) return Forbid();

        var sonuc = await talepler.KapatAsync(hastaneId, id, durum);
        TempData[sonuc.Basarili ? "Mesaj" : "Hata"] = sonuc.Basarili
            ? (durum == TalepDurumu.Karsilandi ? "Talep karşılandı olarak kapatıldı." : "Talep iptal edildi.")
            : sonuc.Hata;
        return RedirectToAction(nameof(Detay), new { id });
    }

    private async Task<TalepOlusturViewModel> ListeleriDoldurAsync(TalepOlusturViewModel model)
    {
        model.KanGruplari = (await tanimlar.KanGruplariAsync())
            .Select(k => new SelectListItem(k.Ad, k.Id.ToString()))
            .ToList();
        return model;
    }
}
