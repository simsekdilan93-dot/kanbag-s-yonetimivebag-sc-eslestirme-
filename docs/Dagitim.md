# Dağıtım Kılavuzu

Uygulamayı geliştirme bilgisayarı dışında (sunucu, kurum içi makine) çalıştırmak için gerekenler.

## 1. Yayın paketi

```bash
bash scripts/yayinla.sh            # Windows: powershell -ExecutionPolicy Bypass -File scripts\yayinla.ps1
```

Betik sırasıyla: tüm testleri çalıştırır (başarısızsa durur) → `dotnet publish -c Release -o yayin` → geliştirme veritabanını
paketten çıkarır. `yayin/` klasörü sunucuya kopyalanır. Sunucuda yalnızca **.NET 10 çalışma zamanı** (ASP.NET Core Runtime) gerekir, SDK gerekmez.

```bash
cd yayin
ASPNETCORE_ENVIRONMENT=Production dotnet KanBagis.Web.dll --urls http://0.0.0.0:5284
```

İlk açılışta veritabanı yoksa oluşturulur, varsa bekleyen migration'lar uygulanır (veriler korunur).

## 2. Ortam değişkenleri

Gizli değerler **asla** `appsettings.json`'a ya da depoya yazılmaz; sunucunun ortam değişkenlerine (systemd `Environment=`,
IIS yapılandırması, bulut panelinin "Application settings" bölümü) girilir. Şablon: [.env.example](../.env.example).
`__` (iki alt çizgi) yapılandırma bölümü ayırıcısıdır.

| Değişken | Gizli mi | Zorunlu mu | Açıklama |
|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | Hayır | Evet | Sunucuda `Production` (ayrıntılı hata sayfası kapalı, HSTS açık) |
| `ASPNETCORE_URLS` | Hayır | Hayır | Ör. `http://0.0.0.0:5284` |
| `ConnectionStrings__KanBagis` | Hayır | Önerilir | `Data Source=/var/lib/kanbagis/kanbagis.db` — uygulama klasörünün **dışında**, kalıcı bir yerde |
| `Dagitim__AnahtarKlasoru` | Hayır (içindeki dosyalar gizli) | **Evet** | Oturum ve CSRF şifreleme anahtarları; ör. `/var/lib/kanbagis/anahtarlar` |
| `Dagitim__TersVekil` | Hayır | Vekil varsa | `true`: `X-Forwarded-Proto/For` başlıkları okunur |
| `DisServisler__Mesafe__Etkin`, `__TemelAdres`, `__ZamanAsimiSaniye` | Hayır | Hayır | OSRM yol mesafesi |
| `DisServisler__Ntfy__Etkin`, `__TemelAdres`, `__KanalOneki` | Hayır | Hayır | Anlık bildirim |
| `DisServisler__Ntfy__Token` | **Evet** | Hayır | Korumalı ntfy sunucusu kullanılıyorsa |
| `DisServisler__Twilio__AccountSid`, `__AuthToken`, `__Gonderen` | **Evet** | Hayır | Üçü de verilmezse SMS kapalı |
| `DisServisler__Gonderim__MaksimumDeneme`, `__IlkBeklemeMilisaniye`, `__ZamanAsimiSaniye` | Hayır | Hayır | Telefon bildirimi yeniden deneme ayarları |
| `DisServisler__YapayZeka__Etkin`, `__Model`, `__MaksimumToken`, `__ZamanAsimiSaniye` | Hayır | Hayır | Tahmin modeli ayarları |
| `ANTHROPIC_API_KEY` (ya da `DisServisler__YapayZeka__ApiAnahtari`) | **Evet** | Hayır | Yoksa istatistiksel tahmin |

Uygulama açılışta hangi servislerin açık olduğunu loglar; **anahtarların kendisini asla loglamaz**:

```
Harici servisler — Yol mesafesi: açık, Anlık bildirim (ntfy): açık, SMS (Twilio): kapalı (ortam değişkenleri tanımlı değil)
Yapay zekâ tahmini: kapalı (ANTHROPIC_API_KEY yok) — istatistiksel tahmin kullanılır
```

Depoda gizli değer olmadığını `DagitimTestleri.Depoda_gizli_anahtar_yok` testi her çalıştırmada denetler
(Anthropic/Twilio anahtar biçimleri, özel anahtar, dolu token alanları, bağlantı dizesinde parola).

