using KanBagis.Business.Stok;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;

namespace KanBagis.Web.ViewComponents;

/// <summary>Menüdeki "Stok" bağlantısının yanında açık uyarı sayısı (yalnızca kurum kullanıcısına gösterilir).</summary>
public class StokUyariRozetiViewComponent(IStokService stok) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var sayi = await stok.AcikUyariSayisiAsync();
        return new HtmlContentViewComponentResult(sayi == 0
            ? HtmlString.Empty
            : new HtmlString($"<span class=\"badge rounded-pill text-bg-danger\" title=\"Açık stok uyarısı\">{sayi}</span>"));
    }
}
