# Erişilebilirlik ve Kullanılabilirlik (Hafta 14)

## Yöntem

- **Otomatik denetim:** [axe-core 4.10](https://github.com/dequelabs/axe-core) (WCAG 2.0/2.1 A ve AA kuralları + iyi uygulamalar),
  gerçek tarayıcıda (Edge, başsız) 17 sayfada çalıştırıldı: anonim sayfalar, hatalı form gönderimi, bağışçı paneli ve profili,
  kurum paneli, talep oluşturma ve detay, stok, tahmin, istatistik, 404.
- **Elle kontrol:** yalnızca klavye ile gezinme (Tab / Shift+Tab / Enter), ekran okuyucunun okuyacağı adlar (etiketler, `aria-*`),
  geri alınamayan işlemler, çift tıklama, mobil genişlik (Bootstrap duyarlı düzen).

## Sonuç

| | Önce | Sonra |
|---|---:|---:|
| İhlali olan sayfa | 3 / 17 | **0 / 17** |
| Farklı kural | 4 | **0** |
| Etkilenen öğe | 11 | **0** |

| Kural (axe) | Etki | Nerede | Düzeltme |
|---|---|---|---|
| `color-contrast` | Ciddi | Kurum panelinde kırmızı uyarı içindeki çerçeveli kırmızı düğme; stok tablosunda renkli satırlardaki gri "Kaydet" düğmeleri | Dolu kırmızı düğme; koyu çerçeveli düğme (`btn-outline-dark`) |
| `region`, `landmark-one-main`, `meta-viewport` | Orta / kritik | 404 durumunda | Uygulama boş yanıt dönüyordu, tarayıcının kendi hata sayfası çıkıyordu. Türkçe durum sayfası eklendi (`/Home/Durum`); durum kodu 404 olarak korunur |

## Elle bulunan ve düzeltilen sorunlar

| Sorun | Kimi etkiler | Düzeltme |
|---|---|---|
| Menüyü atlayıp içeriğe geçme yolu yoktu | Klavye ve ekran okuyucu kullanıcıları her sayfada tüm menüyü geçmek zorundaydı | İlk Tab'da görünen **"Ana içeriğe geç"** bağlantısı → `<main id="icerik">` |
| Mobil menü düğmesinin adı İngilizce ("Toggle navigation"), `aria-controls` var olmayan bir öğeyi gösteriyordu | Ekran okuyucu | "Menüyü aç / kapat", `aria-controls="anaMenu"` |
| Menüde açık sayfa belli değildi | Herkes; ekran okuyucu | Görsel vurgu + `aria-current="page"` (yalnızca açık sayfada) |
| Logo emojisi "kan damlası" diye okunuyordu | Ekran okuyucu | `aria-hidden="true"` |
| Talebi kapatmak (Karşılandı / İptal) **geri alınamaz** ama onay sorulmuyordu | Herkes (yanlış tıklama) | Onay penceresi (`data-onay`) |
| Form iki kez gönderilebiliyordu (yavaş yapay zekâ tahmini, harici servisler) | Herkes | Gönderilen düğme kilitlenir ve "İşleniyor…" yazar (`aria-busy`); sunucudaki asıl koruma H13-04 |
| Başarı/hata mesajları kapatılamıyordu | Herkes | Kapat düğmeli uyarılar; mesajlar `role="status"` / `role="alert"` ile duyurulur |
| E-posta alanında telefon ilk harfi büyütüyordu ("İlker@…") | Mobil kullanıcılar | `autocapitalize="none"`, `spellcheck="false"` (H13-06) |
| Stok tablosundaki her satırda aynı "Kaydet" düğmesi | Ekran okuyucu hangi grubun kaydedileceğini bilmiyordu | Erişilebilir ad: "A Rh+ eşiklerini kaydet" |
| 404 / 403 / 400'de boş sayfa | Herkes | Türkçe açıklama ve "Ana sayfaya dön" |

## Zaten olanlar (önceki haftalar)

`<html lang="tr">`; her form alanının `<label>`'ı; doğrulama hataları alanın yanında metin olarak; grafiklerde `role="img"` ve
başlık, renk tek başına anlam taşımıyor (değer + lejant), grafiklerin altında aynı verinin tablosu; tablolar
`.table-responsive` (mobilde yatay kaydırma); yakınlaştırma serbest (`width=device-width, initial-scale=1`).

## Sınırlar

Otomatik araçlar WCAG sorunlarının yalnızca bir kısmını yakalayabilir. Gerçek bir ekran okuyucuyla (NVDA) uçtan uca deneme ve
klavye odak sırasının tüm sayfalarda tek tek incelenmesi sonraki adımdır. Denetim betiği: Hafta 14 raporundaki `erisim_denetimi.mjs`.
