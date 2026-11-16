# Yapay Zekâ Bileşeni: Talep ve Stok Tahmini (Hafta 10)

Kurum kullanıcısı **Stok tahmini** sayfasında her kan grubu için gelecek haftanın talebini ve stok riskini görür.
Tahmin bir dil modelinden (Anthropic Claude) gelir, ama uygulama çıktıyı **körlemesine kullanmaz**:
önce bağımsız bir istatistiksel tahmin hesaplanır, model çıktısı bu tabana ve kurallara göre doğrulanır,
geçersizse taban tahmine dönülür. Her çalıştırma istem, ham çıktı ve doğrulama notlarıyla günlüğe yazılır.

```
Talep geçmişi (12 hafta) + stok
        │
        ▼
İstatistiksel taban tahmin ──────────────┐  (yapay zekâ kapalıysa doğrudan bu gösterilir)
        │                                │
        ▼                                │
İstem (yalnızca toplu sayılar) → Claude API (JSON şeması) → ham çıktı
                                                     │
                                                     ▼
                                     CiktiDogrulayici (reddet / düzelt)
                                                     │
                                                     ▼
                        Ekranda sonuç + kaynak etiketi + doğrulama notları
                                                     │
                                                     ▼
                                  TahminKayitlari (AI kullanım günlüğü)
```

## 1. Veri: geçmiş talep

| Girdi | Kaynak | Not |
|---|---|---|
| Haftalık talep (ünite) | `KanTalepleri`, son 12 hafta, iptaller hariç | Kan grubu × hafta tablosu; talep olmayan hafta 0 |
| Güncel stok | `KanStoklari` | Kan grubu başına ünite |

Bağışçıların adı, TC'si, telefonu **kullanılmaz ve modele gönderilmez** (KVKK). Test: `Istemde_kisisel_veri_yoktur_yalnizca_toplu_sayilar_vardir`.

**Sentetik geçmiş:** Demo veritabanında 12 haftalık geçmiş olmadığı için Tahmin sayfasındaki bağlantı
son 12 haftaya **sentetik** talepler ekler (`SentetikGecmis.cs`, sabit tohum → her seferinde aynı desen).
Bu kayıtlar `[Sentetik geçmiş]` olarak işaretlenir ve kurumun talep listesinde görünmez. Desen bilerek çeşitlidir:
sabit (A Rh+), azalan (B Rh+), artan (0 Rh+, 0 Rh-) ve çok seyrek (AB Rh-, "veri yetersiz" örneği).

## 2. İstatistiksel taban tahmin (`IstatistikselTahmin.cs`)

- **Ortalama:** 12 haftanın ortalaması.
- **Eğilim:** en küçük kareler doğrusu, eğim = Σ(x−x̄)(y−ȳ) / Σ(x−x̄)².
- **Gelecek hafta:** doğrunun 13. haftadaki değeri (negatifse 0).
- **Karşılama:** stok / gelecek hafta tahmini (kaç hafta yeter).
- **Risk:** Kritik < 1 hafta ≤ Yüksek < 2 ≤ Orta < 4 ≤ Düşük (talep beklenmiyorsa Düşük).
- **Az veri sınırı:** talep olan hafta sayısı 4'ten azsa eğilim **hesaplanmaz**, yalnızca ortalama kullanılır ve
  "veri yetersiz" rozeti gösterilir. 1–2 haftalık veriden çizilen doğru rastlantıyı eğilim sanır.

## 3. Model / LLM API

| Ayar | Değer |
|---|---|
| SDK | Resmî Anthropic C# SDK (`Anthropic` NuGet 13.9.0) |
| Model | `claude-opus-5-5` (`DisServisler:YapayZeka:Model`) |
| Çıktı | Yapılandırılmış çıktı: `OutputConfig.Format` = JSON şeması (`TahminIstemi.Sema()`) |
| Ret yedeği | `fallbacks: "default"` (beta `server-side-fallback-2026-07-01`): model güvenlik nedeniyle reddederse sunucu önerilen başka modelle yeniden dener |
| Zaman aşımı | 90 sn (`ZamanAsimiSaniye`) |
| Anahtar | `ANTHROPIC_API_KEY` ortam değişkeni (depoda yok). Yoksa bileşen kapalı, istatistiksel tahmin gösterilir |

`AnthropicLlmIstemcisi` hiçbir zaman istisna fırlatmaz; tüm hatalar `LlmYaniti.Hata` olarak döner:
zaman aşımı, 429 hız sınırı, 5xx, diğer API hataları (401/400), ağ hatası, `stop_reason = refusal` (ret),
`stop_reason = max_tokens` (yarım JSON). Hepsinde taban tahmine dönülür.

## 4. İstem tasarımı (`TahminIstemi.cs`, sürüm `istem-v1`)

**Sistem istemi** — rol, görev ve kurallar:

