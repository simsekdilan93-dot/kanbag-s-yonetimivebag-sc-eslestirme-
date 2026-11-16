using KanBagis.Business.Bagiscilar;
using KanBagis.Business.DisServisler;
using KanBagis.Business.Konum;
using KanBagis.DataAccess.Repositories;
using KanBagis.Entities;

namespace KanBagis.Business.Talepler;

/// <summary>
/// Bir talep için bulunan, bugün bağış yapabilecek bağışçı. MesafeKm, yol API'si yanıt verdiyse YOL mesafesi,
/// vermediyse kuş uçuşudur (Kaynak alanı hangisi olduğunu söyler).
/// </summary>
public record EslesmeAdayi(Bagisci Bagisci, double MesafeKm, bool TamGrupEslesmesi, DateTime? SonBagisTarihi,
    double? YolDakika = null, MesafeKaynagi Kaynak = MesafeKaynagi.KusUcusu);

public interface IEslestirmeService
{
    /// <summary>Talep için uygun bağışçıları bulur ve öncelik sırasına dizer.</summary>
    Task<List<EslesmeAdayi>> AdaylariBulAsync(int aliciKanGrubuId, string merkezIl, Aciliyet aciliyet, IReadOnlySet<int>? haricBagisciIdleri = null);

    /// <summary>Yalnızca verilen ildeki uygun bağışçılar (kurum panelindeki sayılar için).</summary>
    Task<List<EslesmeAdayi>> IldekiAdaylariBulAsync(int aliciKanGrubuId, string il);
}

/// <summary>
/// TALEP ↔ BAĞIŞÇI EŞLEŞTİRME (modülün çekirdeği)
///
/// Adım 1 — Grup uyumluluğu: Alıcıya kan verebilecek TÜM gruplar bulunur (KanGrubuUyumlulugu).
///          Örn. A Rh+ talebi için A Rh+, A Rh-, 0 Rh+, 0 Rh- aranır.
/// Adım 2 — Konum: Hastanenin ilinden, aciliyete göre belirlenen yarıçap içindeki iller bulunur (kuş uçuşu, hızlı ön eleme).
/// Adım 3 — Veritabanı: Bu gruplardan VE bu illerden bağışçılar tek sorguda çekilir.
/// Adım 4 — Uygunluk: Her adaya yaş, kilo ve son bağış kuralı uygulanır (BagisUygunlukKurali).
/// Adım 4b — Yol mesafesi (Hafta 9): Kalan adayların illeri için harita API'sinden (OSRM) GERÇEK yol mesafesi alınır.
///          API yanıt vermezse kuş uçuşu kullanılır; eşleştirme hiçbir zaman API yüzünden durmaz.
/// Adım 5 — Sıralama:
///    a) Yol mesafesi (yakından uzağa): Bağışçının hastaneye ulaşma olasılığı ve hızı en önemli etken.
///    b) Tam grup eşleşmesi önce: 0 Rh- herkese verebilir ama nadirdir (nüfusun ~%7'si).
///       A Rh+ talebine önce A Rh+ bağışçılar çağrılır; 0 Rh- yalnızca başka kimsenin
///       veremeyeceği talepler için korunur.
///    c) En uzun süredir bağış yapmayan önce: Yük bağışçılar arasında adil dağılır,
///       aynı kişiler sürekli çağrılmaz. Hiç bağış yapmamış olan en başa gelir.
///    d) Id: Sıranın her çalıştırmada aynı olması için (deterministik sonuç).
/// </summary>
public sealed class EslestirmeService(
    IBagisciRepository bagiscilar,
    IRepository<KanGrubu> kanGruplari,
    BagisUygunlukKurali uygunlukKurali,
    IYolMesafesiServisi yolMesafesi) : IEslestirmeService
{
    public Task<List<EslesmeAdayi>> AdaylariBulAsync(int aliciKanGrubuId, string merkezIl, Aciliyet aciliyet, IReadOnlySet<int>? haricBagisciIdleri = null)
    {
        var iller = IlKonumlari.YaricapIcindekiIller(merkezIl, AciliyetKurallari.AramaYaricapiKm(aciliyet));
        return BulAsync(aliciKanGrubuId, merkezIl, iller.Keys, haricBagisciIdleri);
    }

    public Task<List<EslesmeAdayi>> IldekiAdaylariBulAsync(int aliciKanGrubuId, string il) =>
        BulAsync(aliciKanGrubuId, il, [il], null);

    private async Task<List<EslesmeAdayi>> BulAsync(int aliciKanGrubuId, string merkezIl, IEnumerable<string> iller, IReadOnlySet<int>? haric)
    {
        // Adım 1
        var tumGruplar = await kanGruplari.TumunuGetirAsync();
        var alici = tumGruplar.SingleOrDefault(k => k.Id == aliciKanGrubuId);
        if (alici is null) return [];
        var vericiGrupIdleri = KanGrubuUyumlulugu.VerebilecekGruplar(alici, tumGruplar).Select(k => k.Id).ToList();

        // Adım 2-3
        var adaylar = await bagiscilar.AdaylariGetirAsync(vericiGrupIdleri, iller.ToList());

        // Adım 4
        var uygunlar = adaylar
            .Where(a => haric is null || !haric.Contains(a.Bagisci.Id))
            .Where(a => uygunlukKurali.Degerlendir(a.Bagisci, a.SonBagisTarihi).Uygun)
            .ToList();

        // Adım 4b: yalnızca adayların bulunduğu iller için tek bir API isteği (81 il için değil).
        var yollar = await yolMesafesi.HesaplaAsync(merkezIl, uygunlar.Select(a => a.Bagisci.Sehir));

        // Adım 5
        return uygunlar
            .Select(a => new EslesmeAdayi(
                a.Bagisci,
                yollar[a.Bagisci.Sehir].Km,
                a.Bagisci.KanGrubuId == alici.Id,
                a.SonBagisTarihi,
                yollar[a.Bagisci.Sehir].Dakika,
                yollar[a.Bagisci.Sehir].Kaynak))
            .OrderBy(a => a.MesafeKm)
            .ThenByDescending(a => a.TamGrupEslesmesi)
            .ThenBy(a => a.SonBagisTarihi ?? DateTime.MinValue)
            .ThenBy(a => a.Bagisci.Id)
            .ToList();
    }
}
