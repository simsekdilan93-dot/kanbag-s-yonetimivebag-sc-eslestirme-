# Kan Bağışı — yayın paketi oluşturur (Windows PowerShell).
# Kullanım:  powershell -ExecutionPolicy Bypass -File scripts\yayinla.ps1 [-Hedef yayin]
param([string]$Hedef = "yayin")
$ErrorActionPreference = "Stop"
$kok = Split-Path -Parent $PSScriptRoot

Write-Host "1/3 Testler çalıştırılıyor..."
dotnet test "$kok" --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "Testler başarısız; yayın paketi oluşturulmadı." }

Write-Host "2/3 Yayın paketi oluşturuluyor: $Hedef"
dotnet publish "$kok\src\KanBagis.Web" -c Release -o "$Hedef" --nologo
if ($LASTEXITCODE -ne 0) { throw "dotnet publish başarısız." }

# Geliştirme veritabanı pakete girmez; uygulama ilk açılışta migration'larla kendi veritabanını oluşturur.
Remove-Item "$Hedef\*.db*" -ErrorAction SilentlyContinue

Write-Host "3/3 Hazır. Çalıştırmak için (ortam değişkenleri: .env.example, docs\Dagitim.md):"
Write-Host "   cd $Hedef"
Write-Host "   `$env:ASPNETCORE_ENVIRONMENT='Production'; dotnet KanBagis.Web.dll --urls http://localhost:5284"
