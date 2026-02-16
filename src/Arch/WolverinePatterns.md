# Wolverine Patterns — MESNET Uygulama Notları

Bu doküman, MESNET projesinde kullanılan ve gelecekte kullanılabilecek Wolverine pattern'lerini içerir.

## 📋 İçindekiler

1. [Tuple Pattern (Cascading Messages)](#tuple-pattern-cascading-messages)
2. [FluentValidation Entegrasyonu](#fluentvalidation-entegrasyonu)
3. [Multi-Tenancy](#multi-tenancy)
4. [Saga Pattern](#saga-pattern)
5. [Event Sourcing + Marten](#event-sourcing--marten)

---

## Tuple Pattern (Cascading Messages)

### Sorun
`Result<T>` pattern ile handler'ların döndürdüğü event'ler cascading message olarak publish edilmiyordu:

```csharp
// ❌ YANLIŞ: Event cascade edilmiyor
public static Result<ContractCreated> Handle(CreateContract cmd) {
    var @event = new ContractCreated(...);
    return Result<ContractCreated>.Success(@event);
}

await bus.InvokeAsync<Result<ContractCreated>>(cmd);
// Wolverine sadece Result<ContractCreated> döndürür,
// içindeki ContractCreated event'ini cascade etmez!
```

**Etki:** Saga ve Reporting modülleri cross-module event'leri alamıyor → Modüler monolit bozuluyor.

### Çözüm: Tuple Pattern

Wolverine tuple'daki **her elemanı ayrı cascading message** olarak işler:

```csharp
// ✅ DOĞRU: Tuple pattern
public static (Result, ContractCreated) Handle(CreateContract cmd, IDocumentSession session) {
    var @event = new ContractCreated(...);
    session.Events.StartStream(contractId, @event);
    return (Result.Success(), @event);
}

// Endpoint
var (result, @event) = await bus.InvokeAsync<(Result, ContractCreated)>(command);
if (result.IsFailure) return Results.BadRequest(...);
return Results.Created($"/api/contracts/{@event.ContractId}", ...);
```

**Wolverine Davranışı:**
- `Result` → HTTP response (kimse subscribe değilse ignore)
- `ContractCreated` → **Cascading message** ✅ (Saga + Reporting dinleyebilir)

### Pattern Kuralları

1. **Handler Signature:**
   ```csharp
   // Nullable kullan (failure'da null döneriz)
   public static (Result, EventType?) Handle(...) { }
   ```

2. **Failure Returns:**
   ```csharp
   if (invalid)
       return (Result.Failure(error), null);
   ```

3. **Success Returns:**
   ```csharp
   return (Result.Success(), new EventCreated(...));
   ```

4. **Endpoint Destructuring:**
   ```csharp
   var (result, @event) = await bus.InvokeAsync<(Result, EventType)>(cmd);
   ```

### Aggregate Handler'lar

`[AggregateHandler]` ile aynı pattern:

```csharp
[AggregateHandler]
public static (Result, ContractActivated?) Handle(
    ActivateContract cmd,
    InternshipContract contract)
{
    if (!contract.Status.CanTransitionTo(ContractStatus.Active))
        return (Result.Failure(error), null);

    return (Result.Success(), new ContractActivated(...));
}
```

### Kaynak
- [Cascading Messages - Tuple Pattern](https://wolverinefx.net/guide/handlers/cascading.html#using-c-tuples-as-return-values)
- Commit: `9ea6884` (refactor: migrate to tuple pattern for event cascading)

---

## DataAnnotations Validation

### Basit Validation için

**Setup:**
```bash
dotnet add package WolverineFx.DataAnnotationsValidation
```

**Configuration:**
```csharp
builder.Host.UseWolverine(opts => {
    opts.UseDataAnnotationsValidation();
});
```

### Örnek Kullanım

```csharp
public record CreateContract(
    [property: Required] Guid StudentId,
    [property: Required] Guid BusinessId,
    [property: Required] DateTime StartDate,
    [property: Required] DateTime EndDate,
    [property: MinLength(10)] string? Notes
) : IValidatableObject
{
    // Custom validation
    public IEnumerable<ValidationResult> Validate(ValidationContext ctx)
    {
        if (EndDate <= StartDate)
            yield return new ValidationResult(
                "Bitiş tarihi başlangıçtan sonra olmalı",
                new[] { nameof(EndDate) }
            );
    }
}

// Custom Attribute
public class TcKimlikNoAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is not string tcNo || tcNo.Length != 11)
            return false;
        // TC Kimlik No algoritması
        return ValidateTcKimlikNo(tcNo);
    }
}
```

### Hata Yönetimi

**Default:** `DataAnnotationsValidation.ValidationException` fırlatılır ve mesaj discard edilir.

**Custom Failure Action:** FluentValidation ile aynı `IFailureAction<T>` interface.

### DataAnnotations vs FluentValidation

| Özellik | DataAnnotations | FluentValidation |
|---------|-----------------|------------------|
| **Setup** | Daha basit | Biraz daha verbose |
| **Validation Logic** | Attribute-based | Class-based |
| **Complex Rules** | Zor | Kolay |
| **Async Validation** | Sınırlı | Full support |
| **Reusability** | Attribute tekrar kullanılır | Validator sınıfı tekrar kullanılır |
| **MESNET Tercihi** | ❌ (Result pattern tercih edildi) | ⚠️ Phase 2'de değerlendirilebilir |

### Kaynak
- [DataAnnotations Validation](https://wolverinefx.net/guide/handlers/dataannotations-validation.html)

---

## FluentValidation Entegrasyonu

### Complex Validation için

**1. NuGet Package:**
```bash
dotnet add package WolverineFx.FluentValidation
```

**2. Wolverine Configuration:**
```csharp
builder.Host.UseWolverine(opts => {
    opts.UseFluentValidation(); // Auto-discovery
});
```

### Validator Örneği

```csharp
public class CreateContractValidator : AbstractValidator<CreateContract>
{
    public CreateContractValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.BusinessId).NotEmpty();
        RuleFor(x => x.StartDate)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Başlangıç tarihi gelecekte olmalı");
        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .WithMessage("Bitiş tarihi başlangıçtan sonra olmalı");
    }
}

public record CreateContract(
    Guid StudentId,
    Guid BusinessId,
    DateTime StartDate,
    DateTime EndDate);

public static class CreateContractHandler
{
    public static (Result, ContractCreated) Handle(
        CreateContract command,  // Validation otomatik çalışır!
        IDocumentSession session)
    {
        // Buraya sadece valid command'lar gelir
        var @event = new ContractCreated(...);
        return (Result.Success(), @event);
    }
}
```

### Hata Yönetimi

**Default Davranış:**
- Validation hatası → `ValidationException` fırlatılır
- Wolverine otomatik olarak mesajı discard eder

**Custom Failure Action:**
```csharp
public class CustomFailureAction<T> : IFailureAction<T>
{
    public void Throw(T message, IReadOnlyList<ValidationFailure> failures)
    {
        // Custom error handling
        throw new MyValidationException(failures);
    }
}

// DI Registration
opts.Services.AddSingleton(typeof(IFailureAction<>), typeof(CustomFailureAction<>));
```

### HTTP Endpoint Entegrasyonu

FluentValidation middleware **sadece handler'da** çalışır. HTTP endpoint'lerde **ProblemDetails** pattern kullanılabilir:

```csharp
public static ProblemDetails Validate(CreateContract cmd)
{
    if (cmd.StartDate < DateTime.UtcNow)
        return new ProblemDetails {
            Detail = "Başlangıç tarihi gelecekte olmalı",
            Status = 400
        };

    return WolverineContinue.NoProblems;
}

[WolverinePost("/api/contracts")]
public static (Result, ContractCreated) Handle(CreateContract cmd, ...)
{
    // Validation geçti, işleme devam
}
```

### Phase 2 için Notlar

- FluentValidation **Phase 1'de kullanılmadı** (Result pattern tercih edildi)
- **Phase 2'de değerlendirilebilir:**
  - Complex validation rules (cross-field, async DB checks)
  - Centralized validation logic
  - Automatic validation için handler'lar temizlenebilir

### Kaynak
- [FluentValidation Integration](https://wolverinefx.net/guide/handlers/fluent-validation.html)

---

## Multi-Tenancy

### Phase 2 Özelliği

**Not:** Multi-tenancy **Phase 2** kapsamında. Phase 1'de single institution (tek kurum) odaklı.

### Wolverine Multi-Tenancy Patterns

**1. Inline Invocation:**
```csharp
await bus.InvokeForTenantAsync("tenant1", new CreateContract(...));
var result = await bus.InvokeForTenantAsync<(Result, ContractCreated)>(
    "tenant2",
    new CreateContract(...)
);
```

**2. Publishing with DeliveryOptions:**
```csharp
await bus.PublishAsync(
    new ContractCreated(...),
    new DeliveryOptions { TenantId = "institution-123" }
);
```

**3. Cascading Messages:**
```csharp
// Handler'dan
yield return new ContractActivated(...).WithTenantId("tenant1");
yield return new NotifyStudent(...).WithDeliveryOptions(
    new DeliveryOptions {
        TenantId = "tenant2",
        ScheduleDelay = 5.Minutes()
    }
);
```

### Handler'da Tenant Resolution

```csharp
public static (Result, ContractCreated) Handle(
    CreateContract cmd,
    TenantId tenantId,  // Wolverine inject eder
    IDocumentSession session)
{
    Debug.WriteLine($"Institution: {tenantId.Value}");
    // ...
}
```

### Marten Entegrasyonu

**Conjoined Tenancy (Phase 2 için planlanan):**
```csharp
services.AddMarten(opts => {
    opts.Connection(connectionString);
    opts.Policies.AllDocumentsAreMultiTenanted();
});

// Session tenant ile açılır
var session = store.LightweightSession("institution-123");
```

### Phase 2 Implementasyon Planı

1. **Tenant Module Aktivasyonu** (`src/Modules/Tenant/`)
2. **Marten Conjoined Tenancy** konfigürasyonu
3. **Wolverine TenantId** metadata kullanımı
4. **Keycloak Organization** mapping
5. **HTTP Middleware** - tenant resolution (subdomain/header)

### Kaynak
- [Multi-Tenancy](https://wolverinefx.net/guide/handlers/multi-tenancy.html)
- [Marten Multi-Tenancy](https://martendb.io/documents/multi-tenancy.html)

---

## Saga Pattern

### MESNET'te Kullanım

Internship modülü **Wolverine Saga** olarak tasarlandı:
- Own domain data YOK
- Cross-module workflow orchestration
- Stateful coordination (ApprovalChain)

### Örnek: Internship Termination Saga

```csharp
public class InternshipSaga : Saga
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public InternshipPhase Phase { get; set; }
    public TerminationApprovalChain? ApprovalChain { get; set; }

    // Start saga
    public static (InternshipSaga, InternshipStarted) Start(StudentPlaced e)
    {
        var saga = new InternshipSaga {
            Id = Guid.NewGuid(),
            StudentId = e.StudentId,
            Phase = InternshipPhase.AwaitingContract
        };
        return (saga, new InternshipStarted(...));
    }

    // Handle cross-module events
    public void Handle(ContractActivated e)
    {
        ContractId = e.ContractId;
        Phase = InternshipPhase.Active;
    }

    // Start approval workflow
    public InternshipTerminationApprovalChainStarted Handle(
        InternshipTerminationRequested e)
    {
        Phase = InternshipPhase.TerminationInProgress;
        ApprovalChain = new TerminationApprovalChain();
        return new InternshipTerminationApprovalChainStarted(...);
    }

    // Track approvals
    public object? Handle(ApproveTerminationByTeacher e)
    {
        ApprovalChain = ApprovalChain! with { TeacherApproved = true };
        return CheckApprovalChainComplete();
    }

    private object? CheckApprovalChainComplete()
    {
        if (ApprovalChain!.IsComplete)
        {
            Phase = InternshipPhase.Terminated;
            return new InternshipTerminated(Id, StudentId, DateTime.UtcNow);
        }
        return null; // Henüz tamamlanmadı
    }
}
```

### Saga State Management

**Marten ile persistent:**
```csharp
services.AddMarten(opts => {
    opts.Connection(connectionString);
})
.IntegrateWithWolverine()
.AddAsyncDaemon(DaemonMode.HotCold);

builder.Host.UseWolverine(opts => {
    opts.Policies.AutoApplyTransactions();
    opts.Policies.UseDurableLocalQueues();
});
```

### Event Sourcing vs Saga State

- **Event Sourcing** (Contract, Attendance, Payment): Domain state transitions
- **Saga State**: Workflow coordination, geçici state (ApprovalChain)

### Saga Best Practices

1. **Stateless handler'lar:** Saga state'i method parametresi olarak al
2. **Return null:** Workflow devam ediyorsa `null` döndür
3. **Timeout handling:** Saga timeout policy tanımla
4. **Compensation:** Rollback için compensating events
5. **Idempotency:** Event'leri tekrar işleyebilir yap

### Kaynak
- [Wolverine Sagas](https://wolverinefx.net/guide/durability/sagas.html)
- [Saga with Marten](https://wolverinefx.net/guide/durability/marten/sagas.html)
- Dosya: `src/Modules/Internship/MESNET.Internship.Application/Sagas/InternshipSaga.cs`

---

## Event Sourcing + Marten

### MESNET'te Kullanım

**Event Sourced Aggregates:**
- Contract (InternshipContract)
- Attendance (AttendanceRecord)
- Payment (SalaryPeriod)

**Document Storage:**
- Business (BusinessProfile)
- Enrollment (StudentEnrollment)
- Institution (InstitutionProfile)

### Aggregate Pattern: Decider

```csharp
[AggregateHandler]
public static (Result, ContractActivated?) Handle(
    ActivateContract cmd,
    InternshipContract contract)  // Marten aggregate state
{
    if (!contract.Status.CanTransitionTo(ContractStatus.Active))
        return (Result.Failure(error), null);

    if (!contract.AllSignaturesComplete)
        return (Result.Failure(error), null);

    return (Result.Success(), new ContractActivated(...));
}
```

**Wolverine Workflow:**
1. Aggregate state'i fetch et (`IQuerySession`)
2. Business rules validate et
3. Event döndür
4. Wolverine event'i stream'e append eder
5. Transaction commit
6. Event cascade edilir (tuple pattern)

### Self-Aggregating Pattern

```csharp
public sealed record InternshipContract(
    Guid Id,
    ContractStatus Status,
    SignatureSet Signatures)
{
    // Factory (initial event)
    public static InternshipContract Create(ContractCreated e) =>
        new(e.ContractId, ContractStatus.Draft, new SignatureSet());

    // Apply (subsequent events)
    public InternshipContract Apply(ContractSignedByInstitution e) =>
        this with {
            Signatures = Signatures with { InstitutionSignature = new(...) }
        };

    public InternshipContract Apply(ContractActivated e) =>
        this with { Status = ContractStatus.Active };
}
```

### Projection Types

**1. Inline (Strong Consistency):**
```csharp
public class ContractSummaryProjection : SingleStreamProjection<ContractSummary>
{
    public ContractSummary Create(ContractCreated e) => new() {
        ContractId = e.ContractId,
        StudentId = e.StudentId
    };

    public void Apply(ContractActivated e, ContractSummary summary) {
        summary.Status = "Active";
        summary.ActivatedAt = e.ActivatedAt;
    }
}

// Registration
opts.Projections.Add<ContractSummaryProjection>(ProjectionLifecycle.Inline);
```

**2. Async (Eventual Consistency):**
```csharp
opts.Projections.Add<ContractSummaryProjection>(ProjectionLifecycle.Async);
```

**3. Live (On-Demand, No Persist):**
```csharp
var summary = await session.Events.AggregateStreamAsync<ContractSummary>(contractId);
```

### Event Store Schema

```sql
-- Her modül kendi schema'sında
CREATE SCHEMA contract;
CREATE SCHEMA attendance;
CREATE SCHEMA payment;

-- Marten event tables (her schema'da)
contract.mt_events
contract.mt_streams

-- Shared Wolverine messaging
wolverine.wolverine_incoming_messages
wolverine.wolverine_outgoing_messages
```

### Kaynak
- [Marten Event Sourcing](https://martendb.io/events/)
- [Wolverine + Marten Integration](https://wolverinefx.net/guide/durability/marten/event-sourcing.html)
- `src/Arch/ModuleDesign.md`

---

## Öğrenilen Pattern'ler Özeti

| Pattern | Kullanım | MESNET Phase |
|---------|----------|--------------|
| **Tuple Pattern** | Event cascading + Result<T> | Phase 1 ✅ |
| **FluentValidation** | Automatic validation | Phase 2 🔮 |
| **Multi-Tenancy** | Institution isolation | Phase 2 🔮 |
| **Saga** | Workflow orchestration | Phase 1 ✅ |
| **Event Sourcing** | State transitions (Contract, Attendance, Payment) | Phase 1 ✅ |
| **Document Store** | CRUD entities (Business, Enrollment, Institution) | Phase 1 ✅ |
| **ProblemDetails** | HTTP validation | Phase 1 ✅ |

---

## Referanslar

- [Wolverine Documentation](https://wolverinefx.net/)
- [Marten Documentation](https://martendb.io/)
- [MESNET Architecture](../Arch/ModuleDesign.md)
- [Project Scope](../Arch/ProjectScope.md)

**Son Güncelleme:** 2026-02-16
**Proje:** MESNET Phase 1
**Commit:** 9ea6884
