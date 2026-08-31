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
- PostgreSQL 18 + PostGIS 3.6 (`kartoza/postgis:18-3.6` — Debian trixie tabanlı, çok mimarili)
- Keycloak 26+
- MinIO
- .NET Aspire workload (`dotnet workload install aspire`)

## Başlangıç

### Yapılandırma (ilk kurulumda ve `git pull` sonrasında)

Gerçek kimlik bilgisi taşıyan üç yapılandırma dosyası git'te **izlenmiyor**; yanlarındaki
`.sample.json` dosyasından kopyalanır ve placeholder'lar doldurulur:

```bash
cp src/MESNET.AppHost/appsettings.sample.json                   src/MESNET.AppHost/appsettings.json
cp src/MESNET.Presentation/appsettings.Development.sample.json  src/MESNET.Presentation/appsettings.Development.json
cp src/MESNET.Seeder/appsettings.sample.json                    src/MESNET.Seeder/appsettings.json
```

> **Dikkat:** Bu dosyalar takipten çıkarıldığı için (#66), değişikliği içeren commit'i ilk kez
> çektiğinizde git **diskteki kopyanızı da siler**. `git pull` sonrası uygulama
> "endpoint boş" ya da "credentials not initialized" gibi hatalarla açılmıyorsa, önce bu üç
> dosyanın yerinde olduğunu kontrol edin.

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

**Kod: [GNU AGPL-3.0-or-later](LICENSE). Belgeler: [CC BY-SA 4.0](LICENSE-DOCS).**

Telif hakkı (C) 2026 Hakan GÜLEN.

### Neden AGPL

MESNET bir **web uygulamasıdır**. GPL'in copyleft'i yalnız **dağıtımda** tetiklenir: bir
tedarikçi MESNET'i alıp özelleştirip okullara **hizmet olarak** koşturabilir ve hiçbir şey
paylaşmak zorunda kalmazdı. AGPL §13 tam bu boşluğu kapatır — yazılımı ağ üzerinden
kullandıran, kullanıcılarına kaynağı sunmak zorundadır.

Ticari kaygımız yok; amaç bu altyapının **açık kalmasıdır**.

### Bu sizin için ne demek

| Siz | Yükümlülüğünüz |
|---|---|
| Okul / il–ilçe müdürlüğü / bakanlık, kendi koşuyor | Yok. Kaynak zaten burada; kullanıcılara bu deponun adresini göstermek yeter |
| Kendi ihtiyacınıza göre değiştiriyorsunuz, dışarı hizmet vermiyorsunuz | Yok |
| Değiştirilmiş sürümü **başkalarına hizmet olarak** sunuyorsunuz | Kullanıcılarınıza tüm kaynağı AGPL altında vermelisiniz |
| Bir tedarikçiye özelleştirtiyorsunuz | Tedarikçi değişiklikleri **size vermek zorundadır** — bu lisans sizi tedarikçi kilidine karşı korur |

AGPL ticari kullanımı **yasaklamaz**; kapalı ürüne dönüştürmeyi yasaklar. Barındırma ve destek
hizmeti satmak serbesttir — yeter ki kod açık kalsın.

### Belgeler

`src/Docs/` altındaki belge **içeriği** CC BY-SA 4.0 ile lisanslanmıştır: atıf vererek ve aynı
lisansla paylaşmak koşuluyla kopyalayabilir, uyarlayabilir, çevirebilirsiniz. Belge sitesinin
kendi kodu diğer kod gibi AGPL kapsamındadır.

### Bağımlılıklar

Yığının tamamı MIT ve Apache-2.0 lisanslıdır (Marten, Wolverine, Quasar, Vue, Npgsql, Keycloak
istemcileri, MinIO, ZXing, QuestPDF). Apache-2.0 → AGPL-3.0 tek yönlü uyumludur; çakışma yoktur.
