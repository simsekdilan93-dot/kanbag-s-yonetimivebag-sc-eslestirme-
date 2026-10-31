using KanBagis.Entities;

namespace KanBagis.Business.Talepler;

/// <summary>
/// Aciliyet, eşleştirmeyi üç yerde etkiler:
///  1) ARAMA YARIÇAPI: Acil talepte daha uzaktaki bağışçılara da ulaşılır.
///     Normal talepte bağışçının yola çıkması için zaman vardır; yakın çevre yeterlidir.
///  2) BİLDİRİM SAYISI: Bildirim alan herkes gelmez (red eder, ulaşılamaz).
///     Acil talepte kaybedilecek zaman olmadığı için ünite başına daha çok kişiye ilk seferde ulaşılır.
///  3) SIRALAMA: Kurum ve bağışçı listelerinde acil talepler her zaman üstte gösterilir.
/// </summary>
public static class AciliyetKurallari
{
    public const int NormalYaricapKm = 100;
    public const int AcilYaricapKm = 300;

    public const int NormalBildirimCarpani = 3;
    public const int AcilBildirimCarpani = 5;

    public const int MinimumUnite = 1;
    public const int MaksimumUnite = 10;

    public static int AramaYaricapiKm(Aciliyet aciliyet) =>
        aciliyet == Aciliyet.Acil ? AcilYaricapKm : NormalYaricapKm;

    /// <summary>Bir turda kaç bağışçıya bildirim gönderileceği.</summary>
    public static int BildirimKotasi(Aciliyet aciliyet, int uniteSayisi) =>
        uniteSayisi * (aciliyet == Aciliyet.Acil ? AcilBildirimCarpani : NormalBildirimCarpani);

    /// <summary>Listelerde öncelik: önce açık, sonra acil, sonra en yeni.</summary>
    public static IOrderedEnumerable<T> OncelikSirasina<T>(IEnumerable<T> kayitlar, Func<T, KanTalebi> talep) =>
        kayitlar.OrderBy(k => talep(k).Durum != TalepDurumu.Acik)
                .ThenBy(k => talep(k).Aciliyet != Aciliyet.Acil)
                .ThenByDescending(k => talep(k).OlusturmaTarihi);
}