## 3. Dağıtım seçenekleri

| Seçenek | Ne zaman | Notlar |
|---|---|---|
| **Linux sunucu + systemd + nginx** | Kurum kendi sunucusunda | nginx HTTPS'i sonlandırır, uygulamaya `http://127.0.0.1:5284`; `Dagitim__TersVekil=true` |
| **Windows + IIS** | Kurum Windows sunucusu kullanıyorsa | ASP.NET Core Hosting Bundle kurulur; uygulama havuzunun veritabanı klasörüne yazma izni olmalı |
| **Bulut uygulama servisi** (Azure App Service vb.) | Sunucu yönetmek istenmiyorsa | Ortam değişkenleri panelden; SQLite dosyası kalıcı depolama alanında (`/home`) olmalı |
| **Cloudflare tüneli** | Geçici demo | Bu projede sunum için kullanıldı: `cloudflared tunnel --url http://localhost:5284`; adres her açılışta değişir |

### Örnek: systemd hizmeti

```ini
# /etc/systemd/system/kanbagis.service
[Unit]
Description=Kan Bagisi Yonetim Sistemi
After=network.target

[Service]
WorkingDirectory=/opt/kanbagis
ExecStart=/usr/bin/dotnet /opt/kanbagis/KanBagis.Web.dll
Restart=always
User=kanbagis
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5284
Environment=ConnectionStrings__KanBagis=Data Source=/var/lib/kanbagis/kanbagis.db
Environment=Dagitim__AnahtarKlasoru=/var/lib/kanbagis/anahtarlar
Environment=Dagitim__TersVekil=true
# Gizli değerler ayrı, yalnızca root'un okuyabildiği dosyada:
EnvironmentFile=-/etc/kanbagis/gizli.env

[Install]
WantedBy=multi-user.target
```

### Örnek: nginx

```nginx
server {
    listen 443 ssl;
    server_name kanbagis.ornek.edu.tr;
    # ssl_certificate ... (ör. Let's Encrypt)
    location / {
        proxy_pass         http://127.0.0.1:5284;
        proxy_set_header   Host $host;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }
}
```

## 4. Sağlık kontrolü ve izleme

`GET /saglik` → `Healthy` (200) ya da `Unhealthy` (503). Uygulama ayakta mı **ve** veritabanına erişebiliyor mu, ikisini de
denetler; ayrıntı (bağlantı dizesi, hata metni) yanıtta verilmez, yalnızca loglanır. Yük dengeleyici ya da izleme aracı bu adrese bakar.

## 5. Yedekleme

**Yeni sürüm kurmadan önce mutlaka yedek alın:** uygulama açılışta bekleyen migration'ları uygular. SQLite'ta bazı tablo
değişiklikleri tablonun yeniden kurulmasıyla yapılır ve bu adım transaction dışında çalışır; tam o anda süreç kesilirse
veritabanı yarım kalabilir. (Bu durumun açılıştaki uyarısı bilinçli olarak kapatıldı, bkz. `DataAccess/DependencyInjection.cs`.)

Tüm veri tek dosyada: `kanbagis.db` (+ çalışırken `-wal`, `-shm`). Uygulama çalışırken güvenli yedek:

```bash
sqlite3 /var/lib/kanbagis/kanbagis.db ".backup '/yedek/kanbagis-$(date +%F).db'"
```

Anahtar klasörü de yedeklenmeli; kaybolursa yalnızca oturumlar düşer, veri kaybolmaz.

## 6. Dağıtımda karşılaşılabilecek sorunlar

