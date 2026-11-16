using System.Diagnostics;
using System.Text.Json;
using KanBagis.DataAccess;
using KanBagis.DataAccess.Repositories;
using KanBagis.DataAccess.Seed;
using KanBagis.Entities;

namespace KanBagis.Business.Tahmin;

public interface ITahminService
{
    /// <summary>Yeni tahmin üretir ve günlüğe yazar. Yapay zekâ kullanılamazsa istatistiksel tahmin döner; asla boş dönmez.</summary>
    Task<TahminSonucu> CalistirAsync(CancellationToken iptal = default);

    /// <summary>En son kaydedilen tahmin (önbellek). Hiç yoksa null.</summary>
    Task<TahminSonucu?> SonTahminAsync();

    Task<List<TahminKaydi>> GunlukAsync(int adet = 20);
    Task<TahminKaydi?> KayitGetirAsync(int id);

    bool YapayZekaKullanilabilir { get; }

    /// <summary>Demo için son 12 haftaya SENTETİK talep geçmişi ekler (zaten varsa eklemez). Eklenen talep sayısı.</summary>
    Task<int> SentetikGecmisUretAsync();
}

public sealed class TahminService(
    ITahminRepository tahminler,
    IKanStokRepository stoklar,
    IRepository<KanGrubu> kanGruplari,
    ILlmIstemcisi llm,
    KanBagisDbContext db,
    TimeProvider zaman) : ITahminService
{
    public const int HaftaSayisi = 12;

    private static readonly JsonSerializerOptions JsonAyar = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    public bool YapayZekaKullanilabilir => llm.Kullanilabilir;

    public async Task<TahminSonucu> CalistirAsync(CancellationToken iptal = default)
    {
        var simdi = zaman.GetUtcNow().UtcDateTime;
        var gecmis = await GecmisiTopla(simdi);
        var taban = IstatistikselTahmin.Hesapla(gecmis);

        var kayit = new TahminKaydi { Tarih = simdi, Kaynak = TahminKaynagi.Istatistik };
        var notlar = new List<string>();
        IReadOnlyList<GrupTahmini> sonuc = taban;
        string? yorum = null;

        // İstem her zaman hazırlanıp günlüğe yazılır (yapay zekâ kapalıyken de neyin gönderileceği belgelenir).
        var kullanici = TahminIstemi.KullaniciMesaji(gecmis, taban);
        kayit.Istem = $"[{TahminIstemi.Surum}]\n--- SİSTEM ---\n{TahminIstemi.Sistem}\n--- KULLANICI ---\n{kullanici}";

        if (!llm.Kullanilabilir)
        {
            notlar.Add("Yapay zekâ kapalı (API anahtarı tanımlı değil); istem hazırlandı ama gönderilmedi, istatistiksel tahmin gösteriliyor.");
        }
        else
        {
            kayit.Model = llm.Model;

            var sure = Stopwatch.StartNew();
            var yanit = await llm.JsonIsteAsync(TahminIstemi.Sistem, kullanici, TahminIstemi.Sema(), iptal);
            kayit.SureMilisaniye = (int)sure.ElapsedMilliseconds;
            kayit.Model = yanit.Model;
            kayit.HamCikti = yanit.Metin;

            if (yanit.Metin is null)
            {
                notlar.Add($"Yapay zekâ yanıtı alınamadı: {yanit.Hata ?? "bilinmeyen hata"} İstatistiksel tahmin gösteriliyor.");
            }
            else
            {
                var d = CiktiDogrulayici.Dogrula(yanit.Metin, taban);
                notlar.AddRange(d.Notlar);
                if (d.Kullanilabilir)
                {
                    sonuc = d.Gruplar;
                    yorum = d.GenelYorum;
                    kayit.Kaynak = d.Duzeltildi ? TahminKaynagi.YapayZekaDuzeltildi : TahminKaynagi.YapayZeka;
                }
            }
        }

        kayit.DogrulamaNotlari = notlar.Count == 0 ? null : string.Join("\n", notlar);
        var tahmin = new TahminSonucu(0, simdi, kayit.Kaynak, kayit.Model, sonuc, yorum, notlar);
        kayit.SonucJson = JsonSerializer.Serialize(tahmin, JsonAyar);

        await tahminler.EkleAsync(kayit);
        return tahmin with { KayitId = kayit.Id };
    }

    public async Task<TahminSonucu?> SonTahminAsync()
    {
        var kayit = await tahminler.SonKaydiGetirAsync();
        return kayit is null ? null : Coz(kayit);
    }

    public Task<List<TahminKaydi>> GunlukAsync(int adet = 20) => tahminler.SonKayitlariGetirAsync(adet);

    public Task<int> SentetikGecmisUretAsync() =>
        SentetikGecmis.GerekirseUretAsync(db, zaman.GetUtcNow().UtcDateTime);

    public Task<TahminKaydi?> KayitGetirAsync(int id) => tahminler.IdIleGetirAsync(id);

    public static TahminSonucu? Coz(TahminKaydi kayit)
    {
        try
        {
            var t = JsonSerializer.Deserialize<TahminSonucu>(kayit.SonucJson, JsonAyar);
            return t is null ? null : t with { KayitId = kayit.Id };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Son 12 haftayı (bugün dahil) kan grubu × hafta tablosuna çevirir; talep olmayan hafta 0 sayılır.</summary>
    private async Task<List<GrupGecmisi>> GecmisiTopla(DateTime simdi)
    {
        var bitis = simdi.Date.AddDays(1);
        var baslangic = bitis.AddDays(-7 * HaftaSayisi);
        var haftalik = await tahminler.HaftalikTalepleriGetirAsync(baslangic, bitis);
        var stok = (await stoklar.KanGrubuylaTumuAsync()).ToDictionary(s => s.KanGrubuId, s => s.UniteSayisi);
        var gruplar = (await kanGruplari.TumunuGetirAsync()).OrderBy(g => g.Id);

        return gruplar.Select(g =>
        {
            var dizi = new int[HaftaSayisi];
            foreach (var h in haftalik.Where(h => h.KanGrubuId == g.Id))
                dizi[Math.Clamp((h.HaftaBaslangici - baslangic).Days / 7, 0, HaftaSayisi - 1)] += h.Unite;
            return new GrupGecmisi(g.Id, g.Ad, dizi, stok.GetValueOrDefault(g.Id));
        }).ToList();
    }
}
