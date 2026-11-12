# Sürüm Notları

Sürümleme: [Semantic Versioning](https://semver.org/lang/tr/) — `MAJOR.MINOR.PATCH`

- **MAJOR:** geriye uyumsuz büyük değişiklik (1.0.0: ilk kararlı sürüm)
- **MINOR:** yeni özellik (0.1.0 → 0.2.0)
- **PATCH:** yalnızca hata düzeltmesi (0.1.0 → 0.1.1)

Her sürüm git'te aynı adla etiketlenir (`git tag v0.1.0`).

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
