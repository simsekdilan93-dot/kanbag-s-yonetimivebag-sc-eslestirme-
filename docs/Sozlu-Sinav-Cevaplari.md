# Sözlü Sınav Soruları ve Cevapları (Hafta 3–8)

*P08 – Kan Bağışı Yönetimi ve Bağışçı Eşleştirme Sistemi · Dilan ŞİMŞEK · 251161048 · v0.1.0*

Her haftanın taslağındaki sözlü sınav soruları ve projeden örneklerle cevapları. Cevaplar birinci tekil şahısla, sözlüde söylenecek biçimde yazıldı. Dosya ve sınıf adları projedeki gerçek adlardır; sözlüden önce ilgili dosyayı açıp kodun üzerinden geçmek iyi olur.

---

## Hafta 3 — Kurulum, mimari ve veri modeli

### 1) Katmanlı mimariyi neden tercih ettin?

Projeyi dört parçaya ayırdım: Web (sunum), Business (iş kuralları), DataAccess (veritabanı) ve Entities (varlık sınıfları). Bağımlılık tek yönlü: Web yalnızca Business'ı, Business yalnızca DataAccess'i çağırıyor. Böylece her katmanın tek bir işi var; iş kurallarını arayüzden bağımsız test edebiliyorum ve veritabanını değiştirirsem yalnızca DataAccess etkileniyor. "Her şeyi tek katmanda toplamak" sık yapılan bir hata; katmanlı yapı bunu önlüyor.

### 2) İlişki türlerini (1-1, 1-N, N-N) örnekle açıkla.

1-1: KanGrubu – KanStok. Her kan grubunun tek bir stok kaydı var; KanStok.KanGrubuId benzersiz. 1-N: KanGrubu – Bagisci. Bir kan grubunda çok bağışçı olabilir, ama her bağışçının tek bir kan grubu vardır. N-N: Bagisci – KanTalebi. Bir talep birçok bağışçıya, bir bağışçıya birçok talep bildirilebilir. N-N ilişkiyi doğrudan kuramazsınız; Bildirim adında bir ara tabloyla çözdüm.

### 3) Bu projede hangi varlıklar birincil anahtar taşıyor ve neden?

Bütün tabloların birincil anahtarı Id adında bir tam sayı. Veritabanı her kayda kendisi bir numara veriyor. TC kimlik numarası gibi doğal anahtarları birincil anahtar yapmadım: TC gizli kalması gereken kişisel bir veri ve değişebilir. Bunun yerine TC'yi benzersiz (UNIQUE) kısıtla koruyorum. Bildirim ara tablosunun da kendi Id'si var; (BagisciId, KanTalebiId) çifti ayrıca benzersiz.

### 4) Klasör yapını hangi mantıkla kurdun?

src klasöründe katman başına bir proje var: KanBagis.Web, KanBagis.Business, KanBagis.DataAccess, KanBagis.Entities. tests klasöründe testler, docs klasöründe ER diyagramı, mimari şema ve modül açıklamaları duruyor. Projeler ayrı olduğu için katman kuralı derleyici tarafından da korunuyor: Web projesi DataAccess'e referans vermediği için veritabanına doğrudan erişen bir kod yazılsa derlenmez.

### 5) .gitignore neden gereklidir, neler eklenmemeli?

.gitignore, git'in depoya eklemeyeceği dosyaları listeler. Derleme çıktıları (bin/, obj/) her bilgisayarda yeniden üretilir; depoyu gereksiz büyütür. Yerel veritabanı dosyası (*.db) kişiye özeldir ve kişisel veri içerebilir. Parola, API anahtarı gibi gizli ayarlar ise depo herkese açık olduğu için asla yüklenmemeli. Yüklemeden önce git status ile kontrol ettim.

---

## Hafta 4 — Veritabanı ve veri erişim katmanı

### 1) Bir tabloya neden indeks ekledin/eklemedin?

İndeks, kitabın sonundaki dizin gibi: aramayı hızlandırır. Bagiscilar tablosunda (KanGrubuId, Sehir) bileşik indeksi var, çünkü eşleştirme sorgusu tam olarak "bu kan grubundan, bu ildeki bağışçılar" diye arıyor. TcKimlikNo'da benzersiz indeks var. Telefon ve e-postaya indeks eklemedim: bu alanlarda arama yapılmıyor ve her indeks her INSERT ile UPDATE'te ayrıca güncellendiği için yazma işlemlerini boşuna yavaşlatırdı.

### 2) Yabancı anahtar kısıtı ne işe yarar?

