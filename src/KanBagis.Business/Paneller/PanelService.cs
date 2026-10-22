using KanBagis.DataAccess.Repositories;
using KanBagis.Entities;

namespace KanBagis.Business.Paneller;

public record BagisciPaneli(Bagisci Bagisci, DateTime? SonBagisTarihi, DateTime? SonrakiUygunTarih, List<Bildirim> Bildirimler);

public record KurumPaneli(Hastane Hastane, List<KanTalebi> Talepler, List<KanStok> Stoklar);

public interface IPanelService
{
    Task<BagisciPaneli?> BagisciPaneliAsync(int bagisciId);
    Task<KurumPaneli?> KurumPaneliAsync(int hastaneId);
}

public sealed class PanelService(
    IBagisciRepository bagiscilar,
    IHastaneRepository hastaneler,
    IKanTalebiRepository talepler,
    IKanStokRepository stoklar) : IPanelService
{
    /// <summary>İki bağış arasında beklenmesi gereken süre (ay).</summary>
    public const int BagisAraligiAy = 3;

    public async Task<BagisciPaneli?> BagisciPaneliAsync(int bagisciId)
    {
        var bagisci = await bagiscilar.KanGrubuylaGetirAsync(bagisciId);
        if (bagisci is null) return null;

        var sonBagis = await bagiscilar.SonBagisTarihiAsync(bagisciId);
        var bildirimler = await bagiscilar.BildirimleriGetirAsync(bagisciId);

        return new BagisciPaneli(bagisci, sonBagis, sonBagis?.AddMonths(BagisAraligiAy), bildirimler);
    }

    public async Task<KurumPaneli?> KurumPaneliAsync(int hastaneId)
    {
        var hastane = await hastaneler.IdIleGetirAsync(hastaneId);
        if (hastane is null) return null;

        return new KurumPaneli(
            hastane,
            await talepler.HastaneninTalepleriAsync(hastaneId),
            await stoklar.KanGrubuylaTumuAsync());
    }
}
