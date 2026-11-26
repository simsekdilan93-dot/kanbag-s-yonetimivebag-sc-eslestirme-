using System.Globalization;
using System.Text;
using KanBagis.Business.Istatistik;
using KanBagis.DataAccess.Repositories;
using KanBagis.DataAccess.Seed;
using KanBagis.Entities;
using KanBagis.Web;

namespace KanBagis.Tests;

/// <summary>Hafta 12 — İstatistik panosu: metrik tanımları, toplulaştırma doğruluğu, grafik ve CSV.</summary>
public class IstatistikTestleri
{
    private static readonly DateTimeOffset Simdi = new(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTime T0 = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

    // ---------------- Oran ----------------

    [Fact]
    public void Payda_sifirsa_oran_yoktur_yuzde_sifir_yazilmaz()
    {
        var o = new Oran(0, 0);
        Assert.Null(o.Deger);
        Assert.Equal("veri yok", o.Metin);
    }

    [Fact]
    public void Oran_pay_ve_payda_ile_yazilir_kucuk_paydada_az_veri_denir()
    {
        Assert.Equal("%75 (3/4)", new Oran(3, 4).Metin);
        Assert.True(new Oran(3, 4).AzVeri);
        Assert.False(new Oran(4, 5).AzVeri);
    }

    // ---------------- Talep metrikleri ----------------

    [Fact]
    public void Karsilanma_orani_acik_talepleri_paydaya_katmaz()
    {
        TalepGrubu[] g =
        [
            new(1, TalepDurumu.Karsilandi, Aciliyet.Normal, 3, 6),
            new(1, TalepDurumu.Iptal, Aciliyet.Acil, 1, 2),
            new(2, TalepDurumu.Acik, Aciliyet.Acil, 4, 9),
        ];
        var m = MetrikHesap.Talep(g, []);

        Assert.Equal((8, 17, 4, 3, 1, 5), (m.TalepSayisi, m.TalepEdilenUnite, m.Acik, m.Karsilandi, m.Iptal, m.Acil));
        Assert.Equal(new Oran(3, 4), m.KarsilanmaOrani);      // 3 / (3 + 1), açık 4 talep hariç
        Assert.Equal(new Oran(5, 8), m.AcilOrani);
        Assert.Null(m.OrtalamaKarsilanmaSaat);                // süre verisi yok → "veri yok", 0 değil
    }

    [Fact]
    public void Sure_medyan_ve_ortalama_bozuk_kayit_haric()
    {
        KarsilanmaSuresi[] s =
        [
            new(T0, T0.AddHours(2)),
            new(T0, T0.AddHours(4)),
            new(T0, T0.AddHours(30)),
            new(T0, T0.AddHours(-5)),     // kapanış oluşturmadan önce: bozuk, sayılmaz
        ];
        var m = MetrikHesap.Talep([], s);

        Assert.Equal(12, m.OrtalamaKarsilanmaSaat);   // (2+4+30)/3
        Assert.Equal(4, m.MedyanKarsilanmaSaat);      // uç değer (30) medyanı etkilemez
    }

    [Theory]
    [InlineData(new double[] { 5 }, 5)]
    [InlineData(new double[] { 9, 1, 5 }, 5)]
    [InlineData(new double[] { 1, 2, 3, 10 }, 2.5)]
    public void Medyan(double[] degerler, double beklenen) => Assert.Equal(beklenen, MetrikHesap.Medyan(degerler));

    // ---------------- Bildirim özeti ----------------

    [Fact]
    public void Bildirim_oranlari_tanimlara_uygun()
    {
        BildirimGrubu[] g =
        [
            new(BildirimYaniti.Kabul, DisKanalDurumu.Gonderildi, 3),
            new(BildirimYaniti.Red, DisKanalDurumu.Basarisiz, 1),
            new(BildirimYaniti.Beklemede, DisKanalDurumu.Yok, 4),
            new(BildirimYaniti.Beklemede, DisKanalDurumu.Kuyrukta, 2),
        ];
        var b = MetrikHesap.Bildirim(g);

        Assert.Equal(10, b.Gonderilen);
        Assert.Equal(new Oran(4, 10), b.YanitOrani);           // kabul + red
        Assert.Equal(new Oran(3, 4), b.KabulOrani);            // bekleyenler hariç
        Assert.Equal(new Oran(3, 4), b.TelefonBasariOrani);    // telefonu kapalı (4) ve kuyrukta (2) hariç
        Assert.Equal(4, b.DisKanal[DisKanalDurumu.Yok]);
    }

    // ---------------- Haftalık dağılım ----------------

    [Fact]
    public void Haftalik_bos_haftalari_sifir_olarak_icerir_ve_sinirlara_uyar()
    {
        var bitis = T0.AddDays(21);   // 3 hafta: [T0, T0+7), [T0+7, T0+14), [T0+14, T0+21)
        TarihliMiktar[] talepler =
        [
            new(T0, 2),                       // ilk anı: 1. hafta
            new(T0.AddDays(7).AddTicks(-1), 1), // 1. haftanın son anı
            new(T0.AddDays(14), 4),           // 3. hafta
            new(bitis, 99),                   // bitiş dahil değil
            new(T0.AddDays(-1), 99),          // başlangıçtan önce
        ];
        var h = MetrikHesap.Haftalik(bitis, 3, talepler, [new(T0.AddDays(15), 5)]);

        Assert.Equal([3, 0, 4], h.Select(x => x.TalepUnite));
        Assert.Equal([0, 0, 5], h.Select(x => x.BagisUnite));
        Assert.Equal(T0, h[0].Baslangic);
    }

    [Theory]
    [InlineData(7, 1)]
    [InlineData(30, 5)]
    [InlineData(90, 13)]
    [InlineData(365, 13)]
    public void Hafta_sayisi(int gun, int beklenen) => Assert.Equal(beklenen, MetrikHesap.HaftaSayisi(gun));

    // ---------------- Servis: SQL toplulaştırma = elle hesap ----------------

    private static IstatistikService Servis(TestVeritabani db) => new(
        new IstatistikRepository(db.Context), new KanStokRepository(db.Context), new HastaneRepository(db.Context), new AyarlanabilirZaman(Simdi));

    [Fact]
    public async Task Pano_sayilari_ham_kayitlardan_elle_hesaplananla_ayni()
    {
        using var db = new TestVeritabani();
        var pano = await Servis(db).PanoAsync(new PanoFiltresi(365), hastaneId: 1);

        var bas = pano.Baslangic; var bit = pano.Bitis;
        var talepler = db.Context.KanTalepleri.Where(t => t.OlusturmaTarihi >= bas && t.OlusturmaTarihi < bit).ToList();
        var bagislar = db.Context.Bagislar.Where(b => b.BagisTarihi >= bas && b.BagisTarihi < bit).ToList();
        var bildirimler = db.Context.Bildirimler.Where(b => b.GonderimTarihi >= bas && b.GonderimTarihi < bit).ToList();
        Assert.NotEmpty(talepler);
        Assert.NotEmpty(bagislar);

        Assert.Equal(talepler.Count, pano.Talep.TalepSayisi);
        Assert.Equal(talepler.Sum(t => t.UniteSayisi), pano.Talep.TalepEdilenUnite);
        Assert.Equal(talepler.Count(t => t.Durum == TalepDurumu.Karsilandi), pano.Talep.Karsilandi);
        Assert.Equal(bagislar.Sum(b => b.UniteSayisi), pano.Bagis.BagisUnite);
        Assert.Equal(bildirimler.Count, pano.Bildirim.Gonderilen);
        Assert.Equal(bildirimler.Count(b => b.Yanit == BildirimYaniti.Kabul), pano.Bildirim.Kabul);

        // Kan grubu toplamları genel toplamla tutarlı.
        Assert.Equal(pano.Talep.TalepEdilenUnite, pano.KanGruplari.Sum(g => g.TalepUnite));
        Assert.Equal(pano.Bagis.BagisUnite, pano.KanGruplari.Sum(g => g.BagisUnite));
        // Bağış bağışçının kan grubuna sayılır.
        var bagisciGrubu = db.Context.Bagiscilar.ToDictionary(b => b.Id, b => b.KanGrubuId);
        foreach (var g in pano.KanGruplari)
            Assert.Equal(bagislar.Where(b => bagisciGrubu[b.BagisciId] == g.KanGrubuId).Sum(b => b.UniteSayisi), g.BagisUnite);
    }

    [Fact]
    public async Task Sentetik_gecmis_varsayilan_olarak_haric_istenirse_dahil_ve_notlu()
    {
        using var db = new TestVeritabani();
        await SentetikGecmis.GerekirseUretAsync(db.Context, Simdi.UtcDateTime);
        var servis = Servis(db);

        var gercek = await servis.PanoAsync(new PanoFiltresi(90), 1);
        var sentetikli = await servis.PanoAsync(new PanoFiltresi(90, SentetikDahil: true), 1);

        Assert.True(sentetikli.Talep.TalepSayisi > gercek.Talep.TalepSayisi + 50);
        Assert.Contains(sentetikli.VeriNotlari, n => n.Contains("SENTETİK"));
        Assert.DoesNotContain(gercek.VeriNotlari, n => n.Contains("SENTETİK"));
    }

    [Fact]
    public async Task Hastanem_suzgeci_yalnizca_o_hastanenin_kayitlarini_sayar()
    {
        using var db = new TestVeritabani();
        var servis = Servis(db);

        var bolge = await servis.PanoAsync(new PanoFiltresi(365), 1);
        var h1 = await servis.PanoAsync(new PanoFiltresi(365, YalnizcaHastanem: true), 1);
        var h2 = await servis.PanoAsync(new PanoFiltresi(365, YalnizcaHastanem: true), 2);

        Assert.Equal(db.Context.KanTalepleri.Count(t => t.HastaneId == 1), h1.Talep.TalepSayisi);
        Assert.Equal(bolge.Talep.TalepSayisi, h1.Talep.TalepSayisi + h2.Talep.TalepSayisi + db.Context.KanTalepleri.Count(t => t.HastaneId == 3));
        Assert.Equal("Örnek Devlet Hastanesi", h1.Kapsam);
    }

    [Fact]
    public async Task Talep_olmayan_donemde_veri_yok_notu_ve_ozet_metni()
    {
        using var db = new TestVeritabani();
        var pano = await Servis(db).PanoAsync(new PanoFiltresi(7), 1);   // örnek talepler Eylül-Ekim'de

        Assert.Equal(0, pano.Talep.TalepSayisi);
        Assert.Null(pano.Talep.KarsilanmaOrani.Deger);
        Assert.Contains(pano.VeriNotlari, n => n.Contains("hiç talep yok"));
        Assert.Contains("talep açılmadı", pano.OzetMetni);
        Assert.Contains("Kritik stok: AB Rh-, 0 Rh-", pano.OzetMetni);
    }

    [Fact]
    public async Task Gecersiz_gun_varsayilan_90_olur()
    {
        using var db = new TestVeritabani();
        var pano = await Servis(db).PanoAsync(new PanoFiltresi(-5), 1);
        Assert.Equal(90, pano.Filtre.Gun);
    }

    // ---------------- CSV ----------------

    [Fact]
    public async Task Csv_bom_ve_noktali_virgul_ile_turkce_excel_uyumlu()
    {
        using var db = new TestVeritabani();
        var bayt = await Servis(db).CsvAsync(new PanoFiltresi(365), 1);

        Assert.Equal(Encoding.UTF8.GetPreamble(), bayt[..3]);
        var metin = Encoding.UTF8.GetString(bayt[3..]);
        Assert.Contains("Metrik;Değer;Pay;Payda", metin);
        Assert.Contains("Kan grubu;Talep ünite;Bağış ünite;Stok;Stok durumu", metin);
        Assert.Contains("0 Rh-;", metin);
        Assert.Contains(";Kritik", metin);
        Assert.DoesNotContain("Dusuk", metin);   // kod adı değil Türkçe ("Düşük")
    }

    [Theory]
    [InlineData("=HYPERLINK(\"x\")", "\"'=HYPERLINK(\"\"x\"\")\"")]   // formül enjeksiyonu engellenir
    [InlineData("a;b", "\"a;b\"")]
    [InlineData("normal", "normal")]
    public void Csv_hucresi_kacislanir(string girdi, string beklenen) =>
        Assert.Equal(beklenen, IstatistikService.Hucre(girdi, new CultureInfo("tr-TR")));

    [Fact]
    public void Csv_ondalik_turkce_virgul() =>
        Assert.Equal("0,75", IstatistikService.Hucre(0.75, new CultureInfo("tr-TR")));

    // ---------------- Grafik ----------------

    [Theory]
    [InlineData(0, 1)]
    [InlineData(7, 10)]
    [InlineData(23, 25)]
    [InlineData(100, 100)]
    [InlineData(101, 200)]
    public void Eksen_ust_siniri_yuvarlak_ve_veriden_kucuk_degil(int enBuyuk, int beklenen) =>
        Assert.Equal(beklenen, SvgGrafik.EksenUstSiniri(enBuyuk));

    [Fact]
    public void Grafik_eksen_sifirdan_baslar_degerleri_yazar_metni_kodlar()
    {
        var svg = SvgGrafik.Sutun("Test", ["<script>", "B"], [new Seri("Talep", "#000", [3, 7])]).ToString()!;

        Assert.Contains(">0</text>", svg);               // eksen 0'dan
        Assert.Contains(">10</text>", svg);              // üst sınır 10
        Assert.Contains(">7</text>", svg);               // değer etiketi
        Assert.DoesNotContain("<script>", svg);
        Assert.Contains("&lt;script&gt;", svg);
    }

    [Fact]
    public void Bos_veride_grafik_veri_yok_yazar()
    {
        Assert.Contains("Bu dönemde veri yok", SvgGrafik.Sutun("T", ["A"], [new Seri("S", "#000", [0])]).ToString());
        Assert.Contains("Bu dönemde veri yok", SvgGrafik.Yigilmis("T", [new Parca("Kabul", "#000", 0)]).ToString());
    }

    [Fact]
    public void Yigilmis_grafik_sayi_ve_yuzdeyi_birlikte_yazar()
    {
        var svg = SvgGrafik.Yigilmis("Y", [new Parca("Kabul", "#0f0", 3), new Parca("Red", "#999", 1)]).ToString()!;
        Assert.Contains("Kabul: 3 (%75)", svg);
        Assert.Contains("Red: 1 (%25)", svg);
    }
}
