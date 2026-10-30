using KanBagis.Entities;

namespace KanBagis.Business.Bagiscilar;

/// <summary>
/// Eritrosit (kırmızı kan hücresi) bağışında kan grubu uyumluluğu.
///
/// ABO kuralı: Vericinin kanındaki antijen alıcıda yoksa, alıcının bağışıklık sistemi ona saldırır.
///   0  → antijen yok          → herkese verebilir (genel verici)
///   A  → A antijeni           → A ve AB'ye
///   B  → B antijeni           → B ve AB'ye
///   AB → A ve B antijeni      → yalnızca AB'ye (ama herkesten alabilir: genel alıcı)
///
/// Rh kuralı: Rh(+) kan, Rh(−) alıcıya verilemez. Rh(−) kan herkese verilebilir.
/// </summary>
public static class KanGrubuUyumlulugu
{
    public static bool VerebilirMi(KanGrubu verici, KanGrubu alici) =>
        AboUyumluMu(verici.AboGrubu, alici.AboGrubu) && (!verici.RhPozitif || alici.RhPozitif);

    public static bool AboUyumluMu(AboGrubu verici, AboGrubu alici) =>
        verici == AboGrubu.O || verici == alici || alici == AboGrubu.AB;

    /// <summary>Bu alıcıya kan verebilecek grupların listesi (eşleştirmede kullanılır).</summary>
    public static List<KanGrubu> VerebilecekGruplar(KanGrubu alici, IEnumerable<KanGrubu> tumGruplar) =>
        tumGruplar.Where(v => VerebilirMi(v, alici)).ToList();

    /// <summary>Bu vericinin kan verebileceği grupların listesi.</summary>
    public static List<KanGrubu> AlabilecekGruplar(KanGrubu verici, IEnumerable<KanGrubu> tumGruplar) =>
        tumGruplar.Where(a => VerebilirMi(verici, a)).ToList();
}
