# MESNET

**Mesleki Eğitim Stajları Nitelikli, Eşgüdümlü Takip Sistemi**

Mesleki eğitim staj süreçlerinin uçtan uca dijitalleştirilmesini hedefleyen modüler monolit bir uygulamadır.

## Belgeler

Tüm mimari belgeler depoda, Markdown olarak: [`src/Docs/docs/`](src/Docs/docs/). Docusaurus
sitesi olarak da derlenir (`src/Docs`), ama dosyalar GitHub üzerinde doğrudan okunur.

### Başlarken

| Belge | Ne anlatır |
|---|---|
| [Proje Kapsamı](src/Docs/docs/architecture/project-scope.md) | Phase 1 / Phase 2 ayrımı — neyin kapsamda **olmadığı** dahil |
| [Modül Tasarımı](src/Docs/docs/architecture/module-design.md) | Modül sınırları, şema izolasyonu, modüller arası iletişim |
| [Senaryolar](src/Docs/docs/scenarios.md) | Uçtan uca iş akışları |
| [3308 Sayılı Kanun — Özet](src/Docs/docs/architecture/3308-kanun-ozeti.md) | Mevzuat temeli; ücret ve devlet katkısı kuralları buradan türer |
| [İş Kuralları](src/Docs/docs/architecture/business-rules.md) | Domain kuralları tek yerde |

### Mimari kararlar (ADR)

| ADR | Karar |
|---|---|
| [ADR-0001](src/Docs/docs/architecture/adr-0001-yetkilendirme-permission-bazli.md) | Yetkilendirme **permission** bazlıdır, rol bazlı değil |
| [ADR-0002](src/Docs/docs/architecture/adr-0002-izin-agaci-ve-onek-secimi.md) | İzin ağacı ve önek seçimi — wildcard tuzağı |
| [ADR-0003](src/Docs/docs/architecture/adr-0003-cok-kiracilik.md) | Çok kiracılık: **kiracı = okul**, izolasyon satır bazlı |
| [ADR-0004](src/Docs/docs/architecture/adr-0004-isletme-kimlik-iliski-ayrimi.md) | İşletme kimlik/ilişki ayrımı |

### Aktörler ve yetkiler

| Belge | Ne anlatır |
|---|---|
| [Aktör Tanımları](src/Docs/docs/actors/actors.md) | Roller ve sorumlulukları |
| [İzin Matrisi](src/Docs/docs/actors/permissions.md) | Hangi rol hangi izni taşıyor — koddan üretilir, testle kilitli |
| [Kullanıcı Kayıt Akışı](src/Docs/docs/architecture/user-onboarding.md) | Davet, onay ve hesap açma zinciri |

### Geliştirme ve altyapı

| Belge | Ne anlatır |
|---|---|
| [Wolverine Kalıpları](src/Docs/docs/architecture/wolverine-patterns.md) | Handler, saga ve cascading mesaj kuralları — tuzaklarıyla |
| [Web UI](src/Docs/docs/architecture/web-ui.md) | Vue/Quasar bileşen ve durum yönetimi kuralları |
| [Keycloak](src/Docs/docs/infrastructure/keycloak.md) | Realm, client ve claim yapılandırması |
| [GIS & OSRM](src/Docs/docs/infrastructure/gis-osrm.md) | PostGIS ve rota servisi kurulumu |
| [**Dağıtım Ön Koşulları**](src/Docs/docs/infrastructure/dagitim-on-kosullari.md) | Dağıtımdan sonra elle koşturulması gereken adımlar — **atlanırsa sistem hata vermez, özellik sessizce çalışmaz** |

Ayrıca depo kökünde: [Sürümleme Kuralı](VERSIONING.md), [Claude Code talimatları](CLAUDE.md).

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
| **Security** | Kullanıcı hesapları, davet zinciri, rol/izin senkronizasyonu |
| **Audit** | Denetim izi — kim, ne zaman, hangi kayda dokundu |

## Proje Yapısı

