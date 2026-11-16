using System.Text.Encodings.Web;
using System.Text.Unicode;
using KanBagis.Business;
using KanBagis.Business.DisServisler;
using KanBagis.Business.Tahmin;
using Microsoft.Extensions.Options;
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

// ---- Harici servis ayarları (Hafta 9) ----
// appsettings.json'da yalnızca gizli olmayan değerler var. Anahtarlar ORTAM DEĞİŞKENİNDEN gelir, örn.:
//   DisServisler__Twilio__AccountSid / DisServisler__Twilio__AuthToken / DisServisler__Twilio__Gonderen
//   DisServisler__Ntfy__Token
// ASP.NET Core ortam değişkenlerini appsettings'in ÜSTÜNE yazar; "__" bölüm ayırıcısıdır.
builder.Services.Configure<MesafeApiAyarlari>(builder.Configuration.GetSection(MesafeApiAyarlari.Bolum));
builder.Services.Configure<NtfyAyarlari>(builder.Configuration.GetSection(NtfyAyarlari.Bolum));
builder.Services.Configure<TwilioAyarlari>(builder.Configuration.GetSection(TwilioAyarlari.Bolum));
builder.Services.Configure<BildirimGonderimAyarlari>(builder.Configuration.GetSection(BildirimGonderimAyarlari.Bolum));

// ---- Yapay zekâ bileşeni (Hafta 10). Anahtar: ANTHROPIC_API_KEY ortam değişkeni (depoya girmez).
builder.Services.Configure<YapayZekaAyarlari>(builder.Configuration.GetSection(YapayZekaAyarlari.Bolum));

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

// Hangi harici servislerin açık olduğu loglanır (anahtarların KENDİSİ asla loglanmaz).
{
    var log = app.Services.GetRequiredService<ILogger<Program>>();
    var mesafe = app.Services.GetRequiredService<IOptions<MesafeApiAyarlari>>().Value;
    var ntfy = app.Services.GetRequiredService<IOptions<NtfyAyarlari>>().Value;
    var twilio = app.Services.GetRequiredService<IOptions<TwilioAyarlari>>().Value;
    var yz = app.Services.GetRequiredService<IOptions<YapayZekaAyarlari>>().Value;
    log.LogInformation("Harici servisler — Yol mesafesi: {Mesafe}, Anlık bildirim (ntfy): {Ntfy}{Token}, SMS (Twilio): {Sms}",
        mesafe.Etkin ? "açık" : "kapalı (kuş uçuşu)",
        ntfy.Etkin ? "açık" : "kapalı",
        string.IsNullOrEmpty(ntfy.Token) ? "" : " (erişim belirteci tanımlı)",
        twilio.Etkin ? "açık" : "kapalı (ortam değişkenleri tanımlı değil)");
    log.LogInformation("Yapay zekâ tahmini: {Durum}",
        yz.Kullanilabilir ? $"açık ({yz.Model})" : "kapalı (ANTHROPIC_API_KEY yok) — istatistiksel tahmin kullanılır");
}

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
