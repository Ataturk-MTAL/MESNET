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
echo "  1. MESNET süreçlerini durdur, Aspire container'larını sil"
echo "  2. Veri volume'larını sil (PostgreSQL, Keycloak, RabbitMQ, RustFS, Mailpit, OpenObserve, pgAdmin)"
echo "  3. Aspire AppHost'u yeniden başlat"
echo "     → Marten schema'ları otomatik oluşturulur"
echo "     → Keycloak realm yeniden import edilir"
echo "     → Seeder otomatik çalışır (kurum, personel, öğrenci, vb.)"
echo ""

read -rp "Devam etmek istiyor musunuz? [e/H] " confirm
# "evet"/"yes" de kabul edilir — yalnız tek harf beklemek, "evet" yazanı sessizce iptal ediyordu.
if [[ ! "$confirm" =~ ^([eE](vet)?|[yY](es)?)$ ]]; then
    echo "İptal edildi."
    exit 0
fi

echo ""

# Container seçimi stop-dev.sh ile aynı Aspire/DCP etiketine dayanır. Eski ad deseni
# ("mesnet|pgadmin") 9 container'ın yalnız pgadmin'ini yakalıyordu: Aspire container'ları
# `postgres-7691e3ba` gibi adlandırılır, adda "mesnet" geçmez (ölçüldü).
ASPIRE_LABEL="com.microsoft.developer.usvc-dev.name"

# Host süreçleri önce durmalı: API ayaktayken silinen postgres, yarım yazılmış olay bırakır.
"$SCRIPT_DIR/stop-dev.sh" --keep-machine

if ! podman info &>/dev/null; then
    echo "  → Podman VM kapalı, başlatılıyor..."
    podman machine start >/dev/null
fi

aspire_containers() {
    {
        podman ps -a --filter "label=$ASPIRE_LABEL" --format '{{.Names}}'
        podman ps -a --filter 'name=aspire-container-network-tunnelproxy' --format '{{.Names}}'
    } 2>/dev/null | sort -u
}

# Volume'lar ad tahmininden değil, container'ların GERÇEK mount'larından türetilir: pgadmin'in
# anonim volume'u (hash adlı), mailpit-data ve openobserve-data ad desenine hiç uymuyordu.
# Bind mount'lar (OSRM harita verisi, realm dosyası) volume değildir — dokunulmaz.
# Container'lar silinmeden ÖNCE toplanır; sonra inspect edilecek bir şey kalmaz.
CONTAINERS="$(aspire_containers)"
VOLUMES="$(
    {
        for c in $CONTAINERS; do
            podman inspect "$c" --format '{{range .Mounts}}{{if eq .Type "volume"}}{{.Name}}{{"\n"}}{{end}}{{end}}'
        done
        # Önceki bir koşudan container'ı silinmiş, sahipsiz kalmış AppHost volume'ları.
        podman volume ls --format '{{.Name}}' | grep '^mesnet\.apphost-' || true
    } 2>/dev/null | sed '/^$/d' | sort -u
)"

# ── 1. Container'ları sil ──
echo ""
echo "[1/3] Aspire container'ları siliniyor..."

if [[ -z "$CONTAINERS" ]]; then
    echo "  → Aspire container'ı bulunamadı"
else
    for name in $CONTAINERS; do
        podman rm -f "$name" >/dev/null 2>&1 && echo "  ✓ $name silindi" || echo "  ! $name silinemedi" >&2
    done
fi

# ── 2. Volume'ları sil ──
echo ""
echo "[2/3] Veri volume'ları siliniyor..."

if [[ -z "$VOLUMES" ]]; then
    echo "  → Silinecek volume bulunamadı"
else
    for vol in $VOLUMES; do
        podman volume rm -f "$vol" >/dev/null 2>&1 && echo "  ✓ $vol silindi" || echo "  ! $vol silinemedi" >&2
    done
fi

LEFT="$(aspire_containers)"
if [[ -n "$LEFT" ]]; then
    echo "HATA: silinemeyen container kaldı: $(echo "$LEFT" | tr '\n' ' ')" >&2
    exit 1
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
