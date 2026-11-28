# Kan Bağışı — siteyi Gmail ile e-posta göndererek çalıştırır ("Parolamı unuttum" kodu gerçek e-postayla gelir).
# Windows PowerShell 5.1 Türkçe karakterleri doğru okusun diye dosya UTF-8 BOM ile kaydedilir.
# Kullanım (proje klasöründe PowerShell):  powershell -ExecutionPolicy Bypass -File scripts\eposta-ile-calistir.ps1
#
# Gmail adresi ve UYGULAMA ŞİFRESİ sorulur. Şifre ekranda görünmez, diske ve depoya yazılmaz; yalnızca bu pencerede
# çalışan uygulamanın ortam değişkenine konur ve pencere kapanınca kaybolur.
# Uygulama şifresi: Google Hesabı → Güvenlik → 2 Adımlı Doğrulama (açık olmalı) → Uygulama şifreleri → 16 harflik şifre.
param([int]$Port = 5284)

$ErrorActionPreference = "Stop"
$kok = Split-Path -Parent $PSScriptRoot

Write-Host ""
Write-Host "Kan Bağışı — e-posta ile çalıştırma" -ForegroundColor Red
Write-Host "Normal Gmail şifreniz DEĞİL, Google'ın verdiği 16 harflik 'uygulama şifresi' gerekir."
Write-Host ""
$adres = (Read-Host "Gmail adresiniz").Trim()
$gizli = Read-Host "Uygulama şifresi (ekranda görünmez)" -AsSecureString
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($gizli)
try { $sifre = ([Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)) -replace '\s', '' }
finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) }

if ($adres -notmatch '^[^@\s]+@[^@\s]+\.[^@\s]+$') {
    Write-Host "E-posta adresi geçersiz görünüyor. Bu satıra YALNIZCA adresi yazın (ör. adiniz@gmail.com)." -ForegroundColor Yellow
    exit 1
}
# Google'ın uygulama şifresi her zaman 16 harftir (boşluklar hariç). Farklı uzunluk = büyük olasılıkla normal Gmail şifresi.
if ($sifre -notmatch '^[a-zA-Z]{16}$') {
    Write-Host "Girilen şifre $($sifre.Length) karakter. Uygulama şifresi 16 harftir (ör. abcd efgh ijkl mnop)." -ForegroundColor Yellow
    Write-Host "Normal Gmail şifreniz burada ÇALIŞMAZ. Uygulama şifresi almak için: https://myaccount.google.com/apppasswords"
    Write-Host "(Önce Güvenlik > 2 Adımlı Doğrulama açık olmalı.)"
    exit 1
}

# 1) Bilgiler doğru mu? Kendinize bir deneme e-postası gönderilir.
Write-Host "Deneme e-postası gönderiliyor: $adres ..."
try {
    $istemci = New-Object System.Net.Mail.SmtpClient("smtp.gmail.com", 587)
    $istemci.EnableSsl = $true
    $istemci.Credentials = New-Object System.Net.NetworkCredential($adres, $sifre)
    $mesaj = New-Object System.Net.Mail.MailMessage($adres, $adres)
    $mesaj.Subject = "Kan Bağışı — e-posta ayarı çalışıyor"
    $mesaj.Body = "Bu bir deneme e-postasıdır. Parola yenileme kodları bu adresten gönderilecek."
    $mesaj.BodyEncoding = [Text.Encoding]::UTF8
    $mesaj.SubjectEncoding = [Text.Encoding]::UTF8
    $istemci.Send($mesaj)
    Write-Host "Deneme e-postası gönderildi. Gelen kutunuzu kontrol edin." -ForegroundColor Green
}
catch {
    # En içteki hata asıl nedendir ("Failure sending mail" yalnızca bir sarmalayıcı).
    $e = $_.Exception; while ($e.InnerException) { $e = $e.InnerException }
    Write-Host "E-posta gönderilemedi: $($e.Message)" -ForegroundColor Yellow
    if ($e.Message -match 'closed|kapat|5\.7\.8|not accepted|Authentication') {
        # Gmail yanlış kullanıcı adı/şifrede "535 5.7.8 Username and Password not accepted" yanıtı verip bağlantıyı kapatır.
        Write-Host "Gmail kullanıcı adı veya şifreyi kabul etmedi. En sık neden: uygulama şifresi yerine normal Gmail şifresi girilmesi" -ForegroundColor Yellow
        Write-Host "ya da uygulama şifresinin yanlış kopyalanması. Yeni bir uygulama şifresi oluşturup tekrar deneyin:"
        Write-Host "https://myaccount.google.com/apppasswords"
    } else {
        Write-Host "Kontrol edin: internet bağlantısı, adres doğru mu, 2 Adımlı Doğrulama açık mı?"
    }
    exit 1
}

# 2) Port doluysa (ör. önceki demo sitesi) kapatma izni istenir.
$dinleyen = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
if ($dinleyen) {
    $cevap = Read-Host "Port $Port başka bir uygulamada açık (süreç $($dinleyen.OwningProcess)). Kapatılıp site e-postalı başlatılsın mı? (E/H)"
    if ($cevap -notmatch '^[eE]') { Write-Host "İptal edildi."; exit 0 }
    Stop-Process -Id $dinleyen.OwningProcess -Force
    Start-Sleep -Seconds 2
}

# 3) Uygulama, SMTP ayarları yalnızca bu pencerenin ortamında olacak şekilde başlatılır.
$env:DisServisler__Eposta__SmtpSunucu = "smtp.gmail.com"
$env:DisServisler__Eposta__Port = "587"
$env:DisServisler__Eposta__Ssl = "true"
$env:DisServisler__Eposta__Kullanici = $adres
$env:DisServisler__Eposta__Parola = $sifre
$env:DisServisler__Eposta__GonderenAdres = $adres

Write-Host ""
Write-Host "Site başlatılıyor: http://localhost:$Port  (durdurmak için Ctrl+C)" -ForegroundColor Green
Write-Host "Giriş → 'Parolamı unuttum' → hesabınızın e-postası → kod e-postanıza gelir."
dotnet run --project "$kok\src\KanBagis.Web" --urls "http://localhost:$Port"
