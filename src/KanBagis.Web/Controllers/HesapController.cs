using KanBagis.Business.Kimlik;
using KanBagis.Business.Tanimlar;
using KanBagis.Entities;
using KanBagis.Web.Guvenlik;
using KanBagis.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KanBagis.Web.Controllers;

[AllowAnonymous]
public class HesapController(IKimlikService kimlik, ITanimService tanimlar) : Controller
{
    // ---------------- GİRİŞ ----------------

    [HttpGet]
    public IActionResult Giris(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return PaneleYonlendir();

        return View(new GirisViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Giris(GirisViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var sonuc = await kimlik.GirisYapAsync(model.Eposta, model.Parola);

        switch (sonuc.Durum)
        {
            case GirisDurumu.HesapKilitli:
                ModelState.AddModelError(string.Empty,
                    $"Çok sayıda hatalı deneme yapıldı. Hesap {sonuc.KilitBitisTarihi!.Value.ToLocalTime():HH:mm} saatine kadar kilitli.");
                return View(model);

            case GirisDurumu.HataliBilgi:
                // Bilerek genel mesaj: "e-posta yok" ya da "parola yanlış" ayrımı yapılmaz,
                // aksi halde saldırgan hangi e-postaların kayıtlı olduğunu öğrenir.
                ModelState.AddModelError(string.Empty, "E-posta veya parola hatalı.");
                return View(model);
        }

        await OturumAcAsync(sonuc.Kullanici!);

        // Açık yönlendirme (open redirect) koruması: yalnızca sitenin kendi adreslerine dönülür.
        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);

        return PaneleYonlendir(sonuc.Kullanici!.Rol);
    }

    // ---------------- ÇIKIŞ ----------------

    [HttpPost]
    public async Task<IActionResult> Cikis()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    // ---------------- KAYIT ----------------

    [HttpGet]
    public async Task<IActionResult> KayitBagisci() =>
        View(await KanGruplariniDoldurAsync(new BagisciKayitViewModel()));

    [HttpPost]
    public async Task<IActionResult> KayitBagisci(BagisciKayitViewModel model)
    {
        if (KimlikService.ParolaKuralHatasi(model.Parola) is { } parolaHatasi)
            ModelState.AddModelError(nameof(model.Parola), parolaHatasi);

        if (!ModelState.IsValid)
            return View(await KanGruplariniDoldurAsync(model));

        var sonuc = await kimlik.BagisciKaydetAsync(new BagisciKayitIstegi(
            model.Eposta, model.Parola, model.TcKimlikNo, model.Ad, model.Soyad, model.Telefon,
            model.DogumTarihi!.Value, model.Cinsiyet!.Value, model.Kilo!.Value, model.Il, model.KanGrubuId!.Value));

        if (!sonuc.Basarili)
        {
            ModelState.AddModelError(string.Empty, sonuc.Hata!);
            return View(await KanGruplariniDoldurAsync(model));
        }

        await OturumAcAsync(sonuc.Kullanici!);
        return PaneleYonlendir(KullaniciRolu.Bagisci);
    }

    [HttpGet]
    public IActionResult KayitKurum() => View(new KurumKayitViewModel());

    [HttpPost]
    public async Task<IActionResult> KayitKurum(KurumKayitViewModel model)
    {
        if (KimlikService.ParolaKuralHatasi(model.Parola) is { } parolaHatasi)
            ModelState.AddModelError(nameof(model.Parola), parolaHatasi);

        if (!ModelState.IsValid)
            return View(model);

        var sonuc = await kimlik.KurumKaydetAsync(new KurumKayitIstegi(
            model.Eposta, model.Parola, model.HastaneAdi, model.Il, model.Telefon));

        if (!sonuc.Basarili)
        {
            ModelState.AddModelError(string.Empty, sonuc.Hata!);
            return View(model);
        }

        await OturumAcAsync(sonuc.Kullanici!);
        return PaneleYonlendir(KullaniciRolu.Kurum);
    }

    // ---------------- YETKİSİZ ERİŞİM ----------------

    [HttpGet]
    public IActionResult ErisimEngellendi()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    // ---------------- YARDIMCILAR ----------------

    private Task OturumAcAsync(Kullanici kullanici) =>
        // Her girişte YENİ bir çerez üretilir; giriş öncesi çerez kullanılmaz (session fixation koruması).
        HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, KullaniciClaimleri.Olustur(kullanici));

    private IActionResult PaneleYonlendir(KullaniciRolu? rol = null)
    {
        rol ??= Enum.TryParse<KullaniciRolu>(User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value, out var r) ? r : null;

        return rol switch
        {
            KullaniciRolu.Bagisci => RedirectToAction("Index", "BagisciPanel"),
            KullaniciRolu.Kurum => RedirectToAction("Index", "KurumPanel"),
            _ => RedirectToAction("Index", "Home"),
        };
    }

    private async Task<BagisciKayitViewModel> KanGruplariniDoldurAsync(BagisciKayitViewModel model)
    {
        model.KanGruplari = (await tanimlar.KanGruplariAsync())
            .Select(k => new SelectListItem(k.Ad, k.Id.ToString()))
            .ToList();
        return model;
    }
}
