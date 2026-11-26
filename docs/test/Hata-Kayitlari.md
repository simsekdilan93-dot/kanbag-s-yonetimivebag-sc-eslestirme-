# Hata Kayıtları (Hafta 13)

Projede bulunan hataların listesi: **belirti → yeniden üretme → kök neden → düzeltme → regresyon testi**.
Her düzeltilen hatanın bir testi vardır; hata geri gelirse test kırmızıya döner.

## Özet

| No | Hata | Önem | Durum | Regresyon testi |
|---|---|---|---|---|
| H13-01 | "Bugün" UTC'ye göre hesaplanıyordu: her gece 00:00–03:00 arası yaş ve 90/120 gün kuralı bir gün kayık | Yüksek | Düzeltildi | `H13_01_*` (4 test) |
| H13-02 | 29 Şubat doğumluya "28 Şubat'ta uygun olacaksınız" deniyordu, o gün hâlâ uygun değil | Orta | Düzeltildi | `H13_02_*` (2 test) |
| H13-03 | "İlker@…" (Türkçe büyük İ) ile giriş yapılamıyor, aynı adres ikinci kez kaydedilebiliyor | Orta | Düzeltildi | `H13_03_*` (4 test) |
| H13-04 | Aynı anda gelen isteklerle aynı kan grubu için birden çok açık talep açılabiliyordu (yarış durumu) | Yüksek | Düzeltildi | `H13_04_*` (3 test) |
| H13-05 | H13-04 düzeltilirken: başarısız talep eklemede "Collection was modified" istisnası | Yüksek | Düzeltildi | `H13_04_Ayni_anda_*` |
| H13-06 | H13-03 düzeltildikten sonra tarayıcıda: giriş formu "İlker@…" adresini "Geçerli bir e-posta girin" diyerek sunucuya göndermiyordu | Orta | Düzeltildi | `H13_06_Giris_formu_*` (uçtan uca) |
| H12-01 | Dar sütundaki SVG grafiğin yazıları okunaksız küçülüyordu | Düşük | Düzeltildi | tarayıcıda kontrol |
| H12-02 | CSV'de stok durumu kod adıyla ("Dusuk") yazılıyordu | Düşük | Düzeltildi | `Csv_bom_ve_noktali_virgul_*` |
| H11-01 | Menü rozeti: ViewComponent çıktısı `ToString()` ile okunamıyordu | Düşük | Düzeltildi | `Kurum_panelinde_stok_uyarisi_*` |
| H11-02 | Kurum panelinde sabit "5 ünite altı" kuralı stok eşikleriyle çelişiyordu | Orta | Düzeltildi | `Kurum_panelinde_stok_uyarisi_*` |
| H09-01 | Alt bilgi sürüm testi sabit "v0.1.0" bekliyordu, sürüm artınca kırıldı | Düşük | Düzeltildi | `Alt_bilgide_surum_numarasi_gorunur` |
| H08-01 | Testler paylaşılan veritabanında birbirini etkiliyordu (aynı il) | Orta | Düzeltildi | testler farklı illerde |
| H08-02 | `BildirimSayisi` alanı "talep kapandı" anlamında da kullanılıyordu | Düşük | Düzeltildi | `TalepKapandi` alanı |
| H07-01 | EF, izlenmeyen talep nesnesi bağlanınca talebi yeniden eklemeye çalışıyordu | Yüksek | Düzeltildi | `DahaFazlaBildir` testleri |
| H07-02 | Projeksiyonda `Include` yok sayıldığı için kan grubu adı boş geliyordu | Orta | Düzeltildi | kurum paneli testleri |

### Açık / bilinen sınırlamalar

