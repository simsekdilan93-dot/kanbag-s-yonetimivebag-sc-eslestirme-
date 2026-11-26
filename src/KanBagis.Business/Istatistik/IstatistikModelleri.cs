using KanBagis.Entities;

namespace KanBagis.Business.Istatistik;

/// <summary>
/// Oran her zaman PAY ve PAYDA ile birlikte taşınır: ekranda "%80 (4/5)" yazılır, böylece küçük örneklemden
/// çıkan yüzde abartılmaz. Payda 0 ise oran YOKTUR (null) — "%0" yazmak yanıltıcı olurdu.
/// </summary>
public sealed record Oran(int Pay, int Payda)
{
    /// <summary>Bu sayının altındaki paydalarda oran "az veri" olarak işaretlenir.</summary>
    public const int GuvenilirEnAzPayda = 5;

    public double? Deger => Payda == 0 ? null : (double)Pay / Payda;
    public bool AzVeri => Payda < GuvenilirEnAzPayda;

    public string Metin => Deger is { } d ? $"%{d * 100:0} ({Pay}/{Payda})" : "veri yok";
}

public sealed record PanoFiltresi(int Gun = 90, bool YalnizcaHastanem = false, bool SentetikDahil = false)
{
    public static readonly int[] GecerliGunler = [7, 30, 90, 365];
    public int GecerliGun => GecerliGunler.Contains(Gun) ? Gun : 90;
}

public sealed record TalepMetrikleri(
    int TalepSayisi, int TalepEdilenUnite, int Acik, int Karsilandi, int Iptal, int Acil,
    Oran KarsilanmaOrani, Oran AcilOrani, double? OrtalamaKarsilanmaSaat, double? MedyanKarsilanmaSaat);

public sealed record BagisMetrikleri(int BagisSayisi, int BagisUnite, int TalebeBagli, int Gonullu);

public sealed record BildirimOzeti(
    int Gonderilen, int Kabul, int Red, int Beklemede,
    Oran YanitOrani, Oran KabulOrani,
    IReadOnlyDictionary<DisKanalDurumu, int> DisKanal, Oran TelefonBasariOrani);

public sealed record KanGrubuSatiri(int KanGrubuId, string KanGrubu, int TalepUnite, int BagisUnite, int Stok, StokDurumu StokDurumu);

public sealed record HaftaSatiri(DateTime Baslangic, int TalepUnite, int BagisUnite);

public sealed record Pano(
    PanoFiltresi Filtre, DateTime Baslangic, DateTime Bitis, string Kapsam,
    TalepMetrikleri Talep, BagisMetrikleri Bagis, BildirimOzeti Bildirim,
    IReadOnlyList<KanGrubuSatiri> KanGruplari, IReadOnlyList<HaftaSatiri> Haftalar,
    int AcikStokUyarisi, IReadOnlyList<string> VeriNotlari, string OzetMetni);
