using KanBagis.Business.Tahmin;
using KanBagis.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KanBagis.Web.Controllers;

/// <summary>
/// Talep ve stok tahmini (Hafta 10, yapay zekâ bileşeni). Yalnızca kurum kullanıcıları görür:
/// tahmin toplu stok verisine dayanır, bağışçıların kişisel bilgisi kullanılmaz.
/// </summary>
[Authorize(Roles = Roller.Kurum)]
public class TahminController(ITahminService tahminler) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.YapayZeka = tahminler.YapayZekaKullanilabilir;
        return View(await tahminler.SonTahminAsync());
    }

    /// <summary>Yeni tahmin üretir. Yapay zekâ çağrısı uzun sürebileceği için yalnızca butonla tetiklenir.</summary>
    [HttpPost]
    public async Task<IActionResult> Calistir(CancellationToken iptal)
    {
        var sonuc = await tahminler.CalistirAsync(iptal);
        TempData["Mesaj"] = $"Tahmin güncellendi: {RiskMetinleri.Kaynak(sonuc.Kaynak)}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> SentetikGecmis()
    {
        var adet = await tahminler.SentetikGecmisUretAsync();
        TempData["Mesaj"] = adet > 0
            ? $"Son 12 haftaya {adet} sentetik (gerçek olmayan) talep kaydı eklendi. Şimdi tahmini çalıştırabilirsiniz."
            : "Sentetik geçmiş zaten var; yeniden eklenmedi.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>AI kullanım günlüğü: her çalıştırmanın istemi, ham çıktısı ve doğrulama notları.</summary>
    public async Task<IActionResult> Gunluk() => View(await tahminler.GunlukAsync(30));

    public async Task<IActionResult> Kayit(int id)
    {
        var kayit = await tahminler.KayitGetirAsync(id);
        return kayit is null ? NotFound() : View(kayit);
    }
}
