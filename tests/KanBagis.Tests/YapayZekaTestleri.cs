using System.Text.Json;
using KanBagis.Business.Tahmin;
using KanBagis.DataAccess.Repositories;
using KanBagis.DataAccess.Seed;
using KanBagis.Entities;

namespace KanBagis.Tests;

/// <summary>
/// Hafta 10 — yapay zekâ bileşeni. Gerçek API ÇAĞRILMAZ: <see cref="SahteLlm"/> istenen çıktıyı döner.
/// Böylece doğru, bozuk, eksik, boş ve reddedilmiş çıktıların hepsi tekrarlanabilir biçimde sınanır.
/// </summary>
public class YapayZekaTestleri
{
    private static readonly DateTimeOffset Simdi = new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);

    // ---------------- İstatistiksel taban tahmin ----------------

    private static GrupGecmisi Gecmis(int stok, params int[] haftalar) => new(1, "A Rh+", haftalar, stok);

    [Fact]
    public void Sabit_talepte_tahmin_ortalamadir_egilim_sifirdir()
    {
        var t = IstatistikselTahmin.Hesapla(Gecmis(20, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5));

        Assert.Equal(5, t.GelecekHaftaTahmini);
        Assert.Equal(0, t.HaftalikEgilim);
        Assert.Equal(4.0, t.KarsilamaHaftasi);
        Assert.Equal(RiskSeviyesi.Dusuk, t.Risk);
        Assert.False(t.VeriYetersiz);
    }

    [Fact]
    public void Artan_talepte_tahmin_egilimi_izler()
    {
        // 1, 2, ..., 12 → eğim 1, gelecek hafta 13.
        var t = IstatistikselTahmin.Hesapla(Gecmis(10, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12));

        Assert.Equal(1, t.HaftalikEgilim);
        Assert.Equal(13, t.GelecekHaftaTahmini);
        Assert.Equal(RiskSeviyesi.Kritik, t.Risk);   // 10 / 13 < 1 hafta
    }

    [Fact]
    public void Az_veride_egilim_hesaplanmaz_veri_yetersiz_denir()
    {
        // Yalnızca 2 haftada talep var (en az 4 gerekir); sondaki 8 eğilim gibi görünse de kullanılmaz.
        var t = IstatistikselTahmin.Hesapla(Gecmis(5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4, 8));

        Assert.True(t.VeriYetersiz);
        Assert.Equal(0, t.HaftalikEgilim);
        Assert.Equal(1, t.GelecekHaftaTahmini);   // ortalama 1
        Assert.Contains("yalnızca 2 haftasında", t.Gerekce);
    }

    [Fact]
    public void Hic_talep_yoksa_stok_suresiz_yeter_risk_dusuk()
    {
        var t = IstatistikselTahmin.Hesapla(Gecmis(0, new int[12]));

        Assert.Equal(0, t.GelecekHaftaTahmini);
        Assert.Null(t.KarsilamaHaftasi);
        Assert.Equal(RiskSeviyesi.Dusuk, t.Risk);
    }

    [Theory]
    [InlineData(0.5, RiskSeviyesi.Kritik)]
    [InlineData(1.0, RiskSeviyesi.Yuksek)]
    [InlineData(3.9, RiskSeviyesi.Orta)]
    [InlineData(4.0, RiskSeviyesi.Dusuk)]
    public void Risk_esikleri(double hafta, RiskSeviyesi beklenen) =>
        Assert.Equal(beklenen, IstatistikselTahmin.RiskHesapla(hafta));

    // ---------------- Çıktı doğrulama ----------------

    /// <summary>8 grup: A Rh+ stok 20 / ~5 hafta (düşük) ... 0 Rh- stok 2 / 4 haftalık (kritik).</summary>
    private static IReadOnlyList<GrupTahmini> Taban() => IstatistikselTahmin.Hesapla(
        TahminIstemi.KanGruplari.Select((ad, i) => new GrupGecmisi(i + 1, ad,
            Enumerable.Repeat(ad == "0 Rh-" ? 4 : 3, 12).ToArray(), ad == "0 Rh-" ? 2 : 20)).ToList());

    private static string Cikti(IEnumerable<GrupTahmini> gruplar, Func<GrupTahmini, object>? degistir = null, string yorum = "0 Rh- için kampanya gerekli.") =>
        JsonSerializer.Serialize(new
        {
            gruplar = gruplar.Select(g => degistir?.Invoke(g) ?? new
            {
                kanGrubu = g.KanGrubu,
                gelecekHaftaTahmini = g.GelecekHaftaTahmini,
                risk = g.Risk.ToString(),
                gerekce = "Ortalama sabit.",
            }),
            genelYorum = yorum,
        });

    [Fact]
    public void Gecerli_cikti_duzeltmeden_kabul_edilir()
    {
        var taban = Taban();
        var d = CiktiDogrulayici.Dogrula(Cikti(taban), taban);

        Assert.True(d.Kullanilabilir);
        Assert.False(d.Duzeltildi);
        Assert.Empty(d.Notlar);
        Assert.Equal(8, d.Gruplar.Count);
        Assert.Equal("0 Rh- için kampanya gerekli.", d.GenelYorum);
    }

    [Theory]
    [InlineData(null, "boş")]
    [InlineData("   ", "boş")]
    [InlineData("{ bozuk json", "JSON değil")]
    [InlineData("{\"sonuc\": []}", "gruplar")]
    [InlineData("[1,2,3]", "gruplar")]
    public void Bos_ya_da_bozuk_cikti_reddedilir_taban_kullanilir(string? ham, string beklenenNot)
    {
        var taban = Taban();
        var d = CiktiDogrulayici.Dogrula(ham, taban);

        Assert.False(d.Kullanilabilir);
        Assert.Same(taban, d.Gruplar);
        Assert.Contains(d.Notlar, n => n.Contains(beklenenNot));
    }

    [Fact]
    public void Ayni_grup_iki_kez_gelirse_reddedilir()
    {
        var taban = Taban();
        var d = CiktiDogrulayici.Dogrula(Cikti(taban.Append(taban[0])), taban);

        Assert.False(d.Kullanilabilir);
        Assert.Contains(d.Notlar, n => n.Contains("iki kez"));
    }

    [Fact]
    public void Bilinmeyen_kan_grubu_reddedilir()
    {
        var taban = Taban();
        var d = CiktiDogrulayici.Dogrula(Cikti(taban, g => new { kanGrubu = g.KanGrubu == "0 Rh+" ? "O+" : g.KanGrubu, gelecekHaftaTahmini = 3, risk = "Dusuk", gerekce = "x" }), taban);

        Assert.False(d.Kullanilabilir);
        Assert.Contains(d.Notlar, n => n.Contains("bilinmeyen kan grubu (O+)"));
    }

    [Fact]
    public void Gruplarin_yarisindan_fazlasi_eksikse_reddedilir()
    {
        var taban = Taban();
        var d = CiktiDogrulayici.Dogrula(Cikti(taban.Take(3)), taban);

        Assert.False(d.Kullanilabilir);
        Assert.Contains(d.Notlar, n => n.Contains("5/8 kan grubu eksik"));
    }

    [Fact]
    public void Tek_grup_eksikse_tabandan_tamamlanir()
    {
        var taban = Taban();
        var d = CiktiDogrulayici.Dogrula(Cikti(taban.Skip(1)), taban);

        Assert.True(d.Kullanilabilir);
        Assert.True(d.Duzeltildi);
        Assert.Equal(8, d.Gruplar.Count);
        Assert.Contains(d.Notlar, n => n.StartsWith("A Rh+: çıktıda yok"));
    }

    [Theory]
    [InlineData(-3)]
    [InlineData(5000)]
    public void Negatif_ya_da_ucuk_tahmin_tabanla_degistirilir(int tahmin)
    {
        var taban = Taban();
        var d = CiktiDogrulayici.Dogrula(Cikti(taban, g => new { kanGrubu = g.KanGrubu, gelecekHaftaTahmini = g.KanGrubu == "A Rh+" ? tahmin : g.GelecekHaftaTahmini, risk = g.Risk.ToString(), gerekce = "x" }), taban);

        Assert.True(d.Kullanilabilir);
        Assert.Equal(3, d.Gruplar.Single(g => g.KanGrubu == "A Rh+").GelecekHaftaTahmini);
        Assert.Contains(d.Notlar, n => n.StartsWith("A Rh+: tahmin geçersiz"));
    }

    [Fact]
    public void Gecersiz_risk_degeri_hesaplanan_riskle_degistirilir()
    {
        var taban = Taban();
        var d = CiktiDogrulayici.Dogrula(Cikti(taban, g => new { kanGrubu = g.KanGrubu, gelecekHaftaTahmini = g.GelecekHaftaTahmini, risk = g.KanGrubu == "B Rh+" ? "cok-iyi" : g.Risk.ToString(), gerekce = "x" }), taban);

        Assert.Equal(RiskSeviyesi.Dusuk, d.Gruplar.Single(g => g.KanGrubu == "B Rh+").Risk);
        Assert.Contains(d.Notlar, n => n.StartsWith("B Rh+: risk değeri geçersiz"));
    }

    [Fact]
    public void Guvenlik_kurali_yapay_zeka_kritik_uyariyi_dusuremez()
    {
        // Taban: 0 Rh- stok 2, haftada 4 → KRİTİK. Model tahmini de 4 ama "Dusuk" diyor.
        var taban = Taban();
        Assert.Equal(RiskSeviyesi.Kritik, taban.Single(g => g.KanGrubu == "0 Rh-").Risk);

        var d = CiktiDogrulayici.Dogrula(Cikti(taban, g => new { kanGrubu = g.KanGrubu, gelecekHaftaTahmini = g.GelecekHaftaTahmini, risk = g.KanGrubu == "0 Rh-" ? "Dusuk" : g.Risk.ToString(), gerekce = "x" }), taban);

        Assert.True(d.Kullanilabilir);
        Assert.True(d.Duzeltildi);
        Assert.Equal(RiskSeviyesi.Kritik, d.Gruplar.Single(g => g.KanGrubu == "0 Rh-").Risk);
    }

    [Fact]
    public void Model_tahmini_dusurse_bile_taban_kritikse_uyari_korunur()
    {
        // Model 0 Rh- için tahmini 1'e indiriyor (stok 2 → 2 hafta yeter → "Orta") ve "Orta" diyor; taban kritik.
        var taban = Taban();
        var d = CiktiDogrulayici.Dogrula(Cikti(taban, g => new { kanGrubu = g.KanGrubu, gelecekHaftaTahmini = g.KanGrubu == "0 Rh-" ? 1 : g.GelecekHaftaTahmini, risk = g.KanGrubu == "0 Rh-" ? "Orta" : g.Risk.ToString(), gerekce = "x" }), taban);

        var grup = d.Gruplar.Single(g => g.KanGrubu == "0 Rh-");
        Assert.Equal(1, grup.GelecekHaftaTahmini);          // modelin sayısal tahmini kullanılır...
        Assert.Equal(RiskSeviyesi.Kritik, grup.Risk);       // ...ama uyarı seviyesi düşürülmez
        Assert.Contains(d.Notlar, n => n.Contains("güvenlik için 'Kritik' korundu"));
    }

    [Fact]
    public void Riskin_kendi_tahminiyle_celismesi_duzeltilir()
    {
        // A Rh+ stok 20; model tahmini 8 diyor (2.5 hafta → Orta) ama riski "Dusuk" yazıyor.
        var taban = Taban();
        var d = CiktiDogrulayici.Dogrula(Cikti(taban, g => new { kanGrubu = g.KanGrubu, gelecekHaftaTahmini = g.KanGrubu == "A Rh+" ? 8 : g.GelecekHaftaTahmini, risk = g.Risk.ToString(), gerekce = "x" }), taban);

        Assert.Equal(RiskSeviyesi.Orta, d.Gruplar.Single(g => g.KanGrubu == "A Rh+").Risk);
        Assert.Contains(d.Notlar, n => n.StartsWith("A Rh+: risk 'Düşük' denmiş"));
    }

    [Fact]
    public void Bos_gerekce_tabandan_uzun_gerekce_kisaltilir()
    {
        var taban = Taban();
        var uzun = new string('x', 1000);
        var d = CiktiDogrulayici.Dogrula(Cikti(taban, g => new { kanGrubu = g.KanGrubu, gelecekHaftaTahmini = g.GelecekHaftaTahmini, risk = g.Risk.ToString(), gerekce = g.KanGrubu == "A Rh+" ? "" : uzun }), taban);

        Assert.Equal(taban[0].Gerekce, d.Gruplar[0].Gerekce);
        Assert.Equal(CiktiDogrulayici.GerekceEnFazla, d.Gruplar[1].Gerekce.Length);
    }

    // ---------------- Servis: uçtan uca (sahte model + gerçek SQLite) ----------------

    private static (TahminService Servis, TestVeritabani Db) Kur(SahteLlm llm)
    {
        var db = new TestVeritabani();
        var servis = new TahminService(new TahminRepository(db.Context), new KanStokRepository(db.Context),
            new EfRepository<KanGrubu>(db.Context), llm, db.Context, new AyarlanabilirZaman(Simdi));
        return (servis, db);
    }

    [Fact]
    public async Task Anahtar_yoksa_istatistiksel_tahmin_doner_ve_gunluge_yazilir()
    {
        var llm = new SahteLlm(kullanilabilir: false);
        var (servis, db) = Kur(llm);
        using var _ = db;

        var sonuc = await servis.CalistirAsync();

        Assert.Equal(TahminKaynagi.Istatistik, sonuc.Kaynak);
        Assert.Equal(8, sonuc.Gruplar.Count);
        Assert.Equal(0, llm.CagriSayisi);
        var kayit = Assert.Single(await servis.GunlukAsync());
        Assert.StartsWith("[" + TahminIstemi.Surum + "]", kayit.Istem);   // istem yine belgelenir
        Assert.Null(kayit.HamCikti);
        Assert.Contains("Yapay zekâ kapalı", kayit.DogrulamaNotlari);
    }

    [Fact]
    public async Task Gecerli_model_ciktisi_kullanilir_istem_ve_ham_cikti_saklanir()
    {
        var llm = new SahteLlm(taban => Cikti(taban));
        var (servis, db) = Kur(llm);
        using var _ = db;
        await servis.SentetikGecmisUretAsync();

        var sonuc = await servis.CalistirAsync();

        Assert.Equal(TahminKaynagi.YapayZeka, sonuc.Kaynak);
        Assert.Equal("0 Rh- için kampanya gerekli.", sonuc.GenelYorum);
        var kayit = (await servis.KayitGetirAsync(sonuc.KayitId))!;
        Assert.StartsWith("[" + TahminIstemi.Surum + "]", kayit.Istem);
        Assert.Equal(llm.SonCikti, kayit.HamCikti);
        Assert.Equal("sahte-model", kayit.Model);
    }

    [Fact]
    public async Task Istemde_kisisel_veri_yoktur_yalnizca_toplu_sayilar_vardir()
    {
        var llm = new SahteLlm(taban => Cikti(taban));
        var (servis, db) = Kur(llm);
        using var _ = db;
        var bagiscilar = db.Context.Bagiscilar.ToList();
        Assert.NotEmpty(bagiscilar);

        await servis.CalistirAsync();

        foreach (var b in bagiscilar)
        {
            Assert.DoesNotContain(b.TcKimlikNo, llm.SonIstem);
            Assert.DoesNotContain(b.Telefon, llm.SonIstem);
            Assert.DoesNotContain(b.Soyad, llm.SonIstem);
        }
        Assert.Contains("\"mevcutStok\"", llm.SonIstem);
    }

    [Theory]
    [InlineData("bozuk")]
    [InlineData("ret")]
    [InlineData("hata")]
    public async Task Hatali_bos_ya_da_reddedilen_ciktida_taban_tahmine_donulur(string durum)
    {
        var llm = durum switch
        {
            "bozuk" => new SahteLlm(_ => "Tabii! İşte tahmin: {gruplar: ...}"),
            "ret" => new SahteLlm(_ => null, reddet: true),
            _ => new SahteLlm(_ => null, hata: "Zaman aşımı (90 sn)."),
        };
        var (servis, db) = Kur(llm);
        using var _ = db;

        var sonuc = await servis.CalistirAsync();

        Assert.Equal(TahminKaynagi.Istatistik, sonuc.Kaynak);
        Assert.Equal(8, sonuc.Gruplar.Count);
        Assert.NotEmpty(sonuc.Notlar);
        var kayit = (await servis.KayitGetirAsync(sonuc.KayitId))!;
        Assert.NotNull(kayit.Istem);   // istem yine belgelenir
    }

    [Fact]
    public async Task Duzeltilen_cikti_kaynagi_duzeltildi_olarak_isaretlenir()
    {
        var llm = new SahteLlm(taban => Cikti(taban, g => new { kanGrubu = g.KanGrubu, gelecekHaftaTahmini = g.GelecekHaftaTahmini, risk = "Dusuk", gerekce = "x" }));
        var (servis, db) = Kur(llm);
        using var _ = db;

        await servis.SentetikGecmisUretAsync();
        var sonuc = await servis.CalistirAsync();   // 0 Rh-: artan talep, stok 2 → taban kritik → "Dusuk" düzeltilir

        Assert.Equal(TahminKaynagi.YapayZekaDuzeltildi, sonuc.Kaynak);
        Assert.Contains(sonuc.Notlar, n => n.StartsWith("0 Rh-:"));
    }

    [Fact]
    public async Task Son_tahmin_onbellekten_okunur()
    {
        var (servis, db) = Kur(new SahteLlm(kullanilabilir: false));
        using var _ = db;
        Assert.Null(await servis.SonTahminAsync());

        var ilk = await servis.CalistirAsync();
        var son = await servis.SonTahminAsync();

        Assert.NotNull(son);
        Assert.Equal(ilk.KayitId, son.KayitId);
        Assert.Equal(ilk.Gruplar.Select(g => g.GelecekHaftaTahmini), son.Gruplar.Select(g => g.GelecekHaftaTahmini));
    }

    [Fact]
    public async Task Sentetik_gecmis_bir_kez_uretilir_ve_tahmine_yansir()
    {
        var (servis, db) = Kur(new SahteLlm(kullanilabilir: false));
        using var _ = db;

        var once = await servis.CalistirAsync();
        var adet = await servis.SentetikGecmisUretAsync();
        var tekrar = await servis.SentetikGecmisUretAsync();
        var sonra = await servis.CalistirAsync();

        Assert.True(adet > 50);
        Assert.Equal(0, tekrar);
        Assert.All(db.Context.KanTalepleri.Where(t => t.Aciklama == SentetikGecmis.Etiket), t => Assert.Equal(TalepDurumu.Karsilandi, t.Durum));
        Assert.True(sonra.Gruplar.Sum(g => g.OrtalamaHaftalik) > once.Gruplar.Sum(g => g.OrtalamaHaftalik));
        // 0 Rh-: artan talep + 2 ünite stok → kritik
        Assert.Equal(RiskSeviyesi.Kritik, sonra.Gruplar.Single(g => g.KanGrubu == "0 Rh-").Risk);
        // AB Rh-: çok seyrek → veri yetersiz
        Assert.True(sonra.Gruplar.Single(g => g.KanGrubu == "AB Rh-").VeriYetersiz);
    }

    [Fact]
    public void Sema_sekiz_kan_grubunu_ve_dort_risk_degerini_tanimlar()
    {
        var sema = JsonSerializer.Serialize(TahminIstemi.Sema(), new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        foreach (var g in TahminIstemi.KanGruplari) Assert.Contains($"\"{g}\"", sema);
        Assert.Contains("\"Kritik\"", sema);
        Assert.Contains("\"additionalProperties\":false", sema);
    }

    /// <summary>Gerçek API yerine kullanılan sahte model. Kendisine verilen tabana göre çıktı üretebilir.</summary>
    private sealed class SahteLlm(
        Func<IReadOnlyList<GrupTahmini>, string?>? uret = null,
        bool kullanilabilir = true,
        bool reddet = false,
        string? hata = null) : ILlmIstemcisi
    {
        public int CagriSayisi { get; private set; }
        public string SonIstem { get; private set; } = "";
        public string? SonCikti { get; private set; }
        public bool Kullanilabilir => kullanilabilir;
        public string Model => "sahte-model";

        public Task<LlmYaniti> JsonIsteAsync(string sistem, string kullanici, Dictionary<string, JsonElement> sema, CancellationToken iptal = default)
        {
            CagriSayisi++;
            SonIstem = sistem + kullanici;
            if (reddet) return Task.FromResult(new LlmYaniti(null, Model, Reddedildi: true, Hata: "Model isteği reddetti."));
            if (hata is not null) return Task.FromResult(new LlmYaniti(null, Model, Hata: hata));

            // Taban, istemdeki JSON'dan geri okunur: sahte model "veriyi okuyup" yanıt üretmiş olur.
            var json = kullanici[kullanici.IndexOf('{')..];
            var veri = JsonDocument.Parse(json).RootElement.GetProperty("kanGruplari");
            var taban = IstatistikselTahmin.Hesapla(veri.EnumerateArray().Select((g, i) => new GrupGecmisi(i + 1,
                g.GetProperty("kanGrubu").GetString()!,
                g.GetProperty("haftalikTalep").EnumerateArray().Select(v => v.GetInt32()).ToArray(),
                g.GetProperty("mevcutStok").GetInt32())).ToList());
            SonCikti = uret!(taban);
            return Task.FromResult(new LlmYaniti(SonCikti, Model));
        }
    }
}
