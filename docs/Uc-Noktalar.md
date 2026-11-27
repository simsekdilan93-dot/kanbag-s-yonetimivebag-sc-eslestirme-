# HTTP Uç Noktaları

Uygulama sunucu tarafında sayfa üreten bir MVC uygulamasıdır; ayrı bir JSON API'si yoktur. Aşağıdaki adresler
tarayıcıdan çağrılır. Kural: **GET** yalnızca okur, **POST** veriyi değiştirir ve **CSRF belirteci** ister
(`AutoValidateAntiforgeryToken`, belirteç yoksa 400). Yetkisiz istekte giriş yapılmamışsa `/Hesap/Giris`'e, rol uymuyorsa
`/Hesap/ErisimEngellendi`'ye yönlendirilir. Başka hastanenin kaydı ya da olmayan kayıt **404** döner (kayıt olup olmadığı sızdırılmaz).

Rol: **Herkes** · **Bağışçı** · **Kurum**. Kimlik (bağışçı/hastane numarası) her zaman oturum çerezinden okunur.

## Genel

| Yöntem | Adres | Rol | Açıklama |
|---|---|---|---|
| GET | `/` | Herkes | Ana sayfa |
| GET | `/KanGrubu` | Herkes | Kan grubu uyumluluk tablosu |
| GET | `/saglik` | Herkes | Sağlık kontrolü: `Healthy` (200) ya da `Unhealthy` (503); veritabanı erişimini de denetler |
| GET | `/Home/Durum?kod=404` | Herkes | 400/403/404/429/500 için Türkçe açıklama sayfası (durum kodu korunur) |
| GET | `/Home/Error` | Herkes | Beklenmeyen hata sayfası (yalnızca istek numarası gösterilir) |

## Hesap

| Yöntem | Adres | Rol | Açıklama |
|---|---|---|---|
| GET/POST | `/Hesap/Giris` | Herkes | Giriş. 5 hatalı denemede 15 dk kilit. `returnUrl` yalnızca site içi adres olabilir |
| POST | `/Hesap/Cikis` | Giriş yapmış | Oturumu kapatır |
| GET/POST | `/Hesap/KayitBagisci` | Herkes | Bağışçı kaydı (TC algoritması, 18 yaş, il listesi) |
| GET/POST | `/Hesap/KayitKurum` | Herkes | Kurum (hastane) kaydı |
| GET | `/Hesap/ErisimEngellendi` | Herkes | Rol yetmediğinde gösterilir |

## Bağışçı

| Yöntem | Adres | Açıklama |
|---|---|---|
| GET | `/BagisciPanel` | Uygunluk durumu, gelen çağrılar (yol mesafesiyle), bağış geçmişi |
| POST | `/BagisciPanel/Yanitla` | `bildirimId`, `yanit=Kabul\|Red`. Kabulde uygunluk ve "aynı anda tek kabul" kuralı |
| GET/POST | `/BagisciPanel/Profil` | Telefon, il, kilo, (bağışı yoksa) kan grubu |
| POST | `/BagisciPanel/AnlikBildirim` | `ac=true/false`: ntfy kanalı aç/kapat |
| POST | `/BagisciPanel/SmsIzni` | `izin=true/false` |

## Kurum

| Yöntem | Adres | Açıklama |
|---|---|---|
| GET | `/KurumPanel` | Talepler, kan grubu stok durumu, ildeki uygun bağışçı sayısı, stok uyarıları |
| GET/POST | `/KurumTalep/Yeni` | Talep aç (`KanGrubuId`, `UniteSayisi` 1–10, `Aciliyet`, `Aciklama`); aynı grupta ikinci açık talep reddedilir |
| GET | `/KurumTalep/Detay/{id}` | Talep, yanıtlar, kabul edenlerin iletişim bilgisi (yalnızca talebin hastanesi) |
| POST | `/KurumTalep/DahaFazlaBildir/{id}` | Henüz bildirim gitmemiş uygun bağışçılara gönder |
| POST | `/KurumTalep/BagisKaydet/{id}` | `bildirimId`: bağış + stok girişi + gerekirse talebi kapat (tek transaction) |
| POST | `/KurumTalep/Kapat/{id}` | `durum=Karsilandi\|Iptal` (geri alınamaz; arayüz onay sorar) |
| GET | `/Stok` | Stoklar, eşikler, açık uyarılar, giriş/çıkış formu |
| POST | `/Stok/Hareket` | `KanGrubuId`, `Neden`, `Miktar` 1–50, `Aciklama`, `KritikOnayi` |
| POST | `/Stok/Esik` | `kanGrubuId`, `kritikEsik`, `uyariEsigi` (0–500, düşük > kritik) |
| GET | `/Stok/Hareketler` | `?kanGrubuId=&tur=Giris\|Cikis&yalnizcaBenim=true` |
| GET | `/Stok/Uyarilar` | Uyarı geçmişi |
| GET | `/Tahmin` | Son tahmin (önbellek), şeffaflık notu |
| POST | `/Tahmin/Calistir` | Yeni tahmin (yapay zekâ ya da istatistik) |
| POST | `/Tahmin/SentetikGecmis` | Demo için işaretli sentetik talep geçmişi |
| GET | `/Tahmin/Gunluk`, `/Tahmin/Kayit/{id}` | AI kullanım günlüğü: istem, ham çıktı, doğrulama notları |
| GET | `/Istatistik` | `?gun=7\|30\|90\|365&hastanem=true&sentetik=true` |
| GET | `/Istatistik/Csv` | Aynı süzgeçle CSV dosyası (`text/csv`, UTF-8 BOM, `;`) |

## Uygulamanın çağırdığı dış API'ler

| Servis | Adres | Ne için | Ayrıntı |
|---|---|---|---|
| OSRM | `GET {TemelAdres}/table/v1/driving/{koordinatlar}` | İller arası yol mesafesi/süresi | [Harici-API.md](Harici-API.md) |
| ntfy | `POST {TemelAdres}` (JSON) | Telefona anlık bildirim | [Harici-API.md](Harici-API.md) |
| Twilio | `POST /2010-04-01/Accounts/{Sid}/Messages.json` | SMS | [Harici-API.md](Harici-API.md) |
| Anthropic | Messages API (resmî C# SDK) | Talep/stok tahmini | [Yapay-Zeka-Bileseni.md](Yapay-Zeka-Bileseni.md) |
