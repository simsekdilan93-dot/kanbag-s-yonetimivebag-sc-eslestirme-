# 🩸 Kan Bağışı Yönetim Sistemi

Kan bağışçılarını, hastanelerin kan taleplerini ve kan stoğunu tek bir yerden yöneten web uygulaması. Bir kan grubu için talep açıldığında sistem uygun bağışçılara bildirim gönderir.

## Özellikler (planlanan)

- Bağışçı kaydı ve kan grubuna göre listeleme
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

## Proje yapısı

```
KanBagis.slnx
src/
├── KanBagis.Entities/     Varlık sınıfları
├── KanBagis.DataAccess/   Veri katmanı (EF Core, DbContext)
├── KanBagis.Business/     İş katmanı (servisler, iş kuralları)
└── KanBagis.Web/          Sunum katmanı (MVC)
docs/
├── ER-Diyagrami.md        Veri modeli
└── Mimari.md              Katmanlı mimari şeması
```

Ayrıntılar: [Mimari şema](docs/Mimari.md) · [ER diyagramı](docs/ER-Diyagrami.md)

## Çalıştırma

Gereksinim: [.NET 10 SDK](https://dotnet.microsoft.com/download)

```bash
git clone https://github.com/simsekdilan93-dot/kan-bagis-yonetimi.git
cd kan-bagis-yonetimi
dotnet run --project src/KanBagis.Web
```

## Haftalık ilerleme

- [x] **Hafta 3:** Kurulum, katmanlı proje iskeleti, ER diyagramı, GitHub deposu
