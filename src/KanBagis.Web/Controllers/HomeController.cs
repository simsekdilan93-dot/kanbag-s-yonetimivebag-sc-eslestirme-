using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using KanBagis.Web.Models;

namespace KanBagis.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>Gövdesi boş hata yanıtları (404, 403, 400…) için kullanıcı dostu sayfa. Durum kodu değişmez.</summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Durum(int kod)
    {
        Response.StatusCode = kod is >= 400 and <= 599 ? kod : 404;
        return View(Response.StatusCode);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
