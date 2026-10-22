# Güvenlik Notu: Kimlik Doğrulama ve Yetkilendirme

**Kimlik doğrulama (authentication):** "Sen kimsin?" sorusunu yanıtlar. Örnek: e-posta ve parolayla giriş.
**Yetkilendirme (authorization):** "Bunu yapmaya iznin var mı?" sorusunu yanıtlar. Örnek: Kurum panelini yalnızca Kurum rolü görebilir.

## 1. Parola saklama

| Uygulanan | Nerede |
|---|---|
| Parola **asla düz metin saklanmaz**. Yalnızca hash'i tutulur. | `Kullanicilar.ParolaHash` |
| Algoritma: **PBKDF2-HMAC-SHA256**, **600.000 iterasyon** (OWASP önerisi) | [ParolaHasher.cs](../src/KanBagis.Business/Guvenlik/ParolaHasher.cs) |
| Her parola için **16 baytlık rastgele tuz (salt)** üretilir | `RandomNumberGenerator.GetBytes` |
| Karşılaştırma **sabit sürelidir** (timing attack koruması) | `CryptographicOperations.FixedTimeEquals` |
| Parola kuralı: en az 8 karakter, en az bir harf ve bir rakam. Kural **sunucuda** da denetlenir. | `KimlikService.ParolaKuralHatasi` |

Saklanan biçim: `PBKDF2-SHA256$600000$<tuz>$<hash>`. İterasyon sayısı kayıtla birlikte saklandığı için ileride artırılırsa eski parolalar da doğrulanmaya devam eder.

**Neden hash?** Veritabanı çalınsa bile parolalar okunamaz. Hash tek yönlüdür, geri çözülemez.
**Neden tuz?** Tuz olmasaydı aynı parolayı kullanan herkesin hash'i aynı olurdu. Bu durumda önceden hesaplanmış tablolarla (rainbow table) binlerce parola aynı anda kırılabilirdi.
**Neden yavaş algoritma?** SHA-256 tek başına çok hızlıdır, saldırgan saniyede milyarlarca tahmin dener. PBKDF2 her denemeyi yaklaşık 600.000 kat yavaşlatır.

## 2. Giriş akışı

| Durum | Davranış |
|---|---|
| Doğru e-posta ve parola | Yeni oturum çerezi oluşturulur, kullanıcı rolüne göre kendi paneline yönlendirilir. |
| Yanlış parola **veya** olmayan e-posta | İkisinde de **aynı mesaj** gösterilir: "E-posta veya parola hatalı." Saldırgan hangi e-postaların kayıtlı olduğunu öğrenemez. Olmayan e-postada da bir hash hesaplanır, böylece yanıt süresi de bir ipucu vermez. |
| Arka arkaya **5 hatalı deneme** | Hesap **15 dakika kilitlenir**. Kilitliyken doğru parola da kabul edilmez. Bu, kaba kuvvet (brute force) saldırısını yavaşlatır. |
| Başarılı giriş | Hatalı deneme sayacı sıfırlanır, `SonGirisTarihi` kaydedilir. |

## 3. Oturum yönetimi: neden çerez, neden JWT değil?

| | **Çerez (cookie) oturumu**: kullanılan | **JWT (token)** |
|---|---|---|
| Nerede saklanır | Tarayıcı çerezi otomatik gönderir | İstemci saklar ve her isteğe `Authorization` başlığı olarak ekler |
| Uygun olduğu yer | Sunucu taraflı sayfalar (MVC), aynı alan adı | Mobil uygulama, SPA, servisten servise API |
| Çıkış | Çerez silinir | Token süresi dolana kadar geçerli kalır (ek mekanizma gerekir) |
| Bu projede | ✅ MVC sayfaları sunucuda üretildiği için uygun olan bu | İleride bir mobil uygulama ya da API eklenirse kullanılabilir |

Çerez ayarları ([Program.cs](../src/KanBagis.Web/Program.cs)):
- **HttpOnly:** JavaScript çereze erişemez, XSS saldırısıyla çalınamaz.
- **SameSite=Lax:** Başka sitelerden gelen POST isteklerinde çerez gönderilmez.
- **Secure:** Canlı ortamda çerez yalnızca HTTPS üzerinden gönderilir.
- **Süre:** 30 dakika işlem yapılmazsa oturum düşer. İşlem yapıldıkça süre uzar (sliding expiration).
- Çerez içeriği sunucunun anahtarıyla **şifrelenir ve imzalanır**. Kullanıcı rolünü değiştirmeye çalışırsa çerez geçersiz olur.
- Her girişte **yeni** çerez üretilir (session fixation koruması). Çıkış `POST` ile yapılır ve çerezi siler.

