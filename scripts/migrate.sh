#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
API_DIR="$ROOT_DIR/src/MESNET.Presentation"

ACTION="${1:-apply}"
CONFIG="${2:-Debug}"

echo "=== MESNET Marten Migration ==="
echo "  Aksiyon        : $ACTION"
echo "  Konfigürasyon  : $CONFIG"
echo ""

case "$ACTION" in
    apply)
        echo "Schema değişiklikleri uygulanıyor..."
        dotnet run --project "$API_DIR" -c "$CONFIG" -- marten-apply
        ;;
    assert)
        echo "Schema tutarlılığı kontrol ediliyor..."
        dotnet run --project "$API_DIR" -c "$CONFIG" -- marten-assert
        ;;
    patch)
        echo "SQL patch dosyası oluşturuluyor..."
        PATCH_DIR="$ROOT_DIR/migrations"
        mkdir -p "$PATCH_DIR"
        TIMESTAMP=$(date +%Y%m%d_%H%M%S)
        PATCH_FILE="$PATCH_DIR/${TIMESTAMP}_patch.sql"
        dotnet run --project "$API_DIR" -c "$CONFIG" -- marten-patch "$PATCH_FILE"
        echo "Patch dosyası: $PATCH_FILE"
        ;;
    dump)
        echo "Mevcut schema dump ediliyor..."
        DUMP_DIR="$ROOT_DIR/migrations"
        mkdir -p "$DUMP_DIR"
        dotnet run --project "$API_DIR" -c "$CONFIG" -- marten-dump "$DUMP_DIR/schema_dump.sql"
        echo "Dump dosyası: $DUMP_DIR/schema_dump.sql"
        ;;
    *)
        echo "Kullanım: $0 {apply|assert|patch|dump} [Debug|Release]"
        echo ""
        echo "  apply  — Schema değişikliklerini veritabanına uygula (varsayılan)"
        echo "  assert — Mevcut schema'nın kod ile uyumlu olduğunu doğrula"
        echo "  patch  — Fark SQL dosyası oluştur (migrations/ dizinine)"
        echo "  dump   — Tüm schema'yı SQL olarak dışarı aktar"
        exit 1
        ;;
esac

echo ""
echo "Tamamlandı."
