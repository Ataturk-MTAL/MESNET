#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
APPHOST_DIR="$ROOT_DIR/src/MESNET.AppHost"

# Podman container runtime
export ASPIRE_CONTAINER_RUNTIME=podman

CONFIG="${1:-Debug}"

echo "=== MESNET Aspire AppHost ==="
echo "  Konfigürasyon : $CONFIG"
echo "  Container     : Podman"
echo ""

# Podman kontrolü
if ! command -v podman &>/dev/null; then
    echo "HATA: Podman bulunamadı. Lütfen Podman'ı kurun."
    exit 1
fi

if ! podman info &>/dev/null; then
    echo "HATA: Podman machine çalışmıyor. 'podman machine start' komutunu çalıştırın."
    exit 1
fi

echo "[1/2] Build ediliyor..."
dotnet build "$ROOT_DIR/MESNET.slnx" -c "$CONFIG" --no-incremental -q

echo "[2/2] Aspire AppHost başlatılıyor..."
dotnet run --project "$APPHOST_DIR" -c "$CONFIG"
