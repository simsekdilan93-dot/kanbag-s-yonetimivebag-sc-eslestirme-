# 🩸 Kan Bağışı Yönetim Sistemi

Kan bağışçılarını, hastanelerin kan taleplerini ve bölgedeki kan stoğunu tek yerden yöneten web uygulaması.
Hastane bir kan grubu için talep açtığında sistem **kan grubu uyumlu, bağış yapabilecek durumda ve yakındaki**
bağışçıları bulur, onlara bildirim gönderir; bağışçı "Gelebilirim" der, bağış kaydedilince stok artar ve talep kapanır.

> Bingöl Üniversitesi · Bilgisayar Programcılığı · Bilgisayar Uygulamaları I — Proje P08 · Dilan Şimşek
> **Sürüm:** v1.0.0 (final) · [Sürüm notları](CHANGELOG.md) · [Haftalık ilerleme](docs/Haftalik-Ilerleme.md)

## İçindekiler

- [Özellikler](#özellikler)
- [Kurulum](#kurulum)
- [Kullanım](#kullanım)
- [Yapılandırma](#yapılandırma)
- [Testler](#testler)
- [Dağıtım](#dağıtım)
- [Proje yapısı](#proje-yapısı)
- [Dokümantasyon](#dokümantasyon)
- [Güvenlik ve kişisel veriler](#güvenlik-ve-kişisel-veriler)

## Özellikler

| Kullanıcı | Yapabilecekleri |
|---|---|
| **Bağışçı** | Kayıt (TC algoritması, 81 il), profil; bağışa uygun mu (yaş 18–65, kilo ≥ 50, son bağıştan 90/120 gün) ve ne zaman uygun olacağı; gelen kan çağrılarına "Gelebilirim / Gelemem"; hastaneye yol mesafesi ve tarifi; isteğe bağlı telefona anlık bildirim / SMS |
| **Kurum (hastane)** | Kan talebi açma (normal / acil) → uygun bağışçılara otomatik bildirim; kabul edenleri görme, bağışı kaydetme; **stok takibi** (giriş/çıkış, kritik eşik uyarısı); **stok tahmini** (istatistik + yapay zekâ); **istatistik panosu** (grafikler, CSV) |
| **Herkes** | Kan grubu uyumluluk tablosu ("kim kime kan verebilir") |

## Kurulum

### Gereksinimler

| Araç | Sürüm | Kontrol |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | **10.0** | `dotnet --version` → `10.0.x` |
| Git | herhangi | `git --version` |

Veritabanı (SQLite) uygulamanın içinde gelir; ayrıca kurulum **gerekmez**. İnternet yalnızca ilk derlemede NuGet paketleri için gerekir.

### Adımlar

```bash
git clone https://github.com/simsekdilan93-dot/kanbag-s-yonetimivebag-sc-eslestirme-.git kanbagis
cd kanbagis
dotnet run --project src/KanBagis.Web
```

Terminalde `Now listening on: http://localhost:5284` yazınca tarayıcıda **http://localhost:5284** adresini açın.

İlk çalıştırmada:
- Veritabanı (`src/KanBagis.Web/kanbagis.db`) **kendiliğinden** oluşturulur ve tüm migration'lar uygulanır.
- Örnek veri yüklenir: 8 kan grubu, 3 hastane, 10 **sentetik** bağışçı, stoklar, birkaç talep ve bağış.
- Harici servisler (yol mesafesi, anlık bildirim) anahtarsız çalışır; SMS ve yapay zekâ anahtar verilmezse kapalı kalır ve uygulama yine çalışır.

Sıfırdan başlamak için uygulamayı durdurup `src/KanBagis.Web/kanbagis.db` dosyasını silin.

### Sık karşılaşılan sorunlar

| Belirti | Çözüm |
|---|---|
| `The current .NET SDK does not support targeting .NET 10.0` | .NET 10 SDK kurun (`dotnet --list-sdks`). |
| `Failed to bind to address ... address already in use` | 5284 portu dolu: `dotnet run --project src/KanBagis.Web --urls http://localhost:5299` |
| Windows'ta "Bu uygulama engellendi" / DLL yüklenemiyor | Akıllı Uygulama Denetimi (Smart App Control) derlenen DLL'leri engelleyebilir; Windows Güvenliği'nden kapatıp yeniden başlatın. |
| Sayfa açılıyor ama giriş yapılamıyor | Örnek veride hesap **yoktur** (parolalar depoda paylaşılmaz); **Kayıt ol** ile hesap oluşturun. |

## Kullanım

1. **Kurum** hesabı açın (*Kayıt ol → Kurum olarak kayıt ol*), il olarak örneğin Bingöl seçin.
2. *Kurum paneli → + Yeni talep*: kan grubu, ünite, aciliyet. Sistem kaç bağışçıya bildirim gittiğini söyler.
3. Başka bir tarayıcı penceresinde (gizli pencere) **bağışçı** hesabı açın; aynı il ve uyumlu kan grubu.
   Denemek için algoritmaya uyan TC örnekleri: `10000001440`, `10000002744`, `10000003116` (yalnızca yerel deneme veritabanında kullanın).
4. Bağışçı *Panelim*'de çağrıyı görür → **Gelebilirim**.
5. Kurum talep detayında bağışçının adını ve telefonunu görür → **Bağış alındı**: stok artar, talep karşılanır.
6. *Stok takibi*, *Stok tahmini* ve *İstatistik* sayfalarında sonuçları izleyin.

Ayrıntılı adım adım senaryo: [docs/Demo-Senaryosu.md](docs/Demo-Senaryosu.md).

## Yapılandırma

Ayarlar `src/KanBagis.Web/appsettings.json` dosyasında; **gizli değerler (anahtar, parola) bu dosyada yoktur** ve
yalnızca **ortam değişkeninden** okunur. ASP.NET Core'da `__` (iki alt çizgi) bölüm ayırıcısıdır:
`DisServisler__Twilio__AuthToken` → `DisServisler:Twilio:AuthToken`. Örnek dosya: [.env.example](.env.example).

| Ortam değişkeni | Varsayılan | Açıklama |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | `Development` ayrıntılı hata sayfası gösterir; sunucuda kullanmayın |
| `ASPNETCORE_URLS` | `http://localhost:5284` (dotnet run) | Dinlenecek adres |
| `ConnectionStrings__KanBagis` | `Data Source={ContentRoot}/kanbagis.db` | SQLite dosyasının yolu |
| `Dagitim__AnahtarKlasoru` | *(boş)* | Oturum/CSRF anahtarlarının kalıcı klasörü (sunucuda **verin**) |
| `Dagitim__TersVekil` | `false` | nginx / IIS / Cloudflare tüneli arkasındaysa `true` |
| `DisServisler__Mesafe__Etkin` | `true` | OSRM yol mesafesi; `false` → kuş uçuşu |
| `DisServisler__Ntfy__Etkin` / `__Token` | `true` / *(boş)* | Telefona anlık bildirim (ntfy) |
| `DisServisler__Twilio__AccountSid` / `__AuthToken` / `__Gonderen` | *(boş)* | SMS; üçü de verilmezse SMS kapalı |
| `ANTHROPIC_API_KEY` | *(boş)* | Yapay zekâ tahmini; yoksa istatistiksel tahmin |

PowerShell'de yalnızca o oturum için: `$env:ANTHROPIC_API_KEY = "..."`. Bash: `export ANTHROPIC_API_KEY=...`.
Tüm ayarlar ve sunucu örnekleri: [docs/Dagitim.md](docs/Dagitim.md).

## Testler

```bash
dotnet test                                   # tüm testler
dotnet test --filter "Tur=Regresyon"          # düzeltilen hataların regresyon testleri
dotnet test --settings tests/kapsam.runsettings --collect:"XPlat Code Coverage"   # kapsam
```

Birim, entegrasyon (gerçek SQLite) ve uçtan uca (gerçek HTTP) testleri; harici servisler sahte sunucuyla sınanır, internete çıkılmaz.
Sonuçlar ve kapsam: [docs/test/Test-Raporu.md](docs/test/Test-Raporu.md) · Hatalar: [docs/test/Hata-Kayitlari.md](docs/test/Hata-Kayitlari.md).

## Dağıtım

```bash
bash scripts/yayinla.sh            # ya da: powershell -ExecutionPolicy Bypass -File scripts\yayinla.ps1
cd yayin
ASPNETCORE_ENVIRONMENT=Production dotnet KanBagis.Web.dll --urls http://0.0.0.0:5284
```

Betik önce testleri çalıştırır, başarısızsa paket oluşturmaz. Sunucuda veritabanı ve anahtar klasörünün kalıcı olması,
ters vekil, HTTPS, sağlık kontrolü (`/saglik`) ve yedekleme: [docs/Dagitim.md](docs/Dagitim.md).

## Proje yapısı

```
KanBagis.slnx
├── src/
│   ├── KanBagis.Entities     Varlık sınıfları ve enum'lar (Bagisci, KanTalebi, KanStok…)
│   ├── KanBagis.DataAccess   EF Core DbContext, yapılandırmalar, migration'lar, repository'ler, örnek veri
│   ├── KanBagis.Business     İş kuralları ve servisler (eşleştirme, stok, tahmin, istatistik, harici API'ler)
│   └── KanBagis.Web          ASP.NET Core MVC: controller'lar, görünümler, oturum, yapılandırma
├── tests/
│   ├── KanBagis.Tests        xUnit testleri
│   ├── kapsam.runsettings    kapsam ölçüm ayarı
│   └── araclar/              kapsam özeti ve test piramidi betikleri
├── scripts/                  yayın paketi betikleri
└── docs/                     teknik dokümantasyon
```

Bağımlılık tek yönlü: **Web → Business → DataAccess → (SQLite)**; Entities ortak. Ayrıntı: [docs/Mimari.md](docs/Mimari.md).

## Dokümantasyon

| Belge | İçerik |
|---|---|
| [Mimari](docs/Mimari.md) | Katmanlar, modüller, istek akışı, tasarım kararları |
| [Uç noktalar](docs/Uc-Noktalar.md) | Tüm HTTP adresleri, yöntemleri ve yetkileri |
| [ER diyagramı](docs/ER-Diyagrami.md) · [Veritabanı](docs/db/README.md) | Tablolar, ilişkiler, kısıtlar, şema SQL'i |
| Modüller | [Bağışçı ve kan grubu](docs/moduller/Bagisci-ve-Kan-Grubu-Modulu.md) · [Talep ve eşleştirme](docs/moduller/Talep-ve-Eslestirme-Modulu.md) · [Stok takip](docs/moduller/Stok-Takip-ve-Uyari-Modulu.md) · [İstatistik](docs/moduller/Istatistik-Panosu.md) |
| [Harici API'ler](docs/Harici-API.md) · [Yapay zekâ](docs/Yapay-Zeka-Bileseni.md) | OSRM, ntfy, Twilio; Claude ile tahmin |
| [Güvenlik notu](docs/Guvenlik-Notu.md) · [Erişilebilirlik](docs/Erisilebilirlik.md) | Parola, oturum, CSRF, KVKK; WCAG denetimi |
| [Dağıtım](docs/Dagitim.md) | Ortam değişkenleri, yayın, sunucu, sorunlar |
| [Test raporu](docs/test/Test-Raporu.md) · [Hata kayıtları](docs/test/Hata-Kayitlari.md) | Test piramidi, kapsam, düzeltilen hatalar |
| [Demo senaryosu](docs/Demo-Senaryosu.md) · [Haftalık ilerleme](docs/Haftalik-Ilerleme.md) | Sunum akışı, haftalar |
| [Final teknik özet ve savunma](docs/Final-Savunma.md) | Uçtan uca anlatım, mimari kararlar, yapay zekâ, ölçeklendirme, jüri soruları |

## Güvenlik ve kişisel veriler

- Parolalar PBKDF2 (SHA-256, tuzlu) ile saklanır; 5 hatalı denemede hesap 15 dakika kilitlenir.
- Tüm formlarda CSRF koruması; rol ve **sahiplik** kontrolü sunucuda (başka hastanenin talebi açılamaz).
- Bağışçının adı ve telefonu yalnızca **kabul ettiği** talebin hastanesine gösterilir; panolar, telefon mesajları ve yapay zekâ istemi kişisel veri içermez.
- Depodaki örnek bağışçılar sentetiktir (TC numaraları bilerek geçersiz); gizli anahtar depoda yoktur — bunu bir test denetler.