| Sorun | Belirti | Neden / çözüm |
|---|---|---|
| **Anahtarlar kalıcı değil** | Her yeniden başlatmada herkesin oturumu düşer; açık formlar "İstek işlenemedi (400)" | Veri koruma anahtarları bellekte/profilde. `Dagitim__AnahtarKlasoru` verin |
| **Veritabanı uygulama klasöründe** | Yeni sürümü kopyalayınca veriler gitti | `yayin/` üzerine yazılınca `kanbagis.db` silindi. Veritabanını uygulama dışında tutun (`ConnectionStrings__KanBagis`) |
| **Vekil başlıkları okunmuyor** | Sonsuz HTTPS yönlendirmesi ya da oturum çerezi gelmiyor | Uygulama isteği HTTP sanıyor. `Dagitim__TersVekil=true` + nginx'te `X-Forwarded-Proto` |
| **Yazma izni yok** | Açılışta `SQLite Error 14: unable to open database file` | Hizmet kullanıcısının veritabanı klasörüne yazma izni yok |
| **"No XML encryptor configured"** uyarısı | Açılışta | Linux'ta anahtar dosyaları şifresiz yazılır (Windows'ta DPAPI ile şifreleniyor). Klasörü yalnızca hizmet kullanıcısına açın: `chmod 700 /var/lib/kanbagis/anahtarlar` |
| **"Failed to determine the https port"** uyarısı | İlk http isteğinde | Sunucu HTTPS'siz, düz http ile yayında (ör. yerel demo). Vekil arkasında istekler HTTPS geldiği için görünmez; doğrudan yayında HTTPS adresi verin |
| **Yanlış ortam** | Hata olunca yığın izi kullanıcıya görünüyor | `ASPNETCORE_ENVIRONMENT` `Development` kalmış; `Production` yapın |
| **Statik dosyalar 500** | `styles.css` yüklenmiyor | `dotnet run` ile Production; `dotnet publish` çıktısını çalıştırın (Hafta 8'de yaşandı) |
| **Harici servis kapalı / hız sınırı** | Mesafe kuş uçuşu, bildirim "kuyrukta" | Uygulama çalışmaya devam eder; loglarda neden yazar ([Harici-API.md](Harici-API.md)) |
| **Gizli anahtar sızıntısı** | Anahtar depoya ya da loga girdi | Anahtarı hemen iptal edip yenileyin; git geçmişinden silmek yetmez (kopyalanmış olabilir) |

## 7. Kurulumun temiz ortamda doğrulanması (27.11.2026)

README'deki adımlar, geliştirme klasöründen bağımsız boş bir klasörde **depo GitHub'dan yeniden klonlanarak** denendi
(.NET SDK 10.0.402, Windows 11):

| Adım | Komut | Sonuç |
|---|---|---|
| Klonla | `git clone …/kanbag-s-yonetimivebag-sc-eslestirme-.git kanbagis` | Depoda veritabanı ve `.env` yok |
| Çalıştır | `dotnet run --project src/KanBagis.Web --urls http://localhost:5299` ¹ | Veritabanı kendiliğinden oluştu; `/`, `/KanGrubu`, `/Hesap/Giris` → 200; `/saglik` → `Healthy`; olmayan sayfa → 404 |
| Testler | `dotnet test` | 336 / 336 geçti |
| Yayın | `bash scripts/yayinla.sh` | Testler → `dotnet publish`; paket 28 dosya, içinde veritabanı yok |
| Production | `ASPNETCORE_ENVIRONMENT=Production … dotnet KanBagis.Web.dll` | 200; veritabanı ve anahtarlar uygulama klasörünün dışında; hata sayfasında yığın izi yok |

¹ Bu bilgisayarda 5284 portu başka bir uygulamada açıktı; README'nin "Sık karşılaşılan sorunlar" bölümündeki `--urls` çözümü kullanıldı.

**Temiz kurulumda bulunan ve düzeltilen sorunlar** (geliştirme ortamında veritabanı zaten var olduğu için görünmüyordu):

| Bulgu | Düzeltme | Sürüm |
|---|---|---|
| İlk açılışta her migration için EF uyarısı (SQLite tablo yeniden kurma) | Gerekçesiyle kapatıldı; "yeni sürümden önce yedek" notu | v0.7.1 |
| Her SQL komutu loglanıyordu (ilk açılışta ~190 satır) | Varsayılan log düzeyi `Warning` | v0.7.1 |
| Geliştirmede "HTTPS portu bulunamadı" uyarısı | HTTPS yönlendirmesi yalnızca Production'da | v0.7.1 |
| Production'da anahtar dosyaları şifresiz ("No XML encryptor configured") | Windows'ta DPAPI; Linux'ta klasör izni | v0.7.2 |

Düzeltmelerden sonra geliştirme ortamında açılış logu **14 satır, 0 uyarı, 0 hata**.
