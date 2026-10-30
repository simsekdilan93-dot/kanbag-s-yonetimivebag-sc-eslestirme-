using KanBagis.Business.Bagiscilar;
using KanBagis.DataAccess.Repositories;
using KanBagis.Entities;

namespace KanBagis.Business.Paneller;

public record BagisciPaneli(BagisciProfili Profil, List<Bildirim> Bildirimler);

/// <summary>Kurum panelinde bir kan grubunun özeti. KVKK gereği bağışçı kimlikleri değil, yalnızca sayıları gösterilir.</summary>
public record KanGrubuOzeti(KanGrubu KanGrubu, int StokUnite, int IldekiUygunBagisciSayisi);

public record KurumPaneli(Hastane Hastane, List<KanTalebi> Talepler, List<KanGrubuOzeti> KanGruplari);

public interface IPanelService
{
    Task<BagisciPaneli?> BagisciPaneliAsync(int bagisciId);
    Task<KurumPaneli?> KurumPaneliAsync(int hastaneId);
}

public sealed class PanelService(
    IBagisciService bagisciServisi,
    IBagisciRepository bagiscilar,
    IHastaneRepository hastaneler,
    IKanTalebiRepository talepler,
    IKanStokRepository stoklar) : IPanelService
{
    public async Task<BagisciPaneli?> BagisciPaneliAsync(int bagisciId)
    {
        var profil = await bagisciServisi.ProfilGetirAsync(bagisciId);
        if (profil is null) return null;

        return new BagisciPaneli(profil, await bagiscilar.BildirimleriGetirAsync(bagisciId));
    }

    public async Task<KurumPaneli?> KurumPaneliAsync(int hastaneId)
    {
        var hastane = await hastaneler.IdIleGetirAsync(hastaneId);
        if (hastane is null) return null;

        var ozetler = new List<KanGrubuOzeti>();
        foreach (var stok in await stoklar.KanGrubuylaTumuAsync())
        {
            var uygunlar = await bagisciServisi.UygunBagiscilariBulAsync(stok.KanGrubuId, hastane.Sehir);
            ozetler.Add(new KanGrubuOzeti(stok.KanGrubu, stok.UniteSayisi, uygunlar.Count));
        }

        return new KurumPaneli(hastane, await talepler.HastaneninTalepleriAsync(hastaneId), ozetler);
    }
}
