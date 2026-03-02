---
title: GIS & OSRM Kurulumu
---

MESNET, işletme-okul arası gerçek yol mesafelerini hesaplamak için **OSRM (Open Source Routing Machine)** kullanır.
PostGIS ile DBSCAN kümeleme, OSRM ile rota bazlı mesafe servisi birlikte çalışır.

## Mimari Genel Bakış

```text
Mac (geliştirici)
  ├── osmium-tool   → Türkiye PBF'den Mersin bölgesini keser
  ├── osrm-extract  → Yol grafiğini çıkarır
  └── osrm-contract → Contraction Hierarchies ön işlemi

Podman Container (runtime)
  └── osrm-routed --algorithm CH /data/mersin.osrm
        ↑ bind mount: ./osrm/data → /data
```

Veri hazırlama **bir kez Mac'te** yapılır; container sadece hazır dosyaları serve eder.
Bu yaklaşım sayesinde container başlangıcı anında gerçekleşir ve VM bellek sorunu yaşanmaz.

---

## Neden Türkiye Değil Mersin?

| Ölçüt | Türkiye | Mersin |
| --- | --- | --- |
| PBF boyutu | ~600 MB | ~18 MB |
| `osrm-extract` süresi | ~165 sn | ~5 sn |
| `osrm-contract` süresi | ~850 sn | ~48 sn |
| Toplam OSRM dosyaları | ~3.4 GB | ~200 MB |
| Runtime RAM kullanımı | ~2 GB | ~150 MB |

MESNET tek kurum odaklıdır (Mersin). Türkiye geneli veri gereksiz kaynak tüketir.

---

## Algoritma Seçimi

OSRM iki algoritma sunar:

| Ölçüt | CH | MLD |
| --- | --- | --- |
| Ön işlem süresi | Uzun | Kısa |
| Sorgu hızı | Çok hızlı | Hızlı |
| Trafik güncellemesi | Desteklenmez | Desteklenir |
| Bellek | Düşük | Yüksek |

MESNET statik mesafe hesabı yaptığından (canlı trafik gerekmez) **CH (Contraction Hierarchies) tercih edilir**.

---

## Kurulum

### 1. Gerekli araçları kur

**macOS:**

```bash
brew install osmium-tool osrm-backend
```

**Linux (Debian/Ubuntu):**

```bash
apt install osmium-tool osrm-backend
```

### 2. Script ile otomatik hazırlık

`prepare-mersin.sh` scripti tüm adımları otomatik yapar:

```bash
cd src/MESNET.AppHost/osrm
./prepare-mersin.sh
```

Script sırasıyla:

1. Türkiye PBF'yi indirir (~600 MB)
2. Mersin bölgesini bbox ile keser → ~18 MB
3. `osrm-extract` çalıştırır (~5 sn)
4. `osrm-contract` çalıştırır (~48 sn)
5. Türkiye PBF'yi siler (yer açmak için)

Veriler zaten mevcutsa script hiçbir şey yapmadan çıkar.

:::tip Yeniden oluşturmak için

```bash
rm src/MESNET.AppHost/osrm/data/mersin.osrm.*
./prepare-mersin.sh
```

:::

### 3. Manuel adımlar (referans)

Script arka planda şunları çalıştırır:

```bash
# Türkiye PBF indir
curl -L -o turkey-latest.osm.pbf \
  https://download.geofabrik.de/europe/turkey-latest.osm.pbf

# Mersin bölgesini kes (min_lon, min_lat, max_lon, max_lat)
osmium extract --bbox 33.0,36.1,36.0,37.5 \
  turkey-latest.osm.pbf -o mersin.osm.pbf --overwrite

# OSRM extract + contract
osrm-extract -p /path/to/car.lua mersin.osm.pbf
osrm-contract mersin.osrm
```

:::info Bounding Box

`33.0,36.1,36.0,37.5` — Mersin ili sınırları biraz geniş tutulmuştur, ilçe sınırı aşan yollar da dahil edilir.

:::

### 4. Doğrulama

```bash
ls -lh data/mersin.osrm.hsgr     # ~56 MB olmalı
ls -lh data/mersin.osrm.geometry  # ~33 MB olmalı
```

