# Harici API Entegrasyonu (Hafta 9)

| Servis | Ne için | Kimlik doğrulama | Anahtar nerede |
|---|---|---|---|
| **OSRM** (router.project-osrm.org) | İller arası **yol** mesafesi ve süresi | Yok (ücretsiz, açık) | — |
| **ntfy** (ntfy.sh) | Bağışçının telefonuna **anlık bildirim (push)** | İsteğe bağlı `Bearer` belirteci | Ortam değişkeni |
| **Twilio** | **SMS** | HTTP **Basic** (`AccountSid:AuthToken`) | Ortam değişkeni |

Kod: [`src/KanBagis.Business/DisServisler/`](../src/KanBagis.Business/DisServisler)

## 1. Yol mesafesi (harita API'si)

```
GET https://router.project-osrm.org/table/v1/driving/40.5,38.88;39.22,38.67?sources=0&annotations=distance,duration
→ { "code": "Ok", "distances": [[0, 144236.4]], "durations": [[0, 6420.3]] }      (metre, saniye)
```

- Eşleştirmede yarıçap filtresi hızlı olsun diye **kuş uçuşu** (Haversine) ile yapılır. Filtreden geçen adaylar **yol mesafesine** göre sıralanır.
- API'ye 81 il için değil, yalnızca adayların bulunduğu birkaç il için **tek bir istek** gider.
- Başarılı yanıtlar **önbelleğe** alınır. İl merkezleri arasındaki yol değişmediği için aynı iki il için API bir daha çağrılmaz.
- Bağışçı panelinde: "Hastaneye: 144 km yol · ~1 sa 48 dk · **Yol tarifi (harita)**". Yol tarifi bağlantısı OpenStreetMap'te açılır.

**Yanıtın ayrıştırılması** (`OsrmMesafeServisi.Ayristir`): `code` alanı `"Ok"` olmalı. `distances[0][i]` ve `durations[0][i]`, i. hedefin metre ve saniye cinsinden değerleridir; 0. sütun merkezin kendisidir. Değerler km'ye ve dakikaya çevrilir. Yol bulunamayan hedefte değer `null` gelir; o hedef için kuş uçuşu kullanılır.

## 2. Telefona bildirim (push ve SMS)

- **Site içi bildirim her zaman vardır.** Telefon yalnızca ek bir kanal; telefona ulaşmasa bile bağışçı çağrıyı panelinde görür.
- Bağışçı profilinde **"Anlık bildirimi aç"** der. Sistem 20 karakterlik **rastgele** bir kanal adı üretir (`kanbagis-38itsgrm7k66murskzmx`); bağışçı telefonundaki ntfy uygulamasında bu kanala abone olur. Kanal adını bilmeyen kimse mesajları göremez.
- SMS yalnızca bağışçı **izin verirse** (açık rıza) ve Twilio anahtarları tanımlıysa gönderilir.
- **KVKK:** Telefona giden mesajda ad, telefon ya da TC **yok**; yalnızca il, kan grubu ve ünite sayısı var. Örnek: *"ACİL kan ihtiyacı — Bingöl: A Rh+ kan ihtiyacı (2 ünite). Yanıt vermek için Kan Bağışı paneline giriş yapın."*
- Gönderim **arka planda** yapılır (`BildirimGonderimArkaPlanServisi`). Kurumun "talep oluştur" isteği SMS ya da push servisinin yanıtını beklemez.

## 3. Hata yönetimi

| Durum | Yol mesafesi | Bildirim gönderimi |
|---|---|---|
| **Zaman aşımı** (5 sn / 10 sn) | Kuş uçuşuna döner | Geçici hata → yeniden dener |
| **Ağ hatası** (DNS, bağlantı) | Kuş uçuşuna döner | Geçici hata → yeniden dener |
| **5xx** (sunucu hatası) | Kuş uçuşuna döner | Geçici hata → yeniden dener |
| **400 / 401 / 403** (yanlış istek / anahtar) | Kuş uçuşuna döner | **Kalıcı hata** → yeniden denemez, hatayı kaydeder |
| **429** (hız sınırı) | `Retry-After` süresi boyunca API'yi hiç çağırmaz | Bildirim kuyrukta kalır, `Retry-After` sonra yeniden denenir |
| **Bozuk JSON** / `code` ≠ `Ok` | Kuş uçuşuna döner | — |

