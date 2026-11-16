using KanBagis.Business.Bagiscilar;
using KanBagis.Business.DisServisler;
using KanBagis.Business.Paneller;
using KanBagis.Business.Talepler;
using KanBagis.Business.Tanimlar;
using KanBagis.Entities;
using KanBagis.Web.Guvenlik;
using KanBagis.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;

namespace KanBagis.Web.Controllers;

/// <summary>
/// Yalnızca "Bagisci" rolündeki kullanıcılar erişebilir. Kontrol SUNUCUDA yapılır:
/// - Giriş yapmamış kullanıcı → /Hesap/Giris sayfasına yönlendirilir.
/// - Giriş yapmış ama rolü farklı (ör. Kurum) → /Hesap/ErisimEngellendi (403).
/// Menüde bağlantının gizlenmesi tek başına koruma değildir; adres elle yazılsa bile bu filtre çalışır.
/// </summary>
[Authorize(Roles = Roller.Bagisci)]
public class BagisciPanelController(
    IPanelService paneller,
    IBagisciService bagiscilar,
    IBildirimService bildirimServisi,
    ITanimService tanimlar,
    IOptions<NtfyAyarlari> ntfy,
    IOptions<TwilioAyarlari> twilio) : Controller
{
    /// <summary>
    /// Bağışçı Id'si URL'den ya da formdan değil, imzalı oturum çerezinden okunur:
    /// kullanıcı adresi veya formu değiştirerek başkasının kaydına ulaşamaz (sahiplik kontrolü).
    /// </summary>
    private int? OturumdakiBagisciId => User.SayiGetir(KullaniciClaimleri.BagisciId);

    public async Task<IActionResult> Index()
    {
        if (OturumdakiBagisciId is not { } bagisciId) return Forbid();

        var panel = await paneller.BagisciPaneliAsync(bagisciId);
        return panel is null ? NotFound() : View(panel);
    }

    /// <summary>Bildirime Kabul/Red yanıtı. Bildirimin bu bağışçıya ait olduğu iş katmanında kontrol edilir.</summary>
    [HttpPost]
    public async Task<IActionResult> Yanitla(int bildirimId, BildirimYaniti yanit)
    {
        if (OturumdakiBagisciId is not { } bagisciId) return Forbid();

        var sonuc = await bildirimServisi.YanitlaAsync(bagisciId, bildirimId, yanit);
        TempData[sonuc.Basarili ? "Mesaj" : "Hata"] = sonuc.Basarili
            ? (yanit == BildirimYaniti.Kabul
                ? "Teşekkürler! Talebi kabul ettiniz. Hastane sizinle iletişime geçecek."
                : "Yanıtınız kaydedildi.")
            : sonuc.Hata;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Profil()
    {
        if (OturumdakiBagisciId is not { } bagisciId) return Forbid();

        var profil = await bagiscilar.ProfilGetirAsync(bagisciId);
        if (profil is null) return NotFound();

        var b = profil.Bagisci;
        var model = new BagisciProfilViewModel
        {
            Telefon = b.Telefon,
            Il = b.Sehir,
            Kilo = b.Kilo,
            KanGrubuId = b.KanGrubuId,
        };
        return View(await GosterimAlanlariniDoldurAsync(model, profil));
    }

    [HttpPost]
    public async Task<IActionResult> Profil(BagisciProfilViewModel model)
    {
        if (OturumdakiBagisciId is not { } bagisciId) return Forbid();

        var profil = await bagiscilar.ProfilGetirAsync(bagisciId);
        if (profil is null) return NotFound();

        if (!ModelState.IsValid)
            return View(await GosterimAlanlariniDoldurAsync(model, profil));

        var sonuc = await bagiscilar.ProfilGuncelleAsync(bagisciId,
            new ProfilGuncellemeIstegi(model.Telefon, model.Il, model.Kilo!.Value, model.KanGrubuId!.Value));

        if (!sonuc.Basarili)
        {
            ModelState.AddModelError(string.Empty, sonuc.Hata!);
            return View(await GosterimAlanlariniDoldurAsync(model, profil));
        }

        TempData["Mesaj"] = "Profiliniz güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Telefona anlık bildirimi açar/kapatır (rastgele kanal üretilir ya da silinir).</summary>
    [HttpPost]
    public async Task<IActionResult> AnlikBildirim(bool ac)
    {
        if (OturumdakiBagisciId is not { } bagisciId) return Forbid();

        var sonuc = await bagiscilar.AnlikBildirimAyarlaAsync(bagisciId, ac);
        TempData[sonuc.Basarili ? "Mesaj" : "Hata"] = sonuc.Basarili
            ? (ac ? "Anlık bildirim açıldı. Telefonundaki ntfy uygulamasında aşağıdaki kanala abone ol." : "Anlık bildirim kapatıldı.")
            : sonuc.Hata;
        return RedirectToAction(nameof(Profil));
    }

    /// <summary>SMS izni (açık rıza).</summary>
    [HttpPost]
    public async Task<IActionResult> SmsIzni(bool izin)
    {
        if (OturumdakiBagisciId is not { } bagisciId) return Forbid();

        var sonuc = await bagiscilar.SmsIzniAyarlaAsync(bagisciId, izin);
        TempData[sonuc.Basarili ? "Mesaj" : "Hata"] = sonuc.Basarili
            ? (izin ? "SMS ile bildirim almayı kabul ettiniz." : "SMS izni geri alındı.")
            : sonuc.Hata;
        return RedirectToAction(nameof(Profil));
    }

    private async Task<BagisciProfilViewModel> GosterimAlanlariniDoldurAsync(BagisciProfilViewModel model, BagisciProfili profil)
    {
        var b = profil.Bagisci;
        model.AnlikServisAcik = ntfy.Value.Etkin;
        model.AnlikKanal = b.AnlikBildirimKanali;
        model.AnlikAbonelikAdresi = b.AnlikBildirimKanali is { } kanal ? $"{ntfy.Value.TemelAdres.TrimEnd('/')}/{kanal}" : null;
        model.SmsServisAcik = twilio.Value.Etkin;
        model.SmsIzni = b.SmsIzni;
        model.AdSoyad = $"{b.Ad} {b.Soyad}";
        model.TcMaskeli = $"{b.TcKimlikNo[..3]}*****{b.TcKimlikNo[^3..]}";   // ekranda tamamı gösterilmez
        model.DogumTarihi = b.DogumTarihi;
        model.Cinsiyet = b.Cinsiyet == Cinsiyet.Kadin ? "Kadın" : "Erkek";
        model.KanGrubuKilitli = profil.KanGrubuKilitli;
        if (profil.KanGrubuKilitli) model.KanGrubuId = b.KanGrubuId;
        model.KanGruplari = (await tanimlar.KanGruplariAsync())
            .Select(k => new SelectListItem(k.Ad, k.Id.ToString()))
            .ToList();
        return model;
    }
}
