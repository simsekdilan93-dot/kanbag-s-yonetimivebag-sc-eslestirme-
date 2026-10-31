using KanBagis.DataAccess.Repositories;
using KanBagis.Entities;

namespace KanBagis.Business.Bagiscilar;

public record BagisciProfili(Bagisci Bagisci, DateTime? SonBagisTarihi, int BagisSayisi, UygunlukSonucu Uygunluk)
{
    /// <summary>Bağış kaydı olan bağışçının kan grubu laboratuvarda doğrulanmıştır; değiştirilemez.</summary>
    public bool KanGrubuKilitli => BagisSayisi > 0;
}

public record ProfilGuncellemeIstegi(string Telefon, string Il, int Kilo, int KanGrubuId);

public record IslemSonucu(bool Basarili, string? Hata = null)
{
    public static readonly IslemSonucu Tamam = new(true);
    public static IslemSonucu Hatali(string hata) => new(false, hata);
}

public interface IBagisciService
{
    Task<BagisciProfili?> ProfilGetirAsync(int bagisciId);
    Task<IslemSonucu> ProfilGuncelleAsync(int bagisciId, ProfilGuncellemeIstegi istek);
}

/// <summary>Bağışçı Kaydı ve Kan Grubu Yönetimi modülünün iş servisi.</summary>
public sealed class BagisciService(
    IBagisciRepository bagiscilar,
    IRepository<KanGrubu> kanGruplari,
    BagisUygunlukKurali uygunlukKurali) : IBagisciService
{
    public async Task<BagisciProfili?> ProfilGetirAsync(int bagisciId)
    {
        var bagisci = await bagiscilar.KanGrubuylaGetirAsync(bagisciId);
        if (bagisci is null) return null;

        var sonBagis = await bagiscilar.SonBagisTarihiAsync(bagisciId);
        var bagisSayisi = await bagiscilar.BagisSayisiAsync(bagisciId);

        return new BagisciProfili(bagisci, sonBagis, bagisSayisi, uygunlukKurali.Degerlendir(bagisci, sonBagis));
    }

    /// <summary>
    /// SAHİPLİK: bagisciId parametresi çağıran tarafından oturumdan (çerezden) verilir,
    /// formdan alınmaz. Bu yüzden bir bağışçı yalnızca kendi profilini değiştirebilir.
    /// Ad, soyad, TC, doğum tarihi ve cinsiyet kimlik bilgisidir; buradan değiştirilemez.
    /// </summary>
    public async Task<IslemSonucu> ProfilGuncelleAsync(int bagisciId, ProfilGuncellemeIstegi istek)
    {
        if (BagisciDogrulama.OrtakAlanHatasi(istek.Telefon, istek.Il, istek.Kilo) is { } hata)
            return IslemSonucu.Hatali(hata);

        var bagisci = await bagiscilar.IdIleGetirAsync(bagisciId);
        if (bagisci is null)
            return IslemSonucu.Hatali("Bağışçı bulunamadı.");

        if (istek.KanGrubuId != bagisci.KanGrubuId)
        {
            if (await kanGruplari.IdIleGetirAsync(istek.KanGrubuId) is null)
                return IslemSonucu.Hatali("Geçerli bir kan grubu seçin.");

            // İş kuralı: Bağış yapıldığında kan grubu laboratuvarda test edilir.
            // Kayıtta yanlış girilen grup ilk bağışa kadar düzeltilebilir, sonrasında kilitlenir.
            // Aksi halde yanlış gruba kan çağrısı gönderilebilir.
            if (await bagiscilar.BagisSayisiAsync(bagisciId) > 0)
                return IslemSonucu.Hatali("Bağış kaydınız olduğu için kan grubunuz değiştirilemez. Kan grubunuz laboratuvarda doğrulanmıştır.");

            bagisci.KanGrubuId = istek.KanGrubuId;
        }

        bagisci.Telefon = istek.Telefon.Trim();
        bagisci.Sehir = istek.Il;
        bagisci.Kilo = istek.Kilo;

        await bagiscilar.GuncelleAsync(bagisci);
        return IslemSonucu.Tamam;
    }
}
