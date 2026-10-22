using System.Security.Claims;
using KanBagis.Entities;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace KanBagis.Web.Guvenlik;

/// <summary>
/// Oturum çerezinde saklanan kullanıcı bilgileri (claim'ler).
/// Çerez sunucunun gizli anahtarıyla şifrelenir ve imzalanır (Data Protection);
/// kullanıcı içeriğini okuyamaz, değiştirirse çerez geçersiz olur.
/// </summary>
public static class KullaniciClaimleri
{
    public const string BagisciId = "kanbagis:bagisciId";
    public const string HastaneId = "kanbagis:hastaneId";

    public static ClaimsPrincipal Olustur(Kullanici kullanici)
    {
        var claimler = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, kullanici.Id.ToString()),
            new(ClaimTypes.Name, kullanici.Eposta),
            new(ClaimTypes.Role, kullanici.Rol.ToString()),
        };

        if (kullanici.BagisciId is { } bagisciId)
            claimler.Add(new Claim(BagisciId, bagisciId.ToString()));
        if (kullanici.HastaneId is { } hastaneId)
            claimler.Add(new Claim(HastaneId, hastaneId.ToString()));

        var kimlik = new ClaimsIdentity(claimler, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(kimlik);
    }

    public static int? SayiGetir(this ClaimsPrincipal kullanici, string claimTuru) =>
        int.TryParse(kullanici.FindFirstValue(claimTuru), out var deger) ? deger : null;
}
