using KanBagis.Business.Bagiscilar;
using KanBagis.Business.Talepler;
using KanBagis.DataAccess.Repositories;
using KanBagis.Entities;

namespace KanBagis.Business.Paneller;

public record BagisciPaneli(BagisciProfili Profil, List<Bildirim> Bildirimler)
{
    /// <summary>Yanıt bekleyen ve talebi hâlâ açık olan bildirimler.</summary>
    public int BekleyenSayisi => Bildirimler.Count(b => YanitlanabilirMi(b));

    public static bool YanitlanabilirMi(Bildirim b) =>
        b.Yanit == BildirimYaniti.Beklemede && b.KanTalebi.Durum == TalepDurumu.Acik;
}

/// <summary>Kurum panelinde bir kan grubunun özeti. KVKK gereği bağışçı kimlikleri değil, yalnızca sayıları gösterilir.</summary>
public record KanGrubuOzeti(KanGrubu KanGrubu, int StokUnite, int IldekiUygunBagisciSayisi);

public record KurumPaneli(Hastane Hastane, List<TalepOzeti> Talepler, List<KanGrubuOzeti> KanGruplari);

public interface IPanelService
{
    Task<BagisciPaneli?> BagisciPaneliAsync(int bagisciId);
    Task<KurumPaneli?> KurumPaneliAsync(int hastaneId);
}

public sealed class PanelService(
    IBagisciService bagisciServisi,
    IEslestirmeService eslestirme,
    IBagisciRepository bagiscilar,
    IBildirimRepository bildirimler,
    IHastaneRepository hastaneler,
    IKanTalebiRepository talepler,
    IKanStokRepository stoklar) : IPanelService
{
    public async Task<BagisciPaneli?> BagisciPaneliAsync(int bagisciId)
    {
        var profil = await bagisciServisi.ProfilGetirAsync(bagisciId);
        if (profil is null) return null;

        // Önce yanıt bekleyen açık talepler, onların içinde acil olanlar, sonra en yeni.
        var liste = (await bagiscilar.BildirimleriGetirAsync(bagisciId))
            .OrderByDescending(BagisciPaneli.YanitlanabilirMi)
            .ThenBy(b => b.KanTalebi.Aciliyet != Aciliyet.Acil)
            .ThenByDescending(b => b.GonderimTarihi)
            .ToList();

        // Liste yüklendi (Okundu değerleri eski hâliyle duruyor → "Yeni" etiketi bu sefer gösterilir), sonra okundu işaretlenir.
        await bildirimler.OkunduIsaretleAsync(bagisciId);

        return new BagisciPaneli(profil, liste);
    }

    public async Task<KurumPaneli?> KurumPaneliAsync(int hastaneId)
    {
        var hastane = await hastaneler.IdIleGetirAsync(hastaneId);
        if (hastane is null) return null;

        var ozetler = new List<KanGrubuOzeti>();
        foreach (var stok in await stoklar.KanGrubuylaTumuAsync())
        {
            var uygunlar = await eslestirme.IldekiAdaylariBulAsync(stok.KanGrubuId, hastane.Sehir);
            ozetler.Add(new KanGrubuOzeti(stok.KanGrubu, stok.UniteSayisi, uygunlar.Count));
        }

        var talepListesi = AciliyetKurallari.OncelikSirasina(await talepler.HastaneninTalepleriAsync(hastaneId), o => o.Talep).ToList();
        return new KurumPaneli(hastane, talepListesi, ozetler);
    }
}
