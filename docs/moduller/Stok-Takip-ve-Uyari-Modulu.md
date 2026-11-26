# Stok Takip ve Uyarı Modülü (Hafta 11)

Bölgedeki kan stoğunu kan grubu bazında izler, her giriş/çıkışı kaydeder ve stok kritik eşiğe inince uyarı verir.
Yalnızca **kurum** (hastane personeli) rolü erişir: menüde **Stok takibi** (yanında açık uyarı sayısı).

## 1. Veri modeli

```
KanGrubu 1 ── 1 KanStok            (UniteSayisi, KritikEsik, UyariEsigi)
KanGrubu 1 ── N StokHareketi       (Tur, Neden, Miktar, OncekiStok, SonrakiStok, Tarih, Aciklama)
                  N ── 0..1 Hastane      (hareketi yapan kurum)
                  N ── 0..1 Kullanici    (hareketi giren personel)
                  1 ── 0..1 Bagis        (Neden = Bagis ise)
KanGrubu 1 ── N StokUyarisi        (Seviye, StokDegeri, Esik, AcilisTarihi, KapanisTarihi)
```

| Tablo | Önemli kısıtlar |
|---|---|
| `KanStoklari` | `UniteSayisi >= 0`; `KritikEsik >= 0 AND UyariEsigi >= KritikEsik`; KanGrubuId benzersiz (1-1) |
| `StokHareketleri` | `Miktar > 0`; stoklar ≥ 0; **SonrakiStok = OncekiStok ± Miktar** (türe göre); neden türle uyumlu; `Bagis` nedeni ⇔ BagisId dolu; BagisId benzersiz (bir bağış = bir giriş); tüm FK'ler `Restrict` |
| `StokUyarilari` | Seviye yalnızca `Dusuk`/`Kritik`; **kan grubu başına en fazla bir açık uyarı** (filtreli benzersiz indeks `KapanisTarihi IS NULL`) |

Hareketler **değiştirilmez ve silinmez** (denetim izi). Yanlış giriş, ters yönde bir düzeltme hareketiyle (sayım fazlası/eksiği) düzeltilir.

## 2. Kritik eşik

`KanStok.DurumHesapla(stok, kritik, uyari)` — tek yerde tanımlı, sayfalar/uyarılar/testler aynı kuralı kullanır:

- stok ≤ KritikEsik → **Kritik**
- stok ≤ UyariEsigi → **Düşük**
- aksi hâlde **Normal**

Eşiğe **eşit** stok da uyarı verir (eşik "en az bu kadar olmalı" anlamında). Başlangıç eşikleri kan grubunun kullanım
sıklığıyla orantılı (0 Rh+ 10/20, A Rh+ 8/15 … AB Rh- 1/2; Tahmin sayfasındaki haftalık ortalamalarla aynı sıralama).
Kurum her grubun eşiğini sayfadan değiştirebilir; değişince uyarı hemen yeniden değerlendirilir.

## 3. Giriş/çıkış kaydı

| Tür | Nedenler |
|---|---|
| Giriş | Bağış (yalnızca bağış kaydıyla), Transfer (gelen), Sayım fazlası |
| Çıkış | Transfüzyon, Transfer (giden), İmha, Sayım eksiği |

Kayıt `KanStokRepository.HareketUygulaAsync` ile **tek transaction**'da yapılır:

1. Atomik güncelleme: `UPDATE KanStoklari SET UniteSayisi = UniteSayisi + @fark WHERE KanGrubuId = @g AND UniteSayisi + @fark >= 0`.
   Etkilenen satır 0 ise stok yetmiyordur → hiçbir şey yazılmaz. (Önce okuyup sonra yazmak, aynı anda iki çıkışta stoğu eksiye düşürebilirdi.)
2. Önceki/sonraki stokla hareket kaydı eklenir.
3. Uyarı güncellenir (aşağıda). 4. Commit.

## 4. İş kuralları ve doğrulama (`StokService`)

| Kural | Neden |
|---|---|
| Miktar 1–50 | Yanlışlıkla 500 yazılmasını engeller |
| Bağış girişi elle yapılamaz | Bağış, bağışçı ve talep bilgisiyle "Bağış alındı"dan gelir; elle girilirse geçmiş tutarsız olur |
| İmha ve sayım düzeltmesinde açıklama zorunlu (≥ 5 karakter) | Stok kaybı denetlenebilir olmalı |
| Çıkış stoktan fazla olamaz | Stok negatif olamaz (servis + veritabanı) |
| Stoğu kritik eşiğe indiren çıkış **açık onay** ister | Personel kritik duruma bilerek girer |
| Eşikler 0–500, düşük eşik > kritik eşik | Önce "düşük", sonra "kritik" uyarısı anlamlı olsun |
| Hareketi yapan kurum/kullanıcı **oturumdan** alınır | Sahiplik: formdan başka hastane adına kayıt girilemez |

## 5. Uyarı kuralı

Her stok değişikliğinden ve eşik değişikliğinden sonra:

- Durum **Normal** → açık uyarı varsa **kapanır** (kapanış tarihi yazılır).
- Durum **Düşük/Kritik** → açık uyarı yoksa **açılır**; varsa seviyesi, stok değeri ve eşiği **güncellenir** (yeni uyarı açılmaz).

Uyarılar menüde sayı rozeti, kurum panelinde ve stok sayfasında kırmızı şerit olarak görünür; geçmişi `/Stok/Uyarilar`.
Migration, var olan veritabanında eşiğin altındaki stoklar için başlangıç uyarılarını **o anki stok değerinden** üretir.

## 6. Diğer modüllerle entegrasyon

- **Talep/Bağış (Hafta 7–8):** "Bağış alındı" artık stoğu doğrudan değil, `Bagis` nedenli bir **giriş hareketiyle** artırır
  (bağış transaction'ının içinde). Bağış hareket geçmişinde görünür; stok eşiğin üstüne çıkarsa uyarı kendiliğinden kapanır.
- **Kurum paneli:** sabit "5 ünitenin altı" kuralı kaldırıldı; satır renkleri ve rozetler modülün eşik hesabını kullanıyor.
- **Tahmin (Hafta 10):** aynı stok değerini kullanır; tahmin "kaç hafta yeter", bu modül "şu an eşiğin altında mı" sorusunu yanıtlar.

## 7. Testler (`StokModuluTestleri.cs` + web testleri, 34 yeni test)

Durum eşikleri; başlangıç uyarıları; giriş/çıkış ve önceki/sonraki stok; stoktan fazla çıkış (servis ve atomik depo);
CHECK kısıtları; miktar, bağış, açıklama, bilinmeyen grup kuralları; kritik onayı; uyarı açılma/kapanma/seviye değişimi;
tek açık uyarı; eşik doğrulama ve yeniden değerlendirme; bağış → giriş hareketi entegrasyonu; filtreler;
web: yalnızca kurum, CSRF, formdaki hastane kimliğinin yok sayılması, panel/menü uyarısı.
