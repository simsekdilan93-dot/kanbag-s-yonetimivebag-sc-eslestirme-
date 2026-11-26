# Test Sonuç Raporu (Hafta 13, v0.6.0)

| | |
|---|---|
| Tarih | 26.11.2026 |
| Komut | `dotnet test` |
| Sonuç | **316 test, 316 geçti, 0 başarısız, 0 atlandı** (~13 sn) |
| Araçlar | xUnit 2.9, `WebApplicationFactory` (uçtan uca), bellek içi SQLite (gerçek migration'larla), coverlet (kapsam) |

## 1. Test piramidi

```
            ▲   Uçtan uca (HTTP)        40   %13   gerçek uygulama + gerçek HTTP isteği + çerez + CSRF
           ▲▲▲  Entegrasyon (veritabanı) 147  %47   servis + depo + gerçek SQLite (migration, kısıtlar, transaction)
         ▲▲▲▲▲  Birim                   129  %41   saf fonksiyonlar, sahte nesneler (HTTP, LLM, zaman)
```

Sınıflandırma testin gövdesine göre otomatik yapıldı (`tests/araclar/test_piramidi.py`).
Entegrasyon katmanı klasik piramitten biraz geniş: bellek içi SQLite her test için ~50 ms'de sıfırdan kurulduğu için
iş kurallarının bir kısmını veritabanı kısıtlarıyla **birlikte** sınamak ucuz. Uçtan uca testler en az ve en yavaş katman.

| Test sınıfı | Birim | Entegrasyon | Uçtan uca | Toplam |
|---|---:|---:|---:|---:|
| BagisUygunlukKuraliTestleri | 12 | | | 12 |
| BagisciDogrulamaTestleri | 14 | | | 14 |
| KanGrubuUyumlulukTestleri | 10 | | | 10 |
| KonumTestleri | 6 | | | 6 |
| ParolaHasherTestleri | 10 | | | 10 |
| OsrmMesafeTestleri | 12 | | | 12 |
| BildirimKanaliTestleri | 13 | | | 13 |
| IstatistikTestleri | 21 | 10 | | 31 |
| YapayZekaTestleri | 25 | 10 | | 35 |
| HataRegresyonTestleri | 6 | 7 | | 13 |
| BagisciServiceTestleri | | 8 | | 8 |
| BildirimServiceTestleri | | 6 | | 6 |
| EslestirmeServiceTestleri | | 5 | | 5 |
| TalepServiceTestleri | | 13 | | 13 |
| KimlikServiceTestleri | | 21 | | 21 |
| CrudTestleri | | 7 | | 7 |
| KisitTestleri | | 11 | | 11 |
| EntegrasyonTestleri | | 8 | | 8 |
| StokModuluTestleri | | 30 | | 30 |
| DisBildirimGondericiTestleri | | 8 | | 8 |
| AnahtarVeTercihTestleri | | 3 | | 3 |
| YetkilendirmeTestleri | | | 40 | 40 |
| **Toplam** | **129** | **147** | **40** | **316** |

## 2. Kritik akışlar → testler

| Akış | Nerede test ediliyor |
|---|---|
| Kayıt, giriş, kilitleme (5 hatalı deneme / 15 dk), parola hash | KimlikServiceTestleri, ParolaHasherTestleri, YetkilendirmeTestleri |
| Rol ve sahiplik (başka hastanenin talebi, bağışçı sayfaları, CSRF) | YetkilendirmeTestleri |
| Bağış uygunluğu (yaş, kilo, 90/120 gün) | BagisUygunlukKuraliTestleri, HataRegresyonTestleri |
| Talep → eşleştirme → bildirim → kabul → bağış → stok → hareket geçmişi → istatistik | `Talep_bildirim_kabul_akisi_uctan_uca` (tek test, 7 adım, 4 modül) |
| Stok giriş/çıkış, eksiye düşmeme, kritik eşik uyarısı | StokModuluTestleri, YetkilendirmeTestleri |
| Yapay zekâ çıktısının doğrulanması, hatalı çıktıda geri dönüş | YapayZekaTestleri |
| Harici API hataları (zaman aşımı, 5xx, 429, bozuk JSON) | OsrmMesafeTestleri, BildirimKanaliTestleri, DisBildirimGondericiTestleri |
| Eşzamanlı istekler (yarış durumu) | `H13_04_Ayni_anda_*`, `Depo_katmaninda_da_stok_eksiye_dusmez_*` |

## 3. Sınır ve hata senaryoları (örnekler)

Yalnızca mutlu yol değil; her kuralın iki yanı test edilir:

| Kural | Sınırın altı | Sınır | Üstü |
|---|---|---|---|
| Erkek bağış aralığı 90 gün | 89 → uygun değil | 90 → uygun | |
| Kadın bağış aralığı 120 gün | 119 → uygun değil | 120 → uygun | |
| Yaş 18–65 | 17 | 18 / 65 | 66 |
| Kilo ≥ 50 | 49 | 50 | |
| Talep ünitesi 1–10 | 0 | 1 / 10 | 11 |
| Stok çıkış miktarı 1–50 | 0, −2 | | 51 |
| Kritik eşik (stok ≤ eşik) | | 4 → kritik | 5 → düşük |
| Gün sınırı (Türkiye) | 20:59 UTC → 26'sı | 21:00 UTC → 27'si | |
| Artık yıl | 28.02.2026 → 17 yaş | 01.03.2026 → 18 yaş | |

Hata senaryoları: boş/bozuk JSON, CSRF belirteci yok, başka hastanenin kaydı, olmayan kayıt (404), stoktan fazla çıkış,
CHECK kısıtını bozan kayıt, ikinci açık uyarı, ikinci açık talep, eşzamanlı istek, ağ hatası, zaman aşımı, hız sınırı.

## 4. Kapsam (coverage)

```
dotnet test --settings tests/kapsam.runsettings --collect:"XPlat Code Coverage"
python tests/araclar/kapsam_ozet.py <coverage.cobertura.xml> --dosya
```

| Proje | Satır | Dal |
|---|---:|---:|
| KanBagis.Business | %93,2 | %83,5 |
| KanBagis.DataAccess | %98,6 | %89,2 |
| KanBagis.Entities | %96,9 | %100 |
| KanBagis.Web | %83,0 | %53,4 |
| **Toplam** | **%92,4** (2768/2996) | **%75,5** (791/1048) |

Hafta 12 sonunda: satır %91,7, dal %73,5. Bu hafta eklenen testler en çok Web katmanındaki hata yollarını kapsadı
(Web dal kapsamı %46,6 → %53,4; StokController %48 → %100).

**En düşük dosyalar ve neden:**

| Dosya | Satır | Açıklama |
|---|---:|---|
| `Tahmin/LlmIstemcisi.cs` | %16 | Gerçek Claude API çağrısı; anahtar ve ücret gerektirir. Davranışı sahte istemciyle test ediliyor (A-02). |
| `Web/Gorunum.cs` | %54 | Enum → Türkçe metin/renk eşlemeleri; her dalı tek tek test etmek değer katmıyor. |
| `Web/Controllers/*` | %64–77 | Kalan dallar çoğunlukla `Forbid()` (oturumda hastane kimliği yoksa), çerez bozulmadıkça oluşmuyor. |
| `Migrations/*` | hariç | Otomatik üretilmiş kod; her test zaten tüm migration'ları uygulayarak başlıyor. |

## 5. Neden %100 kapsam hedeflenmedi?

- **Kapsam, doğruluğu değil çalıştırılmayı ölçer.** Bir satır testte çalışıp hiçbir şeyi kontrol etmeyebilir.
  Bu haftaki H13-01…04 hatalarının **hepsi zaten "kapsanan" satırlardaydı**; onları kapsam değil, sınır değeri düşünmek buldu.
- **Son %10'un maliyeti yüksek, faydası düşük:** basit eşlemeler (enum → metin), çerçevenin ürettiği kod, dış servis çağrıları.
- Bu yüzden hedef "kritik akışların ve iş kurallarının her iki yanının test edilmesi"; kapsam yalnızca **test edilmemiş yeri bulmak** için kullanıldı.

## 6. Regresyon

Her hafta tüm takım çalıştırılır; önceki haftaların testleri değiştirilmeden geçmelidir. Hafta 13'te bulunan her hata için
önce onu yeniden üreten (kırmızı) bir test yazıldı, sonra düzeltildi (yeşil): `HataRegresyonTestleri.cs`, kayıtlar `Hata-Kayitlari.md`.

| Hafta | Test sayısı |
|---|---:|
| 8 (v0.1.0) | 154 |
| 9 (v0.2.0) | 190 |
| 10 (v0.3.0) | 228 |
| 11 (v0.4.0) | 262 |
| 12 (v0.5.0) | 295 |
| 13 (v0.6.0) | **316** |
