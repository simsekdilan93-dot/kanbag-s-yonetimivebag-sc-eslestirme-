using KanBagis.Business.Bagiscilar;
using KanBagis.Business.Tanimlar;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KanBagis.Web.Controllers;

public record KanGrubuUyumlulukSatiri(string KanGrubu, List<string> VerebilecegiGruplar, List<string> AlabilecegiGruplar);

/// <summary>Herkese açık bilgi sayfası: hangi kan grubu kime kan verebilir?</summary>
[AllowAnonymous]
public class KanGrubuController(ITanimService tanimlar) : Controller
{
    public async Task<IActionResult> Index()
    {
        var gruplar = await tanimlar.KanGruplariAsync();

        var satirlar = gruplar.Select(g => new KanGrubuUyumlulukSatiri(
            g.Ad,
            KanGrubuUyumlulugu.AlabilecekGruplar(g, gruplar).Select(x => x.Ad).ToList(),
            KanGrubuUyumlulugu.VerebilecekGruplar(g, gruplar).Select(x => x.Ad).ToList()))
            .ToList();

        return View(satirlar);
    }
}
