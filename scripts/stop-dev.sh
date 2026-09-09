#!/usr/bin/env bash
set -euo pipefail

# MESNET dev ortamını durdurur: host süreçleri → Aspire container'ları → Podman VM.
#
# Neden ayrı bir script: `dotnet watch` SIGTERM'e güvenilir yanıt VERMİYOR (ölçüldü, iki ayrı
# kapanışta da). Terminalden Ctrl+C sonrası watch süreçleri ve arkalarındaki API ayakta kalıyor,
# port 5270 tutulu kalıyor ve bir sonraki açılış "address already in use" ile düşüyor.
#
# Container seçimi Aspire/DCP etiketine dayanır (`com.microsoft.developer.usvc-dev.name`),
# `podman stop --all` DEĞİL: makinede MESNET dışı container'lar da olabilir ve onları durdurmak
# bu script'in işi değildir. Etiket ölçüldü — 9 Aspire container'ı seçiliyor, ilgisiz olanlar
# dışarıda kalıyor.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"

ASPIRE_LABEL="com.microsoft.developer.usvc-dev.name"
STOP_TIMEOUT=25          # container'a SIGTERM sonrası tanınan süre (postgres temiz kapansın)
PROCESS_GRACE_SECONDS=6  # SIGTERM'den SIGKILL'e geçmeden önce beklenen süre

KEEP_MACHINE=0
for arg in "$@"; do
    case "$arg" in
        --keep-machine) KEEP_MACHINE=1 ;;
        -h|--help)
            echo "Kullanım: $(basename "$0") [--keep-machine]"
            echo ""
            echo "  --keep-machine   Podman VM'i açık bırakır (yalnız süreçler + container'lar durur)"
            exit 0
            ;;
        *)
            echo "HATA: bilinmeyen argüman: $arg" >&2
            exit 2
            ;;
    esac
done

echo ""
echo "═══ MESNET Dev Ortamı Durdurma ═══"
echo ""

# ── 1. Host süreçleri ──
# AppHost, dotnet watch ve onların başlattığı API/Seeder. Desen MESNET projelerine bağlı;
# makinedeki başka dotnet işleri etkilenmez.
echo "[1/3] Host süreçleri durduruluyor..."

PROCESS_PATTERN='MESNET\.AppHost|MESNET\.Presentation|MESNET\.Seeder|dotnet-watch\.dll.*MESNET|dotnet watch.*MESNET'

running_pids() { pgrep -f "$PROCESS_PATTERN" 2>/dev/null || true; }

PIDS="$(running_pids)"
if [[ -z "$PIDS" ]]; then
    echo "  → Çalışan MESNET süreci yok"
else
    echo "$PIDS" | while read -r pid; do
        [[ -n "$pid" ]] && kill "$pid" 2>/dev/null || true
    done
    sleep "$PROCESS_GRACE_SECONDS"

    # dotnet watch SIGTERM'i yutabiliyor — kalan varsa SIGKILL.
    LEFT="$(running_pids)"
    if [[ -n "$LEFT" ]]; then
        echo "  ! SIGTERM yetmedi, SIGKILL uygulanıyor: $(echo "$LEFT" | tr '\n' ' ')"
        echo "$LEFT" | while read -r pid; do
            [[ -n "$pid" ]] && kill -9 "$pid" 2>/dev/null || true
        done
        sleep 2
    fi

    STILL="$(running_pids)"
    if [[ -n "$STILL" ]]; then
        echo "  HATA: durdurulamayan süreç kaldı: $(echo "$STILL" | tr '\n' ' ')" >&2
        exit 1
    fi
    echo "  ✓ Host süreçleri durdu"
fi

# ── 2. Aspire container'ları ──
echo "[2/3] Aspire container'ları durduruluyor..."

if ! podman info &>/dev/null; then
    echo "  → Podman erişilemiyor (VM zaten kapalı), container adımı atlanıyor"
else
    # Etiketli container'lar + tunnelproxy (efemer, her koşuda yeniden yaratılır).
    TARGETS="$(
        {
            podman ps --filter "label=$ASPIRE_LABEL" --format '{{.Names}}'
            podman ps --filter 'name=aspire-container-network-tunnelproxy' --format '{{.Names}}'
        } 2>/dev/null | sort -u
    )"

    if [[ -z "$TARGETS" ]]; then
        echo "  → Çalışan Aspire container'ı yok"
    else
        COUNT="$(echo "$TARGETS" | wc -l | tr -d ' ')"
        # shellcheck disable=SC2086
        podman stop -t "$STOP_TIMEOUT" $TARGETS >/dev/null 2>&1 || true
        echo "  ✓ $COUNT container durduruldu"
    fi
fi

# ── 3. Podman VM ──
echo "[3/3] Podman VM..."

if [[ "$KEEP_MACHINE" -eq 1 ]]; then
    echo "  → --keep-machine verildi, VM açık bırakıldı"
elif ! podman machine list --format '{{.Running}}' 2>/dev/null | grep -q true; then
    echo "  → VM zaten kapalı"
else
    # VM'i durdurmak MESNET DIŞI container'ları da düşürür — sessizce yapmayalım.
    OTHERS="$(podman ps --format '{{.Names}}' 2>/dev/null | grep -v "^aspire-container-network-tunnelproxy" || true)"
    if [[ -n "$OTHERS" ]]; then
        echo "  ! MESNET dışı container'lar çalışıyor, VM kapatılırsa onlar da düşer:"
        echo "$OTHERS" | sed 's/^/      /'
        echo "  → VM açık bırakıldı. Yine de kapatmak için: podman machine stop"
    else
        podman machine stop >/dev/null 2>&1 && echo "  ✓ VM durduruldu"
    fi
fi

echo ""
echo "═══ Durum ═══"
printf "  MESNET süreci : %s\n" "$(pgrep -f "$PROCESS_PATTERN" >/dev/null 2>&1 && echo 'VAR (!)' || echo 'yok')"
printf "  Podman VM     : %s\n" "$(podman machine list --format '{{.Running}}' 2>/dev/null | grep -q true && echo 'açık' || echo 'kapalı')"
# lsof eşleşme bulamazsa 1 döner; `set -e` altında bu, betiği son satırda düşürür.
BUSY="$(lsof -nP -iTCP:5173,5270,8080,5432,17299 -sTCP:LISTEN 2>/dev/null | tail -n +2 | awk '{print $9}' | tr '\n' ' ' || true)"
printf "  Dinlenen port : %s\n" "${BUSY:-yok}"
echo ""
echo "Yeniden başlatmak için: $ROOT_DIR/scripts/run-apphost.sh"
echo ""
