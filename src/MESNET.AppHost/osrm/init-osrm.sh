#!/bin/sh
# OSRM Mersin haritası hazırlık scripti.
# Veriler Mac'te önceden hazırlanır (osmium + osrm-extract + osrm-contract).
# Bu script artık kullanılmıyor — AppHost doğrudan osrm-routed başlatır.
# Referans olarak saklanmıştır.

DATA_DIR="/data"
PBF_FILE="$DATA_DIR/mersin.osm.pbf"
OSRM_FILE="$DATA_DIR/mersin.osrm"

# Eğer OSRM verileri zaten hazırsa direkt başlat (CH algoritması)
if [ -f "$OSRM_FILE.hsgr" ]; then
    echo "OSRM verileri mevcut, backend baslatiliyor..."
    exec osrm-routed --algorithm CH "$OSRM_FILE"
fi

echo "OSRM verileri bulunamadi. Lutfen Mac'te asagidaki adimlari calistirin:"
echo ""
echo "  brew install osmium-tool osrm-backend"
echo "  cd src/MESNET.AppHost/osrm/data"
echo "  curl -L -o turkey-latest.osm.pbf https://download.geofabrik.de/europe/turkey-latest.osm.pbf"
echo "  osmium extract --bbox 33.0,36.1,36.0,37.5 turkey-latest.osm.pbf -o mersin.osm.pbf"
echo "  osrm-extract -p /opt/homebrew/share/osrm/profiles/car.lua mersin.osm.pbf"
echo "  osrm-contract mersin.osrm"
echo ""
exit 1
