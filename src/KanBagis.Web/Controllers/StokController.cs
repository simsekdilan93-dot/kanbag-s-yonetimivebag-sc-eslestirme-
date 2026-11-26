using System.Security.Claims;
using KanBagis.Business.Stok;
using KanBagis.DataAccess.Repositories;
using KanBagis.Entities;
using KanBagis.Web.Guvenlik;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KanBagis.Web.Controllers;

/// <summary>
/// Stok Takip ve Uyarı Modülü (Hafta 11). Yalnızca kurum personeli: stok görüntüleme, giriş/çıkış, eşik ayarı.
/// Hareketi yapan kullanıcı ve hastane OTURUMDAN alınır; formdan gelen bir kimlik bilgisi kullanılmaz.
/// </summary>
[Authorize(Roles = Roller.Kurum)]
public class StokController(IStokService stok) : Controller
{
    public async Task<IActionResult> Index() => View(await stok.OzetGetirAsync());

    [HttpPost]
    public async Task<IActionResult> Hareket(StokHareketIstegi istek)
    {
        var yapan = Yapan();
        if (yapan is null) return Forbid();

        var cevap = await stok.HareketGirAsync(istek, yapan);
        if (cevap.Basarili)
        {
            TempData[cevap.Uyari is UyariDegisimi.Acildi or UyariDegisimi.SeviyeDegisti ? "Hata" : "Mesaj"] = cevap.Mesaj;
            return RedirectToAction(nameof(Index));
        }

        // Hata ya da kritik onayı gerekiyor: form, girilen değerlerle yeniden gösterilir.
        ViewBag.Istek = istek;
        ViewBag.Hatalar = cevap.Hatalar;
        ViewBag.OnayGerekli = cevap.OnayGerekli;
        return View(nameof(Index), await stok.OzetGetirAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Esik(int kanGrubuId, int kritikEsik, int uyariEsigi)
    {
        var cevap = await stok.EsikGuncelleAsync(kanGrubuId, kritikEsik, uyariEsigi);
        TempData[cevap.Basarili ? "Mesaj" : "Hata"] = cevap.Basarili ? cevap.Mesaj : string.Join(" ", cevap.Hatalar);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Hareketler(int? kanGrubuId, HareketTuru? tur, bool yalnizcaBenim = false)
    {
        var hastaneId = User.SayiGetir(KullaniciClaimleri.HastaneId);
        ViewBag.KanGrubuId = kanGrubuId;
        ViewBag.Tur = tur;
        ViewBag.YalnizcaBenim = yalnizcaBenim;
        return View(await stok.HareketlerAsync(new HareketFiltresi(kanGrubuId, yalnizcaBenim ? hastaneId : null, tur, 200)));
    }

    public async Task<IActionResult> Uyarilar() => View(await stok.UyariGecmisiAsync());

    private IslemYapan? Yapan()
    {
        var kullaniciId = User.SayiGetir(ClaimTypes.NameIdentifier);
        var hastaneId = User.SayiGetir(KullaniciClaimleri.HastaneId);
        return kullaniciId is null || hastaneId is null ? null : new IslemYapan(kullaniciId.Value, hastaneId.Value);
    }
}
