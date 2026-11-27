# Demo Senaryosu (v0.7.0)

**Süre:** yaklaşık 8 dakika (çekirdek akış 5 dk + ileri modüller 3 dk) · **Amaç:** Kurulumdan başlayarak tüm modüllerin birlikte çalıştığını göstermek.
Vize sürümündeki (v0.1.0) ilk 11 adım aynen geçerlidir; 12–17. adımlar sonraki haftaların modülleridir.

## Hazırlık (demodan önce)

```bash
git clone https://github.com/simsekdilan93-dot/kanbag-s-yonetimivebag-sc-eslestirme-.git kanbagis && cd kanbagis
dotnet test                              # testlerin hepsi geçmeli
dotnet run --project src/KanBagis.Web    # http://localhost:5284  (README → Kurulum)
```

- Tarayıcıda iki pencere aç: biri normal, biri **gizli pencere**. Böylece kurum ve bağışçı hesaplarıyla aynı anda oturum açık kalır.
- Örnek verideki bağışçıların hesabı yok. Hesaplar demo sırasında açılır (tüm veriler sentetiktir).

## Akış

| # | Kim | Ne yapılır | Ne gösterilir / anlatılır |
|---|---|---|---|
| 1 | — | Ana sayfa → **Kan grupları** | Kan grubu uyumluluk tablosu. "0 Rh- herkese verebilir, AB Rh+ herkesten alabilir" kuralı **kodla hesaplanıyor** (Modül A). |
| 2 | Bağışçı (gizli pencere) | **Kayıt ol** → Deniz Aksoy, A Rh+, **Bingöl**, 64 kg, TC `10000001440` | TC algoritması, 18 yaş, 81 il listesi doğrulaması (Hafta 6). Kayıt bitince **bağışçı paneli**: "Bağış yapabilirsiniz". |
| 3 | Bağışçı | **Profilimi düzenle** | TC maskeli. Kan grubu ilk bağışa kadar değiştirilebiliyor. Kimlik ve sahiplik oturumdan alınıyor (Hafta 5–6). |
| 4 | Kurum (normal pencere) | **Kurum kaydı** → Bingöl'de bir hastane | **Kurum paneli**: stok tablosu ve "Bingöl'de uygun bağışçı" sayıları (isimler yok, KVKK). |
| 5 | Kurum | Adres çubuğuna `/BagisciPanel` yaz | **Erişim engellendi** → rol kontrolü sunucuda yapılıyor (Hafta 5). |
| 6 | Kurum | **+ Yeni talep** → A Rh+, **1 ünite**, **Acil** | "N uygun bağışçıya bildirim gönderildi". Eşleştirme: uyumlu gruplar + 300 km yarıçap + uygunluk + sıralama (Modül B, Hafta 7). |
| 7 | Bağışçı | Paneli yenile | Kırmızı **ACİL** kan çağrısı → **Gelebilirim**. |
| 8 | Kurum | Talep detayını yenile | Kabul eden bağışçının **adı ve telefonu** görünüyor (yalnızca kabul edenler, açık rıza). |
| 9 | Kurum | **Bağış alındı** | "Bağış kaydedildi ve stoğa eklendi… talep karşılandı" → talep **Karşılandı**. Kurum panelinde **A Rh+ stoğu 1 arttı** (Hafta 8 entegrasyonu). |
| 10 | Bağışçı | Paneli yenile | Son bağış **bugün**, "Şu an bağış yapamazsınız" ve **uygun olunacak tarih** (kadın 120 / erkek 90 gün). Modül A, Modül B'nin kaydettiği bağışı görüyor. |
| 11 | — | GitHub → **Tags** (depo sayfasında dal seçicinin yanındaki "Tags") | Her haftanın etiketlenmiş sürümü (`v0.1.0` … `v0.7.0`), `CHANGELOG.md` sürüm notları. |
| 12 | Kurum | **Stok takibi** → A Rh+ için 16 ünite **Transfer (giden)** | Stok kritik eşiğe inecekse **onay** istenir; onaydan sonra **KRİTİK uyarı** açılır, menüde uyarı sayısı artar (Hafta 11). |
| 13 | Kurum | 0 Rh- için 10 ünite **Transfer (gelen)** | Stok eşiğin üstüne çıkınca uyarı **kendiliğinden kapanır**; **Hareket geçmişi**nde önceki → sonraki stok ve 9. adımdaki bağış da görünür. |
| 14 | Kurum | **Stok tahmini** → (gerekirse sentetik geçmiş ekle) → **Tahmini güncelle** | Gelecek hafta tahmini ve risk; yapay zekâ çıktısı doğrulanıyor, anahtar yoksa istatistiksel tahmin; **AI kullanım günlüğü**nde istem (Hafta 10). |
| 15 | Kurum | **İstatistik** → son 365 gün, sonra "yalnızca hastanem" | Karşılanma oranı pay/payda ile, medyan süre, grafikler (eksen 0'dan), bildirim özeti, **CSV indir** (Hafta 12). |
| 16 | — | Adres çubuğuna `/boyle-bir-sayfa-yok`, sonra `/saglik` | Türkçe 404 sayfası; sağlık kontrolü `Healthy` (Hafta 14). |
| 17 | — | Klavyede **Tab** | İlk Tab'da "Ana içeriğe geç" bağlantısı (erişilebilirlik, Hafta 14). |

## Demo sırasında söylenecek tek cümleler

- **Mimari:** "Web yalnızca Business'ı, Business yalnızca DataAccess'i çağırıyor; her adımda veri oradan geçiyor."
- **Entegrasyon:** "Bağış alındı düğmesi tek transaction'da üç iş yapıyor: bağış kaydı, stok artışı, gerekirse talebin kapanması."
- **Regresyon:** "Her hafta eklediğim her şeyden sonra önceki testlerin hepsi de çalışıyor; hiçbir eski özellik bozulmadı."
- **Güvenilirlik:** "Kurallar hem serviste hem veritabanında: aynı anda gelen istekler bile ikinci açık talep ya da eksi stok oluşturamıyor."
- **Dağıtım:** "Gizli anahtarlar depoda değil ortam değişkeninde; bunu her test çalıştırmasında bir test denetliyor."

## Bir şey ters giderse

| Durum | Çözüm |
|---|---|
| "Şu an bağış yapamazsınız" (kilo) | Kayıtta kilo en az 50 olmalı. |
| Bildirim gelmedi | Bağışçı ve kurum aynı ilde mi, kan grubu uyumlu mu? Örneğin A Rh+ talebine A Rh+, A Rh-, 0 Rh+ ve 0 Rh- bağışçılar uyar. |
| "Kabul ettiğiniz başka bir açık talep var" | İş kuralı (tek aktif kabul). Önceki talebi kurum hesabından kapatın. |
| Site açılmıyor | Terminalde `dotnet run --project src/KanBagis.Web` çalışıyor mu? Adres http://localhost:5284 |
