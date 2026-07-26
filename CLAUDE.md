# MESNET - Claude Code Talimatları

## Proje Hakkında

MESNET (Mesleki Eğitim Stajları Nitelikli, Eşgüdümlü Takip Sistemi) — mesleki eğitim staj süreçlerinin uçtan uca dijitalleştirilmesini hedefleyen modüler monolit bir .NET uygulamasıdır.

## Teknoloji Yığını

- **Container Runtime:** Podman (Docker değil)
- **Runtime:** .NET 10.0
- **Veritabanı:** PostgreSQL (JSONB document storage + event store)
- **ORM / Document DB:** Marten (https://martendb.io/)
- **Messaging / CQRS / Mediator:** Wolverine (https://wolverinefx.net/)
- **Frontend:** Quasar Framework (Vue 3 + TypeScript + Pinia) — SPA, paket yöneticisi: **pnpm**
- **Kimlik Doğrulama:** Keycloak (OAuth2 / OIDC, **PKCE** flow — public client, client secret yok)
- **Mimari:** Modüler Monolit + CQRS + Event Sourcing

## Temel Kütüphaneler ve Kullanımları

### Wolverine (WolverineFx)

Wolverine, MediatR yerine kullanılan command bus, message bus ve mediator framework'üdür. MediatR KULLANILMAZ.

**Temel NuGet paketleri:**
- `WolverineFx` — Çekirdek: message bus, handler keşfi, middleware pipeline
- `WolverineFx.Http` — HTTP endpoint code generation (Minimal API yerine)
- `WolverineFx.Marten` — Marten entegrasyonu: transactional outbox, saga, event sourcing
- `WolverineFx.Http.Marten` — HTTP + Marten: `[Document]`, `[Aggregate]` attributeleri
- `WolverineFx.RabbitMQ` — RabbitMQ transport (modüller arası mesajlaşma)
- `WolverineFx.FluentValidation` — FluentValidation middleware

**Handler kuralları:**
- Handler sınıfları `Handler` veya `Consumer` soneki ile biter
- Handler metodları `Handle`, `HandleAsync`, `Consume`, `ConsumeAsync` olarak isimlendirilir
- İlk parametre her zaman mesaj tipidir
- Mesaj tipleri plain C# record/class olmalı, interface implement etmek GEREKMEZ
- Static handler tercih edilir (allocation yok)
- Return değerleri cascading message olarak otomatik publish edilir

**Handler örneği:**
```csharp
public static class CreateOrderHandler
{
    public static OrderCreated Handle(CreateOrder command, IDocumentSession session)
    {
        var order = new Order { Id = command.OrderId };
        session.Store(order);
        return new OrderCreated(command.OrderId); // cascading message
    }
}
```

**HTTP endpoint örneği:**
```csharp
public static class OrderEndpoint
{
    [WolverinePost("/orders")]
    public static (OrderResponse, OrderCreated) Post(CreateOrder command, IDocumentSession session)
    {
        var order = new Order { Name = command.Name };
        session.Store(order);
        return (new OrderResponse(order.Id), new OrderCreated(order.Id));
    }
}
```

**Kritik kurallar:**
- Modüller arası iletişimde `PublishAsync()` kullan, ASLA `InvokeAsync()` kullanma
- `InvokeAsync()` sadece aynı modül içinde senkron çağrılarda kullanılır
- Handler'dan handler'a doğrudan `InvokeAsync()` çağırma, cascading message veya `PublishAsync()` kullan
- Durable local queue'ları aktif et: `opts.Policies.UseDurableLocalQueues()`

### Marten (MartenDB)

PostgreSQL üzerinde document database ve event store olarak çalışır. Entity Framework KULLANILMAZ.

**Temel NuGet paketi:** `Marten`

**Document storage:**
- .NET nesneleri PostgreSQL'e JSON olarak serialize edilir
- LINQ ile sorgulama desteklenir
- Her modül kendi PostgreSQL schema'sına sahiptir

**Event sourcing — Decider pattern:**
```csharp
[AggregateHandler]
public static IEnumerable<object> Handle(MarkItemReady command, Order order)
{
    if (order.Items.TryGetValue(command.ItemName, out var item))
    {
        item.Ready = true;
        yield return new ItemReady(command.ItemName);
    }
}
```

**Self-aggregating pattern:**
```csharp
public sealed record Order(Guid Id, Dictionary<string, Item> Items)
{
    public static Order Create(OrderCreated e) => new(e.OrderId, new());
    public Order Apply(ItemAdded e) => this with { Items = ... };
}
```

**Projection tipleri:**
- `Inline` — aynı transaction'da güncellenir (strong consistency)
- `Async` — background daemon ile eventual consistency
- `Live` — on-demand, persist edilmez

**Multi-tenant yapı:**
- `opts.Policies.AllDocumentsAreMultiTenanted()` ile conjoined tenancy
- Session açarken tenant belirtilir: `store.LightweightSession("tenantId")`

**Modül başına schema ayrımı:**
```csharp
// Her modül kendi schema'sını ConfigureMarten ile kaydeder
services.ConfigureMarten(opts =>
{
    opts.Schema.For<Invoice>().DatabaseSchemaName("billing");
});
```

### QuestPDF

PDF rapor üretimi için kullanılır. Reporting modülüne özeldir.

**NuGet paketi:** `QuestPDF`

**Temel kullanım:**
```csharp
Document.Create(container =>
{
    container.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.Margin(2, Unit.Centimetre);

        page.Header().Text("Rapor Başlığı").FontSize(20).Bold();

        page.Content().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
            });

            // Header row
            table.Header(header =>
            {
                header.Cell().Text("Ad");
                header.Cell().Text("Tarih");
                header.Cell().Text("Durum");
            });

            // Data rows
            foreach (var item in data)
            {
                table.Cell().Text(item.Name);
                table.Cell().Text(item.Date.ToString("dd.MM.yyyy"));
                table.Cell().Text(item.Status);
            }
        });

        page.Footer().AlignCenter().Text(x =>
        {
            x.Span("Sayfa ");
            x.CurrentPageNumber();
            x.Span(" / ");
            x.TotalPages();
        });
    });
}).GeneratePdf(stream);
```

**Kurallar:**
- QuestPDF SADECE Reporting modülünde kullanılır
- Rapor verileri her zaman denormalize document'lardan okunur, başka modülün DB'sine sorgu atılmaz
- Fluent API ile composition tercih edilir
- Tekrar eden rapor bileşenleri (header, footer, tablo stilleri) component olarak extract edilir

## Mimari Kurallar

### Modüler Monolit

- Her modül kendi bounded context'idir ve microservice'e geçişe hazır olmalıdır
- Modüller arası iletişim SADECE asenkron mesajlaşma (Wolverine events) ile yapılır
- Modüller arası doğrudan referans YASAKTIR (Shared projeler hariç)
- Her modülün kendi PostgreSQL schema'sı vardır — **şema izolasyonu** modüler monolit mimarisinin temel taşıdır
- Wolverine message storage paylaşımlıdır: `opts.Durability.MessageStorageSchemaName = "wolverine"`
- Marten event stream'leri varsayılan schema'da (`shared`) tutulur — inline snapshot document'ları modül schema'sında

#### Şema İzolasyonu (Schema Isolation)

Her modül kendi PostgreSQL schema'sına sahiptir. Bu izolasyon, modüllerin bağımsız deploy edilebilirliğini ve microservice'e geçiş yolunu garanti eder.

**Schema yapısı:**

- `shared` — Marten varsayılan schema (event stream'ler: `mt_streams`, `mt_events`)
- `wolverine` — Wolverine durable messaging tabloları (paylaşımlı)
- `institution` — Institution modülü document'ları
- `business` — Business modülü document'ları
- `enrollment` — Enrollment modülü document'ları
- `coordination` — Coordination modülü document'ları + event sourcing snapshot'ları
- `contract`, `attendance`, `payment`, `reporting` — diğer modüller

**Konfigürasyon kuralları:**

```csharp
// Her modülün MartenConfiguration'ında:
options.Schema.For<MyDocument>().DatabaseSchemaName("mymodule");

// Event sourcing aggregate inline snapshot'ları da modül schema'sında:
options.Projections.Snapshot<MyAggregate>(SnapshotLifecycle.Inline);
options.Schema.For<MyAggregate>().DatabaseSchemaName("mymodule");
```

**KESİN KURALLAR:**

- Bir modül ASLA başka modülün schema'sındaki tablolara doğrudan SQL sorgusu atamaz
- Cross-module veri okuma yöntemleri: (1) Event-based read model (consumer), (2) Frontend enrichment (lookup map)
- `AutoCreate.All` ile schema'lar development'ta otomatik oluşturulur

#### KESİN YASAK: Modüller Arası Doğrudan Veri Yazma

Bir modülün Application katmanı, başka bir modülün Core veya ReadModel'ine ASLA doğrudan yazamaz.

**YANLIŞ — Mimariyi BOZAR:**

```csharp
// Business.Application → Enrollment.Core.ReadModels — KESİNLİKLE YASAK
using MESNET.Enrollment.Core.ReadModels;

session.Store(new BusinessProfileView { ... }); // YASAK: başka modülün document'ı
```

**DOĞRU — Olay yayınla, ilgili modül kendi view'ını oluştursun:**

```csharp
// Business.Application sadece kendi entity'sini yazar + event yayınlar
session.Store(business);
await bus.PublishAsync(new BusinessRegistered(business.Id, ...));

// Enrollment.Application/Consumers/BusinessRegisteredConsumer.cs — kendi schema'sına yazar
public static void Consume(BusinessRegistered @event, IDocumentSession session)
{
    session.Store(new BusinessProfileView { Id = @event.BusinessId, ... });
}
```

#### Asenkron Eventlerin Timing Sorunu (Seeder / Test Bağlamı)

`PublishAsync()` mesajı Wolverine durable local queue'a koyar — consumer **asenkron** işler, anında çalışmaz.
Seeder veya entegrasyon testlerinde sıralı API çağrıları arasında gecikme gerekebilir.
**Çözüm: Seeder'da `await Task.Delay(...)` ekle — API veya handler mantığını değiştirme.**

#### csproj Proje Referansı Kuralları

Her modülün `.Application.csproj`'u yalnızca şu referansları içerebilir:

- `MESNET.Common.Shared` — ortak altyapı
- `MESNET.Common.Infrastructure` — ortak altyapı
- `MESNET.{KendiModülü}.Core` — kendi domain modeli
- `MESNET.{KendiModülü}.Shared` — kendi paylaşılan event'leri
- `MESNET.{DiğerModül}.Shared` — başka modülün **yalnızca** Shared katmanı (event tüketimi için)

**YASAK referanslar (Application.csproj'da):**

- `MESNET.{DiğerModül}.Core` — başka modülün domain modeli
- `MESNET.{DiğerModül}.Application` — başka modülün handler'ları
- `MESNET.{DiğerModül}.Persistence` — başka modülün DB katmanı

### Modül Katman Yapısı

Her modül şu katmanlara sahiptir:
- `MESNET.{Module}.Core` — Domain entities, value objects, aggregate roots
- `MESNET.{Module}.Application` — Wolverine handler'ları (command/query), business logic
- `MESNET.{Module}.Api` — Wolverine HTTP endpoints
- `MESNET.{Module}.Persistence` — Marten document/event store konfigürasyonu
- `MESNET.{Module}.Shared` — Domain events (diğer modüllerin consume edebileceği)

### Endpoint — Handler Mimarisi (KESİN KURAL)

Tüm endpoint metodları istek işlemeyi **Wolverine handler'larına** devreder. Endpoint'ler ince bir HTTP adaptör katmanıdır; iş mantığı veya Marten erişimi içermez.

**Yazma (Command) endpoint'i şablonu:**

```csharp
private static async Task<IResult> Post(CreateBusiness command, IMessageBus bus)
{
    var id = await bus.InvokeAsync<Guid>(command);
    return Results.Created($"/api/businesses/{id}", ResponseBuilder.Success(201)
        .AddData(new { id }).Build());
}
```

**Okuma (Query) endpoint'i şablonu:**

```csharp
private static async Task<IResult> Get(Guid id, IMessageBus bus)
{
    var dto = await bus.InvokeAsync<BusinessDto>(new GetBusiness(id));
    return Results.Ok(ResponseBuilder.Success().AddData(dto).Build());
}
```

**Kesinlikle YASAK olan pattern'lar:**

- Endpoint metodunda `IDocumentSession` veya `IQuerySession` inject etmek
- Endpoint metodunda `session.Store()`, `session.LoadAsync()`, `session.Query()` çağırmak
- Endpoint metodunda business logic veya domain kural kontrolü yapmak
- Endpoint metodunda doğrudan `bus.PublishAsync()` ile event yayınlamak (bu handler'ın işi)

**İzin verilen tek istisna:**

- `ICurrentUserService` — token'dan kullanıcı bilgisi okumak için endpoint'e inject edilebilir

### SmartEnum Kuralı (Domain Enum'ları)

Projede **normal C# enum KULLANILMAZ**. Tüm domain enum'ları `Ardalis.SmartEnum` ile tanımlanır. SmartEnum, zengin davranış (slug, geçiş kuralları, IsFinal vb.) ve Marten JSON serializasyonu için tercih edilir.

**Paket:** `Ardalis.SmartEnum`, `Ardalis.SmartEnum.SystemTextJson`

**Standart yapı:**
```csharp
public sealed class MyStatus : SmartEnum<MyStatus>
{
    public static readonly MyStatus Active = new(nameof(Active), 1, "Aktif");
    public static readonly MyStatus Closed = new(nameof(Closed), 2, "Kapalı");

    public string Slug { get; }  // Türkçe UI display

    private MyStatus(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }
}
```

**Kurallar:**

- `Name` = İngilizce (JSON serialize, backend iletişim)
- `Slug` = Türkçe (UI display, hata mesajları)
- Entity property'de `[JsonConverter(typeof(SmartEnumNameConverter<MyStatus, int>))]` attribute kullan
- Marten LINQ'te SmartEnum doğrudan kullanılamaz → duplicate primitive alan ekle (bkz. Marten SmartEnum LINQ Kuralları)
- Modüller arası event'lerde SmartEnum yerine `string` (Name değeri) gönder
- `SmartEnumJsonConverterFactory` Marten STJ serializer'a kayıtlı olmalı (bkz. Common.Shared)

### CQRS Kuralları

- Command handler'lar yazma işlemi yapar, event döndürür (`DomainException` fırlatır, `Result<T>` DÖNDÜRMEZ)
- Query handler'lar sadece okuma yapar, hiçbir side effect oluşturmaz
- Event sourcing kullanan aggregate'ler için Decider pattern (`[AggregateHandler]`) kullan
- Document-based entity'ler için Marten `IDocumentSession` (yazma) ve `IQuerySession` (okuma) kullan — SADECE handler içinde
- Handler'dan hata bildirimi: `throw new DomainException(error)` — HTTP 422 olarak döner

### Sayfalama (Pagination) Kuralları

- Tüm listeleme query endpoint'leri server-side pagination destekler
- Query record'lar `PagedQuery` base record'undan inherit eder (`Page`, `PageSize`, `SortBy`, `Descending`, `Search`)
- Query handler'lar `PagedResult<TDto>` döndürür — `IReadOnlyList<TDto>` DEĞİL
- Pagination/sorting/search → `QueryableExtensions` helper'ları ile (`ApplySort`, `ApplySearch`, `ToPagedResultAsync`)
- SmartEnum filtreleri **her zaman** `.Name` property'si ile LINQ'te yapılır — in-memory filtreleme YASAKTIR
- Tüm query endpoint'leri handler üzerinden çalışır — endpoint'te `IQuerySession` inject etmek YASAKTIR
- Frontend: `useServerPagination` composable + Quasar q-table `@request` event entegrasyonu

**Sayfalı query handler örneği:**

```csharp
public static async Task<PagedResult<BusinessDto>> Handle(
    GetBusinessesByStatus query, IQuerySession session)
{
    IQueryable<Business> q = session.Query<Business>();
    // filtreler...
    q = q.ApplySearch(query.Search, b => b.Name, b => b.Address);
    q = q.ApplySort(query.SortBy, query.Descending, defaultSort: b => b.Name);
    return await q.ToPagedResultAsync(query, b => b.ToDto());
}
```

**Sayfalı endpoint örneği:**

```csharp
private static async Task<IResult> GetAll(
    string? status, int page = 1, int pageSize = 20,
    string? sortBy = null, bool descending = false, string? search = null,
    IMessageBus bus = default!)
{
    var result = await bus.InvokeAsync<PagedResult<BusinessDto>>(
        new GetBusinessesByStatus(status)
        { Page = page, PageSize = pageSize, SortBy = sortBy, Descending = descending, Search = search });
    return Results.Ok(ResponseBuilder.Success().AddData(result).Build());
}
```

### Geçmiş Dönem Kuralı (Salt Okunur Mod)

Akademik dönem "Kapalı" (Closed) durumdaysa, o döneme ait tüm veriler **salt okunur** olmalıdır. Hiçbir yazma/düzenleme/silme işlemi yapılamaz:

- Ders programı düzenlenemez ve kaydedilemez
- Koordinasyon ataması yapılamaz
- Sözleşme oluşturulamaz/değiştirilemez
- Devamsızlık kaydı girilemez
- Maaş/dekont işlemi yapılamaz

**Frontend:** `periodStore.isReadOnly` computed değeri kullanılır — `true` ise tüm yazma butonları/formları `disable` edilir.

**Backend:** Command handler'lar `AcademicPeriod.Status == Closed` kontrolü yaparak `DomainException(CoordinationErrors.AcademicPeriodClosed(id))` fırlatır.

### Marten SmartEnum LINQ Kuralları

SmartEnum property'leri Marten LINQ sorgularında **doğrudan kullanılamaz** — iki ayrı sorun vardır:

1. **Doğrudan karşılaştırma yasak:** `s.Semester == semester` → `BadLinqExpressionException`
2. **Nested path tuzağı:** `s.Semester.Name` → Marten bunu `data->'semester'->>'Name'` olarak çevirir. Ancak SmartEnum JSON'da string olarak serialize edilir (`"Spring"`), obje değil. Bu yüzden nested path **her zaman NULL döner** ve sorgu sonuç bulamaz.

**Çözüm:** Aggregate/entity'ye duplicate primitive alanlar ekle ve LINQ'te bunları kullan:

```csharp
public sealed record MyAggregate(
    AcademicSemester Semester,  // SmartEnum — serialize/deserialize için
    string SemesterName,         // Düz string — LINQ sorguları için
    int SemesterNumber           // Düz int — sayısal karşılaştırma için
);

// LINQ'te:
session.Query<MyAggregate>().Where(s => s.SemesterNumber == semester.Number); // ✅
session.Query<MyAggregate>().Where(s => s.SemesterName == semester.Name);     // ✅
session.Query<MyAggregate>().Where(s => s.Semester.Name == semester.Name);    // ❌ NULL döner
session.Query<MyAggregate>().Where(s => s.Semester == semester);              // ❌ Exception
```

1. **Select projection'da da aynı tuzak geçerli:** `.Select(s => new { StatusName = s.Status.Name })` da `data->'Status'->>'Name'` üretir ve NULL döner. SmartEnum alanını Select projection'da kullanma — entity'yi tamamen çekip in-memory filtrele/projekte et.

### Marten Composite Index İsimlendirme

PostgreSQL identifier sınırı 64 karakterdir. Marten composite index'lerde otomatik isim üretir (`mt_doc_{table}_uidx_{col1}{col2}...`) ve bu isim kolayca sınırı aşar → `PostgresqlIdentifierTooLongException`.

**Çözüm:** Composite index tanımlarken her zaman kısa isim ver:

```csharp
// ❌ YANLIŞ — otomatik isim 64 karakteri aşabilir
options.Schema.For<MyDoc>()
    .Index(x => new { x.InstitutionId, x.BranchCode, x.AcademicPeriodId },
        x => x.IsUnique = true);

// ✅ DOĞRU — kısa isim ver
options.Schema.For<MyDoc>()
    .Index(x => new { x.InstitutionId, x.BranchCode, x.AcademicPeriodId },
        x =>
        {
            x.IsUnique = true;
            x.Name = "idx_mydoc_inst_branch_period";
        });
```

### Event Sourcing vs Document Storage

- **Event sourcing kullan:** Staj sözleşmeleri, fesih süreçleri, devamsızlık kayıtları, dekont onay süreçleri gibi durum geçişleri olan entity'ler
- **Document storage kullan:** İşletme bilgileri, öğrenci profilleri, kurum bilgileri gibi CRUD ağırlıklı entity'ler

### Wolverine Konfigürasyon Şablonu

```csharp
builder.Services.AddMarten(opts =>
{
    opts.Connection(connectionString);
    opts.DatabaseSchemaName = "shared";
})
.IntegrateWithWolverine()
.AddAsyncDaemon(DaemonMode.HotCold);

builder.Host.UseWolverine(opts =>
{
    opts.MultipleHandlerBehavior = MultipleHandlerBehavior.Separated;
    opts.Durability.MessageIdentity = MessageIdentity.IdAndDestination;
    opts.Durability.MessageStorageSchemaName = "wolverine";
    opts.Policies.AutoApplyTransactions();
    opts.Policies.UseDurableLocalQueues();
});
```

## Vue 3 / Quasar / Pinia Frontend Kuralları

### Composable Extraction (Zorunlu)

Bir Vue bileşeninin `<script setup>` bloğu **300 satırı** aştığında veya **3'ten fazla bağımsız ilgi alanı** (concern) içerdiğinde, mantıksal birimler `src/composables/` altında ayrı composable fonksiyonlarına taşınmalıdır.

**Composable isimlendirme:** `use{İşlevAdı}.ts` — örneğin `useWorkloadConfig.ts`, `useClusterMap.ts`

**Composable yapısı:**
```typescript
// src/composables/useFeatureName.ts
import { ref, computed, type Ref, type ComputedRef } from 'vue'

export interface UseFeatureNameOptions {
  someRef: Ref<string | null>
  notify: ReturnType<typeof useNotify>
}

export function useFeatureName(options: UseFeatureNameOptions) {
  const { someRef, notify } = options

  const loading = ref(false)
  const data = ref<SomeType | null>(null)

  const derivedValue = computed(() => /* ... */)

  async function loadData() { /* ... */ }

  return { loading, data, derivedValue, loadData }
}
```

**Kurallar:**
- Composable'lar dışarıdan `Ref` veya `ComputedRef` alır — store/service'e doğrudan erişmek yerine parametre olarak alır (test edilebilirlik)
- Her composable kendi state'ini (`ref`), türetilmiş değerlerini (`computed`) ve aksiyonlarını (async fonksiyonlar) döndürür
- Sayfada composable return değerleri destructure edilerek template'e expose edilir
- Pure fonksiyonlar composable dışında `export function` olarak tanımlanabilir (ör. `estimateGroupCount`)

### Bileşen Kuralları

- **Inline `defineComponent` + `h()` render fonksiyonu YASAK** — her bileşen kendi `.vue` SFC dosyasında `<script setup>` ile tanımlanmalıdır
- **`<script setup>` zorunlu** — Options API veya `setup()` fonksiyonu KULLANILMAZ
- **Props:** `defineProps<{ ... }>()` ile TypeScript type-based props kullanılır, runtime `props:` objesi değil
- **Emit:** `defineEmits<{ ... }>()` ile TypeScript type-based emits
- **İkon butonu = `aria-label` + `<q-tooltip>`:** Yalnızca ikon içeren (label'sız) her `q-btn`, hem ekran okuyucu için `aria-label` hem görsel ipucu için `<q-tooltip>` içermelidir. `title` attribute'ü **KULLANILMAZ** (WCAG için güvenilir değil, görsel tooltip standart açılmaz). Özellikle tablo satır aksiyonları (göz/kalem/sil vb.) bu kurala uyar.
- **Boş-durum nötr olmalı:** Liste/tablo boş durumu bir hata değildir — uyarı (⚠) ikonu yerine nötr ikon (ör. ilgili belge/kayıt ikonu) + mümkünse eylem çağrısı (CTA) gösterilir. Bağlam-bağımlı seçicilerde (önce alan seç → öğretmen) boş-durum metni bağlama göre değişir.
- **Oluştur/Düzenle formları = ayrı SAYFA (route), modal/panel DEĞİL:** Bir entity'nin yeni-kayıt ve düzenleme formları ayrı route sayfasıdır (`pages/.../XFormPage.vue`, route `/entity/new` + `/entity/:id/edit`, route `meta: { formRoute: true }` → MainLayout yönlü kayma geçişi: forma giriş `slide-left`, çıkış `slide-right`). Liste/tetikleyici `router.push` ile gider; sayfa kaydedince `router.push('/entity')` ile listeye döner; edit modunda `getX(id)` ile yükler. Örnek: BusinessFormPage, StudentFormPage, ContractFormPage, AttendanceFormPage, InstitutionFormPage. Sekmeli ayar sayfalarında (Institution) yalnız ANA düzenleme route'tur; sekme-içi alt formlar panelde kalır.
- **Kısa aksiyon formları = `FormDialog` (sağdan kayan yan-panel):** Reddet/imzala/fesih/askı/belge-yükle/yerleştir/sil-onayı/düzeltme gibi tek-amaçlı/bağlamsal aksiyonlar `FormDialog` ile sağdan kayan side-sheet olarak açılır (route değil). `FormDialog` içi `q-dialog position="right"` — merkezî modal KULLANILMAZ.

### Reaktivite Kuralları

- `<script setup>` içinde mutable state **her zaman** `ref()` ile tanımlanır — düz `let` değişken YASAK
  - ✅ `const pendingId = ref<string | null>(null)` → `pendingId.value = newId`
  - ❌ `let pendingId: string | null = null` → `pendingId = newId`
- Deep clone için `structuredClone()` kullanılır — `JSON.parse(JSON.stringify())` YASAK
- Fire-and-forget async çağrılarda `.catch(() => {})` eklenir — `void fn()` hata yutabilir
  - ✅ `loadData().catch(() => {})`
  - ❌ `void loadData()`

### Pinia Store Kuralları

- **Setup store** (Composition API) tercih edilir — Options store değil
- Store'dan destructure edilen ref'ler reaktivitelerini korur (`storeToRefs` gerekmez — `<script setup>` zaten unwrap eder)

## Kullanıcı Arayüzü Dili

- Frontend kullanıcı arayüzü **Türkçe** olmalıdır — tüm label, buton, mesaj ve placeholder'lar Türkçe yazılır
- Türkçe karakterler (ç, ş, ğ, ü, ö, ı, İ) doğru kullanılmalıdır — ASCII yaklaşık karakter KULLANILMAZ
  - ✅ "Öğretmen", "Dönem", "Boş", "Salı", "Çarşamba", "Perşembe", "İptal", "Düzenle", "Sonuç bulunamadı"
  - ❌ "Ogretmen", "Donem", "Bos", "Sali", "Carsamba", "Persembe", "Iptal", "Duzenle", "Sonuc bulunamadi"
- Backend enum/value isimleri İngilizce kalır (`Fall`, `Spring`, `Monday`, `Occupied`, `Free`) — frontend'de Türkçe karşılıkları gösterilir
- SmartEnum pattern'ında `Name` = İngilizce (serialize), `Slug` = Türkçe (UI display)
- MEB terminolojisi kullanılır: "1. Dönem" / "2. Dönem" (Güz/Bahar değil)

## Kapsam

### Phase 1 (Aktif) — Çekirdek Staj Süreçleri

- Sözleşme yönetimi, devamsızlık takibi, dekont/maaş süreçleri
- Staj fesih işlemleri, yeni işletmeye yerleşme
- Lokasyon bazlı işletme yönetimi
- Staj yaşam döngüsü orkestrasyonu (Internship saga)
- Raporlama ve PDF üretimi (Reporting + QuestPDF)
- Detaylar: `src/Docs/docs/architecture/project-scope.md`

### Phase 2 (Beklemede) — Blockchain/NFT
- Blockchain, NFT sertifika, smart contract, Web3 cüzdan
- Phase 1 tamamlandıktan sonra ele alınacak

## Dosya Yapısı Referansları

- Mimari dokümanlar: `src/Docs/docs/architecture/`
- Modül tasarımı: `src/Docs/docs/architecture/module-design.md`
- Proje kapsamı: `src/Docs/docs/architecture/project-scope.md`
- İş kuralları: `src/Docs/docs/architecture/business-rules.md`
- Senaryolar: `src/Docs/docs/scenarios.md`
- Aktörler: `src/Docs/docs/actors/actors.md`
- İzinler: `src/Docs/docs/actors/permissions.md`
- C4 diyagramları: `src/Docs/static/diagrams/c4/`
- PlantUML diyagramları: `src/Docs/static/diagrams/modules/`
- Modüller: `src/Modules/`
- Frontend: `src/WebUI/`
- Ana API: `src/MESNET.Presentation/`

## Sürümleme

Tam kural: `VERSIONING.md`. Özet: SemVer `vMAJOR.MINOR.PATCH` + **minör parite kanalı** —
**tek minör = ön-sürüm (pre-release)** (`v0.1.0`, `v0.3.0`), **çift minör = kararlı sürüm**
(`v0.2.0`, `v0.4.0`). Akış: geliştirme `dev`'de → `dev → main` **PR ile** birleşir → tag
`vX.Y.Z` **main**'de açılır → GitHub Release (tek minörde `--prerelease`). Tag push'u, imajları
(API/WebUI/nginx/Docs) GHCR'ye **private** push eden CI'ı tetikler (public yayınlanmaz).

## Yetkilendirme (KESİN KURAL)

**Tüm yetkilendirme permission bazlıdır, rol bazlı DEĞİLDİR.** Roller yalnızca bir
permission demetine verilen isimdir; erişim kararı her zaman permission'a bakar.

- Uç noktalar `RequireAuthorization(Permissions.X.Y)` ile korunur — `RequireRole` KULLANILMAZ
- Handler içinde karar gerekiyorsa `ICurrentUserService.HasPermission(...)` kullanılır
- Frontend'de buton/menü görünürlüğü permission'a bakar, rol adına değil
- Rol → permission eşleşmesi tek yerde: `src/MESNET.Common.Shared/Security/RolePermissionMap.cs`
  (wildcard destekli, ör. `department:*`). Kaynak doküman: `src/Docs/docs/actors/permissions.md`
- Yeni bir yetki gerektiğinde **yeni permission** tanımlanır ve ilgili rollerin listesine
  eklenir; koda rol adı gömülmez

**Aynı permission'a sahip roller aynı işi yapabilir.** Örnek: işletme koordinatörlük saati
takdiri `department:distribution:manage` ister; bu izin `InstitutionManager` (okul müdürü),
`InstitutionStaff` (müdür yardımcısı) ve `DepartmentHead` (alan şefi) rollerinin hepsinde
`department:*` ile bulunur — üçü de tam yetkilidir.

**Permission erişimi açar, KAPSAMI belirlemez.** "Hangi kurumun/alanın verisi" sorusu ayrı bir
kontroldür ve iki kapsam da token claim'inden okunur, istekten ALINMAZ:

- **Kurum kapsamı:** `institution_id` claim'i
- **Alan (branş) kapsamı:** `branch_codes` claim'i — liste (#126)

### Alan (branş) kapsamı kuralları (#126)

- Alan bilgisi **kayıt sırasında** girilir (`CreateUser.BranchCodes`), sistem türetmez.
  Değişiklik: `ChangeUserBranches` (`POST /api/security/users/{id}/branches`)
- Kapsam kararı saf `BranchScopePolicy.CanWrite(...)` içindedir; koordinasyon **yazma**
  handler'ları `BranchScopeGuard.EnsureCanWrite(...)` çağırır → ihlalde `DomainException` (422)
- **Karar sırası: önce muafiyet, sonra liste.** Muafiyet izni
  `institution:distribution:all-branches` varsa alan listesine HİÇ bakılmaz
- **Boş `branch_codes` hata değildir.** Okul müdürü ve müdür yardımcısı hiçbir alana bağlı
  değildir; doğrulama hatası üretilmez, uyarı gösterilmez. Yalnız muafiyeti olmayan
  kullanıcıyı (branşı girilmemiş alan şefi) kısıtlar
- **Muafiyet izni `department:` önekiyle adlandırılamaz** — üç rolün de `department:*`
  wildcard'ı vardır, o önekteki izin alan şefine de geçer ve kontrol sessizce hiç çalışmaz.
  Kilitleyen test: `tests/MESNET.Coordination.UnitTests/BranchScopeExemptionMappingTests.cs`
- **Okuma açık, yazma kapalı:** alan şefi başka alanın dağıtımını görebilir, değiştiremez.
  Satır bazlı uçlarda kapsam istekten değil **çözümlenmiş satırdan** okunur
- Alan zorunluluğu permission'dan türetilir (`BranchRequirement`), rol adından DEĞİL

Ayrıntı: `src/Docs/docs/actors/permissions.md` → "Alan (Branş) Kapsamı Kontrolü"

### Bu kuralın bilinen istisnaları (teknik borç)

Aşağıdaki iki nokta veri kapsamı kararını rol adına bakarak veriyor; permission'a taşınmalıdır:

- `src/Modules/Attendance/MESNET.Attendance.Application/Handlers/MarkAttendanceHandler.cs:55`
- `src/Modules/Enrollment/MESNET.Enrollment.Application/Handlers/PlacementQueryScope.cs:23-34`

(`src/WebUI/src/stores/auth.ts` kapsam kararı artık `canManageAllBranches` /
`writableBranchCodes` ile permission bazlıdır; `isDepartmentHead` yalnız kapsam DIŞI
görünürlük için kalmıştır.)