---

## Aspire AppHost Konfigürasyonu

Veriler hazırlandıktan sonra container sadece `osrm-routed` çalıştırır:

```csharp title="src/MESNET.AppHost/Program.cs"
var osrm = builder.AddContainer("osrm", "ghcr.io/project-osrm/osrm-backend", "latest")
    .WithHttpEndpoint(port: 5002, targetPort: 5000, name: "osrm")
    .WithBindMount("./osrm/data", "/data")          // Mac'teki hazır veriler
    .WithArgs("osrm-routed", "--algorithm", "CH", "/data/mersin.osrm")
    .WithLifetime(ContainerLifetime.Persistent);
```

:::caution `osrm/data/` dizini `.gitignore`'da

OSRM dosyaları büyük (200 MB+) olduğundan git'e eklenmez.
Her geliştirici yukarıdaki adımları bir kez çalıştırmalıdır.

:::

---

## API Kullanımı

OSRM, `OsrmDistanceService` üzerinden kullanılır. OSRM v5.x HTTP API ile uyumludur.

:::tip Koordinat Sırası

OSRM **longitude,latitude** sırası kullanır (GeoJSON standardı) — coğrafya derslerindeki lat,lon sırasının tersi.

:::

### Tekil rota mesafesi

```text
GET /route/v1/driving/{lon},{lat};{lon},{lat}?overview=false
```

```csharp
// OsrmDistanceService.cs
var url = $"{_baseUrl}/route/v1/driving/{from.Longitude},{from.Latitude};{to.Longitude},{to.Latitude}?overview=false";
```

### Tablo (çoka-çok mesafe matrisi)

```text
GET /table/v1/driving/{koordinatlar}?sources=0&annotations=distance
```

---

## PostGIS Entegrasyonu

DBSCAN kümeleme için PostgreSQL'de PostGIS extension gereklidir.

### PostGIS Kurulumu

`kartoza/postgis:18-3.6` imajı PostGIS içerir.
`./postgres/init-postgis.sql` dosyası `docker-entrypoint-initdb.d/` üzerinden ilk başlatmada çalışır:

```sql title="src/MESNET.AppHost/postgres/init-postgis.sql"
CREATE EXTENSION IF NOT EXISTS postgis;
```

### Sürüm Doğrulama

```sql
SELECT PostGIS_Version();
-- 3.6 USE_GEOS=1 USE_PROJ=1 ...
```

### DBSCAN Kümeleme Sorgusu

`GetBusinessClustersHandler` işletmeleri coğrafi kümelere ayırır:

```sql
SELECT
    (data->>'Id')::uuid AS business_id,
    (data->'Location'->>'Latitude')::float8 AS latitude,
    (data->'Location'->>'Longitude')::float8 AS longitude,
    ST_ClusterDBSCAN(
        ST_SetSRID(
            ST_MakePoint(
                (data->'Location'->>'Longitude')::float8,
                (data->'Location'->>'Latitude')::float8
            ), 4326
        )::geography::geometry,
        eps := @eps,          -- metre cinsinden yarıçap (varsayılan: 500m)
        minpoints := @minPoints  -- minimum nokta sayısı (varsayılan: 3)
    ) OVER () AS cluster_id
FROM coordination.mt_doc_businesscoordinationview
WHERE (data->>'InstitutionId')::uuid = @institutionId
  AND (data->>'AcademicPeriodId')::uuid = @academicPeriodId
```

`cluster_id = NULL` → outlier (hiçbir kümeye dahil olmayan işletme)

---

## Okul Merkezi

Seeder'da tanımlanan okul koordinatı:

| Kurum | Değer |
| --- | --- |
| Ad | Atatürk Mesleki ve Teknik Anadolu Lisesi |
| Koordinat | 36.7956° K, 34.6119° D |
| Adres | Toroslar, Mersin |

Bu koordinat `RecalculateDistances` komutu çalıştığında tüm işletmeler için
OSRM üzerinden gerçek yol mesafesi hesaplamasında referans nokta olarak kullanılır.
