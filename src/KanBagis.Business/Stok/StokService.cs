using KanBagis.DataAccess.Repositories;
using KanBagis.Entities;

namespace KanBagis.Business.Stok;

public record StokHareketIstegi(int KanGrubuId, HareketNedeni Neden, int Miktar, string? Aciklama, bool KritikOnayi = false);

/// <summary>Kimlik formdan değil oturumdan gelir (sahiplik): hareketi kimin girdiği değiştirilemez.</summary>
public record IslemYapan(int KullaniciId, int HastaneId);

public record StokIslemCevabi(bool Basarili, IReadOnlyList<string> Hatalar, string? Mesaj = null, bool OnayGerekli = false,
    StokHareketi? Hareket = null, UyariDegisimi Uyari = UyariDegisimi.Yok)
{
    public static StokIslemCevabi Hatali(params string[] hatalar) => new(false, hatalar);
}

public record StokSatiri(KanStok Stok, StokUyarisi? AcikUyari);

public record StokOzeti(List<StokSatiri> Satirlar, List<StokUyarisi> AcikUyarilar, List<StokHareketi> SonHareketler)
{
    public int KritikSayisi => Satirlar.Count(s => s.Stok.Durum == StokDurumu.Kritik);
    public int DusukSayisi => Satirlar.Count(s => s.Stok.Durum == StokDurumu.Dusuk);
}

public interface IStokService
{
    Task<StokOzeti> OzetGetirAsync(int sonHareketAdedi = 10);
    Task<StokIslemCevabi> HareketGirAsync(StokHareketIstegi istek, IslemYapan yapan);
    Task<StokIslemCevabi> EsikGuncelleAsync(int kanGrubuId, int kritikEsik, int uyariEsigi);
    Task<List<StokHareketi>> HareketlerAsync(HareketFiltresi filtre);
    Task<List<StokUyarisi>> UyariGecmisiAsync(int adet = 50);
    Task<int> AcikUyariSayisiAsync();
}

/// <summary>
/// Stok Takip ve Uyarı Modülü'nün iş kuralları. Veritabanına yazmadan ÖNCE burada doğrulanır;
/// stoğun eksiye düşmemesi ayrıca veritabanında (atomik güncelleme + CHECK) garanti edilir.
/// </summary>
public sealed class StokService(IKanStokRepository stoklar, TimeProvider zaman) : IStokService
{
    public const int EnFazlaMiktar = 50;      // tek harekette en fazla ünite (yanlışlıkla 500 yazılmasına karşı)
    public const int EnFazlaEsik = 500;
    public const int AciklamaEnAz = 5;
    public const int AciklamaEnFazla = 250;

    /// <summary>Bu nedenlerde açıklama zorunlu: stok kaybı ya da düzeltme denetlenebilir olmalı.</summary>
    public static readonly HareketNedeni[] AciklamaZorunlu =
        [HareketNedeni.Imha, HareketNedeni.SayimEksigi, HareketNedeni.SayimFazlasi];

    public async Task<StokOzeti> OzetGetirAsync(int sonHareketAdedi = 10)
    {
        var stokListesi = await stoklar.KanGrubuylaTumuAsync();
        var uyarilar = await stoklar.AcikUyarilarAsync();
        var satirlar = stokListesi
            .Select(s => new StokSatiri(s, uyarilar.FirstOrDefault(u => u.KanGrubuId == s.KanGrubuId)))
            .ToList();
        var hareketler = await stoklar.HareketleriGetirAsync(new HareketFiltresi(Adet: sonHareketAdedi));
        return new StokOzeti(satirlar, uyarilar, hareketler);
    }

    public async Task<StokIslemCevabi> HareketGirAsync(StokHareketIstegi istek, IslemYapan yapan)
    {
        // 1) Girdi doğrulama
        var hatalar = new List<string>();
        if (!Enum.IsDefined(istek.Neden)) hatalar.Add("Geçersiz hareket nedeni.");
        else if (istek.Neden == HareketNedeni.Bagis)
            hatalar.Add("Bağış girişi elle yapılamaz; bağış, talep sayfasındaki \"Bağış alındı\" ile kaydedilir.");

        if (istek.Miktar < 1 || istek.Miktar > EnFazlaMiktar)
            hatalar.Add($"Miktar 1 ile {EnFazlaMiktar} ünite arasında olmalı.");

        var aciklama = string.IsNullOrWhiteSpace(istek.Aciklama) ? null : istek.Aciklama.Trim();
        if (aciklama?.Length > AciklamaEnFazla)
            hatalar.Add($"Açıklama en fazla {AciklamaEnFazla} karakter olabilir.");
        if (AciklamaZorunlu.Contains(istek.Neden) && (aciklama is null || aciklama.Length < AciklamaEnAz))
            hatalar.Add($"{NedenAdi(istek.Neden)} için açıklama zorunlu (en az {AciklamaEnAz} karakter).");

        var stok = await stoklar.KanGrubuIleGetirAsync(istek.KanGrubuId);
        if (stok is null) hatalar.Add("Kan grubu bulunamadı.");
        if (hatalar.Count > 0) return new StokIslemCevabi(false, hatalar);

        var tur = StokHareketKurallari.Tur(istek.Neden);

        // 2) İş kuralları (çıkış)
        if (tur == HareketTuru.Cikis)
        {
            if (istek.Miktar > stok!.UniteSayisi)
                return StokIslemCevabi.Hatali($"Stokta yalnızca {stok.UniteSayisi} ünite {stok.KanGrubu.Ad} var; {istek.Miktar} ünite çıkış yapılamaz.");

            // Çıkış stoğu kritik eşiğe (ya da altına) indirecekse kullanıcıdan açık onay istenir.
            var kalan = stok.UniteSayisi - istek.Miktar;
            if (kalan <= stok.KritikEsik && !istek.KritikOnayi)
                return new StokIslemCevabi(false,
                    [$"Bu çıkıştan sonra {stok.KanGrubu.Ad} stoğu {kalan} üniteye düşecek (kritik eşik {stok.KritikEsik}). Onaylıyorsanız kutuyu işaretleyip tekrar gönderin."],
                    OnayGerekli: true);
        }

        // 3) Kayıt (atomik: stok + hareket + uyarı)
        var sonuc = await stoklar.HareketUygulaAsync(new StokHareketi
        {
            KanGrubuId = istek.KanGrubuId,
            Tur = tur,
            Neden = istek.Neden,
            Miktar = istek.Miktar,
            Aciklama = aciklama,
            Tarih = zaman.GetUtcNow().UtcDateTime,
            HastaneId = yapan.HastaneId,
            KullaniciId = yapan.KullaniciId,
        });

        return sonuc.Durum switch
        {
            StokIslemDurumu.Basarili => new StokIslemCevabi(true, [], Mesaj(sonuc, stok!.KanGrubu.Ad), Hareket: sonuc.Hareket, Uyari: sonuc.Uyari),
            // Doğrulama ile kayıt arasında başka bir kullanıcı çıkış yaptıysa buraya düşülür.
            StokIslemDurumu.StokYetersiz => StokIslemCevabi.Hatali($"Stok bu arada değişti; şu an {sonuc.MevcutStok} ünite var. Çıkış yapılmadı."),
            _ => StokIslemCevabi.Hatali("Kan grubu için stok kaydı yok."),
        };
    }

