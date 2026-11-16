namespace KanBagis.Business.DisServisler;

/// <summary>
/// Harici servis ayarları. appsettings.json'da yalnızca GİZLİ OLMAYAN değerler (adres, zaman aşımı) durur.
/// Anahtar ve parolalar (Twilio AuthToken, ntfy Token) ORTAM DEĞİŞKENİNDEN okunur, depoya girmez:
///   DisServisler__Twilio__AccountSid, DisServisler__Twilio__AuthToken, DisServisler__Twilio__Gonderen,
///   DisServisler__Ntfy__Token
/// (ASP.NET Core yapılandırması "__" işaretini bölüm ayırıcısı olarak okur.)
/// </summary>
public sealed class MesafeApiAyarlari
{
    public const string Bolum = "DisServisler:Mesafe";

    /// <summary>Kapalıysa API çağrılmaz, kuş uçuşu mesafe kullanılır.</summary>
    public bool Etkin { get; set; } = true;

    /// <summary>OSRM (Open Source Routing Machine) sunucusu. Ücretsiz, anahtar gerektirmez.</summary>
    public string TemelAdres { get; set; } = "https://router.project-osrm.org";

    public int ZamanAsimiSaniye { get; set; } = 5;

    /// <summary>Hız sınırı: OSRM'nin ücretsiz sunucusu en fazla saniyede 1 istek ister.</summary>
    public int IstekArasiEnAzMilisaniye { get; set; } = 1000;

    /// <summary>Sunucu 429 döner ama ne kadar bekleneceğini söylemezse bu kadar beklenir.</summary>
    public int HizSiniriVarsayilanBeklemeSaniye { get; set; } = 60;
}

public sealed class NtfyAyarlari
{
    public const string Bolum = "DisServisler:Ntfy";

    public bool Etkin { get; set; } = true;
    public string TemelAdres { get; set; } = "https://ntfy.sh";

    /// <summary>Bağışçı kanallarının ön eki (ör. kanbagis-7f3k…).</summary>
    public string KanalOneki { get; set; } = "kanbagis";

    /// <summary>İsteğe bağlı erişim belirteci (Bearer). GİZLİ: yalnızca ortam değişkeninden.</summary>
    public string? Token { get; set; }
}

public sealed class TwilioAyarlari
{
    public const string Bolum = "DisServisler:Twilio";

    public string TemelAdres { get; set; } = "https://api.twilio.com";

    /// <summary>GİZLİ: yalnızca ortam değişkeninden.</summary>
    public string? AccountSid { get; set; }

    /// <summary>GİZLİ: yalnızca ortam değişkeninden.</summary>
    public string? AuthToken { get; set; }

    /// <summary>Twilio'dan alınan gönderen numara (+1… biçiminde).</summary>
    public string? Gonderen { get; set; }

    /// <summary>Üç bilgi de verilmediyse SMS kanalı kapalıdır; uygulama yine çalışır.</summary>
    public bool Etkin =>
        !string.IsNullOrWhiteSpace(AccountSid) && !string.IsNullOrWhiteSpace(AuthToken) && !string.IsNullOrWhiteSpace(Gonderen);
}

public sealed class BildirimGonderimAyarlari
{
    public const string Bolum = "DisServisler:Gonderim";

    /// <summary>Bir bildirim için en fazla kaç kez denenir (geçici hatalarda).</summary>
    public int MaksimumDeneme { get; set; } = 3;

    /// <summary>Denemeler arası bekleme: 1. hatadan sonra bu kadar, sonra iki katı (üstel geri çekilme).</summary>
    public int IlkBeklemeMilisaniye { get; set; } = 1000;

    public int ZamanAsimiSaniye { get; set; } = 10;
}
