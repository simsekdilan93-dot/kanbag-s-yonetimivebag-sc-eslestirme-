using KanBagis.Business.Paneller;
using KanBagis.Entities;
using KanBagis.Web.Guvenlik;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KanBagis.Web.Controllers;

/// <summary>Yalnızca "Kurum" (hastane) rolündeki kullanıcılar erişebilir.</summary>
[Authorize(Roles = Roller.Kurum)]
public class KurumPanelController(IPanelService paneller) : Controller
{
    public async Task<IActionResult> Index()
    {
        var hastaneId = User.SayiGetir(KullaniciClaimleri.HastaneId);
        if (hastaneId is null) return Forbid();

        var panel = await paneller.KurumPaneliAsync(hastaneId.Value);
        return panel is null ? NotFound() : View(panel);
    }
}