## 4. Rol tabanlı erişim

| Rol | Erişebildiği | Uygulandığı yer |
|---|---|---|
| `Bagisci` | `/BagisciPanel` | `[Authorize(Roles = Roller.Bagisci)]`: [BagisciPanelController.cs](../src/KanBagis.Web/Controllers/BagisciPanelController.cs) |
| `Kurum` | `/KurumPanel` | `[Authorize(Roles = Roller.Kurum)]`: [KurumPanelController.cs](../src/KanBagis.Web/Controllers/KurumPanelController.cs) |

- Kontrol **sunucuda** yapılır. Menüde bağlantının gizlenmesi tek başına koruma değildir: adres elle yazılsa bile filtre isteği durdurur.
- **Giriş yapmamış** kullanıcı korumalı sayfaya giderse giriş sayfasına yönlendirilir (`302 → /Hesap/Giris?ReturnUrl=...`). Giriş yaptıktan sonra gitmek istediği sayfaya geri döner.
- **Rolü yetmeyen** kullanıcı (ör. bağışçının kurum paneline girmeye çalışması) "Erişim engellendi" sayfasına yönlendirilir (403).
- Bağışçı ve hastane kimliği adresten (URL) değil **imzalı çerezden** okunur. Kullanıcı adresteki numarayı değiştirerek başkasının verisine ulaşamaz (IDOR koruması).
- Veritabanında da bir CHECK kısıtı var: bağışçı hesabı hastaneye, kurum hesabı bağışçıya bağlanamaz.

## 5. Diğer korumalar (OWASP Top 10)

| Risk (OWASP) | Önlem |
|---|---|
| A01 Erişim kontrolü hataları | Sunucu taraflı `[Authorize]`, kimliğin çerezden okunması |
| A02 Kriptografik hatalar | PBKDF2 ve tuz, HTTPS'te `Secure` çerez, HSTS |
| A03 Enjeksiyon (SQL, XSS) | EF Core parametreli sorgular üretir. Razor çıktıyı HTML olarak kodlar (`<script>` çalışmaz). |
| A07 Kimlik doğrulama hataları | Hesap kilitleme, genel hata mesajı, parola kuralı, oturum süresi |
| CSRF | Tüm POST formlarında antiforgery belirteci zorunlu (`AutoValidateAntiforgeryToken`) |
| Açık yönlendirme | `ReturnUrl` yalnızca sitenin kendi adresiyse kullanılır (`Url.IsLocalUrl`) |

## 6. Testlerle kanıt

`dotnet test` komutu 49 testi çalıştırır. Bunların güvenlikle ilgili olanları:
- [ParolaHasherTestleri.cs](../tests/KanBagis.Tests/ParolaHasherTestleri.cs): hash'in parolayı içermediği, tuz sayesinde aynı parolanın farklı hash ürettiği, yanlış parolanın reddedildiği
- [KimlikServiceTestleri.cs](../tests/KanBagis.Tests/KimlikServiceTestleri.cs): kayıt, zayıf parolanın reddi, 5 hatalı denemede kilit ve kilidin 15 dakika sonra açılması
- [YetkilendirmeTestleri.cs](../tests/KanBagis.Tests/YetkilendirmeTestleri.cs): siteyi gerçekten başlatıp yetkisiz erişim, rol kontrolü, çerez ayarları, CSRF, açık yönlendirme ve çıkış davranışını HTTP istekleriyle doğrular

## 7. Bilinen sınırlamalar (sonraki adımlar)

- **Kurum hesabı onayı yok:** Şu an herkes kurum olarak kayıt olabilir. Gerçek bir sistemde kurum hesapları bir yönetici tarafından onaylanmalıdır.
- E-posta doğrulama ve "parolamı unuttum" akışı yok.
- Parola değiştirildiğinde açık olan diğer oturumlar kapanmıyor. İleride güvenlik damgası (security stamp) eklenebilir.
- Kilitleme hesap bazlı çalışıyor. IP bazlı istek sınırlama (rate limiting) eklenebilir.