Referans bütünlüğünü sağlar: olmayan bir kayda bağlantı yapılamaz. Örneğin 99 numaralı, hiç olmayan bir kan grubuna bağışçı eklenemez; bunu Olmayan_kan_grubuna_bagisci_eklenemez testi kanıtlıyor. Silme davranışını da belirler: bağışçısı olan kan grubu silinemez (RESTRICT); talep silinirse ona ait bildirimler de silinir (CASCADE).

### 3) N-N ilişkiyi nasıl modelledin?

Bağışçı ile kan talebi arasındaki ilişkiyi Bildirim ara tablosuyla çözdüm. Bildirim tablosu iki tarafa da yabancı anahtar taşıyor: BagisciId ve KanTalebiId. (BagisciId, KanTalebiId) çifti benzersiz, yani aynı talep aynı kişiye iki kez bildirilemez. Ara tablo ilişkinin kendi bilgisini de tutuyor: mesaj, gönderim tarihi, okundu bilgisi ve yanıt (Kabul/Red).

### 4) Migration neden kullanılır?

Veritabanı şemasını kodla birlikte sürüm kontrolünde tutmak için. Her şema değişikliği bir migration dosyası oluyor (örneğin IlkSema, KullaniciVeRoller, BagisTalepBaglantisi). Projeyi indiren herkes aynı komutla aynı veritabanını kuruyor; site açılırken bekleyen migration'lar kendiliğinden uygulanıyor. Değişiklik gerekirse geri de alınabiliyor.

### 5) Bir CRUD işleminin SQL'ini açıkla.

CRUD dört temel işlemdir: Create = INSERT, Read = SELECT, Update = UPDATE, Delete = DELETE. Örneğin bağışçının telefonunu güncellemek: UPDATE Bagiscilar SET Telefon = '05559999999' WHERE Id = 1. WHERE koşulu çok önemli; yazılmazsa bütün bağışçıların telefonu değişir. Bu SQL'i ben yazmıyorum, Entity Framework Core EfRepository sınıfındaki GuncelleAsync metodundan üretiyor; parametreli ürettiği için SQL enjeksiyonuna karşı da güvenli.

---

## Hafta 5 — Kullanıcı yönetimi ve kimlik doğrulama

### 1) Parolayı neden hash'liyoruz, tuz (salt) nedir?

Hash tek yönlüdür: parolayı hash'e çevirebilirsiniz ama hash'ten parolayı geri çıkaramazsınız. Veritabanı çalınsa bile parolalar okunamaz. Tuz, her parola için üretilen rastgele 16 baytlık bir değer. Tuz sayesinde aynı parolayı kullanan iki kişinin hash'i farklı çıkar ve önceden hesaplanmış hash tabloları işe yaramaz. PBKDF2 algoritmasını 600.000 tekrarla kullandım; bilerek yavaş olduğu için kaba kuvvet saldırısını zorlaştırıyor (ParolaHasher.cs).

### 2) Oturum ve token farkı nedir?

Projede çerez (cookie) tabanlı oturum kullandım: tarayıcı çerezi her istekte kendiliğinden gönderiyor; çerez şifreli ve imzalı. Sayfaları sunucuda üretilen bir MVC sitesi için en uygun yöntem bu. JWT (token) ise istemcinin saklayıp her isteğe kendisinin eklediği bir belirteç; mobil uygulama ve API'ler için uygun. JWT'nin bir dezavantajı, çıkış yapılsa bile süresi dolana kadar geçerli kalması.

### 3) Rol tabanlı erişimi nasıl uyguladın?

İki rol var: Bagisci ve Kurum. Controller'ların başına [Authorize(Roles = Roller.Bagisci)] gibi bir satır yazdım. Kontrol sunucuda yapılıyor; menüde bağlantıyı gizlemek tek başına koruma sayılmaz, adres elle yazılsa bile filtre çalışıyor. Ayrıca kişinin kimliği (BagisciId, HastaneId) formdan ya da adresten değil, imzalı oturum çerezinden okunuyor.

### 4) Başarısız giriş denemesinde ne oluyor?

Kullanıcıya "E-posta veya parola hatalı." mesajı gösteriliyor; hangisinin yanlış olduğu söylenmiyor, böylece saldırgan hangi e-postaların kayıtlı olduğunu öğrenemiyor. Arka arkaya 5 hatalı denemede hesap 15 dakika kilitleniyor; kilitliyken doğru parola da kabul edilmiyor. Başarılı girişte hatalı deneme sayacı sıfırlanıyor.

