using KanBagis.Business.Bagiscilar;
using KanBagis.DataAccess.Repositories;
using KanBagis.Entities;

namespace KanBagis.Business.Talepler;

public record TalepOlusturmaIstegi(int KanGrubuId, int UniteSayisi, Aciliyet Aciliyet, string? Aciklama);

public record TalepIslemSonucu(bool Basarili, string? Hata = null, int? TalepId = null, int BildirimSayisi = 0)
{
    public static TalepIslemSonucu Hatali(string hata, int? talepId = null) => new(false, hata, talepId);
}

/// <summary>Talebi kabul eden bağışçı. Kurum yalnızca KABUL edenlerin iletişim bilgisini görür (açık rıza).</summary>
public record KabulEdenBagisci(string AdSoyad, string Telefon, string KanGrubu, string Il, double MesafeKm, DateTime YanitTarihi);

public record TalepDetayi(
    KanTalebi Talep,
    int BekleyenSayisi,
    int KabulSayisi,
    int RedSayisi,
    List<KabulEdenBagisci> KabulEdenler,
    int BildirilmemisUygunAdaySayisi);

public interface ITalepService
{
    Task<TalepIslemSonucu> OlusturAsync(int hastaneId, TalepOlusturmaIstegi istek);
    Task<TalepDetayi?> DetayGetirAsync(int hastaneId, int talepId);
    Task<TalepIslemSonucu> DahaFazlaBildirAsync(int hastaneId, int talepId);
    Task<TalepIslemSonucu> KapatAsync(int hastaneId, int talepId, TalepDurumu yeniDurum);
}

