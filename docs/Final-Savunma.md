# Final Teknik Özet ve Savunma Hazırlığı (v1.0.0)

Kan Bağışı Yönetimi ve Bağışçı Eşleştirme Sistemi · P08 · Dilan Şimşek · Bingöl Üniversitesi, Bilgisayar Programcılığı

## 1. Bir bakışta

| | |
|---|---|
| Amaç | Hastanenin acil kan ihtiyacını, kan grubu uyumlu, bağış yapabilecek durumdaki ve yakındaki gönüllülerle hızla buluşturmak; stoğu izlemek |
| Kullanıcılar | Bağışçı · Kurum (hastane personeli) |
| Teknoloji | C#, ASP.NET Core MVC (.NET 10), EF Core 10, SQLite, Bootstrap; xUnit |
| Mimari | 4 proje, katmanlı: Web → Business → DataAccess → SQLite (+ Entities) |
| Boyut | ~5.000 satır C# (Business 2.538, DataAccess 1.091, Web 1.111, Entities 292) + 1.498 satır Razor; 9 controller, 24 görünüm, 11 tablo, 9 migration |
| Test | **337 test** (birim + entegrasyon + uçtan uca), satır kapsamı %92; 16 adımlı uçtan uca demo senaryosu otomatik doğrulanıyor |
| Harici | OSRM (yol mesafesi), ntfy (anlık bildirim), Twilio (SMS), Claude API (tahmin) — hepsi kapalıyken de sistem çalışır |
| Sürüm | 15 haftada 12 etiketli sürüm: v0.1.0 (vize) … **v1.0.0 (final)** — [CHANGELOG](../CHANGELOG.md) |

## 2. Sistem uçtan uca (sözlü anlatım sırası)

1. **Kayıt ve kimlik.** Bağışçı TC (algoritma), doğum tarihi (18 yaş), il, kilo, kan grubuyla kaydolur; kurum hastane bilgisiyle.
   Parola PBKDF2 ile tuzlanarak saklanır, 5 hatalı denemede hesap kilitlenir, oturum çerezle tutulur, rol ve hastane kimliği oturumdadır.
2. **Talep.** Kurum "A Rh+, 1 ünite, acil" talebi açar. Aynı grup için ikinci açık talep açılamaz (servis + veritabanı benzersiz indeksi).
3. **Eşleştirme.** Sistem A Rh+'ya kan **verebilecek** grupları (A Rh+, A Rh-, 0 Rh+, 0 Rh-) bulur; bugün bağış yapabilecek olanları seçer
   (yaş 18–65, kilo ≥ 50, son bağıştan 90 gün erkek / 120 gün kadın); acilde 300 km, normalde 100 km içindekileri **yol mesafesine** göre sıralar;
   ünite başına acilde 5, normalde 3 kişiye bildirim gönderir. Bildirim site içinde görünür, bağışçı açtıysa telefonuna da gider (kişisel veri içermeden).
4. **Yanıt.** Bağışçı "Gelebilirim" der (aynı anda tek kabul; o an uygun mu yeniden kontrol edilir). Kurum yalnızca kabul edenin adını ve telefonunu görür.
5. **Bağış.** Kurum "Bağış alındı" der: tek transaction'da bağış kaydı + stok **giriş hareketi** + uyarının güncellenmesi + talep yeterli bağışa ulaştıysa kapanışı.
6. **Stok.** Her giriş/çıkış önceki ve sonraki stokla kaydedilir, silinemez; stok asla eksiye düşmez (atomik koşullu güncelleme). Stok eşiğin altına inince
   uyarı açılır, üstüne çıkınca kapanır; kritik seviyeye indiren çıkış onay ister.
7. **Tahmin.** Son 12 haftanın talebinden istatistiksel tahmin (ortalama + eğilim); Claude API varsa yorum ve gerekçe ekler; çıktı doğrulanır,
   geçersizse istatistiğe dönülür; her çalıştırma istem ve ham çıktıyla günlüğe yazılır.
8. **İstatistik.** Karşılanma oranı, medyan karşılanma süresi, bağış miktarı, bildirim yanıt oranları; grafikler ve CSV.
9. **Dağıtım.** README ile kurulum, ortam değişkenleri, sağlık kontrolü, yayın betikleri; gizli anahtar depoda yok (testle denetleniyor).

## 3. Modüller ve haftalar