### 5) Yetkisiz kullanıcı korumalı sayfaya giderse ne olur?

Giriş yapmamışsa giriş sayfasına yönlendiriliyor; giriş yaptıktan sonra gitmek istediği sayfaya geri dönüyor. Giriş yapmış ama rolü uymuyorsa, örneğin kurum hesabıyla bağışçı paneline gitmeye çalışırsa, "Erişim engellendi" (403) sayfası açılıyor. Bunları siteyi gerçekten başlatıp HTTP istekleri gönderen testler kanıtlıyor.

---

## Hafta 6 — Modül A: Bağışçı kaydı ve kan grubu yönetimi

### 1) Bağış uygunluğunu nasıl kontrol ettin?

BagisUygunlukKurali sınıfında üç kural var: yaş 18–65 arası, kilo en az 50, iki bağış arası erkekte 90, kadında 120 gün. Son bağış tarihini Bagislar tablosundan en büyük bağış tarihi olarak hesaplıyorum. Bağışçı uygun değilse sistem tüm nedenleri ve uygun olacağı tarihi gösteriyor; örneğin "Son bağışınızın üzerinden en az 120 gün geçmelidir. Uygun olacağınız tarih: 12.03.2027".

### 2) Kan grubunu nasıl modelledin?

Tek bir metin yerine iki ayrı alan olarak: ABO grubu (A, B, AB, 0) ve Rh faktörü (pozitif/negatif). Uyumluluğu bu iki alandan hesaplıyorum: 0 grubu her ABO grubuna verebilir, AB her gruptan alabilir, Rh pozitif kan Rh negatif alıcıya verilemez. Kan grubunu "0 Rh-" gibi tek bir metin olarak tutsaydım bu kuralları yazamazdım.

### 3) Bu modülün verisini nasıl modelledin?

KanGrubu ile Bagisci 1-N ilişkili. Bağışçıda doğum tarihi (yaş kuralı), cinsiyet (bağış aralığı), kilo ve il var. Kiloya veritabanında CHECK kısıtı koydum (30–250). İl, 81 il listesinden seçiliyor. Son bağış tarihini ayrı bir sütunda tutmuyorum, çünkü iki kopya zamanla birbirini tutmayabilir; Bagislar tablosundan hesaplanıyor.

### 4) Bu modülde hangi iş kuralını neden koydun?

İlk bağıştan sonra kan grubu değiştirilemiyor. Bağışta kan grubu laboratuvarda test edildiği için kayıt kesinleşmiş oluyor; sonradan değiştirilirse yanlış gruba kan çağrısı gidebilir. Ayrıca 18 yaşından küçükler kayıt olamıyor ve TC numarası algoritmayla kontrol ediliyor.

### 5) Bu modülü diğer modüllerle nasıl entegre ettin?

Kayıt formu (Hafta 5) bu modülün doğrulama kurallarını kullanıyor. Kurum paneli, hastanenin ilinde her kan grubuna kan verebilecek uygun bağışçı sayısını gösteriyor; KVKK gereği isim değil yalnızca sayı. Hafta 7'deki eşleştirme de aynı uyumluluk ve uygunluk kurallarını kullanıyor; kurallar iki ayrı yerde yazılmadı.

---

## Hafta 7 — Modül B: Talep ve grup/konum eşleştirme

### 1) Grup uyumluluğunu nasıl kurguladın?

Talep edilen gruba kan verebilecek bütün grupları buluyorum. A Rh+ talebi için A Rh+, A Rh-, 0 Rh+ ve 0 Rh- aranıyor. Sıralamada aynı gruptan olanlar öne alınıyor, böylece nadir bulunan 0 Rh- bağışçılar yalnızca onların verebileceği talepler için saklanıyor.

### 2) Aciliyeti nasıl önceliklendirdin?

Acil talepte arama mesafesi 100 km yerine 300 km, ünite başına bildirim sayısı 3 yerine 5. Bildirim "ACİL:" ile başlıyor ve hem kurumun hem bağışçının listesinde en üstte, kırmızı olarak gösteriliyor (AciliyetKurallari.cs).

### 3) Bu modülün verisini nasıl modelledin?

Bildirim tablosu bağışçı ile talep arasındaki N-N ilişkinin ara tablosu; aynı talep aynı kişiye iki kez gönderilemiyor. Kapanış tarihi ve yanıt tarihi tutarlılığı CHECK kısıtlarıyla korunuyor: kapanmış talebin kapanış tarihi, yanıtlanmış bildirimin yanıt tarihi olmak zorunda. Konum il düzeyinde tutuluyor; KVKK gereği adres saklanmıyor. 81 ilin koordinatlarıyla iller arası kuş uçuşu mesafeyi Haversine formülüyle hesaplıyorum.

