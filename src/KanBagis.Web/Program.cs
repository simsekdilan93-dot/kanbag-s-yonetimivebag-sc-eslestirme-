using System.Text.Encodings.Web;
using System.Text.Unicode;
using KanBagis.Business;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.WebEncoders;

var builder = WebApplication.CreateBuilder(args);

// Tüm POST isteklerinde CSRF (antiforgery) belirteci zorunlu: başka bir site kullanıcının
// tarayıcısı üzerinden onun adına form gönderemez.
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

// Türkçe karakterler (ı, ş, ğ...) HTML'e &#x131; gibi kodlanmadan yazılsın.
// <, >, " gibi XSS açısından tehlikeli karakterler yine kodlanır.
builder.Services.Configure<WebEncoderOptions>(options =>
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

// {ContentRoot} yer tutucusu, veritabanı dosyasının her zaman Web projesinin klasöründe oluşmasını sağlar.
var connectionString = builder.Configuration.GetConnectionString("KanBagis")!
    .Replace("{ContentRoot}", builder.Environment.ContentRootPath);
builder.Services.AddBusiness(connectionString);

// ---- Kimlik doğrulama: çerez (cookie) tabanlı oturum ----
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Hesap/Giris";                    // giriş yapmamış → buraya
        options.AccessDeniedPath = "/Hesap/ErisimEngellendi";  // rolü yetmiyor → buraya
        options.LogoutPath = "/Hesap/Cikis";

        // Oturum süresi: 30 dk işlem yapılmazsa oturum düşer; işlem yapıldıkça süre uzar.
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;

        options.Cookie.Name = "KanBagis.Oturum";
        options.Cookie.HttpOnly = true;                     // JavaScript çereze erişemez (XSS ile çalınamaz)
        options.Cookie.SameSite = SameSiteMode.Lax;         // başka sitelerden gelen POST'larda gönderilmez
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest              // yerelde http ile de çalışsın
            : CookieSecurePolicy.Always;                    // canlıda yalnızca HTTPS
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Uygulama açılırken bekleyen migration'lar uygulanır; veritabanı yoksa oluşturulur.
await DependencyInjection.VeritabaniniHazirlaAsync(app.Services);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Sıra önemli: önce "kimsin?" (authentication), sonra "yetkin var mı?" (authorization).
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

// Entegrasyon testlerinin (WebApplicationFactory) uygulamayı başlatabilmesi için.
public partial class Program;
