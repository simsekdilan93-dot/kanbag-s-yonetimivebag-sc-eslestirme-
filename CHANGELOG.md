# Sürüm Notları

Sürümleme: [Semantic Versioning](https://semver.org/lang/tr/) — `MAJOR.MINOR.PATCH`

- **MAJOR:** geriye uyumsuz büyük değişiklik (1.0.0: ilk kararlı sürüm)
- **MINOR:** yeni özellik (0.1.0 → 0.2.0)
- **PATCH:** yalnızca hata düzeltmesi (0.1.0 → 0.1.1)

Her sürüm git'te aynı adla etiketlenir (`git tag v0.1.0`).

---

## [0.6.0] — 2026-11-26 · Test ve hata ayıklama

### Düzeltildi
- **H13-01:** "Bugün" UTC'ye göre hesaplanıyordu; Türkiye saatiyle 00:00–03:00 arası yaş ve 90/120 gün kuralı bir gün kayıyordu. Takvim günü soruları artık Türkiye saatine göre (`TurkiyeZamani`).
- **H13-02:** 29 Şubat doğumlulara gösterilen "sonraki uygun tarih" bir gün erkendi.
- **H13-03:** Türkçe "İ" ile yazılan e-posta (ör. "İlker@…") eşleşmiyordu; aynı adresle ikinci hesap açılabiliyordu.
- **H13-04:** Aynı anda gelen isteklerle aynı kan grubu için birden çok açık talep açılabiliyordu. Veritabanına filtreli benzersiz indeks eklendi; mükerrer kayıtlar migration'da iptal ediliyor.
- **H13-05:** Başarısız talep eklemede "Collection was modified" istisnası.

### Eklendi
- `HataRegresyonTestleri` (her hata için önce kırmızı, sonra yeşil test), hata yolları için web testleri, uçtan uca akış stok hareketi ve istatistik panosuna kadar uzatıldı. Toplam 316 test.
- `docs/test/Test-Raporu.md` (piramit, kapsam, sınır senaryoları), `docs/test/Hata-Kayitlari.md` (düzeltilen ve bilinen hatalar).
- Kapsam ölçümü: `tests/kapsam.runsettings`, `tests/araclar/kapsam_ozet.py`, `tests/araclar/test_piramidi.py`.

---

## [0.5.0] — 2026-11-26 · İstatistik Panosu ve Bildirim Özeti

### Eklendi
- **İstatistik panosu (kurum):** talep sayısı/ünite, karşılanma oranı (açık talepler hariç), karşılanma süresi (medyan + ortalama), acil oranı, bağış sayısı/ünite, kan grubu bazında talep–bağış farkı ve stok durumu.
- **Toplulaştırma sorguları:** sayma/toplama veritabanında (GROUP BY + COUNT/SUM); dönem, hastane ve sentetik veri süzgeçleri.
- **Grafikler:** sunucuda üretilen SVG (haftalık sütun, kan grubu çubuk, yanıt ve telefon kanalı yığılmış çubuk); eksen 0'dan, değer etiketli, boş hafta 0, "veri yok" durumu.
- **Bildirim özeti:** yanıt ve kabul oranı, telefon gönderim başarısı; sayılardan üretilen kısa özet metni ve veri notları (az veri, açık talep, sentetik veri).
- **CSV dışa aktarma** (Türkçe Excel uyumlu, formül enjeksiyonuna karşı korumalı).
- `docs/moduller/Istatistik-Panosu.md`; 33 yeni test (toplam 295).

---

## [0.4.0] — 2026-11-26 · Stok Takip ve Uyarı Modülü

### Eklendi
- **Stok modeli:** her kan grubu için kritik ve düşük eşik (`KritikEsik`, `UyariEsigi`); durum Normal / Düşük / Kritik.
- **Giriş/çıkış kaydı:** `StokHareketleri` tablosu (tür, neden, miktar, önceki/sonraki stok, yapan kurum/kullanıcı). Atomik güncelleme ile stok asla eksiye düşmez; kayıtlar silinmez.
- **Kritik eşik uyarısı:** `StokUyarilari` tablosu; eşik altına inince açılır, seviye değişince güncellenir, stok normale dönünce kapanır. Kan grubu başına tek açık uyarı.
- İş kuralları: miktar 1–50, elle bağış girişi yok, imha/sayımda açıklama zorunlu, kritik seviyeye indiren çıkışta onay, eşik doğrulaması, kimlik oturumdan.
- Stok takibi, hareket geçmişi ve uyarı geçmişi sayfaları; menüde açık uyarı sayısı; kurum panelinde uyarı şeridi.
- `docs/moduller/Stok-Takip-ve-Uyari-Modulu.md`; 34 yeni test (toplam 262).

### Değişti
- Bağış kaydı stoğu artık "Bağış" nedenli giriş hareketiyle artırıyor (aynı transaction).
- Kurum panelindeki sabit "5 ünite altı" kuralı yerine kan grubunun eşikleri kullanılıyor.

---

## [0.3.0] — 2026-11-16 · Yapay zekâ bileşeni: talep ve stok tahmini

### Eklendi
- **Stok tahmini sayfası (kurum):** kan grubu başına gelecek hafta talep tahmini, stok karşılama süresi ve risk (Kritik/Yüksek/Orta/Düşük).
- **İstatistiksel taban tahmin:** 12 haftalık ortalama + doğrusal eğilim; az veride "veri yetersiz".
- **Yapay zekâ katmanı:** Anthropic Claude (`claude-opus-5-5`), resmî C# SDK, JSON şemalı yapılandırılmış çıktı, ret yedeği (`fallbacks: "default"`). Modele yalnızca toplu sayılar gider.
- **Çıktı doğrulama:** bozuk/boş/eksik çıktı reddedilir, uçuk değerler düzeltilir; yapay zekâ taban tahminin kritik/yüksek uyarısını düşüremez. Hata, zaman aşımı ve retlerde istatistiksel tahmine dönülür.
- **AI kullanım günlüğü:** `TahminKayitlari` tablosu; istem, ham çıktı, doğrulama notları, model, süre. `/Tahmin/Gunluk` sayfası.
- Şeffaflık notu ve kaynak etiketi; demo için işaretli sentetik talep geçmişi.
- `docs/Yapay-Zeka-Bileseni.md`; 38 yeni test (toplam 228).

---

## [0.2.0] — 2026-11-16 · Harici API entegrasyonu

### Eklendi
- **Harita / yol mesafesi (OSRM):** Eşleştirme adayları gerçek **yol** mesafesine göre sıralanıyor; bağışçı panelinde hastaneye yol mesafesi, süresi ve OpenStreetMap yol tarifi bağlantısı var. Sonuçlar önbelleğe alınıyor.
- **Telefona bildirim:** ntfy ile anlık bildirim (push; bağışçı rastgele kanalına abone olur) ve Twilio ile SMS (yalnızca izin verilirse). Gönderim arka planda, kuyrukla yapılıyor.
- **Hata yönetimi:** zaman aşımı, ağ hatası, 5xx, 4xx, 429 (Retry-After), bozuk JSON. Mesafede kuş uçuşuna dönülüyor; bildirimde üstel geri çekilmeyle yeniden deneniyor. Gönderim durumu, deneme sayısı ve son hata kaydediliyor.
- Bağışçı profilinde "Telefon bildirimleri" bölümü; kurum talep sayfasında telefon gönderim özeti.
- Anahtarlar yalnızca ortam değişkeninden okunuyor (`DisServisler__…`); `appsettings.json`'da gizli değer yok.
- `docs/Harici-API.md`; 36 yeni test (toplam 190).

### Değişti
- Eşleştirme sıralamasında kuş uçuşu yerine yol mesafesi kullanılıyor (API yanıt vermezse kuş uçuşu).

---

## [0.1.0] — 2026-11-12 · Vize ara sürümü

İlk çalışan ara sürüm: kullanıcı, veritabanı ve iki çekirdek modül uçtan uca entegre.

### Eklendi
- **Hafta 8: Entegrasyon.** Kurum, talebi kabul eden bağışçı için **"Bağış alındı"** kaydı yapabiliyor. Tek transaction içinde bağış kaydediliyor, kan stoğu artıyor ve istenen ünite sayısına ulaşılınca talep **kendiliğinden kapanıyor**. Bağışçının uygunluğu yeni bağışa göre güncelleniyor.
- `Bagis.KanTalebiId` ve `(BagisciId, KanTalebiId)` için filtreli UNIQUE indeks (aynı talep için ikinci bağış engelleniyor).
- Alt bilgide sürüm numarası (`Directory.Build.props`).
- Demo senaryosu (`docs/Demo-Senaryosu.md`) ve vize sözlü hazırlık notu (`docs/Vize-Hazirlik.md`).
- 10 yeni entegrasyon testi (toplam 154).
- **Hafta 7: Modül B (Talep ve grup/konum eşleştirme).** Talep açma; uyumlu gruplar, aciliyete göre yarıçap (100/300 km) ve uygunluk kuralıyla eşleştirme; mesafeye göre sıralama; bildirim ve Kabul/Red.
- **Hafta 6: Modül A (Bağışçı ve kan grubu).** ABO/Rh modeli ve uyumluluk; bağış uygunluk kuralı (yaş 18–65, kilo ≥ 50, 90/120 gün); TC algoritması; 81 il; profil düzenleme.
- **Hafta 5: Kimlik doğrulama.** Kayıt/giriş, PBKDF2 + tuz, hesap kilidi, çerez oturumu, rol tabanlı erişim, CSRF koruması.
- **Hafta 4: Veritabanı.** 7 tablo, ilişkiler (1-1, 1-N, N-N), kısıtlar, indeksler, migration, sentetik veri, CRUD.
- **Hafta 3: Kurulum.** Katmanlı iskelet, ER diyagramı, mimari şema.

### Düzeltildi
- Kabul edilen talepten sonra akış kopuyordu: bağış ve stok güncellenmiyordu.
- Karşılanmış talep açık kalıyor ve bağışçılara çağrı gitmeye devam ediyordu.
- Şablondan kalan İngilizce `Privacy` sayfası kaldırıldı.
- Hata sayfası Türkçeleştirildi. Ziyaretçiye geliştirme moduna nasıl geçileceğini anlatan metin (bilgi sızıntısı riski) kaldırıldı.

### Bilinen sınırlamalar
- Kurum hesapları için yönetici onayı yok.
- Bildirimler yalnızca sistem içinde; e-posta/SMS gönderilmiyor.
- Konum il düzeyinde, mesafeler kuş uçuşu.