### 4) Bu modülde hangi iş kuralını neden koydun?

Bir bağışçı aynı anda yalnızca bir açık talebi kabul edebiliyor: bir kişi tek seferde bir kez bağış yapabilir, iki talebi birden kabul ederse hastanelerden biri gelmeyecek birini bekler. Bir hastane de aynı kan grubu için ikinci bir açık talep açamıyor; aynı bağışçılar aynı ihtiyaç için iki kez çağrılmasın diye.

### 5) Bu modülü diğer modüllerle nasıl entegre ettin?

Hafta 6'daki uyumluluk ve uygunluk kurallarını yeniden kullandım. Kimlikler Hafta 5'teki oturumdan geliyor. Aday sorgusu Hafta 4'te eklediğim (KanGrubuId, Sehir) indeksini kullanıyor. Kurum paneli ve bağışçı paneli bu modülün verisini gösteriyor; kurum, bağışçının adını ve telefonunu yalnızca bağışçı talebi kabul ettikten sonra görüyor.

---

## Hafta 8 — VİZE: Ara sürüm ve entegrasyon

### 1) Şu ana kadarki mimarini özetle.

Katmanlı mimari: Web → Business → DataAccess → SQLite; Entities bütün katmanlar tarafından kullanılıyor. Hafta 4'te veritabanı, Hafta 5'te kimlik doğrulama, Hafta 6'da Modül A (bağışçı ve kan grubu), Hafta 7'de Modül B (talep ve eşleştirme) eklendi; Hafta 8'de hepsi uçtan uca birleşti ve v0.1.0 olarak etiketlendi. 154 testin çoğu tarayıcı açmadan çalışıyor.

### 2) Hangi modül hangi veriyi nereden alıyor?

Bağışçı modülü bağışçı ve kan grubu bilgisini BagisciRepository'den, son bağışı Bagislar tablosundan alıyor. Eşleştirme, uyumlu grupların bağışçılarını AdaylariGetirAsync sorgusundan, il koordinatlarını koddaki sabit tablodan alıyor. Talep modülü talepleri, bildirimleri ve bağışları ilgili repository'lerden alıyor. Kişinin kimliği ise her zaman imzalı oturum çerezinden okunuyor; formdan ya da adresten değil. Ayrıntılı tablo: docs/Vize-Hazirlik.md.

### 3) En çok zorlandığın entegrasyon neydi, nasıl çözdün?

Talep akışını bağış ve stokla birleştirmek. Hafta 7 sonunda bağışçı kabul ediyordu ama akış orada bitiyordu: bağış kaydedilmiyor, stok artmıyordu. Bağış, stok ve talep durumu üç ayrı tabloda; biri yazılıp diğeri yazılmazsa veri tutarsız kalır. "Bağış alındı" işlemini tek transaction içinde yaptım: bağışı kaydediyor, stoğu artırıyor ve istenen ünite sayısına ulaşılınca talebi kapatıyor; hata olursa hepsi geri alınıyor. Aynı bağışın iki kez kaydedilmemesini veritabanında da filtreli UNIQUE indeksle garanti ettim.

### 4) Yapay zekâdan aldığın bir kod parçasını satır satır açıkla.

KanGrubuUyumlulugu.VerebilirMi: verici ancak iki koşul birlikte sağlanırsa kan verebilir. Birinci koşul ABO uyumu: verici 0 ise herkese, aynı gruptaysa her zaman, alıcı AB ise herkesten alabilir. İkinci koşul Rh: verici Rh negatifse herkese verebilir, Rh pozitifse alıcı da pozitif olmalı. Örnek: 0 Rh- → A Rh+ verebilir; A Rh+ → 0 Rh- veremez. 8×8 tablonun tamamı testle doğrulanıyor. Satır satır açıklama: docs/Vize-Hazirlik.md.

### 5) Bir sonraki aşamanın planı nedir?

Hafta 9'da bildirimleri e-posta ya da SMS servisiyle gerçekten göndermek (harici API), sonra yapay zekâ bileşeni, raporlama, test, bulut dağıtımı ve finalde v1.0.0. Bilinen sınırlamalar: kurum hesapları için yönetici onayı yok, bildirimler yalnızca sistem içinde, konum il düzeyinde ve mesafeler kuş uçuşu.