| No | Konu | Etki | Neden şimdilik açık |
|---|---|---|---|
| A-01 | İstatistik panosu ve tahmin, dönem sınırlarını UTC gününe göre çiziyor | Türkiye saatiyle 00:00–03:00 arası açılan talep önceki güne/haftaya sayılabilir | Toplam sayılar değişmiyor, yalnızca gün sınırı; H13-01'deki yardımcı (`TurkiyeZamani`) ile sonraki sürümde |
| A-02 | Claude API istemcisi gerçek API ile otomatik test edilmiyor (kapsam %16) | Canlı API değişirse fark edilmeyebilir | Anahtar gerekir ve ücretli; davranış sahte modelle test ediliyor |
| A-03 | SQLite tek yazıcıya izin verir | Çok yoğun eşzamanlı yazmada istekler sırayla bekler | Ders projesi ölçeği için yeterli; bekleme süresi 30 sn |
| A-04 | OpenStreetMap yol tarifi bağlantısı dış servis hız sınırına (429) takılabiliyor | Bağlantı açılmayabilir | Dış servis; uygulamanın kendi mesafe hesabı etkilenmez |

---

## H13-04 ve H13-05: adım adım ayıklama

**1. Belirti / şüphe.** Talep servisi, aynı kan grubu için ikinci açık talebi şu sırayla engelliyordu:
`AcikTalebiGetirAsync` (var mı?) → yoksa `EkleAsync`. Kontrol ile ekleme arasında başka bir istek araya girerse
(aynı hastanenin iki personeli aynı anda "Yeni talep" derse ya da form iki kez gönderilirse) ikisi de kontrolü geçer.

**2. Yeniden üretme (test önce yazıldı).** `H13_04_Ayni_anda_gelen_talep_istekleri_...`: dosya tabanlı SQLite,
her istek kendi `DbContext`'i ile (web'deki gibi), 6 istek `Task.WhenAll` ile aynı anda. Beklenen: 1 açık talep.

```
Failed H13_04_Ayni_anda_gelen_talep_istekleri_tek_acik_talep_olusturur_digerleri_hata_alir
   Assert.Equal() Failure: Values differ
   Expected: 1
   Actual:   5
Failed H13_04_Veritabani_ayni_hastane_ve_kan_grubu_icin_ikinci_acik_talebi_reddeder
   Assert.Throws() Failure: No exception was thrown
```

6 istekten **5 açık talep** oluştu; veritabanı da kuralı hiç korumuyordu.

**3. Kök neden.** İş kuralı yalnızca uygulamada "önce oku, sonra yaz" biçimindeydi; veritabanında karşılığı yoktu.
Kontrol ile yazma atomik değil.

**4. Düzeltme.**
- Veritabanı: filtreli benzersiz indeks `UX_KanTalebi_HastaneBasinaTekAcikTalep` — `(HastaneId, KanGrubuId) WHERE Durum = 'Acik'`.
  Kapanmış talepler kuralı etkilemez.
- Migration (`TekAcikTalepKurali`): var olan veritabanında mükerrer açık talep varsa indeks oluşturulamazdı; en eskisi korunup
  diğerleri **silinmeden** not düşülerek iptal ediliyor (bağlı bildirim/bağış kayıtları yetim kalmasın).
- `KanTalebiRepository.AcikTalepEkleAsync`: benzersizlik ihlalini (SQLite hata 19, UNIQUE) yakalayıp `false` döner;
  servis kullanıcıya "zaten açık bir talebiniz var (#N)" der, istisna fırlatmaz.

**5. Yeni hata (H13-05).** Düzeltmeden sonra test yine kırmızı, ama farklı bir hatayla:

```
System.InvalidOperationException : Collection was modified; enumeration operation may not execute.
   at System.Collections.Generic.List`1.Enumerator.MoveNext()
   at KanBagis.DataAccess.Repositories.KanTalebiRepository.AcikTalepEkleAsync(KanTalebi talep) ... line 58
