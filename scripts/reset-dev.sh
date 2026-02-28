#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"

# Podman container runtime
export ASPIRE_CONTAINER_RUNTIME=podman

echo ""
echo "═══ MESNET Dev Ortamı Sıfırlama ═══"
echo ""
echo "Bu script şunları yapacak:"
echo "  1. MESNET ile ilgili tüm Podman container'ları durdur ve sil"
echo "  2. Veri volume'larını sil (PostgreSQL, Keycloak, RabbitMQ, MinIO)"
echo "  3. Aspire AppHost'u yeniden başlat"
echo "     → Marten schema'ları otomatik oluşturulur"
echo "     → Keycloak realm yeniden import edilir"
echo "     → Seeder otomatik çalışır (kurum, personel, öğrenci, vb.)"
echo ""

read -rp "Devam etmek istiyor musunuz? [e/H] " confirm
if [[ ! "$confirm" =~ ^[eEyY]$ ]]; then
    echo "İptal edildi."
    exit 0
fi

echo ""

# ── 1. Container'ları durdur ve sil ──
echo "[1/3] Container'lar durduruluyor..."

CONTAINERS=$(podman ps -a --format '{{.Names}}' 2>/dev/null | grep -i "mesnet\|pgadmin" || true)
if [[ -n "$CONTAINERS" ]]; then
    echo "$CONTAINERS" | while read -r name; do
        podman rm -f "$name" 2>/dev/null && echo "  ✓ $name silindi" || true
    done
else
    echo "  → MESNET container'ı bulunamadı"
fi

# ── 2. Volume'ları sil ──
echo ""
echo "[2/3] Veri volume'ları siliniyor..."

VOLUMES=$(podman volume ls --format '{{.Name}}' 2>/dev/null | grep -i -E "mesnet|minio-data|pgadmin" || true)
if [[ -n "$VOLUMES" ]]; then
    echo "$VOLUMES" | while read -r vol; do
        podman volume rm -f "$vol" 2>/dev/null && echo "  ✓ $vol silindi" || true
    done
else
    echo "  → Silinecek volume bulunamadı"
fi

# ── 3. Aspire AppHost'u başlat ──
echo ""
echo "[3/3] Aspire AppHost yeniden başlatılıyor..."
echo "  → Build + container'lar oluşturulacak"
echo "  → Marten schema auto-create"
echo "  → Keycloak realm import"
echo "  → Seeder otomatik çalışacak"
echo ""

dotnet run --project "$ROOT_DIR/src/MESNET.AppHost" -c Debug
