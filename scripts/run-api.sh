#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
API_DIR="$ROOT_DIR/src/MESNET.Presentation"

CONFIG="${1:-Debug}"

echo "=== MESNET API (Standalone) ==="
echo "  Konfigürasyon : $CONFIG"
echo ""
echo "  NOT: PostgreSQL ve RabbitMQ'nun zaten çalışıyor olması gerekir."
echo "  Connection string'ler appsettings.Development.json'dan okunur."
echo ""

# PostgreSQL bağlantı kontrolü
PG_HOST="${PGHOST:-localhost}"
PG_PORT="${PGPORT:-5432}"
if command -v pg_isready &>/dev/null; then
    if ! pg_isready -h "$PG_HOST" -p "$PG_PORT" -q 2>/dev/null; then
        echo "UYARI: PostgreSQL ($PG_HOST:$PG_PORT) bağlantısı kurulamadı."
        echo "       Devam ediliyor ama uygulama başlangıçta hata verebilir."
        echo ""
    fi
fi

echo "[1/2] Build ediliyor..."
dotnet build "$ROOT_DIR/MESNET.slnx" -c "$CONFIG" --no-incremental -q

echo "[2/2] API başlatılıyor..."
ASPNETCORE_ENVIRONMENT=Development dotnet run --project "$API_DIR" -c "$CONFIG" --no-build
