using KanBagis.Business.Bagiscilar;
using KanBagis.DataAccess.Repositories;
using KanBagis.Entities;

namespace KanBagis.Business.Talepler;

public interface IBildirimService
{
    Task<IslemSonucu> YanitlaAsync(int bagisciId, int bildirimId, BildirimYaniti yanit);
}

/// <summary>Bağışçının kendisine gelen bildirime Kabul/Red yanıtı vermesi.</summary>
public sealed class BildirimService(
    IBildirimRepository bildirimler,
    IBagisciService bagiscilar,
    TimeProvider zaman) : IBildirimService
{
    public async Task<IslemSonucu> YanitlaAsync(int bagisciId, int bildirimId, BildirimYaniti yanit)
    {
        if (yanit is not (BildirimYaniti.Kabul or BildirimYaniti.Red))
            return IslemSonucu.Hatali("Geçerli bir yanıt seçin.");

        // SAHİPLİK: bildirim yalnızca sahibi olan bağışçı için getirilir.
        var bildirim = await bildirimler.BagiscininBildiriminiGetirAsync(bildirimId, bagisciId);
        if (bildirim is null)
            return IslemSonucu.Hatali("Bildirim bulunamadı.");

        if (bildirim.KanTalebi.Durum != TalepDurumu.Acik)
            return IslemSonucu.Hatali("Bu talep kapanmış; artık yanıt verilemez.");

        if (yanit == BildirimYaniti.Kabul)
        {
            // Bildirim gönderildikten sonra durum değişmiş olabilir (ör. bağışçı bu arada başka yerde bağış yaptı).
            var profil = await bagiscilar.ProfilGetirAsync(bagisciId);
            if (profil is null || !profil.Uygunluk.Uygun)
                return IslemSonucu.Hatali("Şu an bağış için uygun değilsiniz: " + string.Join(" ", profil?.Uygunluk.Nedenler ?? []));

            // İş kuralı: Bir kişi tek seferde bir kez bağış yapabilir. İki talebi aynı anda kabul ederse
            // hastanelerden biri gelmeyecek bir bağışçıyı bekler.
            if (await bildirimler.AcikTalepteKabulVarMiAsync(bagisciId, bildirimId))
                return IslemSonucu.Hatali("Kabul ettiğiniz başka bir açık talep var. Aynı anda yalnızca bir talebi kabul edebilirsiniz.");
        }

        bildirim.Yanit = yanit;
        bildirim.YanitTarihi = zaman.GetUtcNow().UtcDateTime;
        bildirim.Okundu = true;
        await bildirimler.GuncelleAsync(bildirim);
        return IslemSonucu.Tamam;
    }
}