```

Yığın izi `AcikTalepEkleAsync` içindeki `foreach (var b in talep.Bildirimler) Context.Entry(b).State = Detached;` satırını gösterdi.
Bir bildirimi takipten çıkarınca EF, ilişki düzeltmesi (fix-up) yaparak onu `talep.Bildirimler` listesinden de siliyor;
dolaşılan liste değiştiği için istisna. Düzeltme: listenin kopyası dolaşılır (`talep.Bildirimler.ToList()`).

**6. Doğrulama.** Regresyon testleri 5 kez art arda çalıştırıldı (eşzamanlılık testi kararlı mı?): 5/5 yeşil.
Tüm test takımı: 317/317. Ayrıca mükerrer kayıt içeren eski bir veritabanına migration uygulandı: en eski talep açık kaldı, diğeri silinmeden iptal edildi (`H13_04_Migration_*`).

## H13-06: birim testi geçti, tarayıcı engelledi

- **Nasıl bulundu:** H13-03 düzeltilip birim testleri yeşile döndükten sonra rapor için tarayıcıda denendi:
  "ilker@…" ile kayıt, "İlker@…" ile giriş → sayfa "Geçerli bir e-posta girin." dedi, istek sunucuya **hiç gitmedi**.
- **Kök neden:** giriş modelindeki `[EmailAddress]` özniteliği sayfaya `type="email"` ve `data-val-email` (jQuery doğrulaması) ekliyor;
  ikisi de e-postanın yerel kısmında yalnızca ASCII kabul ediyor. Servis katmanı doğruydu, hata arayüz katmanındaydı.
- **Düzeltme:** girişte biçim kontrolü kaldırıldı (adres zaten veritabanında aranıyor); tüm e-posta alanlarına
  `autocapitalize="none"` eklendi, böylece telefon ilk harfi büyütmüyor.
- **Ders:** birim testi yalnızca test ettiği katmanı kanıtlar. `H13_06_*` uçtan uca testi sayfanın HTML'ini ve gerçek giriş isteğini kontrol ediyor;
  düzeltme geri alınınca kırmızıya döndüğü doğrulandı.

## H13-01: Türkiye saati

- **Belirti:** 27.11.2008 doğumlu bağışçı, 27.11.2026 gece 01:30'da (Türkiye) "18 yaşından küçük" uyarısı alıyor.
- **Kök neden:** `DateOnly.FromDateTime(zaman.GetUtcNow().UtcDateTime)` — UTC tarihi 26.11. Türkiye UTC+3.
  Aynı hata ters yönde de var: son bağış 29.08 01:00 (Türkiye) = 28.08 22:00 UTC; UTC'den sayınca 90 gün **bir gün erken** doluyordu.
- **Düzeltme:** `TurkiyeZamani.Bugun()` / `TurkiyeZamani.Tarih(utc)`: takvim günü soruları Türkiye saatine göre. Veritabanında zamanlar UTC kalır.
- **Test:** sınır anı 20:59 UTC (= 23:59, hâlâ 26'sı) ve 21:00 UTC (= 00:00, 27'si).

## H13-02: 29 Şubat

- **Belirti:** 29.02.2008 doğumlu; ekranda "28.02.2026'dan itibaren bağış yapabilirsiniz", ama 28.02.2026'da sistem hâlâ "uygun değil" diyor.
- **Kök neden:** yaş hesabı 18'i 01.03'te tamamlatıyor; tarih ise `DateOnly.AddYears(18)` ile hesaplanıyor ve 28.02'ye yuvarlanıyor. İki kural tutarsız.
- **Düzeltme:** `YasDolduguGun(dogum, yas)`: aynı yaş hesabını kullanır; yuvarlanan gün yetmiyorsa bir gün ileri alır.

## H13-03: Türkçe "İ"

- **Belirti:** "ilker@ornek.com" ile kayıtlı kullanıcı telefondan "İlker@ornek.com" yazınca (klavye ilk harfi büyütür) "E-posta veya parola hatalı".
- **Kök neden:** `"İ".ToLowerInvariant()` = `"i̇"` (i + birleşen nokta, U+0307); `"i"` ile eşleşmiyor. Aynı nedenle "İlker@…" ile ikinci hesap da açılabiliyordu.
- **Düzeltme:** normalleştirmede önce `İ → I`, sonra küçük harf.
