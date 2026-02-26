# MESNET

**Mesleki Eğitim Stajları Nitelikli, Eşgüdümlü Takip Sistemi**

Mesleki eğitim staj süreçlerinin uçtan uca dijitalleştirilmesini hedefleyen modüler monolit bir uygulamadır.

## Teknoloji Yığını

| Katman | Teknoloji |
|---|---|
| Runtime | .NET 10.0 |
| Veritabanı | PostgreSQL (JSONB document storage + event store) |
| ORM / Document DB | [Marten](https://martendb.io/) |
| Messaging / CQRS | [Wolverine](https://wolverinefx.net/) |
| Frontend | Vue 3 + TypeScript + Quasar + Pinia |
| Kimlik Doğrulama | Keycloak (OAuth2 / OIDC) |
| Dosya Depolama | MinIO (S3 uyumlu) |
| Orkestrasyon | .NET Aspire |
| PDF Üretimi | QuestPDF |
| Harita | OpenStreetMap + Leaflet |

## Mimari

**Modüler Monolit + CQRS + Event Sourcing**

- Her modül kendi bounded context'idir ve bağımsız PostgreSQL schema'sına sahiptir
- Modüller arası iletişim yalnızca asenkron event'ler (Wolverine) ile yapılır
- Event sourcing: durum geçişi olan entity'ler (sözleşme, devamsızlık, ödeme)
- Document storage: CRUD ağırlıklı entity'ler (işletme, öğrenci, kurum)

## Modüller

| Modül | Açıklama |
|---|---|
| **Institution** | Kurum yönetimi, akademik dönemler |
| **Business** | İşletme kaydı, onay, kapasite, belge yönetimi |
| **Enrollment** | Öğrenci kaydı, durum yönetimi |
| **Contract** | Staj sözleşmeleri, imza süreçleri |
| **Attendance** | Devamsızlık takibi, sağlık raporu |
| **Payment** | Dekont/maaş yönetimi, onay süreçleri |
| **Coordination** | Öğretmen-öğrenci-işletme koordinasyonu |
| **Internship** | Staj yaşam döngüsü orkestrasyonu (Wolverine saga) |
| **Reporting** | Raporlama ve PDF üretimi (QuestPDF) |

## Proje Yapısı

```
src/
├── Arch/                    # Mimari dokümanlar, C4 diyagramları
├── MESNET.Presentation/     # Ana API host (Aspire entegrasyonu)
├── Modules/
│   ├── Business/
│   │   ├── MESNET.Business.Core/          # Domain entities, value objects
│   │   ├── MESNET.Business.Application/   # Wolverine handler'ları
│   │   ├── MESNET.Business.Api/           # HTTP endpoints
│   │   ├── MESNET.Business.Persistence/   # Marten konfigürasyonu
│   │   └── MESNET.Business.Shared/        # Domain events (modüller arası)
│   ├── Institution/
│   ├── Enrollment/
│   ├── Contract/
│   ├── Attendance/
│   ├── Payment/
│   ├── Coordination/
│   ├── Internship/
│   └── Reporting/
├── Common/
│   ├── MESNET.Common.Shared/              # Ortak tipler, helper'lar
│   └── MESNET.Common.Infrastructure/      # Ortak altyapı (auth, storage)
└── WebUI/                   # Vue 3 + Quasar frontend
```

## Gereksinimler

- .NET 10.0 SDK
- Node.js 20+ & pnpm
- PostgreSQL 16+
- Keycloak 26+
- MinIO
- .NET Aspire workload (`dotnet workload install aspire`)

## Başlangıç

### Backend

```bash
# Aspire AppHost ile tüm servisleri başlat
dotnet run --project src/MESNET.AppHost
```

### Frontend

```bash
cd src/WebUI
pnpm install
pnpm dev
```

## Lisans

Bu proje özel kullanım içindir.
