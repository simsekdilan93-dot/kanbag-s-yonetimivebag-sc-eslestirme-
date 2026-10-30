# 🩸 Kan Bağışı Yönetim Sistemi

Kan bağışçılarını, hastanelerin kan taleplerini ve kan stoğunu tek bir yerden yöneten web uygulaması. Bir kan grubu için talep açıldığında sistem uygun bağışçılara bildirim gönderir.

## Özellikler (planlanan)

- ✅ Kayıt ve giriş (bağışçı / kurum), rol bazlı paneller
- ✅ Bağışçı profili, bağış uygunluk kuralı (yaş, kilo, 90/120 gün), kan grubu uyumluluğu
- Kan grubu bazında stok takibi
- Hastane kan talebi oluşturma (normal / acil)
- Talebe uygun bağışçılara bildirim gönderme ve yanıt takibi

## Teknolojiler

| Alan | Teknoloji |
|---|---|
| Dil / Çatı | C#, ASP.NET Core MVC (.NET 10) |
| ORM | Entity Framework Core |
| Veritabanı | SQLite |
| Arayüz | Razor View + Bootstrap |
| Kimlik doğrulama | Çerez (cookie) oturumu, PBKDF2 parola hash'i, rol tabanlı yetkilendirme |

## Proje yapısı

```
KanBagis.slnx
src/
├── KanBagis.Entities/     Varlık sınıfları
├── KanBagis.DataAccess/   Veri katmanı (EF Core, DbContext, Repository, Migrations)
├── KanBagis.Business/     İş katmanı (servisler, iş kuralları)
└── KanBagis.Web/          Sunum katmanı (MVC)
tests/
└── KanBagis.Tests/        CRUD, kısıt, parola, giriş ve yetkilendirme testleri (xUnit)
docs/
├── ER-Diyagrami.md        Veri modeli
├── Mimari.md              Katmanlı mimari şeması
├── Guvenlik-Notu.md       Kimlik doğrulama ve yetkilendirme güvenlik notu
├── moduller/              Modül açıklamaları (Bağışçı ve Kan Grubu)
└── db/                    SQL şema, örnek sorgular, indeks açıklamaları
```

Ayrıntılar: [Mimari şema](docs/Mimari.md) · [ER diyagramı](docs/ER-Diyagrami.md) · [Veritabanı](docs/db/README.md) · [Güvenlik notu](docs/Guvenlik-Notu.md) · [Bağışçı modülü](docs/moduller/Bagisci-ve-Kan-Grubu-Modulu.md)

## Çalıştırma

Gereksinim: [.NET 10 SDK](https://dotnet.microsoft.com/download)

```bash
git clone https://github.com/simsekdilan93-dot/kanbag-s-yonetimivebag-sc-eslestirme-.git
cd kanbag-s-yonetimivebag-sc-eslestirme-
dotnet tool restore                    # dotnet-ef aracını kurar
dotnet run --project src/KanBagis.Web  # ilk açılışta veritabanı örnek veriyle oluşturulur
```

Örnek veride kullanıcı hesabı **yoktur** (depo herkese açık olduğu için parola paylaşılmaz). Siteyi açıp **Kayıt ol** ile bir bağışçı ya da kurum hesabı oluşturun.

Bağışçı kaydında TC kimlik numarası algoritmayla kontrol edilir. Denemek için algoritmaya uyan örnek numaralar: `10000001440`, `10000002744`, `10000003116`. Algoritmaya uyan her numara gerçek bir kişiye ait olabilir; bu numaraları yalnızca kendi bilgisayarınızdaki deneme veritabanında kullanın.

Testleri çalıştırmak için:

```bash
dotnet test
```

## Haftalık ilerleme

- [x] **Hafta 3:** Kurulum, katmanlı proje iskeleti, ER diyagramı, GitHub deposu
- [x] **Hafta 4:** Veritabanı şeması (migration), ilişkiler/kısıtlar/indeksler, sentetik örnek veri, CRUD veri erişim katmanı, testler
- [x] **Hafta 5:** Kayıt/giriş, PBKDF2 parola hash'i + tuz, hesap kilitleme, çerez oturumu, rol tabanlı erişim (Bağışçı/Kurum), güvenlik notu
- [x] **Hafta 6:** Bağışçı kaydı ve kan grubu yönetimi: ABO/Rh modeli ve uyumluluk, bağış uygunluk kuralı, TC algoritması, 81 il listesi, profil düzenleme (sahiplik kontrolü), kan grubu kilidi
