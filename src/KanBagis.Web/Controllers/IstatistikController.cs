using KanBagis.Business.Istatistik;
using KanBagis.Entities;
using KanBagis.Web.Guvenlik;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KanBagis.Web.Controllers;

/// <summary>
/// İstatistik panosu (Hafta 12). Yalnızca kurum personeli; yalnızca TOPLU sayılar gösterilir (KVKK: bağışçı adı yok).
/// "Yalnızca hastanem" süzgecinde hastane, formdan değil OTURUMDAN alınır.
/// </summary>
[Authorize(Roles = Roller.Kurum)]
public class IstatistikController(IIstatistikService istatistik) : Controller
{
    public async Task<IActionResult> Index(int gun = 90, bool hastanem = false, bool sentetik = false)
    {
        var hastaneId = User.SayiGetir(KullaniciClaimleri.HastaneId);
        if (hastaneId is null) return Forbid();
        return View(await istatistik.PanoAsync(new PanoFiltresi(gun, hastanem, sentetik), hastaneId.Value));
    }

    /// <summary>Dışa aktarma: aynı süzgeçle CSV dosyası.</summary>
    public async Task<IActionResult> Csv(int gun = 90, bool hastanem = false, bool sentetik = false)
    {
        var hastaneId = User.SayiGetir(KullaniciClaimleri.HastaneId);
        if (hastaneId is null) return Forbid();
        var icerik = await istatistik.CsvAsync(new PanoFiltresi(gun, hastanem, sentetik), hastaneId.Value);
        return File(icerik, "text/csv; charset=utf-8", $"kanbagis-istatistik-{DateTime.Now:yyyyMMdd}-{gun}gun.csv");
    }
}