1. Rol: "bölge kan merkezinin stok planlama yardımcısı"; tek görev: gelecek hafta talebi + risk.
2. Yalnızca verilen sayılara dayan; veride olmayan olayı (bayram, kaza) varsayma → uydurmayı (halüsinasyon) azaltır.
3. Taban tahmin hazır verilir; ondan sapılıyorsa gerekçe **sayılarla** yazılmalı.
4. `veriYetersiz` gruplarda eğilim çıkarma.
5. Risk tanımı sayısal olarak verilir (Kritik < 1 hafta …) → model kendi ölçütünü uydurmaz.
6. Hasta güvenliği: emin değilsen riski bir üst seviyede göster.
7. Gerekçe Türkçe, en fazla iki cümle; sekiz grubun her biri tam bir kez.

**Kullanıcı mesajı** — veri JSON olarak verilir (sayıların yanlış okunma ihtimali düşer):

```json
{
  "haftaSayisi": 12,
  "birim": "ünite",
  "kanGruplari": [
    { "kanGrubu": "0 Rh-", "haftalikTalep": [2,2,3,3,2,4,4,4,5,5,6,5],
      "mevcutStok": 2, "istatistikselTaban": 6, "haftalikEgilim": 0.27, "veriYetersiz": false }
  ]
}
```

**Çıktı şeması** — `gruplar[]` (kanGrubu: 8 değerli enum, gelecekHaftaTahmini: integer, risk: 4 değerli enum, gerekce: string) + `genelYorum`; `additionalProperties: false`.

Neden ön doldurma (prefill) ya da "yalnızca JSON yaz" talimatı değil: Opus 5.5'te ön doldurma desteklenmez;
şema zorlaması biçimi garanti eder. Ama **içeriği** garanti etmez → 5. bölüm.

## 5. Çıktı doğrulama (`CiktiDogrulayici.cs`)

| Durum | Karar |
|---|---|
| Boş çıktı, bozuk JSON, `gruplar` yok | **Reddet** → taban tahmin |
| Bilinmeyen kan grubu, aynı grup iki kez | **Reddet** |
| Grupların yarısından fazlası eksik | **Reddet** |
| 1–4 grup eksik | O gruplar tabandan tamamlanır (not) |
| Tahmin negatif, tam sayı değil ya da makul aralık dışında (0 … max(10, 3 × ortalama/taban)) | Taban değeri (not) |
| Risk değeri geçersiz | Modelin tahmininden hesaplanan risk (not) |
| Risk, modelin **kendi** tahmininden hesaplanan riskten düşük | Hesaplanan riske yükseltilir (not) |
| **Güvenlik kuralı:** taban Kritik/Yüksek derken model daha düşük risk diyor | Taban risk korunur (not) |
| Gerekçe boş / 300 karakterden uzun | Taban gerekçe / kısaltma (not) |

Sonucun kaynağı ekranda etiketlenir: **Yapay zekâ (doğrulandı)**, **Yapay zekâ (doğrulamada düzeltildi)**
ya da **İstatistiksel tahmin (yapay zekâ kullanılmadı)**. Notlar sayfada ve günlükte görünür.

## 6. AI kullanım günlüğü (`TahminKayitlari` tablosu, `/Tahmin/Gunluk`)

Her çalıştırmada saklanır: tarih, kaynak, model, **istem** (sistem + kullanıcı, istem sürümüyle), **ham çıktı**,
**doğrulama notları**, gösterilen sonuç (JSON), süre. Son kayıt aynı zamanda önbellektir: sayfa her açılışta
API'yi çağırmaz, yalnızca "Tahmini güncelle" butonu çağırır (maliyet ve hız sınırı).

## 7. Etik ve şeffaflık

- Sayfada "Şeffaflık" notu: tahminin nereden geldiği, modele hangi verinin gittiği, kararın insana ait olduğu.
- Modele kişisel veri gitmez; yalnızca kurum rolü görür.
- Model uyarıyı **düşüremez**, yalnızca yükseltebilir: yanlış "düşük risk" hasta güvenliğini etkiler, yanlış "yüksek risk" yalnızca gereksiz kampanya demektir.
- Yapay zekâ olmadan da sistem çalışır (istatistiksel tahmin); YZ'nin katkısı gerekçe ve genel yorum metni ile sayıları bağlamla yorumlamasıdır.

## 8. Testler (`YapayZekaTestleri.cs`, sahte model — gerçek API çağrılmaz)

Taban tahmin (sabit, artan, az veri, sıfır talep, eşikler); doğrulayıcı (geçerli, boş, bozuk JSON, tekrar, bilinmeyen grup,
çok eksik, tek eksik, uçuk/negatif tahmin, geçersiz risk, güvenlik kuralı, tutarsız risk, gerekçe);
servis (anahtar yok, geçerli çıktı + günlük, kişisel veri yok, bozuk/ret/zaman aşımı → taban, düzeltildi etiketi, önbellek,
sentetik geçmiş); web (yalnızca kurum, CSRF, şeffaflık notu).

## Canlı deneme

```powershell
$env:ANTHROPIC_API_KEY = "sk-ant-..."   # anahtar yalnızca bu oturumda; depoya yazılmaz
dotnet run --project src/KanBagis.Web
```

Kurum hesabıyla **Stok tahmini** → (gerekirse sentetik geçmiş ekle) → **Tahmini güncelle** → **AI kullanım günlüğü**.
