# Vize Sözlü Hazırlık (Hafta 8)

Taslaktaki 5 sözlü sınav sorusunun projeden örneklerle cevapları.

---

## 1) Şu ana kadarki mimarini özetle.

**Katmanlı mimari** (sunum / iş / veri) ve ortak varlık projesi:

```
Tarayıcı
   │  HTTP (çerez oturumu, CSRF belirteci)
   ▼
KanBagis.Web          Controller + Razor View      → yalnızca Business'ı çağırır
   ▼
KanBagis.Business     Servisler + iş kuralları     → KimlikService, BagisciService, EslestirmeService, TalepService…
   ▼
KanBagis.DataAccess   EF Core + Repository         → SQLite (migration'larla)
   ▲
KanBagis.Entities     Varlık sınıfları (tüm katmanlar kullanır)
```

| Hafta | Eklenen | Katman |
|---|---|---|
| 3 | İskelet, ER diyagramı | Hepsi |
| 4 | 7 tablo, ilişkiler, kısıtlar, CRUD | DataAccess |
| 5 | Kullanıcı, PBKDF2, çerez oturumu, roller | Business + Web |
| 6 | **Modül A:** bağışçı, kan grubu (ABO/Rh), uygunluk kuralı | Business + Web |
| 7 | **Modül B:** talep, eşleştirme (uyumluluk + konum), bildirim | Business + Web |
| 8 | Entegrasyon: bağış → stok → talep kapanışı, `v0.1.0` | Hepsi |

**Neden katmanlı?** Her katmanın tek bir sorumluluğu var. İş kuralları arayüzden bağımsız olarak test edilebiliyor: 154 testin çoğu tarayıcı açmadan çalışıyor. Veritabanı değişirse yalnızca DataAccess katmanı etkileniyor.

---

## 2) Hangi modül hangi veriyi nereden alıyor?

| Modül / ekran | Okuduğu veri | Nereden | Yazdığı veri |
|---|---|---|---|
| Kimlik (giriş/kayıt) | `Kullanicilar` | `KullaniciRepository` | `Kullanicilar` + `Bagiscilar` ya da `Hastaneler` (tek SaveChanges) |
| Modül A: Bağışçı | `Bagiscilar`, `KanGruplari`, son bağış = `MAX(Bagislar.BagisTarihi)` | `BagisciRepository` | `Bagiscilar` (profil) |
| Modül B: Eşleştirme | Uyumlu grupların bağışçıları + son bağışları, il koordinatları (kodda sabit) | `BagisciRepository.AdaylariGetirAsync`, `IlKonumlari` | — (yalnızca okur) |
| Modül B: Talep | `KanTalepleri`, `Bildirimler`, `Bagislar` | `KanTalebiRepository`, `BildirimRepository` | `KanTalepleri`, `Bildirimler` |
| Bağış kaydı (Hafta 8) | Talep, bildirim (Kabul?) | `TalepService` | `Bagislar` + `KanStoklari` + `KanTalepleri.Durum` (**tek transaction**) |
| Kurum paneli | Talepler, stok, ildeki uygun bağışçı **sayısı** | `PanelService` → `EslestirmeService` | — |

**Kimlik bilgisi (kim olduğu)** hiçbir zaman formdan ya da URL'den alınmıyor; her zaman **imzalı oturum çerezinden** okunuyor (`BagisciId`, `HastaneId` claim'leri). Sahiplik kontrolü bu sayede çalışıyor.

---

## 3) En çok zorlandığın entegrasyon neydi, nasıl çözdün?

**Talep akışını bağış ve stokla birleştirmek (Hafta 8).** Hafta 7 sonunda bağışçı talebi kabul edebiliyordu, ama akış orada bitiyordu: bağış kaydedilmiyordu, stok artmıyordu, Modül A'nın uygunluk kuralı yeni bağışı görmüyordu ve talep açık kalıyordu.

**Sorun:** Bu üç tablo (`Bagislar`, `KanStoklari`, `KanTalepleri`) aynı anda değişmeli. Biri yazılıp diğeri yazılmazsa tutarsız bir durum oluşur, örneğin "bağış kaydedildi ama stok artmadı".

