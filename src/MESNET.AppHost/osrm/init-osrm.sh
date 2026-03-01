#!/bin/bash
# OSRM Türkiye haritası hazırlık scripti.
# İlk çalıştırmada Geofabrik'ten turkey-latest.osm.pbf indirir ve OSRM formatına dönüştürür.
# Sonraki çalıştırmalarda hazır veri varsa doğrudan OSRM backend'i başlatır.

DATA_DIR="/data"
PBF_FILE="$DATA_DIR/turkey-latest.osm.pbf"
OSRM_FILE="$DATA_DIR/turkey-latest.osrm"

# Eğer OSRM verileri zaten hazırsa direkt başlat
if [ -f "$OSRM_FILE.partition" ] && [ -f "$OSRM_FILE.cells" ]; then
    echo "OSRM verileri mevcut, backend baslatiliyor..."
    exec osrm-routed --algorithm mld "$OSRM_FILE"
fi

echo "OSRM verileri bulunamadi, Turkiye haritasi hazirlaniyor..."

# PBF dosyası yoksa indir
if [ ! -f "$PBF_FILE" ]; then
    echo "Turkiye haritasi indiriliyor (Geofabrik)..."
    wget -q --show-progress -O "$PBF_FILE" \
        "https://download.geofabrik.de/europe/turkey-latest.osm.pbf"

    if [ $? -ne 0 ]; then
        echo "HATA: Harita indirilemedi!"
        exit 1
    fi
    echo "Indirme tamamlandi."
fi

# OSRM extract — PBF'i OSRM formatına çevir (MLD profili: araba)
echo "osrm-extract baslatiliyor (bu 5-15 dk surebilir)..."
osrm-extract -p /opt/car.lua "$PBF_FILE"

if [ $? -ne 0 ]; then
    echo "HATA: osrm-extract basarisiz!"
    exit 1
fi

# OSRM partition — MLD algoritması için bölümleme
echo "osrm-partition baslatiliyor..."
osrm-partition "$OSRM_FILE"

# OSRM customize — MLD ağırlık hesaplama
echo "osrm-customize baslatiliyor..."
osrm-customize "$OSRM_FILE"

echo "Turkiye haritasi hazir! OSRM backend baslatiliyor..."
exec osrm-routed --algorithm mld "$OSRM_FILE"
