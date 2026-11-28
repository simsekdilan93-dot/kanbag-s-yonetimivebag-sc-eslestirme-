using KanBagis.Business;
using KanBagis.Business.Bagiscilar;
using KanBagis.Business.Guvenlik;
using KanBagis.Business.Kimlik;
using KanBagis.Business.Talepler;
using KanBagis.DataAccess;
using KanBagis.DataAccess.Repositories;
using KanBagis.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace KanBagis.Tests;

/// <summary>
/// Hafta 13 — Bulunan hataların REGRESYON testleri. Her test önce hatayı yeniden üretmek için yazıldı
/// (düzeltmeden önce KIRMIZI), sonra düzeltme yapıldı (YEŞİL). Hata kayıtları: docs/test/Hata-Kayitlari.md.
/// Testin adı hata numarasıyla başlar; hata geri gelirse hangi kaydın bozulduğu hemen görülür.
/// </summary>
[Trait("Tur", "Regresyon")]
public class HataRegresyonTestleri
{
    private static Bagisci Bagisci(DateOnly dogum, Cinsiyet cinsiyet = Cinsiyet.Erkek) => new()
    {
        Ad = "Test", Soyad = "Bağışçı", DogumTarihi = dogum, Cinsiyet = cinsiyet, Kilo = 70, Sehir = "Bingöl",
        TcKimlikNo = "10000000146", Telefon = "05550000000", KanGrubuId = 1,
    };

    // ---------------- H13-01: "Bugün" UTC'ye göre hesaplanıyordu ----------------

    [Fact]
    public void H13_01_Turkiye_saatiyle_gece_yarisindan_sonra_dogum_gunu_bugun_sayilir()
    {
        // 26.11.2026 22:30 UTC = 27.11.2026 01:30 Türkiye. Bağışçı 27.11.2008 doğumlu → Türkiye'de BUGÜN 18 yaşında.
        var zaman = new AyarlanabilirZaman(new DateTimeOffset(2026, 11, 26, 22, 30, 0, TimeSpan.Zero));
        var sonuc = new BagisUygunlukKurali(zaman).Degerlendir(Bagisci(new DateOnly(2008, 11, 27)), null);

        Assert.True(sonuc.Uygun, string.Join(" ", sonuc.Nedenler));
    }

    [Fact]
    public void H13_01_Bagis_araligi_Turkiye_tarihine_gore_sayilir_bir_gun_erken_izin_verilmez()
    {
        // Son bağış 28.08.2026 22:00 UTC = 29.08.2026 01:00 Türkiye → 90 gün sonrası 27.11.2026.
        // 26.11.2026 (Türkiye) daha 89. gün: bağış YAPILAMAZ. UTC ile hesaplanınca 28.08'den sayılıp izin veriliyordu.
        var zaman = new AyarlanabilirZaman(new DateTimeOffset(2026, 11, 26, 10, 0, 0, TimeSpan.Zero));
        var sonBagis = new DateTime(2026, 8, 28, 22, 0, 0, DateTimeKind.Utc);

        var sonuc = new BagisUygunlukKurali(zaman).Degerlendir(Bagisci(new DateOnly(1990, 1, 1)), sonBagis);

        Assert.False(sonuc.Uygun);
        Assert.Equal(new DateOnly(2026, 11, 27), sonuc.SonrakiUygunTarih);
    }

    [Theory]
    [InlineData(2026, 11, 26, 20, 59, 26)]   // 23:59 Türkiye → hâlâ 26'sı
    [InlineData(2026, 11, 26, 21, 0, 27)]    // 00:00 Türkiye → 27'si
    public void H13_01_Turkiye_tarihi_UTC_arti_3(int yil, int ay, int gun, int saat, int dakika, int beklenenGun)
    {
        var utc = new DateTime(yil, ay, gun, saat, dakika, 0, DateTimeKind.Utc);
        Assert.Equal(new DateOnly(2026, 11, beklenenGun), TurkiyeZamani.Tarih(utc));
    }

