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
