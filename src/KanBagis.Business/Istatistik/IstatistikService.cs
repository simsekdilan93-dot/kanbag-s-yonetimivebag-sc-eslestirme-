using System.Globalization;
using System.Text;
using KanBagis.DataAccess.Repositories;
using KanBagis.Entities;

namespace KanBagis.Business.Istatistik;

public interface IIstatistikService
{
    /// <summary>hastaneId: oturumdaki kurum (yalnızca "hastanem" süzgeci seçiliyse kullanılır).</summary>
    Task<Pano> PanoAsync(PanoFiltresi filtre, int hastaneId);

    /// <summary>Panonun CSV dökümü (Excel'de Türkçe açılsın diye ';' ayraç ve UTF-8 BOM).</summary>
    Task<byte[]> CsvAsync(PanoFiltresi filtre, int hastaneId);
}

public sealed class IstatistikService(
    IIstatistikRepository veri,
    IKanStokRepository stoklar,
    IHastaneRepository hastaneler,
    TimeProvider zaman) : IIstatistikService
{
    public async Task<Pano> PanoAsync(PanoFiltresi filtre, int hastaneId)
    {
        var gun = filtre.GecerliGun;
        var bitis = zaman.GetUtcNow().UtcDateTime.Date.AddDays(1);          // bugün dahil
        var haftaSayisi = MetrikHesap.HaftaSayisi(gun);
        var baslangic = bitis.AddDays(-gun);

        var s = new IstatistikSuzgeci(baslangic, bitis, filtre.YalnizcaHastanem ? hastaneId : null, filtre.SentetikDahil);
        // Haftalık grafik her zaman tam haftalarla çizilir; dönemi kapsayacak kadar geriye gidilir.
        var sHafta = s with { Baslangic = bitis.AddDays(-7 * haftaSayisi) };

        var talepGruplari = await veri.TalepleriTopla(s);
        var bagisGruplari = await veri.BagislariTopla(s);
        var talep = MetrikHesap.Talep(talepGruplari, await veri.KarsilanmaSureleri(s));
        var bagis = MetrikHesap.Bagis(bagisGruplari);
        var bildirim = MetrikHesap.Bildirim(await veri.BildirimleriTopla(s));
        var haftalar = MetrikHesap.Haftalik(bitis, haftaSayisi, await veri.TalepTarihleri(sHafta), await veri.BagisTarihleri(sHafta));

        var stokListesi = await stoklar.KanGrubuylaTumuAsync();
        var kanGruplari = stokListesi.Select(st => new KanGrubuSatiri(
            st.KanGrubuId, st.KanGrubu.Ad,
            talepGruplari.Where(x => x.KanGrubuId == st.KanGrubuId).Sum(x => x.Unite),
            bagisGruplari.Where(x => x.KanGrubuId == st.KanGrubuId).Sum(x => x.Unite),
            st.UniteSayisi, st.Durum)).ToList();

        var kapsam = filtre.YalnizcaHastanem
            ? (await hastaneler.IdIleGetirAsync(hastaneId))?.Ad ?? "Hastanem"
            : "Tüm hastaneler (bölge)";

        var notlar = VeriNotlari(filtre, talep, bildirim, gun, haftaSayisi);
        var pano = new Pano(filtre with { Gun = gun }, baslangic, bitis, kapsam, talep, bagis, bildirim, kanGruplari, haftalar,
            await stoklar.AcikUyariSayisiAsync(), notlar, "");
        return pano with { OzetMetni = OzetMetni(pano) };
    }

    /// <summary>Eksik/az veriyi GİZLEMEK yerine açıkça söyler.</summary>
    public static List<string> VeriNotlari(PanoFiltresi f, TalepMetrikleri t, BildirimOzeti b, int gun, int haftaSayisi)
    {
        var n = new List<string>();
        if (t.TalepSayisi == 0) n.Add("Seçilen dönemde hiç talep yok; talep oranları hesaplanamadı.");
        if (t.KarsilanmaOrani.Payda is > 0 and < Oran.GuvenilirEnAzPayda)
            n.Add($"Karşılanma oranı yalnızca {t.KarsilanmaOrani.Payda} sonuçlanmış talebe dayanıyor; yorumlarken dikkatli olun.");
        if (t.Acik > 0) n.Add($"{t.Acik} talep hâlâ açık; karşılanma oranına dahil edilmedi.");
        if (b.Gonderilen > 0 && b.KabulOrani.AzVeri) n.Add($"Kabul oranı yalnızca {b.KabulOrani.Payda} yanıta dayanıyor.");
        if (b.DisKanal[DisKanalDurumu.Yok] > 0)
            n.Add($"{b.DisKanal[DisKanalDurumu.Yok]} bildirim yalnızca site içinde kaldı (bağışçının telefon bildirimi kapalı); telefon başarı oranına dahil değil.");
        if (gun > 7 * haftaSayisi) n.Add($"Haftalık grafik son {haftaSayisi} haftayı gösterir; kartlar {gun} günün tamamını kapsar.");
        if (f.SentetikDahil) n.Add("SENTETİK talep geçmişi dahil: talep sayıları gerçek veri değildir (yalnızca demo).");
        return n;
    }

    /// <summary>Kurum yöneticisi için kısa düz metin özet (şablonla, sayılardan üretilir).</summary>
    public static string OzetMetni(Pano p)
    {
        var t = p.Talep;
        var sb = new StringBuilder($"Son {p.Filtre.Gun} günde ({p.Kapsam.ToLower(new CultureInfo("tr-TR"))}) ");
        if (t.TalepSayisi == 0) sb.Append("talep açılmadı. ");
        else
        {
            sb.Append($"{t.TalepSayisi} talep ({t.TalepEdilenUnite} ünite) açıldı");
            sb.Append(t.KarsilanmaOrani.Deger is { } k ? $"; sonuçlanan {t.KarsilanmaOrani.Payda} talebin %{k * 100:0}'i karşılandı. " : ". ");
        }
        sb.Append($"{p.Bagis.BagisSayisi} bağışla {p.Bagis.BagisUnite} ünite kan toplandı. ");

        var enCok = p.KanGruplari.Where(g => g.TalepUnite > 0).OrderByDescending(g => g.TalepUnite).FirstOrDefault();
        if (enCok is not null) sb.Append($"En çok talep edilen grup {enCok.KanGrubu} ({enCok.TalepUnite} ünite). ");

        var kritik = p.KanGruplari.Where(g => g.StokDurumu == StokDurumu.Kritik).Select(g => g.KanGrubu).ToList();
        if (kritik.Count > 0) sb.Append($"Kritik stok: {string.Join(", ", kritik)}.");
        else sb.Append("Kritik seviyede stok yok.");
        return sb.ToString().Trim();
    }

    public async Task<byte[]> CsvAsync(PanoFiltresi filtre, int hastaneId)
    {
        var p = await PanoAsync(filtre, hastaneId);
        var tr = new CultureInfo("tr-TR");
        var sb = new StringBuilder();
        void Satir(params object?[] h) => sb.AppendLine(string.Join(";", h.Select(x => Hucre(x, tr))));

        Satir("Kan Bağışı - İstatistik Panosu");
        Satir("Dönem", $"{p.Baslangic:dd.MM.yyyy} - {p.Bitis.AddDays(-1):dd.MM.yyyy}", "Kapsam", p.Kapsam);
        Satir();
        Satir("Metrik", "Değer", "Pay", "Payda");
        Satir("Talep sayısı", p.Talep.TalepSayisi);
        Satir("Talep edilen ünite", p.Talep.TalepEdilenUnite);
        Satir("Açık / Karşılandı / İptal", $"{p.Talep.Acik} / {p.Talep.Karsilandi} / {p.Talep.Iptal}");
        Satir("Karşılanma oranı", p.Talep.KarsilanmaOrani.Deger, p.Talep.KarsilanmaOrani.Pay, p.Talep.KarsilanmaOrani.Payda);
        Satir("Acil talep oranı", p.Talep.AcilOrani.Deger, p.Talep.AcilOrani.Pay, p.Talep.AcilOrani.Payda);
        Satir("Ortalama karşılanma (saat)", p.Talep.OrtalamaKarsilanmaSaat);
        Satir("Medyan karşılanma (saat)", p.Talep.MedyanKarsilanmaSaat);
        Satir("Bağış sayısı", p.Bagis.BagisSayisi);
        Satir("Bağış ünitesi", p.Bagis.BagisUnite);
        Satir("Gönderilen bildirim", p.Bildirim.Gonderilen);
        Satir("Yanıt oranı", p.Bildirim.YanitOrani.Deger, p.Bildirim.YanitOrani.Pay, p.Bildirim.YanitOrani.Payda);
        Satir("Kabul oranı", p.Bildirim.KabulOrani.Deger, p.Bildirim.KabulOrani.Pay, p.Bildirim.KabulOrani.Payda);
        Satir("Telefon gönderim başarısı", p.Bildirim.TelefonBasariOrani.Deger, p.Bildirim.TelefonBasariOrani.Pay, p.Bildirim.TelefonBasariOrani.Payda);
        Satir();
        Satir("Kan grubu", "Talep ünite", "Bağış ünite", "Stok", "Stok durumu");
        foreach (var g in p.KanGruplari) Satir(g.KanGrubu, g.TalepUnite, g.BagisUnite, g.Stok, Stok.StokService.DurumAdi(g.StokDurumu));
        Satir();
        Satir("Hafta başı", "Talep ünite", "Bağış ünite");
        foreach (var h in p.Haftalar) Satir(h.Baslangic.ToString("dd.MM.yyyy"), h.TalepUnite, h.BagisUnite);
        if (p.VeriNotlari.Count > 0)
        {
            Satir();
            foreach (var n in p.VeriNotlari) Satir("Not", n);
        }

        return [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(sb.ToString())];
    }

    /// <summary>
    /// CSV hücresi: oran 0-1 arası ondalık (Türkçe virgül) yazılır, boş değer boş hücre olur.
    /// "=, +, -, @" ile başlayan metinlerin başına ' eklenir (CSV/formül enjeksiyonuna karşı).
    /// </summary>
    public static string Hucre(object? x, CultureInfo tr)
    {
        var s = x switch
        {
            null => "",
            double d => d.ToString("0.####", tr),
            IFormattable f => f.ToString(null, tr),
            _ => x.ToString() ?? "",
        };
        if (s.Length > 0 && "=+-@".Contains(s[0]) && x is string) s = "'" + s;
        return s.Contains(';') || s.Contains('"') || s.Contains('\n') ? $"\"{s.Replace("\"", "\"\"")}\"" : s;
    }
}