/// <summary>
/// Kurumun kan talebi işlemleri. Her metot hastaneId'yi oturumdan alır ve talebin
/// o hastaneye ait olduğunu kontrol eder (SAHİPLİK): bir hastane başka hastanenin
/// talebini göremez, kapatamaz, onun adına bildirim gönderemez.
/// </summary>
public sealed class TalepService(
    IKanTalebiRepository talepler,
    IHastaneRepository hastaneler,
    IRepository<KanGrubu> kanGruplari,
    IBildirimRepository bildirimler,
    IEslestirmeService eslestirme,
    TimeProvider zaman) : ITalepService
{
    public const int MaksimumAciklamaUzunlugu = 300;

    public async Task<TalepIslemSonucu> OlusturAsync(int hastaneId, TalepOlusturmaIstegi istek)
    {
        // ---- Doğrulama ----
        if (istek.UniteSayisi is < AciliyetKurallari.MinimumUnite or > AciliyetKurallari.MaksimumUnite)
            return TalepIslemSonucu.Hatali($"Ünite sayısı {AciliyetKurallari.MinimumUnite}–{AciliyetKurallari.MaksimumUnite} arasında olmalıdır.");
        if (!Enum.IsDefined(istek.Aciliyet))
            return TalepIslemSonucu.Hatali("Geçerli bir aciliyet seçin.");
        if (istek.Aciklama?.Length > MaksimumAciklamaUzunlugu)
            return TalepIslemSonucu.Hatali($"Açıklama en fazla {MaksimumAciklamaUzunlugu} karakter olabilir.");

        var hastane = await hastaneler.IdIleGetirAsync(hastaneId);
        if (hastane is null)
            return TalepIslemSonucu.Hatali("Hastane bulunamadı.");
        var kanGrubu = await kanGruplari.IdIleGetirAsync(istek.KanGrubuId);
        if (kanGrubu is null)
            return TalepIslemSonucu.Hatali("Geçerli bir kan grubu seçin.");

        // ---- İş kuralı: aynı kan grubu için ikinci açık talep açılamaz ----
        // Aynı bağışçılar aynı ihtiyaç için iki kez çağrılır ve kurum ihtiyacı iki yerden takip etmek zorunda kalırdı.
        if (await talepler.AcikTalebiGetirAsync(hastaneId, istek.KanGrubuId) is { } acik)
            return TalepIslemSonucu.Hatali($"{kanGrubu.Ad} için zaten açık bir talebiniz var (#{acik.Id}). Önce onu kapatın ya da o talepten daha fazla bağışçıya bildirim gönderin.", acik.Id);

        var talep = new KanTalebi
        {
            HastaneId = hastaneId,
            KanGrubuId = istek.KanGrubuId,
            UniteSayisi = istek.UniteSayisi,
            Aciliyet = istek.Aciliyet,
            Aciklama = string.IsNullOrWhiteSpace(istek.Aciklama) ? null : istek.Aciklama.Trim(),
            Durum = TalepDurumu.Acik,
            OlusturmaTarihi = Simdi(),
        };

        // ---- Eşleştirme + bildirim: talep ve bildirimleri TEK SaveChanges ile (tek transaction) kaydedilir ----
        var secilenler = await SiradakiAdaylarAsync(talep, hastane.Sehir, haric: null);
        foreach (var aday in secilenler)
            talep.Bildirimler.Add(BildirimOlustur(aday, hastane, kanGrubu, talep));

        await talepler.EkleAsync(talep);
        return new TalepIslemSonucu(true, TalepId: talep.Id, BildirimSayisi: secilenler.Count);
    }

    public async Task<TalepDetayi?> DetayGetirAsync(int hastaneId, int talepId)
    {
        var talep = await talepler.DetayGetirAsync(talepId);
        if (talep is null || talep.HastaneId != hastaneId) return null;   // sahiplik

        var kabulEdenler = talep.Bildirimler
            .Where(b => b.Yanit == BildirimYaniti.Kabul)
            .OrderBy(b => b.YanitTarihi)
            .Select(b => new KabulEdenBagisci(
                $"{b.Bagisci.Ad} {b.Bagisci.Soyad}",
                b.Bagisci.Telefon,
                b.Bagisci.KanGrubu.Ad,
                b.Bagisci.Sehir,
                Konum.IlKonumlari.MesafeKm(talep.Hastane.Sehir, b.Bagisci.Sehir),
                b.YanitTarihi!.Value))
            .ToList();

        var bildirilmemis = talep.Durum == TalepDurumu.Acik
            ? (await eslestirme.AdaylariBulAsync(talep.KanGrubuId, talep.Hastane.Sehir, talep.Aciliyet,
                talep.Bildirimler.Select(b => b.BagisciId).ToHashSet())).Count
            : 0;

        return new TalepDetayi(
            talep,
            talep.Bildirimler.Count(b => b.Yanit == BildirimYaniti.Beklemede),
            kabulEdenler.Count,
            talep.Bildirimler.Count(b => b.Yanit == BildirimYaniti.Red),
            kabulEdenler,
            bildirilmemis);
    }

    public async Task<TalepIslemSonucu> DahaFazlaBildirAsync(int hastaneId, int talepId)
    {
        var talep = await talepler.DetayGetirAsync(talepId);
        if (talep is null || talep.HastaneId != hastaneId)
            return TalepIslemSonucu.Hatali("Talep bulunamadı.");
        if (talep.Durum != TalepDurumu.Acik)
            return TalepIslemSonucu.Hatali("Kapanmış talep için bildirim gönderilemez.");

        // Daha önce bildirim gönderilenler hariç tutulur: aynı kişi aynı talep için iki kez rahatsız edilmez.
        var haric = await bildirimler.BildirilmisBagisciIdleriAsync(talepId);
        var secilenler = await SiradakiAdaylarAsync(talep, talep.Hastane.Sehir, haric);
        if (secilenler.Count == 0)
            return TalepIslemSonucu.Hatali("Arama alanında bildirim gönderilebilecek başka uygun bağışçı yok.");

        await bildirimler.TopluEkleAsync(secilenler.Select(a => BildirimOlustur(a, talep.Hastane, talep.KanGrubu, talep)));
        return new TalepIslemSonucu(true, TalepId: talepId, BildirimSayisi: secilenler.Count);
    }

    public async Task<TalepIslemSonucu> KapatAsync(int hastaneId, int talepId, TalepDurumu yeniDurum)
    {
        if (yeniDurum is not (TalepDurumu.Karsilandi or TalepDurumu.Iptal))
            return TalepIslemSonucu.Hatali("Talep yalnızca 'Karşılandı' ya da 'İptal' olarak kapatılabilir.");

        var talep = await talepler.IdIleGetirAsync(talepId);
        if (talep is null || talep.HastaneId != hastaneId)
            return TalepIslemSonucu.Hatali("Talep bulunamadı.");
        if (talep.Durum != TalepDurumu.Acik)
            return TalepIslemSonucu.Hatali("Bu talep zaten kapatılmış.");

        talep.Durum = yeniDurum;
        talep.KapanisTarihi = Simdi();
        await talepler.GuncelleAsync(talep);
        return new TalepIslemSonucu(true, TalepId: talepId);
    }

    private async Task<List<EslesmeAdayi>> SiradakiAdaylarAsync(KanTalebi talep, string il, IReadOnlySet<int>? haric) =>
        (await eslestirme.AdaylariBulAsync(talep.KanGrubuId, il, talep.Aciliyet, haric))
            .Take(AciliyetKurallari.BildirimKotasi(talep.Aciliyet, talep.UniteSayisi))
            .ToList();

    private Bildirim BildirimOlustur(EslesmeAdayi aday, Hastane hastane, KanGrubu kanGrubu, KanTalebi talep) => new()
    {
        BagisciId = aday.Bagisci.Id,
        // Yeni talepte Id henüz 0'dır; bildirim talep.Bildirimler koleksiyonuna eklendiği için EF kaydederken doldurur.
        // Var olan talepte talep nesnesi takip edilmediğinden yalnızca Id ile bağlanır (nesne bağlansaydı EF talebi yeniden eklemeye çalışırdı).
        KanTalebiId = talep.Id,
        GonderimTarihi = Simdi(),
        Yanit = BildirimYaniti.Beklemede,
        Mesaj = (talep.Aciliyet == Aciliyet.Acil ? "ACİL: " : "")
              + $"{hastane.Ad} ({hastane.Sehir}) {kanGrubu.Ad} kan ihtiyacı: {talep.UniteSayisi} ünite.",
    };

    private DateTime Simdi() => zaman.GetUtcNow().UtcDateTime;
}
