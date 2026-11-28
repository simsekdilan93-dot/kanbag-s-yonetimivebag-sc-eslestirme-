using System.Security.Cryptography;
using KanBagis.Business.Eposta;
using KanBagis.Business.Guvenlik;
using KanBagis.DataAccess.Repositories;
using KanBagis.Entities;

namespace KanBagis.Business.Kimlik;

public enum SifirlamaDurumu
{
    Basarili,
    KodHatali,          // kod yanlış, süresi dolmuş, kullanılmış ya da hiç istenmemiş — kullanıcıya hepsi aynı mesaj
    CokDenendi,         // bu kodla çok sayıda yanlış deneme yapıldı; kod iptal edildi
    ParolaGecersiz,
}

public sealed record SifirlamaSonucu(SifirlamaDurumu Durum, string? Mesaj = null);

public interface IParolaSifirlamaService
{
    /// <summary>
    /// E-posta kayıtlıysa yeni kod üretip gönderir. Kayıtlı olsa da olmasa da AYNI şekilde döner (sonuç dönmez):
    /// böylece formdan hangi e-postaların kayıtlı olduğu öğrenilemez.
    /// </summary>
    Task KodGonderAsync(string eposta);

    Task<SifirlamaSonucu> ParolayiYenileAsync(string eposta, string kod, string yeniParola);
}

/// <summary>
/// "Parolamı unuttum" akışı:
///   1) Kullanıcı e-postasını girer → 6 haneli rastgele kod üretilir, yalnızca hash'i saklanır, kod e-postayla gönderilir.
///   2) Kullanıcı kodu ve yeni parolayı girer → kod doğruysa parola değişir, kod bir daha kullanılamaz.
/// Güvenlik kuralları:
///   • Kod kriptografik rastgele (RandomNumberGenerator), 15 dakika geçerli.
///   • Bir kodla en fazla 5 yanlış deneme; sonra kod iptal (6 hane = 1.000.000 olasılık, 5 denemede tahmin şansı 1/200.000).
///   • Yeni kod istenince eski kod geçersiz; aynı hesaba 60 saniyede bir kod (e-posta bombardımanına karşı).
///   • Kayıtlı olmayan e-posta için de aynı yanıt ve benzer süre (hesap varlığı sızmaz).
///   • Parola yenilenince giriş kilidi açılır.
/// </summary>
public sealed class ParolaSifirlamaService(
    IKullaniciRepository kullanicilar,
    IParolaSifirlamaRepository kodlar,
    IParolaHasher hasher,
    IEpostaGonderici eposta,
    TimeProvider zaman) : IParolaSifirlamaService
{
    public static readonly TimeSpan KodGecerlilikSuresi = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan IstekAraligi = TimeSpan.FromSeconds(60);
    public const int MaksimumDeneme = 5;
    public const int KodUzunlugu = 6;

    public async Task KodGonderAsync(string epostaAdresi)
    {
        var kullanici = await kullanicilar.EpostaIleGetirAsync(KimlikService.EpostaNormallestir(epostaAdresi ?? ""));
        var simdi = zaman.GetUtcNow().UtcDateTime;
        var kod = KodUret();
        var kodHash = hasher.Hashle(kod);   // hesap yoksa da hesaplanır: yanıt süresi hesabın varlığını ele vermesin

        if (kullanici is null) return;
        if (await kodlar.SonIstekTarihiAsync(kullanici.Id) is { } son && simdi - son < IstekAraligi) return;

        await kodlar.YeniKodEkleAsync(new ParolaSifirlamaKodu
        {
            KullaniciId = kullanici.Id,
            KodHash = kodHash,
            OlusturmaTarihi = simdi,
            SonKullanmaTarihi = simdi + KodGecerlilikSuresi,
        }, simdi);

        await eposta.GonderAsync(kullanici.Eposta, "Kan Bağışı — parola yenileme kodu", EpostaMetni(kod));
    }

    public async Task<SifirlamaSonucu> ParolayiYenileAsync(string epostaAdresi, string kod, string yeniParola)
    {
        if (KimlikService.ParolaKuralHatasi(yeniParola) is { } parolaHatasi)
            return new SifirlamaSonucu(SifirlamaDurumu.ParolaGecersiz, parolaHatasi);

        kod = (kod ?? "").Trim().Replace(" ", "");
        var kullanici = await kullanicilar.EpostaIleGetirAsync(KimlikService.EpostaNormallestir(epostaAdresi ?? ""));
        var acik = kullanici is null ? null : await kodlar.AcikKoduGetirAsync(kullanici.Id);
        var simdi = zaman.GetUtcNow().UtcDateTime;

        if (acik is null || acik.SonKullanmaTarihi <= simdi)
        {
            if (acik is not null) { acik.KapanisTarihi = simdi; await kodlar.KaydetAsync(); }   // süresi dolan kod kapanır
            return Hatali();
        }

        if (kod.Length != KodUzunlugu || !kod.All(char.IsAsciiDigit) || !hasher.Dogrula(kod, acik.KodHash))
        {
            acik.DenemeSayisi++;
            if (acik.DenemeSayisi >= MaksimumDeneme)
            {
                acik.KapanisTarihi = simdi;
                await kodlar.KaydetAsync();
                return new SifirlamaSonucu(SifirlamaDurumu.CokDenendi,
                    "Çok sayıda yanlış kod girildi; bu kod iptal edildi. Lütfen yeni kod isteyin.");
            }
            await kodlar.KaydetAsync();
            return Hatali();
        }

        await kodlar.KoduKullanVeParolayiDegistirAsync(acik, hasher.Hashle(yeniParola), simdi);
        return new SifirlamaSonucu(SifirlamaDurumu.Basarili);
    }

    private static SifirlamaSonucu Hatali() =>
        new(SifirlamaDurumu.KodHatali, "Kod hatalı ya da süresi dolmuş. Kodu kontrol edin veya yeni kod isteyin.");

    /// <summary>000000–999999 arası, kriptografik olarak rastgele (Random değil: Random'un çıktısı tahmin edilebilir).</summary>
    public static string KodUret() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    public static string EpostaMetni(string kod) => $"""
        Merhaba,

        Kan Bağışı Yönetim Sistemi hesabınız için parola yenileme isteği aldık.

        Parola yenileme kodunuz:  {kod}

        Kod {(int)KodGecerlilikSuresi.TotalMinutes} dakika geçerlidir ve yalnızca bir kez kullanılabilir.
        Kodu kimseyle paylaşmayın; sistem çalışanları bu kodu asla sizden istemez.

        Bu isteği siz yapmadıysanız bu e-postayı dikkate almayın; parolanız değişmez.

        Kan Bağışı Yönetim Sistemi
        """;
}