    // ---------------- H13-02: 29 Şubat doğumlularda "sonraki uygun tarih" yanlıştı ----------------

    [Fact]
    public void H13_02_Artik_yil_dogumlu_icin_gosterilen_tarihte_gercekten_uygun_olunur()
    {
        // 29.02.2008 doğumlu; 2026 artık yıl değil. Yaş hesabı 18'i 01.03.2026'da tamamlatıyor,
        // ama ekranda "28.02.2026'dan itibaren bağış yapabilirsiniz" yazıyordu (DateOnly.AddYears 28 Şubat'a yuvarlar).
        var dogum = new DateOnly(2008, 2, 29);
        var oncesi = new BagisUygunlukKurali(new AyarlanabilirZaman(new DateTimeOffset(2026, 2, 1, 12, 0, 0, TimeSpan.Zero)))
            .Degerlendir(Bagisci(dogum), null);
        Assert.Equal(new DateOnly(2026, 3, 1), oncesi.SonrakiUygunTarih);

        // Tutarlılık: gösterilen günde gerçekten uygun, bir gün öncesinde değil.
        bool UygunMu(DateOnly gun) => new BagisUygunlukKurali(new AyarlanabilirZaman(new DateTimeOffset(gun.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero)))
            .Degerlendir(Bagisci(dogum), null).Uygun;
        Assert.False(UygunMu(new DateOnly(2026, 2, 28)));
        Assert.True(UygunMu(new DateOnly(2026, 3, 1)));
    }

