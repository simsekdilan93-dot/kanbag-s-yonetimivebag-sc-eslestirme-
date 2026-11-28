using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KanBagis.Business.Eposta;

/// <summary>
/// E-posta (SMTP) ayarları. Parola GİZLİDİR: yalnızca ortam değişkeninden okunur (DisServisler__Eposta__Parola).
/// SMTP sunucusu ve gönderen adres verilmezse e-postalar gönderilmez, <see cref="Klasor"/> klasörüne .eml dosyası olarak yazılır
/// (geliştirme ve demo için: dosya, Outlook / Thunderbird / tarayıcıyla açılıp kod okunabilir).
/// </summary>
public sealed class EpostaAyarlari
{
    public const string Bolum = "DisServisler:Eposta";

    public string? SmtpSunucu { get; set; }
    public int Port { get; set; } = 587;
    public bool Ssl { get; set; } = true;
    public string? Kullanici { get; set; }

    /// <summary>GİZLİ. Gmail için normal parola değil, "uygulama şifresi".</summary>
    public string? Parola { get; set; }

    public string? GonderenAdres { get; set; }
    public string GonderenAd { get; set; } = "Kan Bağışı Yönetim Sistemi";
    public int ZamanAsimiSaniye { get; set; } = 15;

    /// <summary>SMTP yapılandırılmamışsa e-postaların yazılacağı klasör. Web katmanı göreli yolu ContentRoot'a göre çözer.</summary>
    public string Klasor { get; set; } = "App_Data/epostalar";

    public bool SmtpEtkin => !string.IsNullOrWhiteSpace(SmtpSunucu) && !string.IsNullOrWhiteSpace(GonderenAdres);
}

public interface IEpostaGonderici
{
    /// <summary>Asla istisna fırlatmaz; gönderilemezse false döner ve nedeni loglanır.</summary>
    Task<bool> GonderAsync(string alici, string konu, string metin, CancellationToken iptal = default);
}

/// <summary>System.Net.Mail ile SMTP üzerinden düz metin e-posta gönderir.</summary>
public sealed class SmtpEpostaGonderici(IOptions<EpostaAyarlari> ayarlar, ILogger<SmtpEpostaGonderici> log) : IEpostaGonderici
{
    private readonly EpostaAyarlari _a = ayarlar.Value;

    public async Task<bool> GonderAsync(string alici, string konu, string metin, CancellationToken iptal = default)
    {
        try
        {
            var gonderen = _a.SmtpEtkin ? _a.GonderenAdres! : "noreply@kanbagis.local";
            using var mesaj = new MailMessage(new MailAddress(gonderen, _a.GonderenAd), new MailAddress(alici))
            {
                Subject = konu,
                Body = metin,
                IsBodyHtml = false,
                BodyEncoding = System.Text.Encoding.UTF8,
                SubjectEncoding = System.Text.Encoding.UTF8,
            };

            using var istemci = new SmtpClient();
            if (_a.SmtpEtkin)
            {
                istemci.Host = _a.SmtpSunucu!;
                istemci.Port = _a.Port;
                istemci.EnableSsl = _a.Ssl;
                istemci.Timeout = _a.ZamanAsimiSaniye * 1000;
                if (!string.IsNullOrWhiteSpace(_a.Kullanici))
                    istemci.Credentials = new NetworkCredential(_a.Kullanici, _a.Parola);
            }
            else
            {
                Directory.CreateDirectory(_a.Klasor);
                istemci.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
                istemci.PickupDirectoryLocation = Path.GetFullPath(_a.Klasor);
            }

            await istemci.SendMailAsync(mesaj, iptal);
            if (!_a.SmtpEtkin)
                log.LogWarning("SMTP yapılandırılmamış: e-posta gönderilmedi, klasöre yazıldı ({Klasor}).", Path.GetFullPath(_a.Klasor));
            return true;
        }
        catch (Exception e) when (e is SmtpException or InvalidOperationException or IOException or FormatException or UnauthorizedAccessException)
        {
            // Alıcı adresi ve hata türü loglanır; e-postanın içeriği (kod) loglanmaz.
            var ic = e; while (ic.InnerException is not null) ic = ic.InnerException;
            // Gmail yanlış kimlik bilgisinde 535 yanıtı verip bağlantıyı kapatır; SmtpClient bunu "bağlantı kapandı" diye bildirir.
            log.LogError(e, "E-posta gönderilemedi ({Tur}: {Neden}). Bağlantı kapandıysa en olası neden SMTP kullanıcı adı/şifresinin " +
                "kabul edilmemesidir (Gmail'de normal şifre değil, 16 harflik uygulama şifresi gerekir).", e.GetType().Name, ic.Message);
            return false;
        }
    }
}
