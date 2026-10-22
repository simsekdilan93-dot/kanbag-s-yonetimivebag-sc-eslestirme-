using KanBagis.Business.Guvenlik;
using KanBagis.DataAccess.Repositories;
using KanBagis.Entities;

namespace KanBagis.Business.Kimlik;

public interface IKimlikService
{
    Task<KayitSonucu> BagisciKaydetAsync(BagisciKayitIstegi istek);
    Task<KayitSonucu> KurumKaydetAsync(KurumKayitIstegi istek);
    Task<GirisSonucu> GirisYapAsync(string eposta, string parola);
}

public sealed class KimlikService(
    IKullaniciRepository kullanicilar,
    IBagisciRepository bagiscilar,
    IHastaneRepository hastaneler,
    IParolaHasher hasher,
    TimeProvider zaman) : IKimlikService
{
    public const int MaksimumHataliDeneme = 5;
    public static readonly TimeSpan KilitSuresi = TimeSpan.FromMinutes(15);
    public const int MinimumParolaUzunlugu = 8;

    // Olmayan e-postayla giriş denendiğinde de bir hash doğrulaması yapılır (aşağıya bakın).
    private static string? _sahteHash;

    public async Task<KayitSonucu> BagisciKaydetAsync(BagisciKayitIstegi istek)
    {
        var eposta = EpostaNormallestir(istek.Eposta);

        if (ParolaKuralHatasi(istek.Parola) is { } parolaHatasi)
            return KayitSonucu.Hatali(parolaHatasi);
        if (await kullanicilar.EpostaKayitliMiAsync(eposta))
            return KayitSonucu.Hatali("Bu e-posta adresiyle kayıtlı bir hesap var.");
        if (await bagiscilar.TcIleGetirAsync(istek.TcKimlikNo) is not null)
            return KayitSonucu.Hatali("Bu TC kimlik numarasıyla kayıtlı bir bağışçı var.");

        // Kullanıcı ve bağışçı aynı SaveChanges içinde (tek transaction) eklenir:
        // biri başarısız olursa ikisi de kaydedilmez.
        var kullanici = new Kullanici
        {
            Eposta = eposta,
            ParolaHash = hasher.Hashle(istek.Parola),
            Rol = KullaniciRolu.Bagisci,
            OlusturmaTarihi = Simdi(),
            Bagisci = new Bagisci
            {
                TcKimlikNo = istek.TcKimlikNo.Trim(),
                Ad = istek.Ad.Trim(),
                Soyad = istek.Soyad.Trim(),
                Telefon = istek.Telefon.Trim(),
                Eposta = eposta,
                DogumTarihi = istek.DogumTarihi,
                Cinsiyet = istek.Cinsiyet,
                Sehir = istek.Sehir.Trim(),
                KanGrubuId = istek.KanGrubuId,
            },
        };

        await kullanicilar.EkleAsync(kullanici);
        return KayitSonucu.Tamam(kullanici);
    }

    public async Task<KayitSonucu> KurumKaydetAsync(KurumKayitIstegi istek)
    {
        var eposta = EpostaNormallestir(istek.Eposta);

        if (ParolaKuralHatasi(istek.Parola) is { } parolaHatasi)
            return KayitSonucu.Hatali(parolaHatasi);
        if (await kullanicilar.EpostaKayitliMiAsync(eposta))
            return KayitSonucu.Hatali("Bu e-posta adresiyle kayıtlı bir hesap var.");
        if (await hastaneler.AdVeSehirIleVarMiAsync(istek.HastaneAdi.Trim(), istek.Sehir.Trim()))
            return KayitSonucu.Hatali("Bu hastane zaten kayıtlı.");

        var kullanici = new Kullanici
        {
            Eposta = eposta,
            ParolaHash = hasher.Hashle(istek.Parola),
            Rol = KullaniciRolu.Kurum,
            OlusturmaTarihi = Simdi(),
            Hastane = new Hastane
            {
                Ad = istek.HastaneAdi.Trim(),
                Sehir = istek.Sehir.Trim(),
                Telefon = istek.Telefon.Trim(),
            },
        };

        await kullanicilar.EkleAsync(kullanici);
        return KayitSonucu.Tamam(kullanici);
    }

    public async Task<GirisSonucu> GirisYapAsync(string eposta, string parola)
    {
        var kullanici = await kullanicilar.EpostaIleGetirAsync(EpostaNormallestir(eposta));
        var simdi = Simdi();

        if (kullanici is null)
        {
            // Hesap yoksa da bir hash doğrulaması yapılır. Böylece "e-posta yok" yanıtı
            // "parola yanlış" yanıtından daha hızlı dönmez ve saldırgan cevap süresine bakarak
            // hangi e-postaların kayıtlı olduğunu anlayamaz.
            hasher.Dogrula(parola, _sahteHash ??= hasher.Hashle(Guid.NewGuid().ToString()));
            return new GirisSonucu(GirisDurumu.HataliBilgi);
        }

        if (kullanici.KilitBitisTarihi > simdi)
            return new GirisSonucu(GirisDurumu.HesapKilitli, KilitBitisTarihi: kullanici.KilitBitisTarihi);

        if (!hasher.Dogrula(parola, kullanici.ParolaHash))
        {
            kullanici.BasarisizGirisSayisi++;
            if (kullanici.BasarisizGirisSayisi >= MaksimumHataliDeneme)
            {
                // Kaba kuvvet (brute force) saldırısını yavaşlatmak için hesap geçici olarak kilitlenir.
                kullanici.KilitBitisTarihi = simdi + KilitSuresi;
                kullanici.BasarisizGirisSayisi = 0;
                await kullanicilar.GuncelleAsync(kullanici);
                return new GirisSonucu(GirisDurumu.HesapKilitli, KilitBitisTarihi: kullanici.KilitBitisTarihi);
            }

            await kullanicilar.GuncelleAsync(kullanici);
            return new GirisSonucu(GirisDurumu.HataliBilgi);
        }

        kullanici.BasarisizGirisSayisi = 0;
        kullanici.KilitBitisTarihi = null;
        kullanici.SonGirisTarihi = simdi;
        await kullanicilar.GuncelleAsync(kullanici);

        return new GirisSonucu(GirisDurumu.Basarili, kullanici);
    }

    /// <summary>Parola kuralı iş katmanında da denetlenir; yalnızca formdaki doğrulamaya güvenilmez.</summary>
    public static string? ParolaKuralHatasi(string parola)
    {
        if (string.IsNullOrEmpty(parola) || parola.Length < MinimumParolaUzunlugu)
            return $"Parola en az {MinimumParolaUzunlugu} karakter olmalıdır.";
        if (!parola.Any(char.IsLetter) || !parola.Any(char.IsDigit))
            return "Parola en az bir harf ve bir rakam içermelidir.";
        return null;
    }

    private static string EpostaNormallestir(string eposta) => eposta.Trim().ToLowerInvariant();

    private DateTime Simdi() => zaman.GetUtcNow().UtcDateTime;
}
