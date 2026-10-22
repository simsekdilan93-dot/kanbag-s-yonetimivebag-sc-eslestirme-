using KanBagis.Business.Paneller;
using KanBagis.Entities;
using KanBagis.Web.Guvenlik;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KanBagis.Web.Controllers;

/// <summary>
/// Yalnızca "Bagisci" rolündeki kullanıcılar erişebilir. Kontrol SUNUCUDA yapılır:
/// - Giriş yapmamış kullanıcı → /Hesap/Giris sayfasına yönlendirilir.
/// - Giriş yapmış ama rolü farklı (ör. Kurum) → /Hesap/ErisimEngellendi (403).
/// Menüde bağlantının gizlenmesi tek başına koruma değildir; adres elle yazılsa bile bu filtre çalışır.
/// </summary>
[Authorize(Roles = Roller.Bagisci)]
public class BagisciPanelController(IPanelService paneller) : Controller
{
    public async Task<IActionResult> Index()
    {
        // Bağışçı Id'si URL'den değil, imzalı oturum çerezinden okunur:
        // kullanıcı adresi değiştirerek başkasının paneline ulaşamaz.
        var bagisciId = User.SayiGetir(KullaniciClaimleri.BagisciId);
        if (bagisciId is null) return Forbid();

        var panel = await paneller.BagisciPaneliAsync(bagisciId.Value);
        return panel is null ? NotFound() : View(panel);
    }
}
