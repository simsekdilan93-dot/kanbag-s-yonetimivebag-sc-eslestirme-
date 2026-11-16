using System.Threading.Channels;
using KanBagis.DataAccess.Repositories;
using KanBagis.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KanBagis.Business.DisServisler;

/// <summary>Telefona gönderilecek bildirimlerin kuyruğu (uygulama boyunca tek örnek).</summary>
public interface IBildirimKuyrugu
{
    void Ekle(int bildirimId);
}

public sealed class BildirimKuyrugu : IBildirimKuyrugu
{
    private readonly Channel<int> _kanal = Channel.CreateUnbounded<int>();
    public void Ekle(int bildirimId) => _kanal.Writer.TryWrite(bildirimId);
    public IAsyncEnumerable<int> OkuAsync(CancellationToken ct) => _kanal.Reader.ReadAllAsync(ct);
}

public interface IDisBildirimGonderici
{
    /// <summary>Bildirimi açık kanallardan gönderir; sonucu veritabanına yazar. Hız sınırındaysa ne zaman tekrar deneneceğini döner.</summary>
    Task<TimeSpan?> GonderAsync(int bildirimId, CancellationToken ct = default);
}

/// <summary>
/// "BİLDİRİM ULAŞMAZSA NE OLUYOR?"
///  1) Site içi bildirim her zaman veritabanında vardır; telefon yalnızca ek bir kanaldır. Telefon kanalı başarısız
///     olsa bile bağışçı panelinde çağrıyı görür, talep akışı durmaz.
///  2) Geçici hata (zaman aşımı, ağ, 5xx): üstel geri çekilmeyle (1 sn, 2 sn…) MaksimumDeneme kadar yeniden denenir.
///  3) Kalıcı hata (401 yanlış anahtar, 400 geçersiz numara): yeniden denenmez; hata kaydedilir.
///  4) Hız sınırı (429): bildirim "Kuyrukta" kalır, sunucunun söylediği süre (Retry-After) sonra kuyruğa geri konur.
///  5) Her denemenin sayısı, son hatası ve tarihi Bildirim kaydında tutulur; kurum talep sayfasında görünür.
/// </summary>
public sealed class DisBildirimGonderici(
    IBildirimRepository bildirimler,
    IEnumerable<IBildirimKanali> kanallar,
    IOptions<BildirimGonderimAyarlari> ayarlar,
    TimeProvider zaman,
    ILogger<DisBildirimGonderici> log) : IDisBildirimGonderici
{
    private readonly BildirimGonderimAyarlari _a = ayarlar.Value;

    public async Task<TimeSpan?> GonderAsync(int bildirimId, CancellationToken ct = default)
    {
        var bildirim = await bildirimler.GonderimIcinGetirAsync(bildirimId);
        if (bildirim is null || bildirim.DisKanalDurumu != DisKanalDurumu.Kuyrukta) return null;

        var acikKanallar = kanallar.Where(k => k.Etkin && k.BagisciIcinAcikMi(bildirim.Bagisci)).ToList();
        if (acikKanallar.Count == 0)
        {
            bildirim.DisKanalDurumu = DisKanalDurumu.Yok;   // bağışçı telefon bildirimini açmamış ya da servis kapalı
            await bildirimler.KaydetAsync();
            return null;
        }

        var mesaj = MesajOlustur(bildirim.KanTalebi);
        var basarili = false;
        string? sonHata = null;
        TimeSpan? sonraTekrarDene = null;

        foreach (var kanal in acikKanallar)
        {
            var (sonuc, deneme) = await KanaldanGonderAsync(kanal, bildirim.Bagisci, mesaj, ct);
            bildirim.DisKanalDeneme += deneme;
            if (sonuc.Tur == KanalSonucTuru.Basarili) { basarili = true; continue; }

            sonHata = $"{kanal.Ad}: {sonuc.Hata}";
            if (sonuc.Tur == KanalSonucTuru.HizSiniri) sonraTekrarDene = sonuc.SonraTekrarDene;
            // Log'a kişisel veri yazılmaz: yalnızca bildirim numarası, kanal ve hata.
            log.LogWarning("Bildirim {Id} {Kanal} kanalından gönderilemedi: {Hata}", bildirimId, kanal.Ad, sonuc.Hata);
        }

        bildirim.DisKanalTarihi = zaman.GetUtcNow().UtcDateTime;
        bildirim.DisKanalHata = basarili ? null : sonHata?[..Math.Min(sonHata.Length, 300)];
        bildirim.DisKanalDurumu = basarili ? DisKanalDurumu.Gonderildi
            : sonraTekrarDene is not null ? DisKanalDurumu.Kuyrukta     // hız sınırı: sonra tekrar denenecek
            : DisKanalDurumu.Basarisiz;
        await bildirimler.KaydetAsync();

        return bildirim.DisKanalDurumu == DisKanalDurumu.Kuyrukta ? sonraTekrarDene : null;
    }

    private async Task<(KanalSonucu Sonuc, int Deneme)> KanaldanGonderAsync(IBildirimKanali kanal, Bagisci b, DisMesaj m, CancellationToken ct)
    {
        var bekleme = TimeSpan.FromMilliseconds(_a.IlkBeklemeMilisaniye);
        KanalSonucu sonuc = new(KanalSonucTuru.GeciciHata, "denenmedi");

        for (var deneme = 1; deneme <= _a.MaksimumDeneme; deneme++)
        {
            try
            {
                using var zamanAsimi = CancellationTokenSource.CreateLinkedTokenSource(ct);
                zamanAsimi.CancelAfter(TimeSpan.FromSeconds(_a.ZamanAsimiSaniye));
                sonuc = await kanal.GonderAsync(b, m, zamanAsimi.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                sonuc = new(KanalSonucTuru.GeciciHata, $"{_a.ZamanAsimiSaniye} sn içinde yanıt yok (zaman aşımı)");
            }
            catch (HttpRequestException ex)
            {
                sonuc = new(KanalSonucTuru.GeciciHata, "Servise ulaşılamadı: " + ex.Message);
            }

            if (sonuc.Tur != KanalSonucTuru.GeciciHata || deneme == _a.MaksimumDeneme)
                return (sonuc, deneme);

            await Task.Delay(bekleme, ct);   // üstel geri çekilme: 1 sn, 2 sn, 4 sn…
            bekleme *= 2;
        }
        return (sonuc, _a.MaksimumDeneme);
    }

    /// <summary>KVKK: Mesajda kişisel veri yok. Yalnızca il, kan grubu, ünite ve aciliyet.</summary>
    public static DisMesaj MesajOlustur(KanTalebi t) => new(
        t.Aciliyet == Aciliyet.Acil ? "ACİL kan ihtiyacı" : "Kan ihtiyacı",
        $"{t.Hastane.Sehir}: {t.KanGrubu.Ad} kan ihtiyacı ({t.UniteSayisi} ünite). Yanıt vermek için Kan Bağışı paneline giriş yapın.",
        t.Aciliyet == Aciliyet.Acil);
}

/// <summary>
/// Kuyruktaki bildirimleri arka planda gönderir. Böylece kurumun "talep oluştur" isteği telefon servislerinin
/// yanıtını beklemez; servis yavaş ya da kapalı olsa bile talep anında oluşur.
/// Uygulama yeniden başlarsa "Kuyrukta" kalan bildirimler veritabanından bulunup tekrar kuyruğa konur.
/// </summary>
public sealed class BildirimGonderimArkaPlanServisi(
    BildirimKuyrugu kuyruk,
    IServiceScopeFactory kapsamlar,
    ILogger<BildirimGonderimArkaPlanServisi> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken durdur)
    {
        using (var kapsam = kapsamlar.CreateScope())
        {
            var bekleyenler = await kapsam.ServiceProvider.GetRequiredService<IBildirimRepository>().KuyruktakiIdlerAsync();
            foreach (var id in bekleyenler) kuyruk.Ekle(id);
            if (bekleyenler.Count > 0) log.LogInformation("{Sayi} bekleyen telefon bildirimi kuyruğa alındı.", bekleyenler.Count);
        }

        await foreach (var id in kuyruk.OkuAsync(durdur))
        {
            try
            {
                using var kapsam = kapsamlar.CreateScope();
                var sonra = await kapsam.ServiceProvider.GetRequiredService<IDisBildirimGonderici>().GonderAsync(id, durdur);
                if (sonra is { } sure)
                    _ = Task.Delay(sure, durdur).ContinueWith(_ => kuyruk.Ekle(id), TaskContinuationOptions.OnlyOnRanToCompletion);
            }
            catch (OperationCanceledException) when (durdur.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                // Tek bir bildirimdeki beklenmeyen hata arka plan servisini durdurmamalı.
                log.LogError(ex, "Bildirim {Id} gönderilirken beklenmeyen hata.", id);
            }
        }
    }
}
