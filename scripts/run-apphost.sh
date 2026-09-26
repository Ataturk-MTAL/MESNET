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

# Podman VM saatini Mac'e eşitle. Mac uykudan çıkınca VM saati geride kalır (ölçüldü: 7887 sn).
# Keycloak kapta çalıştığı için token'ı geri kalmış saatle damgalar; tarayıcı ve API (Mac'te)
# her yeni token'ı doğduğu anda "süresi dolmuş" görür → yeniden giriş döngüsü.
# Uygulama çalışırken kayarsa: podman machine ssh "sudo date -u -s @$(date -u +%s)"
if [[ "$(uname)" == "Darwin" ]]; then
    podman machine ssh "sudo date -u -s @$(date -u +%s)" >/dev/null 2>&1 \
        || echo "UYARI: Podman VM saati eşitlenemedi; giriş döngüsü olursa elle eşitleyin."
fi

echo "[1/2] Build ediliyor..."
# -q DEĞİL, -v:m: .NET SDK 10.0.401'in sessiz modu sıradan bilgi mesajlarını ("Building target
# ... completely", MSB3492) HATA olarak raporluyor ve çıkış kodu 1 veriyor (ölçüldü: aynı derleme
# -v:m ile 0 hata, -q ile 72). Sessiz mod geri alınmadan önce SDK sürümü doğrulanmalı.
dotnet build "$ROOT_DIR/MESNET.slnx" -c "$CONFIG" --no-incremental -v:m

echo "[2/2] Aspire AppHost başlatılıyor..."
dotnet run --project "$APPHOST_DIR" -c "$CONFIG"