    public async Task<StokIslemCevabi> EsikGuncelleAsync(int kanGrubuId, int kritikEsik, int uyariEsigi)
    {
        var hatalar = new List<string>();
        if (kritikEsik < 0 || uyariEsigi < 0) hatalar.Add("Eşikler negatif olamaz.");
        if (kritikEsik > EnFazlaEsik || uyariEsigi > EnFazlaEsik) hatalar.Add($"Eşikler en fazla {EnFazlaEsik} olabilir.");
        if (uyariEsigi <= kritikEsik) hatalar.Add("Uyarı eşiği kritik eşikten büyük olmalı (önce \"düşük\", sonra \"kritik\" uyarısı).");
        if (hatalar.Count > 0) return new StokIslemCevabi(false, hatalar);

        var sonuc = await stoklar.EsikGuncelleAsync(kanGrubuId, kritikEsik, uyariEsigi, zaman.GetUtcNow().UtcDateTime);
        if (sonuc.Durum != StokIslemDurumu.Basarili) return StokIslemCevabi.Hatali("Kan grubu bulunamadı.");

        var ek = sonuc.Uyari switch
        {
            UyariDegisimi.Acildi => " Yeni eşiğe göre uyarı açıldı.",
            UyariDegisimi.Kapandi => " Yeni eşiğe göre uyarı kapandı.",
            UyariDegisimi.SeviyeDegisti => " Uyarı seviyesi değişti.",
            _ => "",
        };
        return new StokIslemCevabi(true, [], $"Eşikler güncellendi (kritik ≤ {kritikEsik}, düşük ≤ {uyariEsigi}).{ek}", Uyari: sonuc.Uyari);
    }

    public Task<List<StokHareketi>> HareketlerAsync(HareketFiltresi filtre) =>
        stoklar.HareketleriGetirAsync(filtre with { Adet = Math.Clamp(filtre.Adet, 1, 500) });

    public Task<List<StokUyarisi>> UyariGecmisiAsync(int adet = 50) => stoklar.UyariGecmisiAsync(adet);

    public Task<int> AcikUyariSayisiAsync() => stoklar.AcikUyariSayisiAsync();

    private static string Mesaj(StokIslemSonucu s, string grup)
    {
        var h = s.Hareket!;
        var metin = $"{grup}: {(h.Tur == HareketTuru.Giris ? "+" : "−")}{h.Miktar} ünite ({NedenAdi(h.Neden)}). Stok {h.OncekiStok} → {h.SonrakiStok}.";
        return s.Uyari switch
        {
            // tr-TR: "Kritik" → "KRİTİK". ToUpperInvariant "KRITIK" yazıyordu (H15-01).
            UyariDegisimi.Acildi => metin + $" ⚠ {DurumAdi(s.YeniDurum).ToUpper(Turkce)} seviye uyarısı açıldı.",
            UyariDegisimi.SeviyeDegisti => metin + $" ⚠ Uyarı seviyesi: {DurumAdi(s.YeniDurum)}.",
            UyariDegisimi.Kapandi => metin + " Stok normale döndü, uyarı kapandı.",
            _ => metin,
        };
    }

    private static readonly System.Globalization.CultureInfo Turkce = new("tr-TR");

    public static string NedenAdi(HareketNedeni n) => n switch
    {
        HareketNedeni.Bagis => "Bağış",
        HareketNedeni.TransferGiris => "Transfer (gelen)",
        HareketNedeni.SayimFazlasi => "Sayım fazlası",
        HareketNedeni.Transfuzyon => "Transfüzyon (kullanım)",
        HareketNedeni.TransferCikis => "Transfer (giden)",
        HareketNedeni.Imha => "İmha",
        HareketNedeni.SayimEksigi => "Sayım eksiği",
        _ => n.ToString(),
    };

    public static string DurumAdi(StokDurumu d) => d switch
    {
        StokDurumu.Kritik => "Kritik",
        StokDurumu.Dusuk => "Düşük",
        _ => "Normal",
    };
}
