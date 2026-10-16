using KanBagis.Business;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// {ContentRoot} yer tutucusu, veritabanı dosyasının her zaman Web projesinin klasöründe oluşmasını sağlar.
var connectionString = builder.Configuration.GetConnectionString("KanBagis")!
    .Replace("{ContentRoot}", builder.Environment.ContentRootPath);
builder.Services.AddBusiness(connectionString);

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

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
