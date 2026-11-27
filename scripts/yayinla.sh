#!/usr/bin/env bash
# Kan Bağışı — yayın paketi oluşturur (Linux / macOS / Git Bash).
# Kullanım:  bash scripts/yayinla.sh [hedef_klasor]
set -euo pipefail
HEDEF="${1:-yayin}"
KOK="$(cd "$(dirname "$0")/.." && pwd)"

echo "1/3 Testler çalıştırılıyor..."
dotnet test "$KOK" --nologo -v q

echo "2/3 Yayın paketi oluşturuluyor: $HEDEF"
dotnet publish "$KOK/src/KanBagis.Web" -c Release -o "$HEDEF" --nologo

# Geliştirme veritabanı pakete girmez; uygulama ilk açılışta migration'larla kendi veritabanını oluşturur.
rm -f "$HEDEF"/*.db "$HEDEF"/*.db-shm "$HEDEF"/*.db-wal

echo "3/3 Hazır. Çalıştırmak için (ortam değişkenleri: .env.example, docs/Dagitim.md):"
echo "   cd $HEDEF && ASPNETCORE_ENVIRONMENT=Production dotnet KanBagis.Web.dll --urls http://localhost:5284"