    [Fact]
    public void H13_02_Normal_dogum_gununde_tarih_degismez() =>
        Assert.Equal(new DateOnly(2026, 5, 10),
            new BagisUygunlukKurali(new AyarlanabilirZaman(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero)))
                .Degerlendir(Bagisci(new DateOnly(2008, 5, 10)), null).SonrakiUygunTarih);

    // ---------------- H13-03: Türkçe "İ" ile yazılan e-posta eşleşmiyordu ----------------

    [Theory]
    [InlineData("İlker@ornek.com")]      // telefon klavyesi ilk harfi büyük yazar
    [InlineData("ILKER@ORNEK.COM")]
    [InlineData("  ilker@ornek.com ")]
    public async Task H13_03_Eposta_buyuk_I_ile_yazilsa_da_giris_yapilir(string girisEpostasi)
    {
        using var db = new TestVeritabani();
        var servis = new KimlikService(new KullaniciRepository(db.Context), new BagisciRepository(db.Context),
            new HastaneRepository(db.Context), new EfRepository<KanGrubu>(db.Context), new Pbkdf2ParolaHasher(iterasyon: 1_000),
            new AyarlanabilirZaman(new DateTimeOffset(2026, 11, 26, 12, 0, 0, TimeSpan.Zero)));
        var kayit = await servis.KurumKaydetAsync(new KurumKayitIstegi("ilker@ornek.com", "Gizli1234", "Test Hastanesi", "Bingöl", "04260000000"));
        Assert.True(kayit.Basarili, kayit.Hata);

        var giris = await servis.GirisYapAsync(girisEpostasi, "Gizli1234");

        Assert.Equal(GirisDurumu.Basarili, giris.Durum);
    }

    [Fact]
    public async Task H13_03_Ayni_eposta_I_ile_ikinci_kez_kaydedilemez()
    {
        using var db = new TestVeritabani();
        var servis = new KimlikService(new KullaniciRepository(db.Context), new BagisciRepository(db.Context),
            new HastaneRepository(db.Context), new EfRepository<KanGrubu>(db.Context), new Pbkdf2ParolaHasher(iterasyon: 1_000),
            new AyarlanabilirZaman(new DateTimeOffset(2026, 11, 26, 12, 0, 0, TimeSpan.Zero)));
        await servis.KurumKaydetAsync(new KurumKayitIstegi("ilker@ornek.com", "Gizli1234", "A Hastanesi", "Bingöl", "04260000000"));

        var ikinci = await servis.KurumKaydetAsync(new KurumKayitIstegi("İlker@ornek.com", "Gizli1234", "B Hastanesi", "Bingöl", "04260000001"));

        Assert.False(ikinci.Basarili);
    }

    // ---------------- H13-04: Aynı kan grubu için iki açık talep (yarış durumu) ----------------

    [Fact]
    public void H13_04_Veritabani_ayni_hastane_ve_kan_grubu_icin_ikinci_acik_talebi_reddeder()
    {
        using var db = new TestVeritabani();
        KanTalebi Talep() => new()
        {
            HastaneId = 3, KanGrubuId = 5, UniteSayisi = 1, Aciliyet = Aciliyet.Normal, Durum = TalepDurumu.Acik,
            OlusturmaTarihi = new DateTime(2026, 11, 26, 12, 0, 0, DateTimeKind.Utc),
        };
        db.Context.KanTalepleri.Add(Talep());
        db.Context.SaveChanges();

        db.Context.KanTalepleri.Add(Talep());
        Assert.Throws<DbUpdateException>(() => db.Context.SaveChanges());
        db.TakibiTemizle();

        // Kapanmış talepler kuralı etkilemez: aynı grup için geçmişte istenildiği kadar talep olabilir.
        var kapanis = new DateTime(2026, 11, 27, 12, 0, 0, DateTimeKind.Utc);
        var kapali1 = Talep(); kapali1.Durum = TalepDurumu.Karsilandi; kapali1.KapanisTarihi = kapanis;
        var kapali2 = Talep(); kapali2.Durum = TalepDurumu.Iptal; kapali2.KapanisTarihi = kapanis;
        db.Context.KanTalepleri.AddRange(kapali1, kapali2);
        db.Context.SaveChanges();
    }

    [Fact]
    public async Task H13_04_Ayni_anda_gelen_talep_istekleri_tek_acik_talep_olusturur_digerleri_hata_alir()
    {
        // Gerçek eşzamanlılık: dosya tabanlı SQLite, her istek kendi DbContext'i ile (web'deki gibi).
        var yol = Path.Combine(Path.GetTempPath(), $"kanbagis-yaris-{Guid.NewGuid():N}.db");
        var baglanti = $"Data Source={yol};Pooling=False;Default Timeout=30";
        try
        {
            using (var ilk = Baglam(baglanti)) ilk.Database.Migrate();

            var gorevler = Enumerable.Range(0, 6).Select(_ => Task.Run(async () =>
            {
                await using var ctx = Baglam(baglanti);
                return await TalepServisi(ctx).OlusturAsync(3, new TalepOlusturmaIstegi(5, 1, Aciliyet.Normal, "yarış"));
            })).ToArray();
            var sonuclar = await Task.WhenAll(gorevler);   // hiçbiri istisna fırlatmamalı

            await using var kontrol = Baglam(baglanti);
            Assert.Equal(1, kontrol.KanTalepleri.Count(t => t.HastaneId == 3 && t.KanGrubuId == 5 && t.Durum == TalepDurumu.Acik));
            Assert.Equal(1, sonuclar.Count(s => s.Basarili));
            Assert.All(sonuclar.Where(s => !s.Basarili), s => Assert.Contains("zaten açık bir talebiniz var", s.Hata));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(yol)) File.Delete(yol);
        }
    }

    [Fact]
    public async Task H13_04_Migration_eski_veritabanindaki_mukerrer_acik_talepleri_silmeden_iptal_eder()
    {
        // Eski hata yüzünden mükerrer açık talebi olan bir veritabanı: önceki migration'a kadar kurulur, veri eklenir,
        // sonra yeni migration uygulanır. Migration patlamamalı, en eski talep açık kalmalı, diğeri iptal olmalı.
        using var baglanti = new SqliteConnection("Data Source=:memory:");
        baglanti.Open();
        using var db = new KanBagisDbContext(new DbContextOptionsBuilder<KanBagisDbContext>().UseSqlite(baglanti).Options);
        var migrator = db.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>();
        var tumu = db.Database.GetMigrations().ToList();
        var yeni = tumu.Single(m => m.EndsWith("_TekAcikTalepKurali"));
        await migrator.MigrateAsync(tumu[tumu.IndexOf(yeni) - 1]);

        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO KanTalepleri (HastaneId, KanGrubuId, UniteSayisi, Aciliyet, Durum, OlusturmaTarihi)
            VALUES (3, 5, 1, 'Normal', 'Acik', '2026-11-20 10:00:00'), (3, 5, 2, 'Acil', 'Acik', '2026-11-21 10:00:00');
            """);

        await migrator.MigrateAsync();

        var talepler = db.KanTalepleri.Where(t => t.HastaneId == 3 && t.KanGrubuId == 5).OrderBy(t => t.Id).ToList();
        Assert.Equal(2, talepler.Count);                                  // silinmedi
        Assert.Equal(TalepDurumu.Acik, talepler[0].Durum);               // en eski açık kaldı
        Assert.Equal(TalepDurumu.Iptal, talepler[1].Durum);
        Assert.NotNull(talepler[1].KapanisTarihi);
        Assert.Contains("otomatik iptal", talepler[1].Aciklama);
    }

    // ---------------- H15-01: Türkçe büyük harf (final uçtan uca provada bulundu) ----------------

    [Fact]
    public async Task H15_01_Kritik_uyari_mesaji_Turkce_buyuk_harfle_KRITIK_degil_KRİTİK()
    {
        using var db = new TestVeritabani();
        var kullanici = new Kullanici { Eposta = "h15@ornek.com", ParolaHash = "x", Rol = KullaniciRolu.Kurum, HastaneId = 1, OlusturmaTarihi = DateTime.UtcNow };
        db.Context.Kullanicilar.Add(kullanici);
        db.Context.SaveChanges();
        var servis = new KanBagis.Business.Stok.StokService(new KanStokRepository(db.Context),
            new AyarlanabilirZaman(new DateTimeOffset(2026, 11, 28, 12, 0, 0, TimeSpan.Zero)));

        // A Rh+ 24, kritik eşik 8 → 16 ünite çıkış kritik uyarı açar.
        var cevap = await servis.HareketGirAsync(new KanBagis.Business.Stok.StokHareketIstegi(1, HareketNedeni.TransferCikis, 16, "test", KritikOnayi: true),
            new KanBagis.Business.Stok.IslemYapan(kullanici.Id, 1));

        Assert.Contains("KRİTİK seviye uyarısı açıldı", cevap.Mesaj);
        Assert.DoesNotContain("KRITIK", cevap.Mesaj);
    }

    private static KanBagisDbContext Baglam(string baglanti) =>
        new(new DbContextOptionsBuilder<KanBagisDbContext>().UseSqlite(baglanti).Options);

    private static TalepService TalepServisi(KanBagisDbContext ctx)
    {
        var zaman = new AyarlanabilirZaman(new DateTimeOffset(2026, 11, 26, 12, 0, 0, TimeSpan.Zero));
        var kanGruplari = new EfRepository<KanGrubu>(ctx);
        var yol = new KanBagis.Business.DisServisler.KusUcusuMesafeServisi();
        var eslestirme = new EslestirmeService(new BagisciRepository(ctx), kanGruplari, new BagisUygunlukKurali(zaman), yol);
        return new TalepService(new KanTalebiRepository(ctx), new HastaneRepository(ctx), kanGruplari,
            new BildirimRepository(ctx), new BagisRepository(ctx), eslestirme, yol,
            new KanBagis.Business.DisServisler.BildirimKuyrugu(), zaman);
    }
}