| Hafta | Modül / iş | Sürüm |
|---|---|---|
| 3–4 | Proje iskeleti, veritabanı şeması, kısıtlar, CRUD | — |
| 5 | Kimlik doğrulama, parola güvenliği, roller | — |
| 6 | Bağışçı ve kan grubu modülü (uyumluluk, uygunluk) | — |
| 7 | Talep ve eşleştirme modülü | — |
| 8 | Uçtan uca entegrasyon (vize) | v0.1.0 |
| 9 | Harici API'ler: yol mesafesi, anlık bildirim, SMS | v0.2.0 |
| 10 | Yapay zekâ bileşeni: talep ve stok tahmini | v0.3.0 |
| 11 | Stok takip ve uyarı modülü | v0.4.0 |
| 12 | İstatistik panosu ve bildirim özeti | v0.5.0 |
| 13 | Test ve hata ayıklama (6 hata) | v0.6.x |
| 14 | Dokümantasyon, erişilebilirlik, dağıtım hazırlığı | v0.7.x |
| 15 | Final entegrasyon, uçtan uca doğrulama | **v1.0.0** |

## 4. Mimari kararlar: artılar ve eksiler

| Karar | Artısı | Eksisi / bedeli | Bugün yine seçer miydim? |
|---|---|---|---|
| **Katmanlı mimari** (Web/Business/DataAccess) | Sorumluluk ayrımı; iş kuralları HTTP'siz test ediliyor (testlerin %87'si); veritabanı değişimi tek katmanı etkiler | Basit bir işlem için bile 3 dosya; bazı servisler yalnızca depoyu "geçiriyor" | Evet; bu boyutta faydası maliyetinden büyük |
| **Sunucu tarafı MVC (Razor)** | Tek proje, JavaScript az, erişilebilirlik ve SEO kolay, CSRF hazır | Etkileşimli ekranlar (canlı bildirim) için sayfa yenileme gerekir | Evet; mobil uygulama gerekirse ayrıca API eklenir |
| **SQLite** | Kurulum yok, tek dosya, test başına saniyenin altında sıfırdan veritabanı | Tek yazıcı; çok sunuculu dağıtım yok; bazı ALTER işlemleri tablo yeniden kurar | Ders projesi için evet; gerçek kullanımda PostgreSQL |
| **Kurallar hem serviste hem veritabanında** | Kullanıcıya anlaşılır mesaj + eşzamanlı istekte bile tutarlılık (H13-04: 6 istekte 5 talep → 1) | Kural iki yerde; değişince ikisi birden güncellenmeli | Evet; ikisini de test ediyorum |
| **Harici servisler isteğe bağlı** (kapalıyken çalışma) | Servis çökse de sistem çalışır; anahtarsız kurulum | Kuş uçuşu mesafe daha az doğru; davranış ortama göre değişir | Evet |
| **Yapay zekâ çıktısını doğrulama + istatistiksel taban** | Model hatası hastaya zarar veremez; anahtarsız da tahmin | Ek kod; modelin katkısı sınırlı (yorum/gerekçe) | Evet |
| **Bildirimler arka plan kuyruğunda** (bellek içi) | Talep açmak harici servisleri beklemez | Uygulama yeniden başlarsa kuyruk kaybolur (veritabanındaki "Kuyrukta" kayıtlar yeniden yüklenir) | Küçük ölçekte evet; büyükte mesaj kuyruğu |
| **Kimlik oturumdan, formdan değil** | Sahiplik saldırılarına karşı | — | Evet |

## 5. Yapay zekâ: nerede, neden

**Üründe (Hafta 10):** Stok tahmini sayfasında. İstatistiksel tahmini **yorumlamak ve gerekçelendirmek** için Claude API kullanılıyor;
modele yalnızca kan grubu başına toplu sayılar gidiyor, kişisel veri gitmiyor. Çıktı JSON şemasıyla zorlanıyor ve ayrıca doğrulanıyor:
bozuk/eksik çıktı reddediliyor, uçuk değer düzeltiliyor, model kritik uyarıyı **düşüremiyor**. Anahtar yoksa ya da hata olursa
istatistiksel tahmin gösteriliyor. *Neden:* Tahmin karar desteğidir, karar kurumundur; model hatası hasta güvenliğini etkilememeli.