- **Yeniden deneme:** üstel geri çekilmeyle, yani her denemede bekleme süresi ikiye katlanarak (1 sn, 2 sn…), en fazla 3 deneme.
- **Kayıt:** Her bildirimin telefona gönderim durumu (`Yok / Kuyrukta / Gonderildi / Basarisiz`), deneme sayısı, son hata ve tarihi veritabanında tutulur. Kurumun talep sayfasında özet olarak görünür.
- **Yeniden başlatma:** Uygulama kapanıp açılırsa "Kuyrukta" kalan bildirimler veritabanından bulunup yeniden gönderilir.
- **Loglar:** Hata loglarında kişisel veri ve anahtar yok; yalnızca bildirim numarası, kanal adı ve HTTP kodu var.

## 4. Hız sınırı (rate limit)

- **İstemci tarafı:** OSRM'nin ücretsiz sunucusu saniyede en fazla 1 istek istiyor. İki istek arasında en az 1 saniye bekleniyor (`IstekArasiEnAzMilisaniye`). Önbellek, istek sayısını ayrıca azaltıyor.
- **Sunucu tarafı:** Sunucu 429 dönerse `Retry-After` başlığındaki süreye uyuluyor.

## 5. Anahtarlar (ortam değişkenleri)

`appsettings.json` dosyasında **yalnızca gizli olmayan** ayarlar (adresler, süreler) var. Anahtarlar **ortam değişkeninden** okunuyor; depoya hiç girmiyor. Bunu bir test de doğruluyor (`Depodaki_appsettings_dosyasinda_anahtar_yok`).

ASP.NET Core ortam değişkenlerini `appsettings.json`'un üstüne yazar; `__` (iki alt çizgi) bölüm ayırıcısıdır.

**PowerShell (yalnızca o terminal için):**
```powershell
$env:DisServisler__Twilio__AccountSid = "AC…"
$env:DisServisler__Twilio__AuthToken  = "…"
$env:DisServisler__Twilio__Gonderen   = "+1…"
$env:DisServisler__Ntfy__Token        = "tk_…"     # isteğe bağlı
dotnet run --project src/KanBagis.Web
```

**Kalıcı (Windows kullanıcı değişkeni):** `setx DisServisler__Twilio__AuthToken "…"` (yeni terminal açınca geçerli olur).

Anahtar tanımlı değilse ilgili kanal **kendiliğinden kapalı** kalır ve uygulama çalışmaya devam eder. Açılışta hangi kanalların açık olduğu loglanır; anahtarların kendisi loglanmaz:
```
Harici servisler — Yol mesafesi: açık, Anlık bildirim (ntfy): açık, SMS (Twilio): kapalı (ortam değişkenleri tanımlı değil)
```

## 6. Testler

[HariciApiTestleri.cs](../tests/KanBagis.Tests/HariciApiTestleri.cs): Testler **sahte bir HTTP sunucusuyla** (`SahteHttp`) gerçek internete çıkmadan çalışır. 36 test şunları kapsar:
- yanıtın ayrıştırılması, önbellek, aynı il
- 500, 400, bozuk JSON, `NoTable`, zaman aşımı, ağ hatası, 429 ve sonrasında API'nin çağrılmaması, kısmen bulunamayan yol
- ntfy: JSON gövdesi ve `Bearer` başlığı; Twilio: `Basic` kimlik doğrulama ve form alanları; hata mesajının ayrıştırılması
- yeniden deneme, kalıcı hata, hız sınırı, kapalı kanal, telefon mesajında kişisel veri olmaması
- `appsettings.json`'da anahtar olmaması, rastgele kanal adı üretimi
