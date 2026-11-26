# İstatistik Panosu ve Bildirim Özeti (Hafta 12)

Kurum personeli için bağış, talep, bildirim ve stok istatistikleri. Menüde **İstatistik** (yalnızca kurum rolü).
Yalnızca **toplu sayılar** gösterilir; bağışçı adı, TC ya da telefonu panoda yoktur (KVKK).

## 1. Panoyu besleyen veriler

| Kaynak tablo | Kullanılan alanlar | Pano bölümü |
|---|---|---|
| `KanTalepleri` | OlusturmaTarihi, KapanisTarihi, Durum, Aciliyet, UniteSayisi, KanGrubuId, HastaneId | Talep kartları, karşılanma oranı/süresi, haftalık grafik, kan grubu |
| `Bagislar` (+ `Bagiscilar.KanGrubuId`) | BagisTarihi, UniteSayisi, KanTalebiId, HastaneId | Bağış kartı, haftalık grafik, kan grubu |
| `Bildirimler` | GonderimTarihi, Yanit, DisKanalDurumu (+ talebin HastaneId) | Bildirim özeti, telefon gönderimi |
| `KanStoklari`, `StokUyarilari` | UniteSayisi, eşikler, açık uyarılar | Kan grubu tablosu, kritik stok |

Süzgeçler: dönem (7 / 30 / 90 / 365 gün, bugün dahil), **yalnızca hastanem** (hastane oturumdan alınır),
**sentetik geçmişi dahil et** (Hafta 10 demosu; varsayılan **hariç**, dahil edilirse sayfada uyarı notu çıkar).

## 2. Toplulaştırma (`IstatistikRepository`)

Sayma ve toplama veritabanında yapılır; belleğe yalnızca gruplanmış küçük sonuç gelir:

```sql
SELECT KanGrubuId, Durum, Aciliyet, COUNT(*), SUM(UniteSayisi)
FROM KanTalepleri
WHERE OlusturmaTarihi >= @bas AND OlusturmaTarihi < @bit [AND HastaneId = @h] [AND Aciklama <> '[Sentetik geçmiş]']
GROUP BY KanGrubuId, Durum, Aciliyet;
```

Bağışlar `Bagisci.KanGrubuId` ve "talebe bağlı mı" ile, bildirimler `Yanit` ve `DisKanalDurumu` ile gruplanır.
Haftalık dağılım ve karşılanma süresi için yalnızca gereken iki sütun (tarih, ünite / oluşturma, kapanış) çekilir.
Tarih aralığı yarı açıktır: `[başlangıç, bitiş)` — sınırdaki kayıt iki dönemde birden sayılmaz.

## 3. Metrikler ve neden seçildi (`MetrikHesap`)

| Metrik | Tanım | Neden |
|---|---|---|
| Talep sayısı / ünite | dönemde açılan talepler | İhtiyacın büyüklüğü |
| **Karşılanma oranı** | karşılanan / (karşılanan + iptal) | Sistemin asıl amacı: talep karşılanıyor mu? **Açık talepler paydaya girmez** (henüz sonuçlanmadı) |
| **Karşılanma süresi** | kapanış − oluşturma; **medyan** öne çıkar, ortalama da yazılır | Acil kan için hız kritik; medyan birkaç uç değerden etkilenmez |
| Acil talep oranı | acil / tüm talepler | Yükün ne kadarının acil olduğu |
| Bağış sayısı / ünite | talebe bağlı + gönüllü | Toplanan kan; bağışçının kan grubuna sayılır |
| Bildirim yanıt oranı | (kabul + red) / gönderilen | Bağışçılar çağrılara dönüyor mu? |
| Kabul oranı | kabul / (kabul + red) | Bekleyenler hariç; sonuçlanmış yanıtlar |
| Telefon gönderim başarısı | gönderildi / (gönderildi + başarısız) | Harici kanal (Hafta 9) çalışıyor mu? Telefonu kapalı ve kuyruktakiler hariç |
| Kan grubu talep − bağış farkı, stok durumu | grup bazında | Hangi grup için kampanya gerekli |

**Oranlar pay/payda ile yazılır** (`%80 (4/5)`). Payda 0 ise oran **yok** ("veri yok"; `%0` yazmak yanıltıcı olurdu),
payda 5'ten küçükse "az veri" rozeti çıkar.

## 4. Grafikler (`SvgGrafik`)

Sunucuda üretilen SVG; harici kütüphane yok, sayfa çevrimdışı da çalışır.

| Grafik | Veri | Tür |
|---|---|---|
| Haftalık talep ve bağış | `Haftalar` (talep ünite, bağış ünite) | Gruplu sütun |
| Kan grubuna göre | `KanGruplari` (talep, bağış) | Yatay gruplu çubuk |
| Bildirim yanıtları | Kabul / Red / Beklemede | %100 yığılmış çubuk |
| Telefona gönderim | Gönderildi / Başarısız / Kuyrukta / Kapalı | %100 yığılmış çubuk |

Yanıltıcı görselden kaçınma: değer ekseni **0'dan başlar**; eksen üst sınırı yuvarlak sayı (1-2-5 × 10ⁿ) ve tüm seriler aynı ölçekte;
her çubuğun değeri üstünde yazılı; renk tek başına anlam taşımaz (lejant + sayı + yüzde); **kaydı olmayan hafta 0 olarak gösterilir**
(atlanırsa eğilim yanlış görünür); hiç veri yoksa boş eksen yerine "Bu dönemde veri yok". Grafik genişliği sayfadaki sütuna
yakın tutulur (SVG küçültülünce yazılar okunaksızlaşır). Grafiklerin altında aynı verinin tablosu da var.

## 5. Bildirim özeti ve dışa aktarma

- Pano başında sayılardan üretilen **kısa özet metni** (ör. "Son 90 günde 12 talep… %89'u karşılandı… Kritik stok: 0 Rh-").
- **Veri notları:** az veri, açık talepler, telefonu kapalı bildirimler, sentetik veri gibi durumlar gizlenmez, yazılır.
- **CSV indir:** aynı süzgeçle; Türkçe Excel için `;` ayraç, UTF-8 BOM, ondalık virgül. `= + - @` ile başlayan metinlerin başına `'` eklenir (formül enjeksiyonu).

## 6. Yanlış/eksik veri sonucu nasıl etkiler?

- Kapatılmamış (açık kalmış) talepler karşılanma oranına girmez; "N talep hâlâ açık" notu çıkar.
- Kapanış tarihi oluşturmadan önce olan bozuk kayıt süre hesabına katılmaz.
- Sentetik veri karıştırılırsa sayılar şişer → varsayılan hariç, dahil edilirse uyarı.
- Az veride (%100 = 1/1) oran yanıltıcıdır → pay/payda ve "az veri" rozeti.

## 7. Testler (`IstatistikTestleri.cs` + web, 33 yeni test)

Oran tanımları ve "veri yok"; karşılanma oranı (açık hariç), medyan/ortalama, bozuk süre; bildirim oranları;
haftalık dağılım sınırları ve boş haftalar; **SQL toplulaştırma sonucu = ham kayıtlardan elle hesaplama**; kan grubu toplamları
genel toplamla tutarlı; sentetik ve hastane süzgeci; boş dönem notları; CSV (BOM, ayraç, kaçış, ondalık); eksen üst sınırı;
grafikte 0'dan başlayan eksen, değer etiketleri, HTML kodlama, "veri yok"; web: yalnızca kurum, CSV indirme, KVKK.