**Geliştirmede:** Haftalık AI kullanım günlüklerinde (her haftanın `ai-log`'u) kayıtlı. Yapay zekâ (Claude) kod, test ve belge taslakları
için kullanıldı. Benim payım: haftalık gereksinimi belirlemek, üretilen kodu çalıştırıp denemek, testleri çalıştırmak, tarayıcıda kontrol etmek,
hataları raporlamak ve kararları vermek. Doğrulamanın somut sonuçları: Hafta 13'te bulunan 6 hata (Türkiye saati, 29 Şubat, Türkçe "İ",
yarış durumu…), Hafta 14'te temiz kurulumda bulunan 4 sorun, Hafta 15'te final provada bulunan "KRITIK" yazımı (H15-01).
Yapay zekânın önerdiği her şey doğru değildi; örneğin Hafta 15'te şüphelenilen ikinci büyük/küçük harf hatası test edildi ve hata **olmadığı** görüldü.

## 6. Ölçeklendirme

Bugünkü yapı bir il / birkaç hastane için yeterli. Türkiye ölçeğine (yüzlerce hastane, yüz binlerce bağışçı) çıkarken sırasıyla:

| Darboğaz | Ne yapılır |
|---|---|
| SQLite tek yazıcı | PostgreSQL ya da SQL Server — yalnızca DataAccess değişir; migration'lar yeniden üretilir |
| Tek sunucu | Uygulamayı birden çok sunucuya çoğaltmak: oturum anahtarları ortak depoda (zaten dosya klasörüne alındı → ortak klasör/Redis), yük dengeleyici `/saglik`'a bakar |
| Bellek içi bildirim kuyruğu | RabbitMQ / Azure Service Bus gibi kalıcı mesaj kuyruğu; gönderim ayrı bir işçi (worker) hizmetinde |
| Eşleştirme sorgusu (her talepte il listesi + uygunluk) | Bağışçının son bağış tarihini ve konumunu indeksli tutmak; uygunluğu bağış anında hesaplayıp saklamak |
| OSRM ücretsiz sunucusu (saniyede 1 istek) | Kendi OSRM sunucusu ya da ücretli harita API'si; il çiftleri zaten önbellekte |
| Önbellek | Bellek içi önbellek → dağıtık önbellek (Redis) |
| İstatistik sorguları | Gece hesaplanan özet tablolar (materialized view) |
| Güvenlik | Kurum hesabı onayı (yönetici rolü), IP bazlı istek sınırlama, merkezi loglama ve izleme |

## 7. Yeniden yapsam neyi farklı yapardım?

1. **Testleri ve veritabanı kısıtlarını ilk günden.** Eşzamanlılık hatası (H13-04) Hafta 7'den beri vardı; filtreli benzersiz indeks baştan konsaydı hiç oluşmazdı.
2. **Saat dilimi ve Türkçe karakterleri baştan tasarlamak.** UTC/Türkiye günü (H13-01), "İ" (H13-03, H13-06) ve "KRİTİK" (H15-01) hatalarının hepsi aynı kökten: kültür ve zaman varsayımları.
3. **Her hafta temiz ortamda kurulum denemek.** Hafta 14'te bulunan 4 sorun aylarca geliştirme ortamında görünmemişti; bir CI hattı (GitHub Actions) her commit'te testleri ve temiz kurulumu çalıştırırdı.
4. **PostgreSQL ile başlamak.** SQLite'ın tablo yeniden kurma uyarıları ve tek yazıcı sınırı sonradan iş çıkardı.
5. **Kurum hesabı onayı ve yönetici rolü.** Şu an herkes kurum olarak kayıt olabiliyor; gerçek sistemde en önce bu gerekir.

## 8. Savunma demosu (8 dakika)

Hazırlık: `dotnet test` (337 geçmeli) → `dotnet run --project src/KanBagis.Web` → iki tarayıcı penceresi (biri gizli).
Senaryonun tamamı otomatik olarak da doğrulandı (16/16 adım): [Demo-Senaryosu.md](Demo-Senaryosu.md).

