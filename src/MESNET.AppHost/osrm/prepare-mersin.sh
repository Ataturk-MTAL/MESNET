#!/bin/sh
# MESNET — Mersin OSRM Veri Hazırlama Scripti
#
# Kullanım:
#   chmod +x prepare-mersin.sh
#   ./prepare-mersin.sh
#
# Gereksinimler:
#   macOS  → brew install osmium-tool osrm-backend
#   Linux  → apt install osmium-tool osrm-backend  (veya aşağıdaki Docker yöntemi)
#
# Docker ile de çalıştırılabilir (kurulum gerektirmez):
#   docker run --rm -v "$(pwd)/data:/data" \
#     ghcr.io/project-osrm/osrm-backend osrm-extract ...
#   (Bu script Docker kullanmaz, native binary varsayar)

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
DATA_DIR="$SCRIPT_DIR/data"
PBF_TURKEY="$DATA_DIR/turkey-latest.osm.pbf"
PBF_MERSIN="$DATA_DIR/mersin.osm.pbf"
OSRM_FILE="$DATA_DIR/mersin.osrm"

# Mersin bounding box: min_lon, min_lat, max_lon, max_lat
BBOX="33.0,36.1,36.0,37.5"

# ── Renk çıktı yardımcıları ──
info()    { printf '\033[1;34m[INFO]\033[0m  %s\n' "$*"; }
success() { printf '\033[1;32m[OK]\033[0m    %s\n' "$*"; }
warn()    { printf '\033[1;33m[WARN]\033[0m  %s\n' "$*"; }
error()   { printf '\033[1;31m[ERROR]\033[0m %s\n' "$*" >&2; exit 1; }

# ── Gereksinim kontrolü ──
check_cmd() {
  command -v "$1" >/dev/null 2>&1 || error "'$1' bulunamadı. Kurulum için README'ye bakın."
}

info "Gereksinimler kontrol ediliyor..."
check_cmd osmium
check_cmd osrm-extract
check_cmd osrm-contract

# Profil dosyasını bul (macOS Homebrew veya Linux farklı yolda)
if [ -f "/opt/homebrew/share/osrm/profiles/car.lua" ]; then
  PROFILE="/opt/homebrew/share/osrm/profiles/car.lua"
elif [ -f "/usr/local/share/osrm/profiles/car.lua" ]; then
  PROFILE="/usr/local/share/osrm/profiles/car.lua"
elif [ -f "/usr/share/osrm/profiles/car.lua" ]; then
  PROFILE="/usr/share/osrm/profiles/car.lua"
else
  error "OSRM araba profili bulunamadı. osrm-backend kurulumunu kontrol edin."
fi
success "Profil: $PROFILE"

mkdir -p "$DATA_DIR"

# ── Eğer Mersin OSRM zaten hazırsa atla ──
if [ -f "$OSRM_FILE.hsgr" ]; then
  success "Mersin OSRM verileri zaten mevcut ($OSRM_FILE.hsgr)"
  info "Yeniden oluşturmak için: rm $DATA_DIR/mersin.osrm.* && $0"
  exit 0
fi

# ── Türkiye PBF ──
if [ ! -f "$PBF_TURKEY" ]; then
  info "Türkiye haritası indiriliyor (~600 MB)..."
  curl -L --progress-bar \
    -o "$PBF_TURKEY" \
    "https://download.geofabrik.de/europe/turkey-latest.osm.pbf" \
    || error "İndirme başarısız."
  success "İndirme tamamlandı: $PBF_TURKEY"
else
  info "Türkiye PBF mevcut, indirme atlanıyor."
fi

# ── Mersin bölgesini kes ──
info "Mersin bölgesi kesiliyor (bbox: $BBOX)..."
osmium extract \
  --bbox "$BBOX" \
  "$PBF_TURKEY" \
  -o "$PBF_MERSIN" \
  --overwrite
success "Mersin PBF oluşturuldu: $(du -sh "$PBF_MERSIN" | cut -f1)"

# ── OSRM Extract ──
info "osrm-extract çalıştırılıyor..."
cd "$DATA_DIR"
osrm-extract -p "$PROFILE" mersin.osm.pbf
success "osrm-extract tamamlandı."

# ── OSRM Contract (Contraction Hierarchies) ──
info "osrm-contract çalıştırılıyor (~1 dakika)..."
osrm-contract mersin.osrm
success "osrm-contract tamamlandı."

# ── Türkiye PBF'yi sil (yer açmak için) ──
info "Türkiye PBF siliniyor (yer açmak için)..."
rm -f "$PBF_TURKEY"

# ── Sonuç ──
echo ""
success "Mersin OSRM verileri hazır!"
du -sh "$DATA_DIR"
echo ""
info "Şimdi Aspire'ı başlatabilirsiniz — OSRM container anında ayağa kalkar."