```
src/
├── MESNET.AppHost/          # .NET Aspire orkestrasyonu — geliştirmede tüm servisleri ayağa kaldırır
├── MESNET.Presentation/     # Ana API host
├── MESNET.ServiceDefaults/  # Aspire ortak yapılandırması (telemetri, sağlık, dayanıklılık)
├── MESNET.Seeder/           # Geliştirme verisi üreticisi (HTTP üzerinden, idempotent)
├── MESNET.Common.Shared/    # Ortak tipler, SmartEnum'lar, izin sabitleri, kiracılık haritası
├── MESNET.Common.Infrastructure/  # Ortak altyapı (kimlik, kiracılık, sayfalama, depolama)
├── Modules/
│   ├── Business/
│   │   ├── MESNET.Business.Core/          # Domain entity'leri, value object'ler, saf politikalar
│   │   ├── MESNET.Business.Application/   # Wolverine handler'ları ve tüketicileri
│   │   ├── MESNET.Business.Api/           # HTTP uç noktaları (ince adaptör)
│   │   ├── MESNET.Business.Persistence/   # Marten yapılandırması, şema adı
│   │   └── MESNET.Business.Shared/        # Modüller arası domain olayları
│   ├── Institution/  Enrollment/  Contract/  Attendance/
│   ├── Payment/      Coordination/  Internship/  Reporting/
│   ├── Security/     Audit/
│   └── …             # her modül yukarıdaki beş katmanı taşır
├── WebUI/                   # Vue 3 + Quasar frontend
├── Docs/                    # Docusaurus belge sitesi — belge kaynağı `Docs/docs/`
└── caddy/                   # Ters vekil yapılandırması

tests/                       # Modül başına birim testleri + kara kutu API testleri
scripts/                     # Yerel CI, dağıtım ön koşul koşucusu, geliştirme yardımcıları
```

> **Not:** Mimari belgeler eskiden `src/Arch/` altındaydı; tek kaynak artık
> [`src/Docs/docs/`](src/Docs/docs/).

## Gereksinimler

- .NET 10.0 SDK
- Node.js 20+ & pnpm
- PostgreSQL 18 + PostGIS 3.6 (`kartoza/postgis:18-3.6` — Debian trixie tabanlı, çok mimarili)
- Keycloak 26+
- MinIO
- .NET Aspire workload (`dotnet workload install aspire`)
- **Podman** (Docker değil) — Aspire kapsayıcıları bununla koşar

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

## Test ve kalite kapıları

```bash
# Tüm birim testleri
dotnet test MESNET.slnx

# Frontend
cd src/WebUI && pnpm vitest run && pnpm exec vue-tsc --noEmit

# CI işlerini yerelde koş (GitHub Actions'ın aynası)
./scripts/ci-local.sh all      # backend | frontend | docs | integration | all
```

Depoda çok sayıda **sapma testi** (drift test) var: kaynak tarayarak mimari kuralları
kilitlerler — kiracısız Marten session'ı, rol adına bakan kapsam kararı, kiracılar arası sorgu,
onarım ucundan yaşam döngüsü olayı yayınlamak gibi. Bunlar derleyicinin göremediği, davranış
testinin kırılmadığı ve **sessizce yanlış çalışan** sınıfı yakalar. Bir sapma testi kırmızıya
döndüğünde hata mesajı hem nedeni hem çıkış yolunu yazar.

## Dağıtım

Bazı değişiklikler dağıtımdan sonra **elle bir adım** gerektirir. Atlanınca sistem hata
**vermez** — özellik sessizce çalışmaz; belirti hep aynıdır: liste boş gelir, sayı sıfır çıkar.

```bash
./scripts/deploy-prereqs.sh --dry-run    # önce planı gör
./scripts/deploy-prereqs.sh              # sonra koştur
```

Açılışta ayrıca `DeploymentPrerequisiteVerificationHostedService` bu adımların **belirtisini
ölçer** ve eksik olanı kritik log satırı olarak bildirir.

Ayrıntı ve sıra gerekçeleri: [Dağıtım Ön Koşulları](src/Docs/docs/infrastructure/dagitim-on-kosullari.md).

## Sürümleme

SemVer üzerine **minör parite kanalı**: tek minör ön-sürüm (`v0.1.0`), çift minör kararlı
sürüm (`v0.2.0`). Tam kural: [VERSIONING.md](VERSIONING.md).

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

### Belgelerin lisansı

`src/Docs/` altındaki belge **içeriği** CC BY-SA 4.0 ile lisanslanmıştır: atıf vererek ve aynı
lisansla paylaşmak koşuluyla kopyalayabilir, uyarlayabilir, çevirebilirsiniz. Belge sitesinin
kendi kodu diğer kod gibi AGPL kapsamındadır.

### Bağımlılıklar

Yığının tamamı MIT ve Apache-2.0 lisanslıdır (Marten, Wolverine, Quasar, Vue, Npgsql, Keycloak
istemcileri, MinIO, ZXing, QuestPDF). Apache-2.0 → AGPL-3.0 tek yönlü uyumludur; çakışma yoktur.