| Süre | Adım | Söylenecek tek cümle |
|---|---|---|
| 0:00 | Kan grupları tablosu | "Uyumluluk metinle değil, ABO ve Rh kuralıyla hesaplanıyor." |
| 0:30 | Bağışçı ve kurum kaydı | "Kimlik ve rol oturumdan geliyor; kurum bağışçı sayfasını açamıyor." |
| 1:30 | Acil talep → "5 bağışçıya bildirim" | "Uyumlu, uygun, yakın ve yol mesafesine göre sıralı." |
| 2:30 | Bağışçı kabul eder | "Kabul anında uygunluk yeniden kontrol ediliyor; aynı anda tek kabul." |
| 3:15 | Bağış alındı → stok +1, talep kapandı | "Tek transaction: yarım kalırsa hepsi geri alınır." |
| 4:00 | Stok: kritik çıkış onayı, uyarı aç/kapa | "Stok eksiye düşemez; veritabanı da engelliyor." |
| 5:00 | Tahmin ve AI günlüğü | "Yapay zekâ çıktısı doğrulanıyor; kritik uyarıyı düşüremez." |
| 6:00 | İstatistik, CSV | "Oranlar pay/payda ile; az veride uyarı veriyor." |
| 6:45 | 404, /saglik, testler, GitHub etiketleri | "337 test, 12 sürüm; kurulum README ile temiz ortamda doğrulandı." |
| 7:30 | Soru-cevap | |

**Yedek plan:** İnternet yoksa yol mesafesi kuş uçuşuna, anlık bildirim yalnızca site içine düşer; demo değişmeden çalışır.
Bilgisayar sorunu olursa: haftalık raporlardaki ekran görüntüleri ve sunumdaki akış.

## 9. Olası jüri soruları

| Soru | Kısa cevap |
|---|---|
| Uygun bağışçıyı nasıl buluyorsun? | Uyumlu gruplar → uygunluk (yaş, kilo, 90/120 gün) → aciliyete göre yarıçap → yol mesafesi sıralaması → kota |
| İki personel aynı anda talep açarsa? | Filtreli benzersiz indeks yalnızca birini kabul eder; diğerine "zaten açık talep" denir (test: 6 eşzamanlı istek → 1 talep) |
| Stok eksiye düşebilir mi? | Hayır: `UPDATE … WHERE UniteSayisi + fark >= 0` atomik; ayrıca CHECK kısıtı |
| Parolalar nasıl saklanıyor? | PBKDF2-SHA256, rastgele tuz, yüksek iterasyon; düz metin ya da tek SHA asla |
| KVKK'ya nasıl uydun? | Kişisel veri yalnızca kabul edilen talebin hastanesine; telefon mesajı, panolar ve yapay zekâ istemi toplu/anonim; örnek veri sentetik |
| Harici servis çökerse? | Zaman aşımı, yeniden deneme, kuş uçuşuna dönüş; bildirim "kuyrukta" kalır, sistem çalışır |
| Yapay zekâ yanlış derse? | Doğrulayıcı reddeder ya da düzeltir; kritik uyarıyı düşüremez; her çalıştırma günlükte |
| Neden SQLite? | Kurulumsuz, testte hızlı; sınırlarını biliyorum (tek yazıcı) ve geçiş yolu belli |
| Test kapsamın neden %100 değil? | Kapsam çalışmayı ölçer, doğruluğu değil; bulduğum hataların hepsi zaten kapsanan satırlardaydı |
| En zor hata hangisiydi? | Eşzamanlılık (H13-04) ve onu düzeltirken çıkan "Collection was modified" (H13-05): yığın izinden kök nedene |
| Sürüm yönetimini nasıl yaptın? | Semantic versioning, her hafta etiket, CHANGELOG; yayımlanmış etiket taşınmaz, düzeltme yeni yama sürümü olur |
| Projeyi başka biri nasıl çalıştırır? | .NET 10 + `git clone` + `dotnet run`; veritabanı kendiliğinden oluşur |
| Gizli anahtarlar nerede? | Ortam değişkeninde; depoda değil — bir test her çalıştırmada depoyu tarıyor |
| Sistemi nasıl ölçeklendirirsin? | PostgreSQL, birden çok sunucu + ortak anahtar deposu, kalıcı mesaj kuyruğu, dağıtık önbellek (bölüm 6) |
| Yeniden yapsan? | Testler ve kısıtlar ilk günden, kültür/saat dilimi tasarımı, CI, PostgreSQL, kurum onayı (bölüm 7) |

## 10. Bilinen sınırlamalar

Ayrıntı: [Hata kayıtları](test/Hata-Kayitlari.md) → "Açık / bilinen sınırlamalar".
Kurum hesabı onayı yok · e-posta doğrulama ve parola sıfırlama yok · istatistik/tahmin gün sınırı UTC (A-01) ·
Claude API canlı ortamda otomatik test edilmiyor (A-02) · SQLite tek yazıcı (A-03) · gerçek ekran okuyucuyla deneme yapılmadı.