**Çözüm:**
1. `Bagis` tablosuna `KanTalebiId` eklendi: hangi bağışın hangi talep için yapıldığı artık biliniyor.
2. `BagisRepository.BagisKaydetVeStokArtirAsync` **tek transaction** içinde bağışı kaydediyor, bağışçının kan grubunun stoğunu artırıyor ve gerekirse talebi kapatıyor. Hata olursa hepsi geri alınıyor (ROLLBACK).
3. `(BagisciId, KanTalebiId)` için **filtreli UNIQUE indeks** eklendi. İş kuralı atlanıp doğrudan veri katmanına gidilse bile aynı bağış iki kez kaydedilemiyor; bunu bir test kanıtlıyor.

**Başka bir zorluk (Hafta 7):** "Daha fazla bildir" akışında, değişiklik takibi kapalı okunan talep nesnesi yeni bildirimlere bağlandığında EF Core talebi **yeniden eklemeye** çalışıyordu. Bildirimleri nesneyle değil `KanTalebiId` ile bağlayarak çözdüm.

---

## 4) Yapay zekâdan aldığın bir kod parçasını satır satır açıkla.

**`KanGrubuUyumlulugu.cs`** (Modül A, eşleştirmenin temeli):

```csharp
public static bool VerebilirMi(KanGrubu verici, KanGrubu alici) =>
    AboUyumluMu(verici.AboGrubu, alici.AboGrubu) && (!verici.RhPozitif || alici.RhPozitif);

public static bool AboUyumluMu(AboGrubu verici, AboGrubu alici) =>
    verici == AboGrubu.O || verici == alici || alici == AboGrubu.AB;
```

| Satır | Anlamı |
|---|---|
| `VerebilirMi(verici, alici)` | Verici bu alıcıya kan verebilir mi? İki koşulun **ikisi de** sağlanmalı (`&&`). |
| `AboUyumluMu(...)` | 1. koşul: ABO grupları uyumlu mu? |
| `!verici.RhPozitif \|\| alici.RhPozitif` | 2. koşul: Verici Rh(-) ise herkese verebilir. Verici Rh(+) ise alıcı da Rh(+) olmalı. Yani Rh(+) kan Rh(-) alıcıya verilemez. |
| `verici == AboGrubu.O` | 0 grubunda A ve B antijeni yoktur, herkese verebilir (genel verici). |
| `verici == alici` | Aynı grup her zaman uyumludur. |
| `alici == AboGrubu.AB` | AB grubunda iki antijen de vardır, herkesten alabilir (genel alıcı). |

**Örnek:** 0 Rh- → A Rh+ : `AboUyumluMu` true (verici 0), Rh: `!false` = true → **verebilir**.
A Rh+ → 0 Rh- : `AboUyumluMu` false (A ≠ 0, alıcı AB değil) → **veremez**.

Doğruluk testi: `BagisciModuluTestleri.cs` içinde 8×8 uyumluluk tablosunun tamamı test ediliyor.

---

## 5) Bir sonraki aşamanın planı nedir?

| Hafta | Plan (ders planına göre) |
|---|---|
| 9 | **Harici API:** bildirimleri e-posta/SMS servisiyle gerçekten göndermek (şu an yalnızca sistem içinde) |
| 10 | **Yapay zekâ bileşeni:** örneğin stok verisine göre kan ihtiyacı tahmini ya da talep metninden aciliyet önerisi |
| 11 | **İleri modül:** bağış geçmişi, bağışçıya hatırlatma ("90 gün doldu, tekrar bağış yapabilirsiniz") |
| 12 | **Raporlama:** kan grubu/il bazında talep ve bağış raporları, yönetici rolü (kurum hesabı onayı) |
| 13 | **Test ve hata ayıklama:** kapsam ölçümü, yük altında eşleştirme performansı |
| 14 | **Dokümantasyon ve bulut dağıtımı:** sitenin internetten erişilebilir olması |
| 15 | **Final:** v1.0.0 sürümü ve sunum |

**Bilinen sınırlamalar (sonraki sürümlerde):** Kurum hesapları için onay mekanizması yok. Bildirimler yalnızca sistem içinde. Konum il düzeyinde ve mesafeler kuş uçuşu.
