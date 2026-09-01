# Denetim İzi (C parçası) — Uygulama Planı

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Her yazma komutunu tek bir genel Wolverine middleware'inde kaydeden, kiracı damgalı, kapsamla okunabilen ve 24 ay sonra temizlenen bir denetim izi kurmak.

**Architecture:** Yeni bir `Audit` modülü (Core / Application / Api / Persistence). Yakalama noktası tek: `opts.Policies.AddMiddleware(typeof(AuditMiddleware), chain => …)` ile `*.Commands` ad alanındaki her handler zincirine takılan genel middleware. Denetim yazması komutun işleminden **ayrı bir Marten oturumunda** yapılır ki reddedilen komutun izi geri alınmasın. Okuma A parçasındaki `InstitutionScopePolicy` yol önekini yeniden kullanır; yeni kapsam ekseni doğmaz.

**Tech Stack:** .NET 10, Wolverine 6.15.0, Marten 9, PostgreSQL, Ardalis.SmartEnum, Vue 3 + Quasar + Pinia, Vitest.

**Spec:** `docs/superpowers/specs/2026-08-28-denetim-izi-design.md`

---

## Global Constraints

Her görevin gereksinimleri bu bölümü **örtük olarak içerir**.

### Ölçülmüş Wolverine middleware sözleşmesi (spec'i DÜZELTİR)

Spec `OnException`'ı sonuç sınıflandırması için varsayıyordu. Wolverine 6.15.0 üzerinde çalışan bir ana bilgisayarla ölçüldü (28.08.2026); dördü de kanıtlanmış davranıştır:

1. **`AddMiddleware<T>` STATİK SINIF ALMAZ** — `CS0718: 'X': static types cannot be used as type arguments`. Genel middleware **`opts.Policies.AddMiddleware(typeof(AuditMiddleware), filter)`** ile kaydedilir (tip parametreli aşırı yükleme DEĞİL).
2. **`OnException` istisnayı YUTAR.** Rethrow edilmezse çağıran hiçbir istisna görmez — ölçüldü, `InvokeAsync` normal döndü. `DomainException` bu yolda kaybolsaydı HTTP 422 doğmaz, reddedilen komut **başarılı görünürdü**. Rethrow zorunludur: `System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex).Throw();`
3. **`OnException`, `Before`'un DÖNDÜRDÜĞÜ değeri GÖREMEZ.** `OnException(Exception ex, AuditContext ctx)` yazmak derlemeyi kırar: `CS0103: The name 'ctx' does not exist in the current context`. `OnException` yalnız `Exception`, `Wolverine.Envelope` ve DI servislerini alabilir.
4. **Çalışma sırası:** `Before` → [handler] → `After` (**yalnız başarıda**) → `Finally` (her zaman) → `OnException` (yalnız istisnada, `Finally`'den SONRA).

Bundan çıkan ve bu planda bağlayıcı olan şekil:

| Hook | Görevi |
|---|---|
| `Before(...)` | `AuditContext` kurar (aktör, komut, hedefler, kapsam, başlangıç zamanı). `Succeeded = false`. |
| `After(AuditContext ctx)` | `ctx.MarkSucceeded()` — başka bir şey yapmaz. |
| `FinallyAsync(AuditContext ctx, …)` | **YALNIZ `ctx.Succeeded` ise** `Succeeded` satırını yazar. Aksi hâlde hiç yazmaz. |
| `OnExceptionAsync(Exception ex, Envelope env, …)` | `Rejected`/`Failed` satırını yazar, **sonra istisnayı rethrow eder.** |

### Kiracılık

- Denetim oturumu **kiracıyı açıkça verir**: `store.LightweightSession(tenantId)`. Argümansız session açmak yasaktır (`DefaultTenantUsageDisabledException`).
- Kiracı kaynağı: `Wolverine.Envelope.TenantId`. Boşsa `MESNET.Common.Shared.Tenancy.TenantResolution.Platform` (`"platform"`) kullanılır — kiracısız yazma denemesi istisnaya döner ve iz kaybolurdu.
- `AuditEntry` → `DocumentTenancyMap` içinde `DocumentTenancy.Tenant`. Sınıflandırılmamış belge bırakılamaz; `DocumentTenancyDriftTests` kırılır.

### Marten

- Şema adı: `audit`.
- Composite/uzun indekslere **elle kısa ad** verilir (PostgreSQL 64 karakter sınırı): `idx_audit_occurred`, `idx_audit_actor`, `idx_audit_subject_path`.
- **SmartEnum LINQ tuzağı:** `Outcome` bir sorgu süzgecidir. SmartEnum JSON'a düz string yazılır, nesne değil; `data->'outcome'->>'Name'` **her zaman NULL** döner. Bu yüzden belgede saklanan alan **`OutcomeName` (düz `string`)**'dir; SmartEnum ondan hesaplanır ve `[JsonIgnore]` ile serialize edilmez — `Institution.NodeTypeName` / `Institution.NodeType` ile birebir aynı desen.

### Mimari sınırlar

- `MESNET.Audit.*` **hiçbir modülün** Core/Application/Persistence katmanını referans etmez. Yalnız `MESNET.Common.Shared` + `MESNET.Common.Infrastructure`.
- Endpoint metotları iş mantığı içermez, `IDocumentSession`/`IQuerySession` inject etmez; `IMessageBus` üzerinden handler'a devreder. Tek istisna `ICurrentUserService`.
- Anonim uç EKLENMEZ (`AnonymousEndpointDriftTests`).

### İzin

- Yeni önek **`audit:`**. `institution:` önekli bir izin `InstitutionManager`'ın `institution:*` wildcard'ı üzerinden istenmeden dağılır (ADR-0002 önek tuzağı).
- Uçlar `RequireAuthorization(Permissions.X.Y)` ile korunur; `RequireRole` KULLANILMAZ. Rol adına bakan kontrol yazılmaz.

### Arayüz

- Tüm kullanıcı metni **Türkçe**, doğru Türkçe karakterlerle (ç ş ğ ü ö ı İ). ASCII yaklaşımı yasak.
- `<script setup>` zorunlu; `defineProps<{}>()` tip tabanlı.
- Yalnız ikon içeren her `q-btn` hem `aria-label` hem `<q-tooltip>` taşır; `title` attribute KULLANILMAZ.
- Mutable state `ref()` ile; düz `let` yasak. Fire-and-forget çağrılara `.catch(() => {})`.

### Test disiplini

- **Her kilitleyen testin gerçekten kilitlediği KANITLANIR.** Testi yeşil görmek yetmez: korunan şeyi kasıtlı boz (satırı **sil** — yorum satırına almak yetmez, kaynak tarayan testler yorumu da okur), testin **kırmızıya döndüğünü** ve **hangi ismi verdiğini** rapora yaz, sonra geri al.
- Bu depoda dört kez "yeşil ama hiçbir şeyi korumayan kilit" ölçüldü (A parçası). Kanıt adımı isteğe bağlı değildir.
- Testler AAA (Arrange-Act-Assert) yapısında ve açıklayıcı Türkçe adlarla yazılır.

### Bilinen ve KABUL EDİLEN bedeller (bunları "düzeltmeye" çalışmayın)

1. **İz en-iyi-çabadır.** Denetim yazması patlarsa loglanır ve iş akışı devam eder.
2. **Yetki reddi (403) ize girmez.** ASP.NET yetkilendirme katmanı isteği handler'dan önce keser; middleware hiç çalışmaz.
3. **Komut gövdesi saklanmaz.** "Ne değişti" sorusu olay deposundan cevaplanır.
4. **`Consumers/` kaydedilmez.** Yalnız zinciri başlatan kullanıcı eylemi görünür.
5. **Türkçe komut etiketi kısmi bir sözlüktür**; eşleşmeyen komut ham tip adıyla görünür (bkz. Görev 3).

---

## Dosya Yapısı

**Yeni modül — `src/Modules/Audit/`**

| Dosya | Sorumluluk |
|---|---|
| `MESNET.Audit.Core/MESNET.Audit.Core.csproj` | Yalnız `Common.Shared` referansı |
| `MESNET.Audit.Core/Entities/AuditEntry.cs` | Marten belgesi |
| `MESNET.Audit.Core/Enums/AuditOutcome.cs` | SmartEnum (`Succeeded` / `Rejected` / `Failed`) |
| `MESNET.Audit.Core/Services/AuditTargetExtractor.cs` | Saf: komuttan hedef kimlikleri (önbellekli yansıma) |
| `MESNET.Audit.Core/Services/AuditCommandDescriptor.cs` | Saf: tip → kısa ad + modül adı |
| `MESNET.Audit.Core/Services/AuditCommandLabels.cs` | Türkçe etiket sözlüğü + ham ada düşme |
| `MESNET.Audit.Core/Services/AuditEntryFactory.cs` | Saf: girdilerden `AuditEntry` kurar (`CrossedTenantBoundary` dahil) |
| `MESNET.Audit.Application/Auditing/AuditContext.cs` | Middleware'in hook'lar arası taşıdığı **mutable** durum |
| `MESNET.Audit.Application/Auditing/AuditMiddleware.cs` | `Before` / `After` / `FinallyAsync` / `OnExceptionAsync` |
| `MESNET.Audit.Application/Auditing/IAuditWriter.cs` + `AuditWriter.cs` | Ayrı oturumda yazar, hatayı yutar ve loglar |
| `MESNET.Audit.Application/Auditing/AuditCommandFilter.cs` | Saf: bir tip kaydedilmeli mi (`*.Commands` konvansiyonu) |
| `MESNET.Audit.Application/Queries/GetAuditEntries.cs` | `PagedQuery` türevi |
| `MESNET.Audit.Application/Dtos/AuditEntryDto.cs` | |
| `MESNET.Audit.Application/Handlers/GetAuditEntriesHandler.cs` | Kapsam + süzgeç + sayfalama |
| `MESNET.Audit.Application/Services/AuditRetentionService.cs` | Günlük `BackgroundService` |
| `MESNET.Audit.Application/ServiceRegistration.cs` | `AddAuditModule()` |
| `MESNET.Audit.Api/AuditEndpoints.cs` | `GET /api/audit` |
| `MESNET.Audit.Persistence/AuditMartenConfig.cs` | Şema + indeksler |
| `MESNET.Audit.Persistence/ServiceRegistration.cs` | `AddAuditPersistence()` |

**Ortak katman — değişiklik**

| Dosya | Değişiklik |
|---|---|
| `src/MESNET.Common.Shared/Tenancy/DocumentTenancyMap.cs` | `["AuditEntry"] = Tenant` |
| `src/MESNET.Common.Shared/Security/Permissions.cs` | `Permissions.Audit` sınıfı |
| `src/MESNET.Common.Shared/Security/RolePermissionMap.cs` | `audit:view:institution` → `InstitutionManager`, `DeputyDirector` |
| `src/MESNET.Common.Shared/Security/AssignablePermissionScope.cs` | `audit:` öneki bireysel atanabilir domain listelerine EKLENMEZ |
| `src/MESNET.Common.Infrastructure/Tenancy/IInstitutionPathLookup.cs` + `InstitutionPathLookup.cs` | Kurum kimliğinden yol (önbellekli ham SQL) |
| `src/MESNET.Presentation/Program.cs` | Modül kaydı, middleware kaydı, uç kaydı |
| `MESNET.slnx` | 4 yeni proje + 1 yeni test projesi |

**Testler**

| Dosya | Neyi kilitler |
|---|---|
| `tests/MESNET.Audit.UnitTests/AuditTargetExtractorTests.cs` | Hedef çıkarımı |
| `tests/MESNET.Audit.UnitTests/AuditEntryFactoryTests.cs` | Sonuç eşlemesi + `CrossedTenantBoundary` |
| `tests/MESNET.Audit.UnitTests/AuditCommandFilterTests.cs` | Süzgeç konvansiyonu |
| `tests/MESNET.Audit.UnitTests/AuditMiddlewareContractTests.cs` | **Ret satırı kalır + istisna yayılır** (canlı Wolverine ana bilgisayarı) |
| `tests/MESNET.Audit.UnitTests/AuditCommandCoverageDriftTests.cs` | `Commands/` altındaki her tip süzgece takılır |
| `tests/MESNET.Security.UnitTests/AuditPermissionMappingTests.cs` | `audit:` öneki wildcard'la sızmaz |
| `src/WebUI/src/pages/audit/auditListQuery.ts` + `AuditLogPage.spec.ts` | Sunucu sözleşmesi |

**Ön yüz**

| Dosya | |
|---|---|
| `src/WebUI/src/api/audit.ts` | `auditApi.list` |
| `src/WebUI/src/pages/audit/auditListQuery.ts` | Sayfa VE testin okuduğu **tek** kaynak |
| `src/WebUI/src/pages/audit/AuditLogPage.vue` | |
| `src/WebUI/src/router/index.ts` | `/audit` rotası |
| `src/WebUI/src/composables/useNavigation.ts` | "Kurum Yönetimi → Son İşlemler" |

---

## Görev Sırası

Sıra bağlayıcıdır. 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9 → 10 → 11 → 12.

Görev 1 dört boş proje kurar ve derlenir; sonraki görevler bu projelerin içini doldurur. Hiçbir görev derlemeyi kırık bırakmaz.

---

### Task 1: Audit modül iskeleti + `AuditEntry` + `AuditOutcome`

Dört boş proje, belge tipi, SmartEnum, Marten yapılandırması ve kiracılık sınıflandırması. Bu görevin sonunda çözüm derlenir ve `DocumentTenancyDriftTests` yeşil kalır.

**Files:**
- Create: `src/Modules/Audit/MESNET.Audit.Core/MESNET.Audit.Core.csproj`
- Create: `src/Modules/Audit/MESNET.Audit.Core/Enums/AuditOutcome.cs`
- Create: `src/Modules/Audit/MESNET.Audit.Core/Entities/AuditEntry.cs`
- Create: `src/Modules/Audit/MESNET.Audit.Application/MESNET.Audit.Application.csproj`
- Create: `src/Modules/Audit/MESNET.Audit.Persistence/MESNET.Audit.Persistence.csproj`
- Create: `src/Modules/Audit/MESNET.Audit.Persistence/AuditMartenConfig.cs`
- Create: `src/Modules/Audit/MESNET.Audit.Persistence/ServiceRegistration.cs`
- Create: `src/Modules/Audit/MESNET.Audit.Api/MESNET.Audit.Api.csproj`
- Create: `tests/MESNET.Audit.UnitTests/MESNET.Audit.UnitTests.csproj`
- Modify: `MESNET.slnx`
- Modify: `src/MESNET.Common.Shared/Tenancy/DocumentTenancyMap.cs`

**Interfaces:**
- Consumes: `MESNET.Common.Shared.Tenancy.DocumentTenancy`
- Produces: `MESNET.Audit.Core.Entities.AuditEntry`, `MESNET.Audit.Core.Enums.AuditOutcome` (`Succeeded` / `Rejected` / `Failed`, `Resolve(string?)`)

- [ ] **Step 1: `MESNET.Audit.Core.csproj`**

`Directory.Build.props` `TargetFramework`/`Nullable`/`ImplicitUsings` değerlerini zaten verir; csproj `MESNET.Security.Core.csproj` kadar sade kalır.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="Ardalis.SmartEnum" />
    <ProjectReference Include="../../../MESNET.Common.Shared/MESNET.Common.Shared.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: `AuditOutcome` SmartEnum**

`src/Modules/Audit/MESNET.Audit.Core/Enums/AuditOutcome.cs`:

```csharp
using Ardalis.SmartEnum;

namespace MESNET.Audit.Core.Enums;

/// <summary>
/// Bir komutun denetim izindeki sonucu.
/// </summary>
/// <remarks>
/// <para><b>Üç değer, iki farklı soru:</b> <see cref="Rejected"/> "sistem çalıştı, kural
/// izin vermedi" der; <see cref="Failed"/> "sistem çalışmadı" der. Denetim okuyucusu için
/// bu ayrım load-bearing'dir: ilki bir davranış kaydı, ikincisi bir arıza kaydıdır.</para>
///
/// <para><b>Yetki reddi (403) burada YOKTUR</b> ve olamaz — ASP.NET yetkilendirme katmanı
/// isteği handler'dan önce keser, denetim middleware'i hiç çalışmaz. İzdeki
/// <see cref="Rejected"/> satırları yalnız <c>DomainException</c> kaynaklıdır (kurum kapsamı
/// ihlali dahil: o guard middleware'de çalışır ve yakalanır).</para>
/// </remarks>
public sealed class AuditOutcome : SmartEnum<AuditOutcome>
{
    public static readonly AuditOutcome Succeeded = new(nameof(Succeeded), 1, "Başarılı");
    public static readonly AuditOutcome Rejected = new(nameof(Rejected), 2, "Reddedildi");
    public static readonly AuditOutcome Failed = new(nameof(Failed), 3, "Hata");

    /// <summary>Türkçe arayüz etiketi.</summary>
    public string Slug { get; }

    private AuditOutcome(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }

    /// <summary>
    /// Saklanan düz metinden tipi çözer. <b>Tanınmayan ya da boş değer
    /// <see cref="Failed"/>'a düşer</b>: bilinmeyen bir sonucu "başarılı" saymak, denetim
    /// izinde en zararlı varsayılan olurdu.
    /// </summary>
    public static AuditOutcome Resolve(string? name)
        => !string.IsNullOrWhiteSpace(name) && TryFromName(name, out var outcome)
            ? outcome
            : Failed;
}
```

- [ ] **Step 3: `AuditEntry` belgesi**

`src/Modules/Audit/MESNET.Audit.Core/Entities/AuditEntry.cs`:

```csharp
using System.Text.Json.Serialization;
using MESNET.Audit.Core.Enums;

namespace MESNET.Audit.Core.Entities;

/// <summary>
/// Tek bir yazma komutunun denetim satırı.
/// </summary>
/// <remarks>
/// <para><b>Komut gövdesi SAKLANMAZ.</b> Gövdeler sağlık raporu, maaş ve öğrenci verisi
/// taşır; ize kopyalamak kiracı damgalı belgelerin dışında ikinci bir hassas veri kopyası
/// yaratırdı ve bir silme talebinde iki yerden silmek gerekirdi. "Ne değişti" sorusu olay
/// deposundan (<c>mt_events</c>) cevaplanır.</para>
///
/// <para><b><see cref="ActorName"/> bilinçli olarak denormalizedir.</b> Kullanıcı kaydı
/// silinse bile iz okunur kalmalıdır; ayrıca okuma anında ad çözmek modüller arası sorgu
/// demektir ve yasaktır.</para>
///
/// <para><b><see cref="ErrorCode"/> saklanır, hata MESAJI saklanmaz.</b>
/// <c>Error.Code</c> makine okunurdur ve sabittir; mesaj PII taşıyabilir (öğrenci adı,
/// ilçe adı).</para>
/// </remarks>
public sealed record AuditEntry
{
    public Guid Id { get; init; }

    public DateTimeOffset OccurredAt { get; init; }

    public Guid ActorId { get; init; }

    /// <summary>Aktörün o andaki adı — denormalize; kayıt silinse de iz okunur kalır.</summary>
    public string ActorName { get; init; } = string.Empty;

    /// <summary>Komut tipinin kısa adı, ör. <c>MarkAttendance</c>. Makine anahtarıdır.</summary>
    public string CommandType { get; init; } = string.Empty;

    /// <summary>
    /// Komutun Türkçe arayüz etiketi. <b>Sunucudan gelir</b> — arayüz kendi eşleme tablosunu
    /// tutsaydı yeni bir komutta sessizce ham tip adı görünürdü. Sözlükte karşılığı olmayan
    /// komutta <see cref="CommandType"/> ile aynıdır.
    /// </summary>
    public string CommandLabel { get; init; } = string.Empty;

    /// <summary>Komutun ait olduğu modül, ör. <c>Attendance</c>.</summary>
    public string Module { get; init; } = string.Empty;

    /// <summary>Satırın yazıldığı kiracı. Kurum üstü işlerde <c>platform</c>.</summary>
    public string? TenantId { get; init; }

    public Guid? ActorInstitutionId { get; init; }

    public Guid? SubjectInstitutionId { get; init; }

    /// <summary>
    /// Konu kurumun ağaçtaki yolu. Okuma süzgeci bunu kullanır:
    /// <c>SubjectInstitutionPath.StartsWith(okuyucununYolu)</c> — A parçasındaki
    /// <c>InstitutionScopePolicy</c> ile aynı kural, yeni kapsam ekseni doğmaz.
    ///
    /// <para><c>null</c> = yol çözülemedi (geçiş ucu koşmamış ya da arama başarısız). Satır
    /// yine yazılır; yalnız yol önekiyle okuyan kullanıcıya görünmez.</para>
    /// </summary>
    public string? SubjectInstitutionPath { get; init; }

    /// <summary>
    /// Aktörün kurumu ile konu kurumu ayrıştığında <c>true</c>.
    /// <b>Hesaplanmış olarak saklanır</b> çünkü sonradan türetmek iki alanın o günkü
    /// değerini bilmeyi gerektirir — kurum ağacı değişince geçmiş yeniden yazılırdı.
    /// </summary>
    public bool CrossedTenantBoundary { get; init; }

    /// <summary>
    /// Sonucun <b>saklanan</b> hâli — <c>AuditOutcome.Name</c>.
    /// </summary>
    /// <remarks>
    /// <b>Neden düz string:</b> Marten LINQ'te <c>e.Outcome.Name</c> SQL'e
    /// <c>data->'outcome'->>'Name'</c> çevrilir; SmartEnum JSON'a düz string yazıldığı için
    /// bu yol HER ZAMAN NULL döner ve süzgeç hiçbir şey bulmaz — ne derleyici ne test görür.
    /// Aynı tuzak <c>Institution.NodeTypeName</c> yorumunda anlatıldı.
    /// </remarks>
    public string OutcomeName { get; init; } = AuditOutcome.Failed.Name;

    /// <summary><c>Rejected</c>'ta <c>Error.Code</c>, <c>Failed</c>'da istisna tipinin adı.</summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Komuttan konvansiyonla çıkarılan hedef kayıt kimlikleri (ör.
    /// <c>{"StudentId": …, "ContractId": …}</c>). Bilinen ad kümesinde olmayan komut
    /// <b>hedefsiz</b> kaydolur — satır yine oluşur.
    /// </summary>
    public Dictionary<string, Guid> TargetIds { get; init; } = [];

    public int DurationMs { get; init; }

    /// <summary>
    /// Sonuç tipi. <see cref="OutcomeName"/>'den hesaplanır ve <b>serialize edilmez</b> —
    /// tek stok alan olsun ki ikisi ayrışamasın.
    /// </summary>
    [JsonIgnore]
    public AuditOutcome Outcome => AuditOutcome.Resolve(OutcomeName);
}
```

- [ ] **Step 4: `MESNET.Audit.Application.csproj` (bu görevde boş, sonraki görevler doldurur)**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <!-- BackgroundService, IConfiguration, IMemoryCache ve DI için (Payment.Application ile aynı). -->
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="WolverineFx" />
    <PackageReference Include="Marten" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../MESNET.Audit.Core/MESNET.Audit.Core.csproj" />
    <ProjectReference Include="../../../MESNET.Common.Infrastructure/MESNET.Common.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

**KESİN KURAL:** Bu csproj'a başka hiçbir modülün projesi eklenmez. Denetim middleware'i her modülün komutunu görür ama hiçbirini TANIMAZ; tanıması gerekseydi konvansiyon yerine 201 komutluk bir kayıt listesi tutmak zorunda kalırdık.

- [ ] **Step 5: `MESNET.Audit.Persistence.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="Marten" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../MESNET.Audit.Core/MESNET.Audit.Core.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 6: `AuditMartenConfig`**

`src/Modules/Audit/MESNET.Audit.Persistence/AuditMartenConfig.cs`:

```csharp
using Marten;
using MESNET.Audit.Core.Entities;

namespace MESNET.Audit.Persistence;

public class AuditMartenConfig : IConfigureMarten
{
    public void Configure(IServiceProvider services, StoreOptions options)
    {
        options.Schema.For<AuditEntry>().DatabaseSchemaName("audit");

        // İsimler ELLE verilir: PostgreSQL tanımlayıcı sınırı 64 karakter ve Marten'in
        // otomatik adı (mt_doc_auditentry_idx_...) uzun alan adlarıyla bunu aşar.
        options.Schema.For<AuditEntry>()
            .Index(x => x.OccurredAt, x => x.Name = "idx_audit_occurred");
        options.Schema.For<AuditEntry>()
            .Index(x => x.ActorId, x => x.Name = "idx_audit_actor");
        // Yol öneki sorgusu (StartsWith → LIKE 'önek%'). Düz btree'dir; PostgreSQL bunu önek
        // araması için ancak C collation ya da text_pattern_ops opclass'ıyla kullanır.
        // Aynı not Institution.Path indeksinde de duruyor — bedel aynı ölçekte ölçülemez.
        options.Schema.For<AuditEntry>()
            .Index(x => x.SubjectInstitutionPath, x => x.Name = "idx_audit_subject_path");
    }
}
```

- [ ] **Step 7: `ServiceRegistration` (Persistence)**

`src/Modules/Audit/MESNET.Audit.Persistence/ServiceRegistration.cs`:

```csharp
using Marten;
using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Audit.Persistence;

public static class ServiceRegistration
{
    public static IServiceCollection AddAuditPersistence(this IServiceCollection services)
    {
        services.AddSingleton<IConfigureMarten, AuditMartenConfig>();
        return services;
    }
}
```

- [ ] **Step 8: `MESNET.Audit.Api.csproj` (bu görevde boş)**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../MESNET.Audit.Application/MESNET.Audit.Application.csproj" />
    <ProjectReference Include="../MESNET.Audit.Persistence/MESNET.Audit.Persistence.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 9: Test projesi `tests/MESNET.Audit.UnitTests/MESNET.Audit.UnitTests.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Shouldly" />
    <!--
      Görev 7'deki middleware sözleşme testi CANLI bir Wolverine ana bilgisayarı kurar ve
      handler kodunu çalışma anında üretir. Çekirdek WolverineFx artık Roslyn derleyicisini
      taşımıyor: bu paket olmadan ana bilgisayar "no IAssemblyGenerator (Roslyn) is
      registered" ile açılışta ölür (ölçüldü, GH-2876).
    -->
    <PackageReference Include="WolverineFx.RuntimeCompilation" />
    <!-- Görev 7'deki sözleşme testi Host.CreateDefaultBuilder() kullanır. -->
    <PackageReference Include="Microsoft.Extensions.Hosting" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/Modules/Audit/MESNET.Audit.Application/MESNET.Audit.Application.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 10: `MESNET.slnx`'e beş proje ekle**

`/src/Modules/` altına yeni bir `Folder` ve `/tests/` klasörüne test projesi:

```xml
  <Folder Name="/src/Modules/Audit/">
    <Project Path="src/Modules/Audit/MESNET.Audit.Api/MESNET.Audit.Api.csproj"/>
    <Project Path="src/Modules/Audit/MESNET.Audit.Application/MESNET.Audit.Application.csproj"/>
    <Project Path="src/Modules/Audit/MESNET.Audit.Core/MESNET.Audit.Core.csproj"/>
    <Project Path="src/Modules/Audit/MESNET.Audit.Persistence/MESNET.Audit.Persistence.csproj"/>
  </Folder>
```

ve `/tests/` klasörünün içine:

```xml
    <Project Path="tests/MESNET.Audit.UnitTests/MESNET.Audit.UnitTests.csproj"/>
```

- [ ] **Step 11: `DocumentTenancyMap`'e `AuditEntry` ekle**

`src/MESNET.Common.Shared/Tenancy/DocumentTenancyMap.cs` içindeki **kiracıya ait** bölüme, alfabetik olarak uygun yere:

```csharp
        // Denetim izi (C parçası). Satır bir okulun verisi HAKKINDADIR ve satır düzeyinde
        // süzülmelidir: bir okulun müdürü diğer okulun iz satırını görmemelidir. Kurum üstü
        // işler (ulusal parametre, rebuild-hierarchy) platform kiracısına düşer.
        ["AuditEntry"] = Tenant,
```

- [ ] **Step 12: Derle ve kiracılık sapma testini koştur**

```bash
dotnet build MESNET.slnx
dotnet test tests/MESNET.Security.UnitTests --filter "FullyQualifiedName~DocumentTenancyDrift"
```

Beklenen: derleme başarılı, sapma testi YEŞİL.

**Kanıt adımı (zorunlu):** `DocumentTenancyMap`'ten eklediğiniz `["AuditEntry"] = Tenant,` satırını **silin**, aynı testi koşun ve kırmızı olduğunu + `AuditEntry` adını verdiğini raporunuza yazın. Sonra satırı geri koyun. Sapma testi gerçekten yeni belgeyi görüyor mu, ölçülmeden bilinmez.

- [ ] **Step 13: Commit**

```bash
git add src/Modules/Audit tests/MESNET.Audit.UnitTests MESNET.slnx src/MESNET.Common.Shared/Tenancy/DocumentTenancyMap.cs
git commit -m "feat(audit): denetim modülü iskeleti + AuditEntry belgesi"
```

---

### Task 2: Hedef kimliği çıkarımı + komut tanımlayıcısı (saf)

Middleware komutları tanımaz. Hedef kimlikleri **bilinen bir ad kümesinden** konvansiyonla çıkarılır; tip başına özellik listesi bir kez çözülüp önbelleğe alınır (istek başına yansıma yapılmaz).

**Files:**
- Create: `src/Modules/Audit/MESNET.Audit.Core/Services/AuditTargetExtractor.cs`
- Create: `src/Modules/Audit/MESNET.Audit.Core/Services/AuditCommandDescriptor.cs`
- Test: `tests/MESNET.Audit.UnitTests/AuditTargetExtractorTests.cs`
- Test: `tests/MESNET.Audit.UnitTests/AuditCommandDescriptorTests.cs`

**Interfaces:**
- Produces:
  - `AuditTargetExtractor.KnownTargetNames` → `IReadOnlySet<string>`
  - `AuditTargetExtractor.Extract(object? command)` → `Dictionary<string, Guid>`
  - `AuditCommandDescriptor.Describe(Type type)` → `(string CommandType, string Module)`

- [ ] **Step 1: Başarısız testi yaz**

`tests/MESNET.Audit.UnitTests/AuditTargetExtractorTests.cs`:

```csharp
using MESNET.Audit.Core.Services;
using Shouldly;

namespace MESNET.Audit.UnitTests;

/// <summary>
/// Hedef kimliği çıkarımı KONVANSİYONA dayalıdır: middleware komutları tanımaz, tanısaydı
/// 201 komutluk elle bakımlı bir kayıt listesi tutmak gerekirdi. Bedeli burada ölçülür:
/// kümede olmayan bir ad kullanan komut HEDEFSİZ kaydolur — satır yine oluşur (kim, ne,
/// ne zaman durur), yalnız hangi kayda dokunulduğu yazılmaz.
/// </summary>
public class AuditTargetExtractorTests
{
    private sealed record MarkAttendance(Guid StudentId, Guid ContractId, DateTime Date);
    private sealed record UnknownShape(Guid WidgetId, string Name);
    private sealed record NullableTarget(Guid? StudentId);
    private sealed record EmptyTarget(Guid StudentId);

    [Fact]
    public void Bilinen_adlardaki_kimlikleri_cikarir()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var contractId = Guid.NewGuid();
        var command = new MarkAttendance(studentId, contractId, DateTime.UtcNow);

        // Act
        var targets = AuditTargetExtractor.Extract(command);

        // Assert
        targets.Count.ShouldBe(2);
        targets["StudentId"].ShouldBe(studentId);
        targets["ContractId"].ShouldBe(contractId);
    }

    [Fact]
    public void Bilinmeyen_ad_kullanan_komut_hedefsiz_kaydolur()
    {
        // Bu SESSİZ bir eksikliktir ve bilinçlidir. Satırın kendisi kaybolmaz.
        var targets = AuditTargetExtractor.Extract(new UnknownShape(Guid.NewGuid(), "x"));

        targets.ShouldBeEmpty();
    }

    [Fact]
    public void Dolu_nullable_kimlik_cikarilir()
    {
        var studentId = Guid.NewGuid();

        var targets = AuditTargetExtractor.Extract(new NullableTarget(studentId));

        targets["StudentId"].ShouldBe(studentId);
    }

    [Fact]
    public void Bos_nullable_kimlik_cikarilmaz()
    {
        var targets = AuditTargetExtractor.Extract(new NullableTarget(null));

        targets.ShouldBeEmpty();
    }

    [Fact]
    public void Guid_Empty_hedef_sayilmaz()
    {
        // Guid.Empty "kimlik verilmedi" demektir; iz onu gerçek bir kayıtmış gibi göstermemeli.
        var targets = AuditTargetExtractor.Extract(new EmptyTarget(Guid.Empty));

        targets.ShouldBeEmpty();
    }

    [Fact]
    public void Null_komut_bos_sozluk_dondurur()
    {
        AuditTargetExtractor.Extract(null).ShouldBeEmpty();
    }

    [Fact]
    public void Ayni_tip_iki_kez_cozulunce_ayni_sonucu_verir()
    {
        // Önbellek doğru anahtarlanmazsa ikinci çağrı boş dönerdi.
        var command = new MarkAttendance(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        AuditTargetExtractor.Extract(command).Count
            .ShouldBe(AuditTargetExtractor.Extract(command).Count);
    }

    [Fact]
    public void Bilinen_ad_kumesi_beklenen_dokuz_adi_icerir()
    {
        // Küme SABİTTİR ve testle kilitlidir — sessizce daralması hedeflerin kaybolması demek.
        AuditTargetExtractor.KnownTargetNames.ShouldBe(
            new[]
            {
                "AcademicPeriodId", "AttendanceId", "BusinessId", "ContractId", "InstitutionId",
                "PaymentId", "StudentId", "TeacherId", "UserAccountId",
            },
            ignoreOrder: true);
    }
}
```

- [ ] **Step 2: Testi koştur, kırmızı olduğunu gör**

```bash
dotnet test tests/MESNET.Audit.UnitTests
```

Beklenen: `AuditTargetExtractor` tipi yok — derleme hatası.

- [ ] **Step 3: `AuditTargetExtractor`'ı yaz**

`src/Modules/Audit/MESNET.Audit.Core/Services/AuditTargetExtractor.cs`:

```csharp
using System.Collections.Concurrent;
using System.Reflection;

namespace MESNET.Audit.Core.Services;

/// <summary>
/// Bir komuttan denetim izine yazılacak <b>hedef kayıt kimliklerini</b> çıkarır.
/// </summary>
/// <remarks>
/// <para><b>Neden konvansiyon:</b> komutlar heterojendir ve denetim middleware'i onları
/// tanımaz — tanısaydı 201 komut için elle bakımlı bir kayıt listesi tutmak gerekirdi ve o
/// liste sessizce eskirdi. Bunun yerine <see cref="KnownTargetNames"/> kümesindeki adları
/// taşıyan <c>Guid</c> özellikleri okunur.</para>
///
/// <para><b>Bedeli açıktır:</b> kümede olmayan bir ad kullanan komut HEDEFSİZ kaydolur.
/// Satır yine oluşur — kim, ne, ne zaman durur; yalnız hangi kayda dokunulduğu yazılmaz.</para>
///
/// <para><b>Yansıma maliyeti:</b> tip başına özellik listesi bir kez çözülür ve
/// <see cref="Cache"/>'te tutulur. İstek başına yansıma YAPILMAZ.</para>
/// </remarks>
public static class AuditTargetExtractor
{
    /// <summary>
    /// Hedef sayılan özellik adları. <b>Sabittir ve testle kilitlidir</b>; sessizce daralması
    /// hedeflerin izden kaybolması demektir.
    /// </summary>
    public static readonly IReadOnlySet<string> KnownTargetNames =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "AcademicPeriodId",
            "AttendanceId",
            "BusinessId",
            "ContractId",
            "InstitutionId",
            "PaymentId",
            "StudentId",
            "TeacherId",
            "UserAccountId",
        };

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> Cache = new();

    public static Dictionary<string, Guid> Extract(object? command)
    {
        var targets = new Dictionary<string, Guid>(StringComparer.Ordinal);
        if (command is null) return targets;

        foreach (var property in ResolveTargetProperties(command.GetType()))
        {
            // Guid.Empty "kimlik verilmedi" demektir; izde gerçek bir kayıtmış gibi
            // görünmesi, olmayan bir kaydı aramaya yollardı.
            if (property.GetValue(command) is Guid id && id != Guid.Empty)
                targets[property.Name] = id;
        }

        return targets;
    }

    private static PropertyInfo[] ResolveTargetProperties(Type type)
        => Cache.GetOrAdd(type, static t => t
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead
                        && KnownTargetNames.Contains(p.Name)
                        && (p.PropertyType == typeof(Guid) || p.PropertyType == typeof(Guid?)))
            .ToArray());
}
```

**NOT — `Guid?` özelliği:** `property.GetValue` bir `Guid?` için kutulanmış `Guid` ya da `null` döner, hiçbir zaman kutulanmış `Nullable<Guid>` değil. Bu yüzden tek bir `is Guid id` deseni her iki tipi de karşılar; ayrı bir dal YAZILMAZ.

- [ ] **Step 4: Testi koştur, yeşil olduğunu gör**

```bash
dotnet test tests/MESNET.Audit.UnitTests --filter "FullyQualifiedName~AuditTargetExtractor"
```

Beklenen: 8/8 PASS.

- [ ] **Step 5: `AuditCommandDescriptor` testini yaz**

`tests/MESNET.Audit.UnitTests/AuditCommandDescriptorTests.cs`:

```csharp
using MESNET.Audit.Core.Services;
using Shouldly;

namespace MESNET.Audit.UnitTests;

public class AuditCommandDescriptorTests
{
    [Fact]
    public void Modul_adini_ad_alanindan_okur()
    {
        // MESNET.<Modül>.Application.Commands.<Komut> → "AuditFixtures"
        var (commandType, module) = AuditCommandDescriptor.Describe(
            typeof(MESNET.AuditFixtures.Sample.Application.Commands.MarkAttendanceSample));

        commandType.ShouldBe("MarkAttendanceSample");
        module.ShouldBe("AuditFixtures");
    }

    [Fact]
    public void Beklenmeyen_ad_alaninda_modul_bos_kalir_tip_adi_yine_yazilir()
    {
        // Satırın kendisi kaybolmamalı: modül bilinmese de "kim, ne" durur.
        var (commandType, module) = AuditCommandDescriptor.Describe(typeof(NoNamespaceCommand));

        commandType.ShouldBe("NoNamespaceCommand");
        module.ShouldBeEmpty();
    }

    private sealed record NoNamespaceCommand(Guid StudentId);
}
```

Bu test gerçek komut ad alanı ŞEKLİNİ taklit eden örnek tiplere ihtiyaç duyar. `tests/MESNET.Audit.UnitTests/Fixtures.cs` dosyasını **tam olarak** şöyle oluşturun:

```csharp
// Denetim testleri gerçek komut AD ALANI şeklini taklit eden tipler ister; Audit modülü
// hiçbir modülü referans etmediği için (ve etmemeli), örnekler burada tanımlanır.
//
// AD ALANLARI KASITLI OLARAK "MESNET.AuditFixtures.Sample.*"tır, "MESNET.Attendance.*" DEĞİL:
// Görev 4'te bu test projesi on modülün Application assembly'sini referans edecek ve gerçek
// MESNET.Attendance.Application.Commands.MarkAttendance ile çakışırdı (CS0433). Şimdi doğru
// adı koymak, sonra taşımaktan ucuzdur.
//
// Dosya-kapsamlı ad alanı (namespace X;) bir dosyada yalnız BİR kez kullanılabilir; üç ad
// alanı olduğu için BLOK gövdeli yazılır.

namespace MESNET.AuditFixtures.Sample.Application.Commands
{
    public sealed record MarkAttendanceSample(Guid StudentId, Guid ContractId);
    public sealed record CorrectAttendanceSample(Guid AttendanceId);

    /// <summary>Commands/ klasörüne yanlış yerleşmiş bir SORGUYU taklit eder (Görev 4).</summary>
    public sealed record GetUserAccountsSample(int Page);
}

namespace MESNET.AuditFixtures.Sample.Application.Queries
{
    public sealed record GetAttendanceSample(Guid AttendanceId);
}

namespace MESNET.AuditFixtures.Sample.Application.Consumers
{
    public sealed record AttendanceMarkedSample(Guid AttendanceId);
}
```

Test dosyalarında bu tiplere **tam ad alanıyla** ulaşın (`MESNET.AuditFixtures.Sample.Application.Commands.MarkAttendanceSample`).

- [ ] **Step 6: `AuditCommandDescriptor`'ı yaz**

`src/Modules/Audit/MESNET.Audit.Core/Services/AuditCommandDescriptor.cs`:

```csharp
using System.Collections.Concurrent;

namespace MESNET.Audit.Core.Services;

/// <summary>
/// Komut tipinden denetim satırının iki kimlik alanını çıkarır: kısa tip adı ve modül adı.
/// </summary>
/// <remarks>
/// Modül adı ad alanı konvansiyonundan okunur: <c>MESNET.&lt;Modül&gt;.Application.Commands</c>.
/// Konvansiyon depoda zaten klasör yapısıyla zorlanıyor; yeni bir kural icat edilmiyor.
/// Beklenmeyen bir ad alanında modül BOŞ kalır — satır yine yazılır, çünkü "kim, ne, ne zaman"
/// modül adı olmadan da anlamlıdır.
/// </remarks>
public static class AuditCommandDescriptor
{
    private const string RootNamespace = "MESNET.";

    private static readonly ConcurrentDictionary<Type, (string CommandType, string Module)> Cache = new();

    public static (string CommandType, string Module) Describe(Type type)
        => Cache.GetOrAdd(type, static t => (t.Name, ResolveModule(t.Namespace)));

    private static string ResolveModule(string? ns)
    {
        if (string.IsNullOrEmpty(ns) || !ns.StartsWith(RootNamespace, StringComparison.Ordinal))
            return string.Empty;

        var rest = ns[RootNamespace.Length..];
        var dot = rest.IndexOf('.');
        return dot < 0 ? rest : rest[..dot];
    }
}
```

- [ ] **Step 7: Testleri koştur**

```bash
dotnet test tests/MESNET.Audit.UnitTests
```

Beklenen: tümü PASS.

- [ ] **Step 8: Commit**

```bash
git add src/Modules/Audit/MESNET.Audit.Core/Services tests/MESNET.Audit.UnitTests
git commit -m "feat(audit): hedef kimliği çıkarımı ve komut tanımlayıcısı"
```

---

### Task 3: Türkçe komut etiketi sözlüğü

Arayüzde "MarkAttendance" değil "Devamsızlık girildi" yazmalı. **Eşleme sunucudadır**: arayüz kendi tablosunu tutsaydı yeni bir komutta sessizce ham tip adı görünürdü ve kimse fark etmezdi.

**Files:**
- Create: `src/Modules/Audit/MESNET.Audit.Core/Services/AuditCommandLabels.cs`
- Test: `tests/MESNET.Audit.UnitTests/AuditCommandLabelsTests.cs`

**Interfaces:**
- Produces: `AuditCommandLabels.For(string commandType)` → `string` (eşleşme yoksa `commandType`'ın kendisi)

- [ ] **Step 1: Kapsanacak komut adlarını çıkar**

```bash
for m in Institution Business Enrollment Contract Attendance Payment Coordination Internship Reporting Security; do
  grep -rh "^public sealed record \|^public record " \
    $(find src/Modules/$m -path '*/Commands/*.cs' -not -path '*/obj/*') 2>/dev/null \
  | sed -E 's/^public (sealed )?record ([A-Za-z0-9_]+).*/\2/'
done | sort -u
```

Bu listeden **şunları çıkarın** (denetim satırı üretmezler):
- `Get` ile başlayanlar — `Commands/` klasörüne yanlış yerleştirilmiş SORGULARDIR (`GetDocumentById`, `GetUserAccounts`, `GetInvitations`, `GetPermissionScopes`, `GetRoleIntegrityReport`, `GetStudentsWithoutGuardian`, `GetUserAccount`). Görev 4'teki süzgeç bunları da eler.
- `Result`, `Dto`, `Input`, `Item` ile bitenler — dönüş tipleri ve iç içe girdi kayıtlarıdır, mesaj değildir; Wolverine onlar için zincir kurmaz.

Kalan her ad için sözlükte bir giriş **zorunludur**.

- [ ] **Step 2: Başarısız testi yaz**

`tests/MESNET.Audit.UnitTests/AuditCommandLabelsTests.cs`:

```csharp
using MESNET.Audit.Core.Services;
using Shouldly;

namespace MESNET.Audit.UnitTests;

/// <summary>
/// Etiket eşlemesi SUNUCUDADIR. Arayüz kendi tablosunu tutsaydı, yeni bir komut eklendiğinde
/// listede sessizce ham tip adı ("MarkAttendance") belirirdi ve bunu hiçbir test göremezdi.
/// Sözlük kısmi olabilir — eşleşmeyen komut ham adıyla görünür, satır KAYBOLMAZ.
/// </summary>
public class AuditCommandLabelsTests
{
    [Fact]
    public void Bilinen_komut_Turkce_etiketiyle_doner()
    {
        AuditCommandLabels.For("MarkAttendance").ShouldBe("Devamsızlık girildi");
    }

    [Fact]
    public void Bilinmeyen_komut_ham_tip_adiyla_doner()
    {
        // Sessiz boşluk YOK: satır görünür kalır, yalnız etiketi çevrilmemiştir.
        AuditCommandLabels.For("SomeBrandNewCommand").ShouldBe("SomeBrandNewCommand");
    }

    [Fact]
    public void Bos_giris_bos_doner()
    {
        AuditCommandLabels.For(string.Empty).ShouldBe(string.Empty);
    }

    [Fact]
    public void Etiketler_ASCII_yaklasimi_kullanmaz()
    {
        // Türkçe karakterler doğru yazılmalı: "Ogretmen" değil "Öğretmen". Bu bir stil
        // tercihi değil, arayüz dili kuralıdır (CLAUDE.md).
        var supheliler = new[] { "Ogretmen", "Donem", "Iptal", "Duzenle", "Sozlesme", "Odeme", "Ucret" };

        foreach (var (_, label) in AuditCommandLabels.All)
        {
            foreach (var supheli in supheliler)
                label.ShouldNotContain(supheli, Case.Insensitive);
        }
    }

    [Fact]
    public void Her_etiket_dolu_ve_benzersiz_anahtarlidir()
    {
        AuditCommandLabels.All.ShouldAllBe(x => !string.IsNullOrWhiteSpace(x.Value));
    }
}
```

- [ ] **Step 3: Testi koştur, kırmızı gör**

```bash
dotnet test tests/MESNET.Audit.UnitTests --filter "FullyQualifiedName~AuditCommandLabels"
```

Beklenen: `AuditCommandLabels` tipi yok — derleme hatası.

- [ ] **Step 4: Sözlüğü yaz**

`src/Modules/Audit/MESNET.Audit.Core/Services/AuditCommandLabels.cs`.

**Etiket yazım kuralları (bağlayıcı):**
1. **Geçmiş zaman, edilgen çatı** — satır olmuş bir işi anlatır: "Devamsızlık girildi", "Sözleşme imzalandı". Emir kipi ("Devamsızlık gir") YAZILMAZ.
2. **Türkçe karakterler doğru** (ç ş ğ ü ö ı İ). ASCII yaklaşımı yasak.
3. **MEB terminolojisi**: "1. Dönem" / "2. Dönem" (Güz/Bahar değil), "işletme" (firma değil), "usta öğretici", "koordinatör öğretmen", "dekont".
4. Kısa tutun — liste hücresine sığmalı, tek satır.

**Zorunlu örnekler (birebir bunlar kullanılacak):**

```csharp
namespace MESNET.Audit.Core.Services;

/// <summary>
/// Komut tipinin Türkçe arayüz etiketi.
/// </summary>
/// <remarks>
/// <para><b>Neden sunucuda:</b> arayüz kendi eşleme tablosunu tutsaydı, yeni bir komut
/// eklendiğinde denetim listesinde sessizce ham tip adı ("MarkAttendance") belirirdi ve
/// bunu ne derleyici ne bir test görebilirdi.</para>
///
/// <para><b>Sözlük KISMİDİR ve bu bilinçlidir.</b> Eşleşmeyen komut ham adıyla görünür —
/// satır kaybolmaz, yalnız etiketi çevrilmemiştir. Alternatifi 200 satırlık bir tabloyu
/// her komut eklendiğinde kırmızıya çeviren bir kilit olurdu; o kilit denetim izinin
/// kendisini geciktirirdi.</para>
/// </remarks>
public static class AuditCommandLabels
{
    public static IReadOnlyDictionary<string, string> All { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // ── Devamsızlık ───────────────────────────────────────────────
            ["MarkAttendance"] = "Devamsızlık girildi",
            ["CorrectAttendance"] = "Devamsızlık düzeltildi",
            ["ApproveAttendance"] = "Devamsızlık onaylandı",
            ["DeleteAttendance"] = "Devamsızlık silindi",
            ["AttachHealthReport"] = "Sağlık raporu yüklendi",
            ["ApproveHealthReport"] = "Sağlık raporu onaylandı",
            ["RejectHealthReport"] = "Sağlık raporu reddedildi",
            ["RequestPaidLeave"] = "Ücretli izin talep edildi",
            ["BusinessApprovePaidLeave"] = "Ücretli izin işletmece onaylandı",
            ["ApprovePaidLeave"] = "Ücretli izin okulca onaylandı",
            ["RejectPaidLeave"] = "Ücretli izin reddedildi",

            // ── Sözleşme ──────────────────────────────────────────────────
            ["CreateContract"] = "Sözleşme oluşturuldu",
            ["SignContract"] = "Sözleşme imzalandı",
            ["ActivateContract"] = "Sözleşme yürürlüğe girdi",
            ["TerminateContract"] = "Sözleşme feshedildi",
            ["SuspendContract"] = "Sözleşme askıya alındı",

            // ── Maaş / dekont ─────────────────────────────────────────────
            ["UploadReceiptByBusiness"] = "Dekont işletmece yüklendi",
            ["UploadReceiptByStudent"] = "Dekont öğrencice yüklendi",
            ["ApproveReceiptByTeacher"] = "Dekont koordinatör öğretmence onaylandı",
            ["RejectReceipt"] = "Dekont reddedildi",
            ["UpdateMinimumWage"] = "Asgari ücret güncellendi",

            // ── Kurum ─────────────────────────────────────────────────────
            ["CreateInstitution"] = "Kurum oluşturuldu",
            ["UpdateInstitution"] = "Kurum bilgileri güncellendi",
            ["SetInstitutionBrandPalette"] = "Kurum marka paleti değiştirildi",
            ["RebuildInstitutionHierarchy"] = "Kurum ağacı yeniden kuruldu",

            // ── Kullanıcı ve yetki ────────────────────────────────────────
            ["CreateUser"] = "Kullanıcı oluşturuldu",
            ["ChangeUserRoles"] = "Kullanıcı rolleri değiştirildi",
            ["ChangeUserPermissions"] = "Kullanıcı izinleri değiştirildi",
            ["ChangeUserInstitution"] = "Kullanıcının kurumu değiştirildi",
            ["ChangeUserBranches"] = "Kullanıcının alanları değiştirildi",
            ["DeleteUser"] = "Kullanıcı silindi",

            // Kalan komutlar Step 1'deki listeden aynı kurallarla doldurulur.
        };

    /// <summary>
    /// Komutun Türkçe etiketi; sözlükte yoksa <b>ham tip adı</b>. Boş dönmez — boş bir etiket
    /// listede boş hücre demek olurdu ve satır okunamaz hâle gelirdi.
    /// </summary>
    public static string For(string commandType)
        => All.TryGetValue(commandType, out var label) ? label : commandType;
}
```

Step 1'deki listede kalan HER ad için aynı kurallarla bir giriş ekleyin. Modül başlıklarıyla gruplayın.

- [ ] **Step 5: Kapsamı doğrula**

Sözlükte olmayan komutları listeleyen tek seferlik kontrol:

```bash
dotnet build src/Modules/Audit/MESNET.Audit.Core -v q
```

Ardından Step 1'deki listeyi sözlükle karşılaştırın; eksik kalan her adı raporunuza yazın. (Kalıcı bir kapsam testi **bilerek eklenmez**: her yeni komutta kırmızıya dönen bir kilit, denetim izini değil çeviri borcunu bloke ederdi.)

- [ ] **Step 6: Testleri koştur**

```bash
dotnet test tests/MESNET.Audit.UnitTests --filter "FullyQualifiedName~AuditCommandLabels"
```

Beklenen: 5/5 PASS.

- [ ] **Step 7: Commit**

```bash
git add src/Modules/Audit/MESNET.Audit.Core/Services/AuditCommandLabels.cs tests/MESNET.Audit.UnitTests/AuditCommandLabelsTests.cs
git commit -m "feat(audit): komut tiplerinin Türkçe arayüz etiketleri"
```

---

### Task 4: Süzgeç konvansiyonu + kapsam sapma testi

Hangi handler zincirinin denetleneceğine karar veren **saf** yüklem, ve `Commands/` altındaki hiçbir komutun süzgeçten kaçmadığını kanıtlayan sapma testi.

**Files:**
- Create: `src/Modules/Audit/MESNET.Audit.Application/Auditing/AuditCommandFilter.cs`
- Test: `tests/MESNET.Audit.UnitTests/AuditCommandFilterTests.cs`
- Test: `tests/MESNET.Audit.UnitTests/AuditCommandCoverageDriftTests.cs`
- Modify: `tests/MESNET.Audit.UnitTests/MESNET.Audit.UnitTests.csproj` (sapma testi 10 Application assembly'sini referans eder)

**Interfaces:**
- Produces: `AuditCommandFilter.ShouldAudit(Type messageType)` → `bool`

- [ ] **Step 1: Başarısız testi yaz**

`tests/MESNET.Audit.UnitTests/AuditCommandFilterTests.cs`:

```csharp
using MESNET.Audit.Application.Auditing;
using Shouldly;

namespace MESNET.Audit.UnitTests;

/// <summary>
/// Süzgeç bir AD ALANI konvansiyonudur; depoda zaten klasör yapısıyla zorlanıyor
/// (<c>Commands/</c> ve <c>Queries/</c> ayrı) ve <c>InstitutionScopeDriftTests</c> de ona
/// dayanıyor — yeni bir kural icat edilmiyor, var olan kural kullanılıyor.
/// </summary>
public class AuditCommandFilterTests
{
    [Fact]
    public void Commands_ad_alanindaki_tip_denetlenir()
    {
        AuditCommandFilter
            .ShouldAudit(typeof(MESNET.AuditFixtures.Sample.Application.Commands.MarkAttendanceSample))
            .ShouldBeTrue();
    }

    [Fact]
    public void Queries_ad_alanindaki_tip_denetlenmez()
    {
        // Okuma iz üretmez; aksi hâlde hacim listeleme trafiğiyle dolar.
        AuditCommandFilter
            .ShouldAudit(typeof(MESNET.AuditFixtures.Sample.Application.Queries.GetAttendanceSample))
            .ShouldBeFalse();
    }

    [Fact]
    public void Consumers_ad_alanindaki_tip_denetlenmez()
    {
        // Tüketiciler kullanıcı eylemi değil OLAY TEPKİSİDİR. Kullanıcı eylemi zaten onu
        // tetikleyen komutta kaydedilmiştir; ikinci kez yazmak zinciri çift gösterirdi.
        AuditCommandFilter
            .ShouldAudit(typeof(MESNET.AuditFixtures.Sample.Application.Consumers.AttendanceMarkedSample))
            .ShouldBeFalse();
    }

    [Fact]
    public void Commands_ad_alanindaki_Get_ile_baslayan_tip_denetlenmez()
    {
        // Depoda Commands/ klasörüne YANLIŞ yerleştirilmiş sorgular var (GetUserAccounts,
        // GetDocumentById, GetInvitations …). Ad alanı onları komut sanardı ve liste
        // trafiğinin tamamı ize düşerdi. Bu ikinci kural o yanlış yerleşimin bedelidir.
        AuditCommandFilter
            .ShouldAudit(typeof(MESNET.AuditFixtures.Sample.Application.Commands.GetUserAccountsSample))
            .ShouldBeFalse();
    }

    [Fact]
    public void Ad_alani_olmayan_tip_denetlenmez()
    {
        AuditCommandFilter.ShouldAudit(typeof(int)).ShouldBeFalse();
    }
}
```

- [ ] **Step 2: Testi koştur, kırmızı gör**

```bash
dotnet test tests/MESNET.Audit.UnitTests --filter "FullyQualifiedName~AuditCommandFilter"
```

Beklenen: `AuditCommandFilter` tipi yok — derleme hatası.

- [ ] **Step 3: Süzgeci yaz**

`src/Modules/Audit/MESNET.Audit.Application/Auditing/AuditCommandFilter.cs`:

```csharp
namespace MESNET.Audit.Application.Auditing;

/// <summary>
/// Bir mesaj tipinin denetim izine yazılıp yazılmayacağına karar veren SAF yüklem.
/// </summary>
/// <remarks>
/// <para><b>Neden ad alanı konvansiyonu:</b> denetim middleware'i modülleri tanımaz ve
/// tanımamalıdır (<c>MESNET.Audit.*</c> hiçbir modülü referans etmez). Kayıt listesi
/// tutulsaydı 201 komutluk elle bakımlı bir tablo doğardı ve o tablo sessizce eskirdi.
/// Konvansiyon depoda zaten klasör yapısıyla zorlanıyor.</para>
///
/// <para><b>İkinci kural neden var:</b> <c>Commands/</c> klasörüne YANLIŞ yerleştirilmiş
/// sorgular var (<c>GetUserAccounts</c>, <c>GetDocumentById</c>, <c>GetInvitations</c>,
/// <c>GetPermissionScopes</c>, <c>GetRoleIntegrityReport</c>, <c>GetStudentsWithoutGuardian</c>,
/// <c>GetUserAccount</c>). Yalnız ad alanına bakılsaydı bütün liste trafiği ize düşerdi.
/// Doğru çözüm o tipleri <c>Queries/</c>'e taşımaktır; bu plan onu kapsam DIŞI bırakır ve
/// bedeli burada, tek satırda görünür tutar.</para>
/// </remarks>
public static class AuditCommandFilter
{
    private const string CommandsNamespaceSuffix = ".Commands";
    private const string QueryNamePrefix = "Get";

    public static bool ShouldAudit(Type messageType)
    {
        var ns = messageType.Namespace;
        if (string.IsNullOrEmpty(ns)) return false;
        if (!ns.EndsWith(CommandsNamespaceSuffix, StringComparison.Ordinal)) return false;

        return !messageType.Name.StartsWith(QueryNamePrefix, StringComparison.Ordinal);
    }
}
```

- [ ] **Step 4: Testi koştur, yeşil gör**

```bash
dotnet test tests/MESNET.Audit.UnitTests --filter "FullyQualifiedName~AuditCommandFilter"
```

Beklenen: 5/5 PASS.

- [ ] **Step 5: Kapsam sapma testini yaz**

Bu test **gerçek** modül assembly'lerini tarar. `MESNET.Audit.UnitTests.csproj`'a on Application projesini referans olarak ekleyin:

```xml
  <ItemGroup>
    <!--
      Kapsam sapma testi GERÇEK komut tiplerini tarar; kaynak metni değil. Test projesinin
      modülleri referans etmesi, ÜRÜN kodunun onları referans etmesiyle aynı şey değildir —
      MESNET.Audit.Application hâlâ hiçbir modülü tanımıyor (Görev 1, Step 4).
    -->
    <ProjectReference Include="../../src/Modules/Institution/MESNET.Institution.Application/MESNET.Institution.Application.csproj" />
    <ProjectReference Include="../../src/Modules/Business/MESNET.Business.Application/MESNET.Business.Application.csproj" />
    <ProjectReference Include="../../src/Modules/Enrollment/MESNET.Enrollment.Application/MESNET.Enrollment.Application.csproj" />
    <ProjectReference Include="../../src/Modules/Contract/MESNET.Contract.Application/MESNET.Contract.Application.csproj" />
    <ProjectReference Include="../../src/Modules/Attendance/MESNET.Attendance.Application/MESNET.Attendance.Application.csproj" />
    <ProjectReference Include="../../src/Modules/Payment/MESNET.Payment.Application/MESNET.Payment.Application.csproj" />
    <ProjectReference Include="../../src/Modules/Coordination/MESNET.Coordination.Application/MESNET.Coordination.Application.csproj" />
    <ProjectReference Include="../../src/Modules/Internship/MESNET.Internship.Application/MESNET.Internship.Application.csproj" />
    <ProjectReference Include="../../src/Modules/Reporting/MESNET.Reporting.Application/MESNET.Reporting.Application.csproj" />
    <ProjectReference Include="../../src/Modules/Security/MESNET.Security.Application/MESNET.Security.Application.csproj" />
  </ItemGroup>
```

**Neden çakışma yok:** `Fixtures.cs` ad alanları Görev 2'de zaten `MESNET.AuditFixtures.Sample.*` olarak konuldu. Gerçek modül ad alanları kullanılsaydı aynı tip iki assembly'de tanımlı olur ve `CS0433` alırdınız.

`tests/MESNET.Audit.UnitTests/AuditCommandCoverageDriftTests.cs`:

```csharp
using System.Reflection;
using MESNET.Audit.Application.Auditing;
using Shouldly;

namespace MESNET.Audit.UnitTests;

/// <summary>
/// Süzgecin GERÇEK komutları kapsadığını kanıtlar.
/// </summary>
/// <remarks>
/// <para><b>Neden gerekli:</b> süzgeç bir konvansiyondur ve konvansiyonlar sessizce kırılır.
/// Bir modül komutlarını <c>Commands/</c> dışında bir ad alanına taşısa, denetim izi o modül
/// için sessizce boşalırdı — derleme geçer, testler geçer, dead letter boş kalır. Tam olarak
/// bu depoda ölçülmüş sessiz-boşluk kalıbıdır.</para>
///
/// <para><b>Muafiyet listesi bilinçli olarak DAR:</b> yalnız <c>Commands/</c> klasörüne yanlış
/// yerleştirilmiş sorgular. Liste büyürse test kırılır ve büyümenin sebebini sormak zorunda
/// kalırsınız.</para>
/// </remarks>
public class AuditCommandCoverageDriftTests
{
    /// <summary>
    /// <c>Commands/</c> klasöründe duran ama SORGU olan tipler. Doğru çözüm bunları
    /// <c>Queries/</c>'e taşımaktır; bu plan onu kapsam dışı bıraktı.
    /// </summary>
    private static readonly string[] BilinenYanlisYerlesimler =
    [
        "GetDocumentById",
        "GetDocumentPdf",
        "GetDocumentsByStudent",
        "GetInvitations",
        "GetPendingDocuments",
        "GetPermissionScopes",
        "GetRoleIntegrityReport",
        "GetStudentsWithoutGuardian",
        "GetUserAccount",
        "GetUserAccounts",
    ];

    private static readonly Assembly[] ModulAssemblyleri =
    [
        typeof(MESNET.Institution.Application.Commands.CreateInstitution).Assembly,
        typeof(MESNET.Business.Application.Commands.RegisterBusiness).Assembly,
        typeof(MESNET.Enrollment.Application.Commands.RegisterStudent).Assembly,
        typeof(MESNET.Contract.Application.Commands.CreateContract).Assembly,
        typeof(MESNET.Attendance.Application.Commands.MarkAttendance).Assembly,
        typeof(MESNET.Payment.Application.Commands.UploadReceiptByStudent).Assembly,
        typeof(MESNET.Coordination.Application.Commands.CreateBusinessEvaluation).Assembly,
        typeof(MESNET.Internship.Application.Commands.RequestTermination).Assembly,
        typeof(MESNET.Reporting.Application.Commands.GenerateInternshipContractDocument).Assembly,
        typeof(MESNET.Security.Application.Commands.CreateUser).Assembly,
    ];

    private static IEnumerable<Type> KomutAdAlanindakiTipler()
        => ModulAssemblyleri
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsPublic: true, IsAbstract: false }
                        && t.Namespace is { } ns
                        && ns.EndsWith(".Commands", StringComparison.Ordinal));

    [Fact]
    public void Commands_ad_alanindaki_her_komut_denetim_suzgecine_takilir()
    {
        // Arrange
        var kacanlar = KomutAdAlanindakiTipler()
            .Where(t => !BilinenYanlisYerlesimler.Contains(t.Name))
            .Where(t => !AuditCommandFilter.ShouldAudit(t))
            .Select(t => t.FullName)
            .OrderBy(x => x)
            .ToList();

        // Assert
        kacanlar.ShouldBeEmpty(
            $"Bu tipler Commands ad alanında ama denetim süzgecine takılmıyor — izleri sessizce eksik kalır:{Environment.NewLine}"
            + string.Join(Environment.NewLine, kacanlar));
    }

    [Fact]
    public void Bilinen_yanlis_yerlesim_listesi_gerceklikle_ortusur()
    {
        // Listede olup artık var olmayan bir ad, listenin ölü büyümesi demektir.
        var gercekAdlar = KomutAdAlanindakiTipler().Select(t => t.Name).ToHashSet(StringComparer.Ordinal);

        var oluGirisler = BilinenYanlisYerlesimler.Where(ad => !gercekAdlar.Contains(ad)).ToList();

        oluGirisler.ShouldBeEmpty(
            "Bu adlar muafiyet listesinde ama artık Commands ad alanında yoklar: "
            + string.Join(", ", oluGirisler));
    }
}
```

- [ ] **Step 6: Sapma testini koştur**

```bash
dotnet test tests/MESNET.Audit.UnitTests --filter "FullyQualifiedName~AuditCommandCoverageDrift"
```

Beklenen: 2/2 PASS. Eğer `BilinenYanlisYerlesimler` listesi gerçekle örtüşmüyorsa test size **tam adı** söyler; listeyi ona göre düzeltin (yeni komut EKLEMEYİN).

- [ ] **Step 7: Kanıt adımı (zorunlu)**

`AuditCommandFilter.ShouldAudit`'in ilk satırındaki ad alanı kontrolünü geçici olarak `if (!ns.EndsWith(".Komutlar", StringComparison.Ordinal)) return false;` yapın, testi koşun. Beklenen: `Commands_ad_alanindaki_her_komut_denetim_suzgecine_takilir` KIRMIZI ve kaçan tiplerin tam adlarını listeliyor. Raporunuza listenin ilk üç adını yazın, sonra düzeltmeyi geri alın.

- [ ] **Step 8: Commit**

```bash
git add src/Modules/Audit/MESNET.Audit.Application/Auditing/AuditCommandFilter.cs tests/MESNET.Audit.UnitTests
git commit -m "feat(audit): komut süzgeci konvansiyonu + kapsam sapma testi"
```

---

### Task 5: `AuditEntryFactory` — sonuç eşlemesi ve kiracı sınırı hesabı (saf)

Middleware'in "ne yazacağı" kararının tamamı burada, **saf** bir fonksiyondadır. Middleware yalnız girdi toplar — `InstitutionScopeGuardMiddleware` / `InstitutionScopePolicy` ayrımıyla birebir aynı idiom.

**Files:**
- Create: `src/Modules/Audit/MESNET.Audit.Core/Services/AuditEntryFactory.cs`
- Test: `tests/MESNET.Audit.UnitTests/AuditEntryFactoryTests.cs`

**Interfaces:**
- Consumes: `AuditEntry`, `AuditOutcome`, `AuditCommandLabels`, `AuditCommandDescriptor`, `AuditTargetExtractor`
- Produces:
  - `record AuditInput(Guid Id, DateTimeOffset OccurredAt, Guid ActorId, string ActorName, Type CommandType, object? Command, string? TenantId, Guid? ActorInstitutionId, string? ActorInstitutionPath, string? SubjectInstitutionPathOverride, int DurationMs)`
  - `AuditEntryFactory.Succeeded(AuditInput input)` → `AuditEntry`
  - `AuditEntryFactory.Failed(AuditInput input, Exception exception)` → `AuditEntry`

- [ ] **Step 1: Başarısız testi yaz**

`tests/MESNET.Audit.UnitTests/AuditEntryFactoryTests.cs`:

```csharp
using MESNET.Audit.Core.Enums;
using MESNET.Audit.Core.Services;
using MESNET.Common.Shared;
using Shouldly;

namespace MESNET.Audit.UnitTests;

public class AuditEntryFactoryTests
{
    private static readonly Guid AktorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AktorKurumu = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid BaskaKurum = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static AuditInput Girdi(
        object? command = null,
        Guid? actorInstitutionId = null,
        string? actorPath = null,
        string? subjectPathOverride = null)
        => new(
            Id: Guid.NewGuid(),
            OccurredAt: new DateTimeOffset(2026, 8, 28, 9, 0, 0, TimeSpan.Zero),
            ActorId: AktorId,
            ActorName: "Ayşe Öğretmen",
            CommandType: (command ?? new object()).GetType(),
            Command: command,
            TenantId: AktorKurumu.ToString(),
            ActorInstitutionId: actorInstitutionId ?? AktorKurumu,
            ActorInstitutionPath: actorPath,
            SubjectInstitutionPathOverride: subjectPathOverride,
            DurationMs: 42);

    private sealed record OrnekKomut(Guid StudentId, Guid InstitutionId);

    // ── Sonuç eşlemesi ────────────────────────────────────────────────────

    [Fact]
    public void Basarili_komut_Succeeded_yazar_ve_hata_kodu_tasimaz()
    {
        var entry = AuditEntryFactory.Succeeded(Girdi());

        entry.OutcomeName.ShouldBe(AuditOutcome.Succeeded.Name);
        entry.ErrorCode.ShouldBeNull();
    }

    [Fact]
    public void DomainException_Rejected_yazar_ve_Error_Code_saklar()
    {
        // "Sistem çalıştı, kural izin vermedi" — bir davranış kaydıdır, arıza değil.
        var ex = new DomainException(new Error("INSTITUTION_SCOPE_DENIED", "Kurum kapsamı dışında."));

        var entry = AuditEntryFactory.Failed(Girdi(), ex);

        entry.OutcomeName.ShouldBe(AuditOutcome.Rejected.Name);
        entry.ErrorCode.ShouldBe("INSTITUTION_SCOPE_DENIED");
    }

    [Fact]
    public void DomainException_hata_MESAJINI_saklamaz()
    {
        // Mesaj PII taşıyabilir (öğrenci adı, ilçe adı). Kod makine okunurdur ve sabittir.
        var ex = new DomainException(new Error("X", "Ahmet Yılmaz adlı öğrenci bulunamadı."));

        var entry = AuditEntryFactory.Failed(Girdi(), ex);

        entry.ErrorCode.ShouldBe("X");
        // Satırın hiçbir alanında mesaj geçmemeli.
        entry.ToString().ShouldNotContain("Ahmet");
    }

    [Fact]
    public void Diger_istisna_Failed_yazar_ve_istisna_tipinin_adini_saklar()
    {
        var entry = AuditEntryFactory.Failed(Girdi(), new InvalidOperationException("bağlantı düştü"));

        entry.OutcomeName.ShouldBe(AuditOutcome.Failed.Name);
        entry.ErrorCode.ShouldBe(nameof(InvalidOperationException));
    }

    // ── Kiracı sınırı ─────────────────────────────────────────────────────

    [Fact]
    public void Ayni_kurum_kiracı_sinirini_asmaz()
    {
        var komut = new OrnekKomut(Guid.NewGuid(), AktorKurumu);

        var entry = AuditEntryFactory.Succeeded(Girdi(komut, actorInstitutionId: AktorKurumu));

        entry.SubjectInstitutionId.ShouldBe(AktorKurumu);
        entry.CrossedTenantBoundary.ShouldBeFalse();
    }

    [Fact]
    public void Farkli_kurum_kiracı_sinirini_asar()
    {
        // B parçasının sorumluluk sorgusu tek bu alana iner.
        var komut = new OrnekKomut(Guid.NewGuid(), BaskaKurum);

        var entry = AuditEntryFactory.Succeeded(Girdi(komut, actorInstitutionId: AktorKurumu));

        entry.SubjectInstitutionId.ShouldBe(BaskaKurum);
        entry.CrossedTenantBoundary.ShouldBeTrue();
    }

    [Fact]
    public void Kurumsuz_aktor_siniri_asmis_sayilmaz()
    {
        // Platform aktörünün kurumu yoktur; "ayrıştı" demek yanlış olurdu — karşılaştıracak
        // bir taraf yok. Sınır aşımı bir İDDİADIR, veri eksikliği onu doğurmaz.
        var komut = new OrnekKomut(Guid.NewGuid(), BaskaKurum);

        var entry = AuditEntryFactory.Succeeded(Girdi(komut, actorInstitutionId: null));

        entry.CrossedTenantBoundary.ShouldBeFalse();
    }

    [Fact]
    public void Kurum_hedefi_olmayan_komutta_konu_kurum_aktorun_kurumudur()
    {
        var entry = AuditEntryFactory.Succeeded(Girdi(new { X = 1 }));

        entry.SubjectInstitutionId.ShouldBe(AktorKurumu);
        entry.CrossedTenantBoundary.ShouldBeFalse();
    }

    // ── Yol ───────────────────────────────────────────────────────────────

    [Fact]
    public void Konu_aktorun_kurumuysa_yol_aktorun_claim_yolundan_gelir()
    {
        // Sıcak yolda EK OKUMA YOK: okul kullanıcısının kendi kurumuna yazması bu daldadır.
        var komut = new OrnekKomut(Guid.NewGuid(), AktorKurumu);

        var entry = AuditEntryFactory.Succeeded(
            Girdi(komut, actorInstitutionId: AktorKurumu, actorPath: "/il/ilce/okul/"));

        entry.SubjectInstitutionPath.ShouldBe("/il/ilce/okul/");
    }

    [Fact]
    public void Konu_baska_kurumsa_yol_disaridan_verilen_degerden_gelir()
    {
        var komut = new OrnekKomut(Guid.NewGuid(), BaskaKurum);

        var entry = AuditEntryFactory.Succeeded(Girdi(
            komut,
            actorInstitutionId: AktorKurumu,
            actorPath: "/il/",
            subjectPathOverride: "/il/ilce/baska-okul/"));

        entry.SubjectInstitutionPath.ShouldBe("/il/ilce/baska-okul/");
    }

    [Fact]
    public void Yol_cozulemezse_satir_yine_yazilir_yol_null_kalir()
    {
        // Sessiz kayıp yok: satır durur, yalnız yol önekiyle okuyana görünmez.
        var komut = new OrnekKomut(Guid.NewGuid(), BaskaKurum);

        var entry = AuditEntryFactory.Succeeded(
            Girdi(komut, actorInstitutionId: AktorKurumu, actorPath: "/il/", subjectPathOverride: null));

        entry.SubjectInstitutionPath.ShouldBeNull();
        entry.ActorId.ShouldBe(AktorId);
    }

    [Fact]
    public void ResolveSubject_yazicinin_sordugu_soruya_ayni_cevabi_verir()
    {
        // Yazıcı, yolu aramaya gerek olup olmadığını satırı kurmadan önce bu yardımcıdan
        // öğrenir. İki yerde iki ayrı "konu kurum" tanımı doğmasın diye tek kaynak.
        var komut = new OrnekKomut(Guid.NewGuid(), BaskaKurum);

        var (subjectId, crossed) = AuditEntryFactory.ResolveSubject(komut, AktorKurumu);
        var entry = AuditEntryFactory.Succeeded(Girdi(komut, actorInstitutionId: AktorKurumu));

        subjectId.ShouldBe(entry.SubjectInstitutionId);
        crossed.ShouldBe(entry.CrossedTenantBoundary);
    }

    // ── Kimlik alanları ───────────────────────────────────────────────────

    [Fact]
    public void Komut_tipi_kisa_adi_modul_ve_Turkce_etiket_yazilir()
    {
        var entry = AuditEntryFactory.Succeeded(Girdi(
            new MESNET.AuditFixtures.Sample.Application.Commands.MarkAttendanceSample(
                Guid.NewGuid(), Guid.NewGuid())));

        entry.CommandType.ShouldBe("MarkAttendanceSample");
        entry.Module.ShouldBe("AuditFixtures");
        // Sözlükte yok → ham ad. Boş DÖNMEZ.
        entry.CommandLabel.ShouldBe("MarkAttendanceSample");
    }

    [Fact]
    public void Hedef_kimlikleri_satira_yazilir()
    {
        var studentId = Guid.NewGuid();
        var komut = new OrnekKomut(studentId, AktorKurumu);

        var entry = AuditEntryFactory.Succeeded(Girdi(komut));

        entry.TargetIds["StudentId"].ShouldBe(studentId);
    }
}
```

- [ ] **Step 2: Testi koştur, kırmızı gör**

```bash
dotnet test tests/MESNET.Audit.UnitTests --filter "FullyQualifiedName~AuditEntryFactory"
```

Beklenen: `AuditEntryFactory` / `AuditInput` tipleri yok — derleme hatası.

- [ ] **Step 3: `AuditEntryFactory`'yi yaz**

`src/Modules/Audit/MESNET.Audit.Core/Services/AuditEntryFactory.cs`:

```csharp
using MESNET.Audit.Core.Entities;
using MESNET.Audit.Core.Enums;
using MESNET.Common.Shared;

namespace MESNET.Audit.Core.Services;

/// <summary>
/// Denetim satırının kurulması için gereken girdilerin tamamı. Middleware yalnız bunları
/// toplar; karar <see cref="AuditEntryFactory"/>'dedir.
/// </summary>
/// <param name="SubjectInstitutionPathOverride">
/// Konu kurum aktörün kurumundan farklıysa dışarıdan çözülen yol. Çözülemediyse
/// <c>null</c> — satır yine yazılır.
/// </param>
public sealed record AuditInput(
    Guid Id,
    DateTimeOffset OccurredAt,
    Guid ActorId,
    string ActorName,
    Type CommandType,
    object? Command,
    string? TenantId,
    Guid? ActorInstitutionId,
    string? ActorInstitutionPath,
    string? SubjectInstitutionPathOverride,
    int DurationMs);

/// <summary>
/// Denetim satırının içeriğine karar veren SAF fonksiyon.
/// </summary>
/// <remarks>
/// Karar burada, girdi toplama middleware'de — <c>InstitutionScopePolicy</c> /
/// <c>InstitutionScopeGuardMiddleware</c> ile aynı ayrım. Böylece sonuç eşlemesi ve kiracı
/// sınırı hesabı canlı bir Wolverine ana bilgisayarı olmadan test edilebilir.
/// </remarks>
public static class AuditEntryFactory
{
    private const string InstitutionTargetName = "InstitutionId";

    public static AuditEntry Succeeded(AuditInput input)
        => Build(input, AuditOutcome.Succeeded, errorCode: null);

    /// <summary>
    /// Başarısız komutun satırı.
    ///
    /// <para><b><see cref="DomainException"/> → <see cref="AuditOutcome.Rejected"/>:</b>
    /// "sistem çalıştı, kural izin vermedi". Kurum kapsamı ihlali de buradadır — o guard
    /// middleware'de çalışır ve <c>DomainException</c> fırlatır.</para>
    ///
    /// <para><b>Diğer istisna → <see cref="AuditOutcome.Failed"/>:</b> "sistem çalışmadı".
    /// Saklanan tek şey istisna TİPİNİN adıdır; mesaj PII taşıyabilir.</para>
    /// </summary>
    public static AuditEntry Failed(AuditInput input, Exception exception)
        => exception is DomainException domain
            ? Build(input, AuditOutcome.Rejected, domain.Error.Code)
            : Build(input, AuditOutcome.Failed, exception.GetType().Name);

    /// <summary>
    /// Konu kurumu ve kiracı sınırının aşılıp aşılmadığını çözer.
    /// </summary>
    /// <remarks>
    /// <b>Neden ayrıca public:</b> denetim yazıcısı, yolu aramaya gerek olup olmadığını
    /// satırı kurmadan ÖNCE bilmek zorundadır (arama bir veritabanı gidişidir ve komutların
    /// büyük çoğunluğu kendi kurumuna yazar). Yazıcı bu kararı kendi kopyalasaydı iki yerde
    /// iki ayrı "konu kurum" tanımı doğardı.
    /// </remarks>
    public static (Guid? SubjectInstitutionId, bool CrossedTenantBoundary) ResolveSubject(
        object? command, Guid? actorInstitutionId)
    {
        var targets = AuditTargetExtractor.Extract(command);

        // Konu kurum: komut bir kurumu HEDEFLİYORSA o, aksi hâlde aktörün kurumu.
        // IInstitutionScoped arayüzüne bakılmaz — o tip Institution.Application'dadır ve
        // Audit hiçbir modülü referans etmez (Görev 1, Step 4).
        var subjectInstitutionId = targets.TryGetValue(InstitutionTargetName, out var targeted)
            ? targeted
            : actorInstitutionId;

        // Sınır aşımı bir İDDİADIR; veri eksikliği onu doğurmaz. Kurumsuz aktörde
        // karşılaştıracak taraf yoktur, o yüzden false.
        var crossed = actorInstitutionId is { } actorInstitution
                      && subjectInstitutionId is { } subject
                      && actorInstitution != subject;

        return (subjectInstitutionId, crossed);
    }

    private static AuditEntry Build(AuditInput input, AuditOutcome outcome, string? errorCode)
    {
        var targets = AuditTargetExtractor.Extract(input.Command);
        var (commandType, module) = AuditCommandDescriptor.Describe(input.CommandType);

        var (subjectInstitutionId, crossed) = ResolveSubject(input.Command, input.ActorInstitutionId);

        // Sıcak yolda EK OKUMA YOK: konu aktörün kendi kurumuysa yol claim'den gelir.
        var subjectPath = crossed
            ? input.SubjectInstitutionPathOverride
            : input.ActorInstitutionPath;

        return new AuditEntry
        {
            Id = input.Id,
            OccurredAt = input.OccurredAt,
            ActorId = input.ActorId,
            ActorName = input.ActorName,
            CommandType = commandType,
            CommandLabel = AuditCommandLabels.For(commandType),
            Module = module,
            TenantId = input.TenantId,
            ActorInstitutionId = input.ActorInstitutionId,
            SubjectInstitutionId = subjectInstitutionId,
            SubjectInstitutionPath = subjectPath,
            CrossedTenantBoundary = crossed,
            OutcomeName = outcome.Name,
            ErrorCode = errorCode,
            TargetIds = targets,
            DurationMs = input.DurationMs,
        };
    }
}
```

- [ ] **Step 4: Testi koştur, yeşil gör**

```bash
dotnet test tests/MESNET.Audit.UnitTests --filter "FullyQualifiedName~AuditEntryFactory"
```

Beklenen: 14/14 PASS.

- [ ] **Step 5: Kanıt adımı (zorunlu)**

`Failed` metodundaki `exception is DomainException domain` dalını **silin** (her istisna `Failed`'a düşsün), testi koşun. Beklenen: `DomainException_Rejected_yazar_ve_Error_Code_saklar` KIRMIZI. Raporunuza yazın, sonra geri alın.

- [ ] **Step 6: Commit**

```bash
git add src/Modules/Audit/MESNET.Audit.Core/Services/AuditEntryFactory.cs tests/MESNET.Audit.UnitTests/AuditEntryFactoryTests.cs
git commit -m "feat(audit): denetim satırı kurucusu — sonuç eşlemesi ve kiracı sınırı"
```

---

### Task 6: `IInstitutionPathLookup` — kurum yolunun altyapı araması

Denetim satırı, aktörün kendi kurumundan **başka** bir kurumu hedeflediğinde o kurumun ağaç yolunu ister. Audit modülü `institution` şemasına sorgu atamaz; arama altyapıya konur.

**Files:**
- Create: `src/MESNET.Common.Infrastructure/Tenancy/IInstitutionPathLookup.cs`
- Create: `src/MESNET.Common.Infrastructure/Tenancy/InstitutionPathLookup.cs`
- Modify: `src/MESNET.Presentation/Program.cs` (kayıt)

**Interfaces:**
- Produces: `IInstitutionPathLookup.GetPathAsync(Guid institutionId, CancellationToken)` → `Task<string?>`

- [ ] **Step 1: Arayüzü yaz**

`src/MESNET.Common.Infrastructure/Tenancy/IInstitutionPathLookup.cs`:

```csharp
namespace MESNET.Common.Infrastructure.Tenancy;

/// <summary>
/// Kurum kimliğinden ağaç yolunu (<c>/{ilId}/{ilçeId}/{okulId}/</c>) çözer.
/// </summary>
/// <remarks>
/// <para><b>Neden altyapıda:</b> denetim modülü <c>institution</c> şemasına sorgu ATAMAZ
/// (şema izolasyonu). Aynı arama <c>PermissionClaimsTransformation</c> içinde de yapılıyor —
/// o kopya <b>bilinçli olarak yerinde bırakıldı</b>: onun önbelleği KULLANICI başınadır ve
/// token geçersizleme yoluna bağlıdır; buradaki KURUM başınadır. İkisini tek önbellekte
/// birleştirmek, denetim yazmasını token geçersizleme yaşam döngüsüne bağlardı.</para>
///
/// <para><b>Boş sonuç hata değildir:</b> geçiş ucu (<c>POST /api/institutions/rebuild-hierarchy</c>)
/// o kurum için henüz koşmamış olabilir. <c>null</c> döner ve çağıran satırı yine yazar.</para>
/// </remarks>
public interface IInstitutionPathLookup
{
    Task<string?> GetPathAsync(Guid institutionId, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 2: Uygulamasını yaz**

`src/MESNET.Common.Infrastructure/Tenancy/InstitutionPathLookup.cs`:

```csharp
using Marten;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace MESNET.Common.Infrastructure.Tenancy;

/// <inheritdoc />
public sealed class InstitutionPathLookup(
    IDocumentStore store,
    IMemoryCache cache,
    ILogger<InstitutionPathLookup> logger) : IInstitutionPathLookup
{
    /// <summary>
    /// Kurum ağacı nadiren değişir; beş dakika <c>PermissionClaimsTransformation</c>'ın
    /// kapsam önbelleğiyle aynı süredir. Uzun tutulsaydı <c>rebuild-hierarchy</c> koştuktan
    /// sonra yeni yollar geç görünürdü.
    /// </summary>
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    // Alias'sız `data`: Marten'in kendi `d.data` belirsizliği yok. Proje deseni
    // (GetBusinessClustersHandler, PermissionClaimsTransformation) ile aynı.
    private const string PathLookupSql = """
        SELECT data->>'path' AS path
        FROM institution.mt_doc_institution
        WHERE data->>'id' = @institutionId
        LIMIT 1
        """;

    public async Task<string?> GetPathAsync(Guid institutionId, CancellationToken cancellationToken = default)
    {
        if (institutionId == Guid.Empty) return null;

        var cacheKey = $"institution-path:{institutionId:D}";
        if (cache.TryGetValue(cacheKey, out string? cached))
            return cached;

        var path = await LookupAsync(institutionId, cancellationToken);

        // SONUÇSUZ ARAMA ÖNBELLEĞE ALINMAZ: geçiş ucu koşturulduğu anda yol doğar ve o
        // kurumun beş dakika daha yolsuz kalması için bir neden yoktur. Aynı gerekçe
        // PermissionClaimsTransformation'daki institution_path aramasında da yazılı.
        if (!string.IsNullOrEmpty(path))
            cache.Set(cacheKey, path, CacheDuration);

        return path;
    }

    private async Task<string?> LookupAsync(Guid institutionId, CancellationToken cancellationToken)
    {
        try
        {
            var conn = store.Storage.Database.CreateConnection();
            await conn.OpenAsync(cancellationToken);
            await using (conn)
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = PathLookupSql;
                cmd.Parameters.Add(new NpgsqlParameter("institutionId", institutionId.ToString()));

                var result = await cmd.ExecuteScalarAsync(cancellationToken);
                return result as string;
            }
        }
        catch (Exception ex)
        {
            // Arama patlarsa denetim satırı YOLSUZ yazılır — satırı tümden kaybetmekten iyidir.
            logger.LogWarning(ex, "Kurum yolu araması başarısız: {InstitutionId}", institutionId);
            return null;
        }
    }
}
```

**NOT:** `Npgsql` paketi `MESNET.Common.Infrastructure.csproj`'da zaten var (aynı ham SQL deseni `PermissionClaimsTransformation` içinde kullanılıyor). Yoksa `<PackageReference Include="Npgsql" />` ekleyin — sürüm `Directory.Packages.props`'tan gelir.

- [ ] **Step 3: DI kaydını ekle**

`src/MESNET.Presentation/Program.cs` içinde, diğer altyapı servislerinin kaydedildiği yere (modül kayıtlarından ÖNCE, `builder.Services.AddInstitutionModule()` satırının hemen üstü):

```csharp
    // Kurum ağacı yolu araması — denetim izi (C parçası) konu kurumu aktörün kurumundan
    // farklı olduğunda kullanır. Singleton: önbellek kurum başınadır ve istek ömrüne bağlı
    // değildir; IDocumentStore da singleton'dır.
    builder.Services.AddSingleton<MESNET.Common.Infrastructure.Tenancy.IInstitutionPathLookup,
        MESNET.Common.Infrastructure.Tenancy.InstitutionPathLookup>();
```

`IMemoryCache` zaten kayıtlı (`PermissionClaimsTransformation` kullanıyor); değilse `builder.Services.AddMemoryCache();` satırının varlığını doğrulayın.

- [ ] **Step 4: Derle**

```bash
dotnet build MESNET.slnx
```

Beklenen: başarılı.

- [ ] **Step 5: Commit**

```bash
git add src/MESNET.Common.Infrastructure/Tenancy src/MESNET.Presentation/Program.cs
git commit -m "feat(audit): kurum ağacı yolu araması altyapıya taşındı"
```

---

### Task 7: Denetim middleware'i + ayrı oturumlu yazıcı + kayıt

Bu görev planın kalbidir. **Global Constraints'teki dört ölçülmüş Wolverine davranışı burada bağlayıcıdır** — hiçbirini "daha temiz" bir şekle çevirmeye çalışmayın; ölçüldüler.

**Files:**
- Create: `src/Modules/Audit/MESNET.Audit.Application/Auditing/AuditContext.cs`
- Create: `src/Modules/Audit/MESNET.Audit.Application/Auditing/AuditContextAccessor.cs`
- Create: `src/Modules/Audit/MESNET.Audit.Application/Auditing/IAuditWriter.cs`
- Create: `src/Modules/Audit/MESNET.Audit.Application/Auditing/AuditWriter.cs`
- Create: `src/Modules/Audit/MESNET.Audit.Application/Auditing/AuditMiddleware.cs`
- Create: `src/Modules/Audit/MESNET.Audit.Application/ServiceRegistration.cs`
- Create: `src/Modules/Audit/MESNET.Audit.Api/ModuleServiceRegistration.cs`
- Modify: `src/MESNET.Presentation/Program.cs`
- Test: `tests/MESNET.Audit.UnitTests/AuditMiddlewareContractTests.cs`

**Interfaces:**
- Consumes: `AuditEntryFactory`, `AuditCommandFilter`, `IInstitutionPathLookup`, `ICurrentUserService`, `IDocumentStore`
- Produces: `IAuditWriter`, `AuditMiddleware`, `ServiceRegistration.AddAuditModule()`

- [ ] **Step 1: `AuditContext`**

`src/Modules/Audit/MESNET.Audit.Application/Auditing/AuditContext.cs`:

```csharp
using System.Diagnostics;

namespace MESNET.Audit.Application.Auditing;

/// <summary>
/// Middleware'in <c>Before</c> → <c>After</c> → <c>Finally</c> hook'ları arasında taşıdığı
/// durum.
/// </summary>
/// <remarks>
/// <para><b>Neden MUTABLE — projede tek istisna:</b> Wolverine'in ürettiği kodda
/// <c>After</c>, başarı yolunda <c>try</c> bloğunun içinde çalışır ve <c>Finally</c> ondan
/// sonra gelir. Bir <i>değer</i> döndürüp aktarmak mümkün değildir: <c>try</c> içinde atanan
/// bir değişken <c>finally</c> bloğunda "kesin atanmış" sayılmaz ve derleme kırılır. Tek
/// mutasyon <see cref="Succeeded"/>'dır ve yalnız <see cref="MarkSucceeded"/> ile yapılır.</para>
///
/// <para><b>Varsayılan başarısızdır.</b> <c>After</c> hiç çalışmazsa (istisna yolu)
/// bayrak <c>false</c> kalır ve <c>Finally</c> hiçbir şey yazmaz — o satırı
/// <c>OnExceptionAsync</c> yazar.</para>
/// </remarks>
public sealed class AuditContext
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    private readonly long _startTimestamp = Stopwatch.GetTimestamp();

    public required Guid ActorId { get; init; }
    public required string ActorName { get; init; }
    public required Type CommandType { get; init; }
    public required object? Command { get; init; }
    public required string? TenantId { get; init; }
    public required Guid? ActorInstitutionId { get; init; }
    public required string? ActorInstitutionPath { get; init; }

    public bool Succeeded { get; private set; }

    public void MarkSucceeded() => Succeeded = true;

    public int ElapsedMs => (int)Stopwatch.GetElapsedTime(_startTimestamp).TotalMilliseconds;
}
```

- [ ] **Step 2: `IAuditWriter` + `AuditWriter`**

`src/Modules/Audit/MESNET.Audit.Application/Auditing/IAuditWriter.cs`:

```csharp
namespace MESNET.Audit.Application.Auditing;

/// <summary>
/// Denetim satırını <b>komutun işleminden AYRI</b> bir oturumda yazar.
/// </summary>
public interface IAuditWriter
{
    /// <param name="exception">
    /// Başarısızlık yolunda istisna; başarı yolunda <c>null</c>.
    /// </param>
    Task WriteAsync(AuditContext context, Exception? exception, CancellationToken cancellationToken = default);
}
```

`src/Modules/Audit/MESNET.Audit.Application/Auditing/AuditWriter.cs`:

```csharp
using Marten;
using MESNET.Audit.Core.Services;
using MESNET.Common.Infrastructure.Tenancy;
using MESNET.Common.Shared.Tenancy;
using Microsoft.Extensions.Logging;

namespace MESNET.Audit.Application.Auditing;

/// <inheritdoc />
/// <remarks>
/// <para><b>EN KRİTİK KARAR — ayrı oturum.</b> Reddedilen bir komut <c>DomainException</c>
/// atar ve Wolverine'in <c>AutoApplyTransactions()</c> politikası işlemi geri alır. Denetim
/// satırı aynı oturumda yazılsaydı <b>ret kaydı da geri alınırdı</b> — yani en çok istediğimiz
/// satır ("kim neye erişmeye çalıştı") tam da kaydedilmediği an olurdu.</para>
///
/// <para><b>Bedeli: iz en-iyi-çabadır.</b> Denetim yazması patlarsa iş akışı DURMAZ; hata
/// loglanır ve devam edilir. Aksi hâlde bozuk bir denetim tablosu bütün okulu kilitlerdi.
/// Garantili iz bloklayıcı bir tasarım ister; bir okul sisteminde erişilebilirliğin kazanması
/// gerektiği kanısıyla bu seçildi ve BİLİNÇLİDİR.</para>
/// </remarks>
public sealed class AuditWriter(
    IDocumentStore store,
    IInstitutionPathLookup pathLookup,
    ILogger<AuditWriter> logger) : IAuditWriter
{
    public async Task WriteAsync(
        AuditContext context, Exception? exception, CancellationToken cancellationToken = default)
    {
        try
        {
            var (subjectId, crossed) = AuditEntryFactory.ResolveSubject(
                context.Command, context.ActorInstitutionId);

            // Sıcak yolda EK OKUMA YOK: komutların büyük çoğunluğu aktörün kendi kurumuna
            // yazar ve o dalda yol claim'den gelir. Arama yalnız sınır aşıldığında yapılır.
            string? subjectPathOverride = crossed && subjectId is { } id
                ? await pathLookup.GetPathAsync(id, cancellationToken)
                : null;

            var input = new AuditInput(
                Id: context.Id,
                OccurredAt: context.OccurredAt,
                ActorId: context.ActorId,
                ActorName: context.ActorName,
                CommandType: context.CommandType,
                Command: context.Command,
                TenantId: context.TenantId,
                ActorInstitutionId: context.ActorInstitutionId,
                ActorInstitutionPath: context.ActorInstitutionPath,
                SubjectInstitutionPathOverride: subjectPathOverride,
                DurationMs: context.ElapsedMs);

            var entry = exception is null
                ? AuditEntryFactory.Succeeded(input)
                : AuditEntryFactory.Failed(input, exception);

            // Kiracı AÇIKÇA verilir. Argümansız session yasaktır
            // (DefaultTenantUsageEnabled = false) ve kiracısız yazma istisnaya döner —
            // yani iz kaybolurdu. Kurum üstü işler platform kiracısına düşer.
            var tenantId = string.IsNullOrEmpty(context.TenantId)
                ? TenantResolution.Platform
                : context.TenantId;

            await using var session = store.LightweightSession(tenantId);
            session.Store(entry);
            await session.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // İZ EN-İYİ-ÇABADIR (bkz. sınıf yorumu). Burada fırlatmak, denetim tablosundaki
            // bir arızayı bütün okulun iş akışına yayardı.
            logger.LogError(ex,
                "Denetim satırı yazılamadı — Komut: {CommandType}, Aktör: {ActorId}",
                context.CommandType.Name, context.ActorId);
        }
    }
}
```

- [ ] **Step 3: `AuditMiddleware`**

`src/Modules/Audit/MESNET.Audit.Application/Auditing/AuditMiddleware.cs`:

```csharp
using System.Runtime.ExceptionServices;
using MESNET.Common.Infrastructure.Security;
using Wolverine;

namespace MESNET.Audit.Application.Auditing;

/// <summary>
/// Her yazma komutunu denetim izine kaydeden GENEL Wolverine middleware'i.
/// </summary>
/// <remarks>
/// <para><b>Hook şekli ÖLÇÜLDÜ (Wolverine 6.15.0, 28.08.2026) — değiştirmeyin:</b></para>
/// <list type="number">
/// <item><c>Before</c> → [handler] → <c>After</c> (yalnız başarıda) → <c>Finally</c>
/// (her zaman) → <c>OnException</c> (yalnız istisnada, <c>Finally</c>'den SONRA).</item>
/// <item><c>OnException</c>, <c>Before</c>'un DÖNDÜRDÜĞÜ değeri göremez —
/// <c>CS0103: The name 'ctx' does not exist in the current context</c>. Yalnız
/// <c>Exception</c>, <see cref="Envelope"/> ve DI servisleri alabilir.</item>
/// <item><b><c>OnException</c> istisnayı YUTAR.</b> Rethrow edilmezse çağıran hiçbir istisna
/// görmez; <c>DomainException</c> kaybolur, HTTP 422 doğmaz ve <b>reddedilen komut başarılı
/// görünür</b>. <see cref="ExceptionDispatchInfo"/> ile rethrow ZORUNLUDUR ve
/// <c>AuditMiddlewareContractTests</c> ile kilitlidir.</item>
/// </list>
///
/// <para><b>Neden statik sınıf ama <c>AddMiddleware&lt;T&gt;</c> DEĞİL:</b> tip parametreli
/// aşırı yükleme statik sınıf kabul etmez (<c>CS0718</c>). Kayıt
/// <c>opts.Policies.AddMiddleware(typeof(AuditMiddleware), filter)</c> ile yapılır.</para>
///
/// <para><b>Yetki reddi (403) buraya ULAŞMAZ</b> — ASP.NET yetkilendirme katmanı isteği
/// handler'dan önce keser. Bilinen ve kabul edilen bedeldir.</para>
/// </summary>
public static class AuditMiddleware
{
    /// <summary>
    /// Bağlamı kurar, <see cref="AuditContextAccessor"/>'e koyar ve döndürür.
    /// </summary>
    /// <remarks>
    /// <b>Hem döndürülür hem accessor'a konur</b> ve bu bir tekrar değildir: döndürülen değer
    /// <c>After</c>/<c>Finally</c>'nin parametresi olur (Wolverine değişken zincirlemesi),
    /// accessor ise <c>OnExceptionAsync</c>'in TEK erişim yoludur — catch bloğu try'dan önce
    /// üretilen değişkenleri göremez (ölçüldü, <c>CS0103</c>).
    /// </remarks>
    public static AuditContext Before(
        Envelope envelope, ICurrentUserService currentUser, AuditContextAccessor accessor)
    {
        var actor = currentUser.GetCurrentUser();

        var context = new AuditContext
        {
            ActorId = actor?.UserId ?? Guid.Empty,
            // Denormalize: kullanıcı kaydı silinse bile iz okunur kalmalı, ayrıca okuma
            // anında ad çözmek modüller arası sorgu demektir ve yasaktır.
            ActorName = actor?.FullName ?? string.Empty,
            CommandType = envelope.Message?.GetType() ?? typeof(object),
            Command = envelope.Message,
            TenantId = envelope.TenantId,
            ActorInstitutionId = actor?.InstitutionId,
            ActorInstitutionPath = currentUser.GetInstitutionPath(),
        };

        accessor.Set(context);
        return context;
    }

    /// <summary>Yalnız başarı yolunda çalışır. Tek işi bayrağı kaldırmak.</summary>
    public static void After(AuditContext auditContext) => auditContext.MarkSucceeded();

    /// <summary>
    /// Her zaman çalışır ama <b>yalnız başarıda yazar</b>. Başarısızlık satırının sahibi
    /// <see cref="OnExceptionAsync"/>'dir; <c>Finally</c> istisnayı göremez.
    /// </summary>
    public static async Task FinallyAsync(
        AuditContext auditContext, IAuditWriter writer, CancellationToken cancellationToken)
    {
        if (!auditContext.Succeeded) return;

        await writer.WriteAsync(auditContext, exception: null, cancellationToken);
    }

    /// <summary>
    /// Başarısızlık satırını yazar ve <b>istisnayı yeniden fırlatır</b>.
    /// </summary>
    /// <remarks>
    /// <b>Rethrow SİLİNEMEZ.</b> Silinirse Wolverine istisnayı yutar: <c>DomainException</c>
    /// HTTP katmanına hiç ulaşmaz, 422 yerine 200 döner ve reddedilen her komut başarılı
    /// görünür. Ölçüldü. Kilitleyen test: <c>AuditMiddlewareContractTests</c>.
    /// </remarks>
    public static async Task OnExceptionAsync(
        Exception exception,
        Envelope envelope,
        AuditContextAccessor accessor,
        IAuditWriter writer,
        CancellationToken cancellationToken)
    {
        if (accessor.Current is { } auditContext)
            await writer.WriteAsync(auditContext, exception, cancellationToken);

        ExceptionDispatchInfo.Capture(exception).Throw();
    }
}
```

**`AuditContextAccessor` neden var — dikkatle okuyun.** `OnExceptionAsync`, `Before`'un döndürdüğü `AuditContext`'i **göremez** (ölçüldü, `CS0103`). Bağlam bu yüzden ikinci bir yoldan taşınır: `Before` onu hem döndürür (`After`/`Finally` için) hem accessor'a koyar (`OnExceptionAsync` için).

`src/Modules/Audit/MESNET.Audit.Application/Auditing/AuditContextAccessor.cs`:

```csharp
namespace MESNET.Audit.Application.Auditing;

/// <summary>
/// <c>Before</c>'da kurulan bağlamı <c>OnExceptionAsync</c>'e taşıyan <b>scoped</b> köprü.
/// </summary>
/// <remarks>
/// <para><b>Neden var:</b> Wolverine'in ürettiği kodda <c>catch</c> bloğu, <c>try</c>'dan
/// önce üretilen değişkenleri görmez — <c>OnException(Exception, AuditContext)</c> yazmak
/// derlemeyi <c>CS0103</c> ile kırar (ölçüldü). Bağlamı taşımanın tek yolu DI'dır.</para>
///
/// <para><b>Neden <c>AsyncLocal</c> değil scoped servis:</b> istek kapsamı Wolverine
/// <c>InvokeAsync</c> çağrısının tamamını sarar ve DI zaten o kapsamı yönetir;
/// <c>AsyncLocal</c> eklemek ikinci bir yaşam döngüsü icat etmek olurdu.</para>
///
/// <para><b>Tek komut varsayımı:</b> bir kapsamda iç içe komut çalışırsa (handler içinden
/// <c>InvokeAsync</c>) iç komut dıştakini EZER ve dış komutun istisna satırı iç komutun
/// bağlamıyla yazılır. Bu depoda handler'dan handler'a <c>InvokeAsync</c> YASAKTIR
/// (CLAUDE.md); varsayım oraya dayanır.</para>
/// </remarks>
public sealed class AuditContextAccessor
{
    public AuditContext? Current { get; private set; }

    public void Set(AuditContext context) => Current = context;
}
```

- [ ] **Step 4: `ServiceRegistration` (Application + Api)**

`src/Modules/Audit/MESNET.Audit.Application/ServiceRegistration.cs`:

```csharp
using MESNET.Audit.Application.Auditing;
using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Audit.Application;

public static class ServiceRegistration
{
    public static IServiceCollection AddAuditApplication(this IServiceCollection services)
    {
        services.AddScoped<AuditContextAccessor>();
        services.AddScoped<IAuditWriter, AuditWriter>();
        return services;
    }
}
```

`src/Modules/Audit/MESNET.Audit.Api/ModuleServiceRegistration.cs`:

```csharp
using MESNET.Audit.Application;
using MESNET.Audit.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Audit.Api;

public static class ModuleServiceRegistration
{
    public static IServiceCollection AddAuditModule(this IServiceCollection services)
    {
        services.AddAuditPersistence();
        services.AddAuditApplication();
        return services;
    }
}
```

- [ ] **Step 5: `Program.cs` — modül ve middleware kaydı**

Modül kaydı, `builder.Services.AddSecurityModule();` satırının hemen ALTINA:

```csharp
    builder.Services.AddAuditModule();
```

Middleware kaydı, `UseWolverine` bloğunda, **`InstitutionScopeGuardMiddleware` kaydının hemen ALTINA**:

```csharp
        // Denetim izi (C parçası) — her YAZMA komutu. Süzgeç ad alanı konvansiyonudur;
        // Queries/ ve Consumers/ dışarıda kalır (okuma iz üretmez, tüketici kullanıcı
        // eylemi değildir).
        //
        // TİP PARAMETRELİ AŞIRI YÜKLEME KULLANILAMAZ: AddMiddleware<T> statik sınıf almaz
        // (CS0718: static types cannot be used as type arguments). Ölçüldü.
        opts.Policies.AddMiddleware(
            typeof(MESNET.Audit.Application.Auditing.AuditMiddleware),
            chain => MESNET.Audit.Application.Auditing.AuditCommandFilter.ShouldAudit(chain.MessageType));
```

`src/MESNET.Presentation/MESNET.Presentation.csproj`'a proje referansını ekleyin:

```xml
    <ProjectReference Include="../Modules/Audit/MESNET.Audit.Api/MESNET.Audit.Api.csproj" />
```

Wolverine handler keşfine Audit'i **EKLEMEYİN** — Audit modülünde handler yoktur (sorgu handler'ı Görev 9'da eklenecek ve o zaman `opts.Discovery.IncludeAssembly` satırı da eklenecek).

- [ ] **Step 6: Sözleşme testini yaz — planın en önemli testi**

`tests/MESNET.Audit.UnitTests/AuditMiddlewareContractTests.cs`:

```csharp
using MESNET.Audit.Application.Auditing;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Wolverine;

namespace MESNET.Audit.UnitTests;

/// <summary>
/// Denetim middleware'inin CANLI Wolverine ana bilgisayarındaki sözleşmesi.
/// </summary>
/// <remarks>
/// <para><b>Bu testin varlık nedeni ölçülmüş iki davranıştır:</b></para>
/// <list type="number">
/// <item><b>Reddedilen komut iz bırakmalıdır.</b> Wolverine'in <c>AutoApplyTransactions()</c>
/// politikası <c>DomainException</c>'da işlemi geri alır; denetim satırı aynı oturumda
/// yazılsaydı ret kaydı da geri alınırdı. Ayrı oturum kararını bu test kilitler.</item>
/// <item><b><c>OnException</c> istisnayı YUTAR.</b> Rethrow silinirse
/// <c>DomainException</c> HTTP katmanına hiç ulaşmaz: 422 yerine 200 döner ve reddedilen
/// her komut başarılı görünür. Derleme geçer, diğer birim testleri geçer, log temiz kalır.
/// Bu testin ikinci iddiası tam olarak o sessiz felakete karşıdır.</item>
/// </list>
///
/// <para>Sahte yazıcı kullanılır — Marten/PostgreSQL gerekmez. Ölçülen şey yazıcının
/// ÇAĞRILDIĞI ve istisnanın ÇAĞIRANA ULAŞTIĞIDIR.</para>
/// </remarks>
public class AuditMiddlewareContractTests
{
    private sealed record OrnekKomut(Guid StudentId, bool Reddet);

    public static class OrnekKomutHandler
    {
        public static string Handle(OrnekKomut command)
        {
            if (command.Reddet)
                throw new DomainException(new Error("KURAL_IHLALI", "İş kuralı izin vermedi."));

            return "tamam";
        }
    }

    private sealed class SahteYazici : IAuditWriter
    {
        public List<(string CommandType, Exception? Exception)> Yazilanlar { get; } = [];

        public Task WriteAsync(AuditContext context, Exception? exception, CancellationToken ct = default)
        {
            Yazilanlar.Add((context.CommandType.Name, exception));
            return Task.CompletedTask;
        }
    }

    private sealed class SahteKullanici : ICurrentUserService
    {
        private static readonly UserContext Kullanici = new(
            UserId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            FullName: "Ayşe Öğretmen",
            InstitutionId: Guid.Parse("22222222-2222-2222-2222-222222222222"));

        public UserContext? GetCurrentUser() => Kullanici;
        public Guid GetUserId() => Kullanici.UserId;
        public string GetFullName() => Kullanici.FullName;
        public bool HasPermission(string permission) => false;
        public bool IsInRole(string role) => false;
        public IReadOnlyList<string> GetBranchCodes() => [];
        public IReadOnlyList<Guid> GetLinkedStudentIds() => [];
        public string? GetInstitutionPath() => "/il/ilce/okul/";
    }

    private static async Task<(IHost Host, SahteYazici Yazici)> AnaBilgisayarKurAsync()
    {
        var yazici = new SahteYazici();

        var host = await Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IAuditWriter>(yazici);
                services.AddScoped<AuditContextAccessor>();
                services.AddSingleton<ICurrentUserService, SahteKullanici>();
            })
            .UseWolverine(opts =>
            {
                opts.Policies.AddMiddleware(
                    typeof(AuditMiddleware),
                    chain => chain.MessageType == typeof(OrnekKomut));
            })
            .StartAsync();

        return (host, yazici);
    }

    [Fact]
    public async Task Basarili_komut_bir_iz_satiri_birakir()
    {
        // Arrange
        var (host, yazici) = await AnaBilgisayarKurAsync();
        await using var _ = host;
        var bus = host.Services.GetRequiredService<IMessageBus>();

        // Act
        var sonuc = await bus.InvokeAsync<string>(new OrnekKomut(Guid.NewGuid(), Reddet: false));

        // Assert
        sonuc.ShouldBe("tamam");
        yazici.Yazilanlar.Count.ShouldBe(1);
        yazici.Yazilanlar[0].CommandType.ShouldBe(nameof(OrnekKomut));
        yazici.Yazilanlar[0].Exception.ShouldBeNull();

        await host.StopAsync();
    }

    [Fact]
    public async Task Reddedilen_komut_da_iz_satiri_birakir()
    {
        // AYRI OTURUM kararının kilidi: aynı oturuma dönülürse bu satır geri alınırdı.
        var (host, yazici) = await AnaBilgisayarKurAsync();
        await using var _ = host;
        var bus = host.Services.GetRequiredService<IMessageBus>();

        await Should.ThrowAsync<DomainException>(
            () => bus.InvokeAsync<string>(new OrnekKomut(Guid.NewGuid(), Reddet: true)));

        yazici.Yazilanlar.Count.ShouldBe(1);
        yazici.Yazilanlar[0].Exception.ShouldBeOfType<DomainException>();

        await host.StopAsync();
    }

    [Fact]
    public async Task Reddedilen_komutun_istisnasi_CAGIRANA_ULASIR()
    {
        // OnException rethrow'unun kilidi. Rethrow silinirse Wolverine istisnayı yutar,
        // DomainException HTTP katmanına ulaşmaz ve 422 yerine 200 döner — reddedilen her
        // komut başarılı görünür. Ölçüldü.
        var (host, _) = await AnaBilgisayarKurAsync();
        await using var __ = host;
        var bus = host.Services.GetRequiredService<IMessageBus>();

        var ex = await Should.ThrowAsync<DomainException>(
            () => bus.InvokeAsync<string>(new OrnekKomut(Guid.NewGuid(), Reddet: true)));

        ex.Error.Code.ShouldBe("KURAL_IHLALI");

        await host.StopAsync();
    }

    [Fact]
    public async Task Reddedilen_komut_TEK_satir_birakir()
    {
        // Finally hem başarıda hem başarısızlıkta çalışır; koşul kalkarsa istisna yolunda
        // İKİ satır doğar (biri yanlışlıkla "başarılı").
        var (host, yazici) = await AnaBilgisayarKurAsync();
        await using var _ = host;
        var bus = host.Services.GetRequiredService<IMessageBus>();

        await Should.ThrowAsync<DomainException>(
            () => bus.InvokeAsync<string>(new OrnekKomut(Guid.NewGuid(), Reddet: true)));

        yazici.Yazilanlar.Count.ShouldBe(1);

        await host.StopAsync();
    }
}
```

- [ ] **Step 7: Testi koştur**

```bash
dotnet test tests/MESNET.Audit.UnitTests --filter "FullyQualifiedName~AuditMiddlewareContract"
```

Beklenen: 4/4 PASS.

Ana bilgisayar `no IAssemblyGenerator (Roslyn) is registered` ile açılmıyorsa `WolverineFx.RuntimeCompilation` paket referansı eksiktir (Görev 1, Step 9).

- [ ] **Step 8: KANIT ADIMLARI (üçü de zorunlu)**

1. `AuditMiddleware.OnExceptionAsync`'teki `ExceptionDispatchInfo.Capture(exception).Throw();` satırını **silin**. Beklenen: `Reddedilen_komutun_istisnasi_CAGIRANA_ULASIR` ve `Reddedilen_komut_da_iz_satiri_birakir` KIRMIZI (istisna hiç fırlamaz). Raporunuza yazın, geri alın.
2. `FinallyAsync`'teki `if (!auditContext.Succeeded) return;` satırını **silin**. Beklenen: `Reddedilen_komut_TEK_satir_birakir` KIRMIZI (2 satır). Raporunuza yazın, geri alın.
3. `After` metodunu **silin**. Beklenen: `Basarili_komut_bir_iz_satiri_birakir` KIRMIZI (0 satır). Raporunuza yazın, geri alın.

- [ ] **Step 9: Tüm çözümü derle ve koştur**

```bash
dotnet build MESNET.slnx
dotnet test tests/MESNET.Audit.UnitTests
```

- [ ] **Step 10: Commit**

```bash
git add src/Modules/Audit src/MESNET.Presentation tests/MESNET.Audit.UnitTests
git commit -m "feat(audit): genel denetim middleware'i + ayrı oturumlu yazıcı"
```

---

### Task 8: `audit:` izin öneki + rol eşlemesi

**Files:**
- Modify: `src/MESNET.Common.Shared/Security/Permissions.cs`
- Modify: `src/MESNET.Common.Shared/Security/RolePermissionMap.cs`
- Modify: `src/MESNET.Common.Shared/Security/AssignablePermissionScope.cs`
- Test: `tests/MESNET.Security.UnitTests/AuditPermissionMappingTests.cs`
- Modify: `src/Docs/docs/actors/permissions.md` (matris testinin yazdırdığı metin)

**Interfaces:**
- Produces: `Permissions.Audit.ViewInstitution` = `"audit:view:institution"`

- [ ] **Step 1: Başarısız testi yaz**

`tests/MESNET.Security.UnitTests/AuditPermissionMappingTests.cs`:

```csharp
using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Denetim izi okuma izninin rol haritasındaki yeri (C parçası).
///
/// <para><b>Neden YENİ bir önek:</b> <c>institution:</c> önekli bir izin
/// <c>InstitutionManager</c>'ın <c>institution:*</c> wildcard'ı üzerinden HER okul müdürüne
/// geçerdi (ADR-0002 önek tuzağı — #126'da alan muafiyeti izninde bire bir yaşandı). Okul
/// müdürünün kendi okulunun izini görmesi İSTENEN bir şeydir, ama kararın wildcard'ın yan
/// etkisiyle değil AÇIKÇA verilmesi gerekir. Bu testler o açıklığı kilitler.</para>
/// </summary>
public sealed class AuditPermissionMappingTests
{
    private static IReadOnlyList<string> PermissionsOf(string role)
        => RolePermissionMap.GetPermissionsForRoles([role]);

    public static TheoryData<string> IzniOlanRoller =>
    [
        MesnetRoles.InstitutionManager,
        MesnetRoles.DeputyDirector,
    ];

    public static TheoryData<string> IzniOlmayanRoller =>
    [
        MesnetRoles.InstitutionStaff,
        MesnetRoles.Teacher,
        MesnetRoles.DepartmentHead,
        MesnetRoles.CompanyManager,
        MesnetRoles.MasterTrainer,
        MesnetRoles.CompanyHR,
        MesnetRoles.Student,
        MesnetRoles.Parent,
        MesnetRoles.ProvincialAdmin,
        MesnetRoles.DistrictAdmin,
        MesnetRoles.SystemAdmin,
    ];

    [Theory]
    [MemberData(nameof(IzniOlanRoller))]
    public void Kurum_denetim_izini_okuyabilen_roller(string role)
    {
        PermissionsOf(role).ShouldContain(Permissions.Audit.ViewInstitution);
    }

    [Theory]
    [MemberData(nameof(IzniOlmayanRoller))]
    public void Diger_hicbir_rol_kurum_denetim_izini_okuyamaz(string role)
    {
        // Wildcard sızıntısının kilidi: "audit:" öneki hiçbir rolün wildcard'ında yok.
        PermissionsOf(role).ShouldNotContain(Permissions.Audit.ViewInstitution);
    }

    [Fact]
    public void Hicbir_rolun_wildcardi_audit_onekini_yutmaz()
    {
        // Doğrudan kaynak taraması: "audit:*" biçiminde bir wildcard eklenirse test kırılır.
        foreach (var role in MesnetRoles.All)
        {
            RolePermissionMap.GetRawPermissionsForRole(role)
                .ShouldNotContain("audit:*", $"{role} rolüne audit: wildcard'ı eklenmiş.");
        }
    }

    [Fact]
    public void Denetim_izni_bireysel_atanamaz()
    {
        // InstitutionManager'ın atanabilir kapsamı "*"tır. Bu koruma olmasaydı okul müdürü
        // denetim görünürlüğünü herhangi bir kullanıcıya — bir İŞLETME kullanıcısına bile —
        // verebilirdi; o kullanıcı okulun bütün eylem günlüğünü okurdu.
        AssignablePermissionScope.NeverDirectlyAssignable
            .ShouldContain(Permissions.Audit.ViewInstitution);
    }

    [Fact]
    public void Audit_oneki_atanabilir_domain_listesinde_YOKTUR()
    {
        AssignablePermissionScope.AllDomains.ShouldNotContain("audit:");
    }
}
```

- [ ] **Step 2: Testi koştur, kırmızı gör**

```bash
dotnet test tests/MESNET.Security.UnitTests --filter "FullyQualifiedName~AuditPermissionMapping"
```

Beklenen: `Permissions.Audit` yok — derleme hatası.

- [ ] **Step 3: `Permissions.Audit` sınıfını ekle**

`src/MESNET.Common.Shared/Security/Permissions.cs` içine, `Platform` sınıfının hemen ALTINA:

```csharp
    /// <summary>
    /// Denetim izi (C parçası).
    /// </summary>
    /// <remarks>
    /// <para><b>Neden ayrı bir önek, neden <c>institution:</c> DEĞİL:</b> <c>institution:</c>
    /// önekli bir izin <c>InstitutionManager</c>'ın <c>institution:*</c> wildcard'ı üzerinden
    /// her okul müdürüne sessizce geçerdi (ADR-0002 önek tuzağı). Okul müdürünün kendi
    /// okulunun izini görmesi istenen bir şeydir; ama kararın wildcard'ın yan etkisiyle değil
    /// AÇIKÇA verilmesi gerekir. Yeni ve çakışmasız önek bunu sağlar.</para>
    ///
    /// <para><b>"Kendi işlemlerim" için izin YOKTUR</b> ve bu bilinçlidir: kullanıcının kendi
    /// geçmişini görmesi bir yetki sorusu değildir. Kapsam <c>ActorId == aktör</c> ile
    /// sunucuda daraltılır.</para>
    /// </remarks>
    public static class Audit
    {
        /// <summary>Kendi kurum ağacının (yol öneki) denetim izini okuma.</summary>
        public const string ViewInstitution = "audit:view:institution";
    }
```

- [ ] **Step 4: Rol eşlemesini ekle**

`RolePermissionMap.cs` — `InstitutionManager` demetinin sonuna:

```csharp
            // Denetim izi (C parçası). "institution:*" bunu KAPSAMAZ ve kapsamamalıdır —
            // önek bilerek ayrıdır. Bu yüzden açıkça yazılır.
            Permissions.Audit.ViewInstitution
```

`DeputyDirector` demetinin sonuna aynı satır (aynı yorumla).

**Başka HİÇBİR role eklemeyin.** İl/ilçe yetkilisi izi B parçasında alacaktır; C'nin işi izi kurmaktır, dağıtmak değil.

- [ ] **Step 5: `NeverDirectlyAssignable`'a ekle**

`AssignablePermissionScope.cs` içindeki `NeverDirectlyAssignable` kümesine:

```csharp
            // Denetim izi okuma (C parçası). InstitutionManager'ın atanabilir kapsamı "*"tır;
            // bu koruma olmasaydı okul müdürü denetim görünürlüğünü herhangi bir kullanıcıya —
            // bir İŞLETME kullanıcısına bile — verebilirdi ve o kullanıcı okulun bütün eylem
            // günlüğünü okurdu. Rol → domain haritası çalışma zamanında değiştirilebildiği
            // için (user:roles:manage) yalnız yapılandırmaya güvenmek yetmez.
            Permissions.Audit.ViewInstitution,
```

`AllDomains`'e `"audit:"` **EKLEMEYİN**.

- [ ] **Step 6: Testleri koştur**

```bash
dotnet test tests/MESNET.Security.UnitTests
```

`PermissionMatrixDocTests` KIRMIZI olacaktır — beklenen davranıştır: yeni izin eklendi. Test doğru metni üretir; onu `src/Docs/docs/actors/permissions.md` dosyasına yazın ve testi tekrar koşun.

Ayrıca `permissions.md` içindeki "Alan (Branş) Kapsamı Kontrolü" bölümünün ardına kısa bir "Denetim İzi" başlığı ekleyin: hangi rolün izi gördüğü, kapsamın yol öneki olduğu ve `audit:` önekinin neden ayrı olduğu — üç cümle.

- [ ] **Step 7: KANIT ADIMI (zorunlu)**

`RolePermissionMap`'te `InstitutionManager` demetine eklediğiniz `Permissions.Audit.ViewInstitution` satırını **silin**, `AuditPermissionMappingTests`'i koşun. Beklenen: `Kurum_denetim_izini_okuyabilen_roller` KIRMIZI ve `InstitutionManager` adını veriyor. Raporunuza yazın, geri alın.

İkinci kanıt: `Permissions.Audit.ViewInstitution` değerini geçici olarak `"institution:audit:view"` yapın ve tüm `MESNET.Security.UnitTests`'i koşun. Beklenen: `Diger_hicbir_rol_kurum_denetim_izini_okuyamaz` **birden çok rol için** KIRMIZI (wildcard sızıntısı gerçekleşti). Raporunuza kaç rolün kırıldığını yazın, sonra geri alın. Bu, önek kararının gerçekten load-bearing olduğunun ölçümüdür.

- [ ] **Step 8: Commit**

```bash
git add src/MESNET.Common.Shared/Security tests/MESNET.Security.UnitTests/AuditPermissionMappingTests.cs src/Docs/docs/actors/permissions.md
git commit -m "feat(audit): audit: izin öneki ve rol eşlemesi"
```

---

### Task 9: Okuma — sorgu, DTO ve handler

Okuma süzgeci **A parçasındaki `InstitutionScopePolicy.VisibleScope`'u yeniden kullanır**; yeni kapsam ekseni doğmaz.

**Files:**
- Create: `src/Modules/Audit/MESNET.Audit.Application/Queries/GetAuditEntries.cs`
- Create: `src/Modules/Audit/MESNET.Audit.Application/Dtos/AuditEntryDto.cs`
- Create: `src/Modules/Audit/MESNET.Audit.Application/Extensions/AuditMappingExtensions.cs`
- Create: `src/Modules/Audit/MESNET.Audit.Application/Handlers/GetAuditEntriesHandler.cs`
- Modify: `src/MESNET.Presentation/Program.cs` (`opts.Discovery.IncludeAssembly`)

**Interfaces:**
- Consumes: `InstitutionScopePolicy.VisibleScope`, `QueryableExtensions` (`ApplySort`, `ApplySearch`, `ToPagedResultAsync`)
- Produces: `GetAuditEntries` (PagedQuery), `AuditEntryDto`, `GetAuditEntriesHandler.Handle`

- [ ] **Step 1: Sorgu record'u**

`src/Modules/Audit/MESNET.Audit.Application/Queries/GetAuditEntries.cs`:

```csharp
using MESNET.Common.Shared.Pagination;

namespace MESNET.Audit.Application.Queries;

/// <summary>
/// Denetim izi listesi.
/// </summary>
/// <param name="Scope">
/// <c>"mine"</c> = yalnız aktörün kendi işlemleri (izin GEREKTİRMEZ — kendi geçmişini görmek
/// bir yetki sorusu değildir). <c>"institution"</c> = kurum ağacı (yol öneki), uç seviyesinde
/// <c>audit:view:institution</c> ile korunur.
/// </param>
public sealed record GetAuditEntries(
    string Scope,
    Guid? ActorId = null,
    string? CommandType = null,
    string? Outcome = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    bool? CrossedTenantBoundary = null) : PagedQuery
{
    public const string ScopeMine = "mine";
    public const string ScopeInstitution = "institution";
}
```

- [ ] **Step 2: DTO + eşleyici**

`src/Modules/Audit/MESNET.Audit.Application/Dtos/AuditEntryDto.cs`:

```csharp
namespace MESNET.Audit.Application.Dtos;

/// <param name="OutcomeSlug">Türkçe rozet metni; arayüz kendi eşleme tablosunu tutmaz.</param>
public sealed record AuditEntryDto(
    Guid Id,
    DateTimeOffset OccurredAt,
    Guid ActorId,
    string ActorName,
    string CommandType,
    string CommandLabel,
    string Module,
    Guid? SubjectInstitutionId,
    bool CrossedTenantBoundary,
    string Outcome,
    string OutcomeSlug,
    string? ErrorCode,
    IReadOnlyDictionary<string, Guid> TargetIds,
    int DurationMs);
```

`src/Modules/Audit/MESNET.Audit.Application/Extensions/AuditMappingExtensions.cs`:

```csharp
using MESNET.Audit.Application.Dtos;
using MESNET.Audit.Core.Entities;

namespace MESNET.Audit.Application.Extensions;

public static class AuditMappingExtensions
{
    /// <remarks>
    /// <b>Kiracı kimliği ve yol DTO'ya ÇIKMAZ.</b> İkisi de kapsam kararının iç girdisidir;
    /// dışarı verilmeleri, kapsamı olmayan bir okuyucuya ağacın şeklini sızdırırdı.
    /// </remarks>
    public static AuditEntryDto ToDto(this AuditEntry entry) => new(
        entry.Id,
        entry.OccurredAt,
        entry.ActorId,
        entry.ActorName,
        entry.CommandType,
        entry.CommandLabel,
        entry.Module,
        entry.SubjectInstitutionId,
        entry.CrossedTenantBoundary,
        entry.Outcome.Name,
        entry.Outcome.Slug,
        entry.ErrorCode,
        entry.TargetIds,
        entry.DurationMs);
}
```

- [ ] **Step 3: Handler**

`src/Modules/Audit/MESNET.Audit.Application/Handlers/GetAuditEntriesHandler.cs`:

```csharp
using Marten;
using MESNET.Audit.Application.Dtos;
using MESNET.Audit.Application.Extensions;
using MESNET.Audit.Application.Queries;
using MESNET.Audit.Core.Entities;
using MESNET.Common.Infrastructure.Pagination;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared.Pagination;
using MESNET.Common.Shared.Security;

namespace MESNET.Audit.Application.Handlers;

/// <summary>
/// Denetim izi listesi.
/// </summary>
/// <remarks>
/// <para><b>Yeni kapsam ekseni DOĞMAZ.</b> Kurum kapsamı A parçasındaki
/// <see cref="InstitutionScopePolicy.VisibleScope"/> ile aynıdır:
/// <c>SubjectInstitutionPath.StartsWith(okuyucununYolu)</c>. Marten
/// <c>string.StartsWith</c>'i SQL'de <c>LIKE 'önek%'</c> çevirir.</para>
///
/// <para><b>Kiracılık tek başına yetmez</b> ve bu yüzden yol süzgeci ZORUNLUDUR: kiracı
/// damgası satırı okulun içinde tutar, ama il yetkilisi bir gün (B parçası) birden çok
/// kiracıya erişince ayrım yalnız yoldan gelir.</para>
///
/// <para><b><c>OutcomeName</c> ile süzülür, <c>Outcome.Name</c> ile DEĞİL:</b> SmartEnum
/// JSON'a düz string yazılır; <c>data->'outcome'->>'Name'</c> her zaman NULL döner ve süzgeç
/// sessizce hiçbir şey bulmaz.</para>
/// </remarks>
public static class GetAuditEntriesHandler
{
    public static async Task<PagedResult<AuditEntryDto>> Handle(
        GetAuditEntries query,
        IQuerySession session,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var actor = currentUser.GetCurrentUser();

        IQueryable<AuditEntry> queryable = session.Query<AuditEntry>();

        queryable = ApplyScope(queryable, query, currentUser, actor?.UserId);

        if (query.ActorId is { } actorId)
            queryable = queryable.Where(e => e.ActorId == actorId);

        if (!string.IsNullOrWhiteSpace(query.CommandType))
            queryable = queryable.Where(e => e.CommandType == query.CommandType);

        if (!string.IsNullOrWhiteSpace(query.Outcome))
            queryable = queryable.Where(e => e.OutcomeName == query.Outcome);

        if (query.From is { } from)
            queryable = queryable.Where(e => e.OccurredAt >= from);

        if (query.To is { } to)
            queryable = queryable.Where(e => e.OccurredAt <= to);

        if (query.CrossedTenantBoundary is { } crossed)
            queryable = queryable.Where(e => e.CrossedTenantBoundary == crossed);

        queryable = queryable.ApplySearch(query.Search, e => e.ActorName, e => e.CommandLabel);

        // Sıralama ZORUNLU: sırasız liste her yazmadan sonra kayar (Postgres güncellenen
        // satırı heap'te yerinden oynatır). Denetim izinde varsayılan yeniden eskiye.
        queryable = queryable.ApplySort(
            query.SortBy, descending: query.SortBy is null || query.Descending,
            defaultSort: e => e.OccurredAt);

        var page = await queryable.ToPagedResultAsync(query, cancellationToken);

        return PagedResult<AuditEntryDto>.Create(
            page.Items.Select(e => e.ToDto()).ToList(),
            page.TotalCount,
            page.Page,
            page.PageSize);
    }

    /// <summary>
    /// Kapsam daraltması. İki mod vardır ve <b>ikisi de sunucudadır</b>; istemcinin gönderdiği
    /// <c>scope</c> bir NİYETTİR, yetki değil — <c>institution</c> modunun izni uç seviyesinde
    /// kontrol edilir (<c>audit:view:institution</c>).
    /// </summary>
    private static IQueryable<AuditEntry> ApplyScope(
        IQueryable<AuditEntry> queryable,
        GetAuditEntries query,
        ICurrentUserService currentUser,
        Guid? actorUserId)
    {
        if (!string.Equals(query.Scope, GetAuditEntries.ScopeInstitution, StringComparison.Ordinal))
        {
            // "Kendi işlemlerim". Kimliği çözülemeyen aktörde Guid.Empty hiçbir satırla
            // eşleşmez — her şeyi görmek yerine hiçbir şey görmek.
            var userId = actorUserId ?? Guid.Empty;
            return queryable.Where(e => e.ActorId == userId);
        }

        var scope = InstitutionScopePolicy.VisibleScope(
            currentUser.GetCurrentUser()?.InstitutionId,
            currentUser.GetInstitutionPath(),
            currentUser.HasPermission(Permissions.Platform.TenantManage));

        if (scope.Unrestricted)
            return queryable;

        if (scope.PathPrefix is { } prefix)
        {
            return queryable.Where(e =>
                e.SubjectInstitutionPath != null && e.SubjectInstitutionPath.StartsWith(prefix));
        }

        // Yol yok: kimliğe düş — geçiş ucu koşmamış kurumda bugünkü davranış korunur.
        var institutionId = scope.InstitutionId ?? Guid.Empty;
        return queryable.Where(e => e.SubjectInstitutionId == institutionId);
    }
}
```

- [ ] **Step 4: Wolverine handler keşfine Audit'i ekle**

`src/MESNET.Presentation/Program.cs`, `opts.Discovery.IncludeAssembly(...)` bloğunun sonuna:

```csharp
        opts.Discovery.IncludeAssembly(typeof(MESNET.Audit.Application.Queries.GetAuditEntries).Assembly);
```

- [ ] **Step 5: Derle**

```bash
dotnet build MESNET.slnx
```

`ApplySearch` iki alan alıyorsa imzayı doğrulayın; `QueryableExtensions.ApplySearch` params ifade dizisi alır (`src/MESNET.Common.Infrastructure/Pagination/QueryableExtensions.cs`). İmza uymazsa **helper'ı değiştirmeyin**, çağrıyı ona uydurun.

- [ ] **Step 6: Commit**

```bash
git add src/Modules/Audit/MESNET.Audit.Application src/MESNET.Presentation/Program.cs
git commit -m "feat(audit): denetim izi sorgusu — kapsam A parçasının yol önekini kullanır"
```

---

### Task 10: Uç noktası

**Files:**
- Create: `src/Modules/Audit/MESNET.Audit.Api/AuditEndpoints.cs`
- Modify: `src/MESNET.Presentation/Program.cs`

- [ ] **Step 1: Uçları yaz**

`src/Modules/Audit/MESNET.Audit.Api/AuditEndpoints.cs`:

```csharp
using MESNET.Audit.Application.Dtos;
using MESNET.Audit.Application.Queries;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Pagination;
using MESNET.Common.Shared.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Audit.Api;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/audit")
            .WithTags("Audit").RequireAuthorization();

        // "Kendi işlemlerim" ek izin GEREKTİRMEZ: kullanıcının kendi geçmişini görmesi bir
        // yetki sorusu değildir. Kapsam sunucuda ActorId ile daraltılır; istemcinin
        // gönderdiği scope bir niyettir, yetki değil.
        group.MapGet("/mine", GetMine);

        // Kurum ağacı izi. Yol önekiyle daraltma handler'da; buradaki izin ERİŞİMİ açar,
        // kapsamı belirlemez.
        group.MapGet("/institution", GetForInstitution)
            .RequireAuthorization(Permissions.Audit.ViewInstitution);

        return app;
    }

    private static Task<IResult> GetMine(
        Guid? actorId, string? commandType, string? outcome,
        DateTimeOffset? from, DateTimeOffset? to,
        int page = 1, int pageSize = 20, string? sortBy = null, bool descending = true,
        string? search = null, IMessageBus bus = default!)
        => Query(
            new GetAuditEntries(GetAuditEntries.ScopeMine, actorId, commandType, outcome, from, to),
            page, pageSize, sortBy, descending, search, bus);

    private static Task<IResult> GetForInstitution(
        Guid? actorId, string? commandType, string? outcome,
        DateTimeOffset? from, DateTimeOffset? to, bool? crossedTenantBoundary,
        int page = 1, int pageSize = 20, string? sortBy = null, bool descending = true,
        string? search = null, IMessageBus bus = default!)
        => Query(
            new GetAuditEntries(
                GetAuditEntries.ScopeInstitution, actorId, commandType, outcome, from, to,
                crossedTenantBoundary),
            page, pageSize, sortBy, descending, search, bus);

    private static async Task<IResult> Query(
        GetAuditEntries query,
        int page, int pageSize, string? sortBy, bool descending, string? search,
        IMessageBus bus)
    {
        var result = await bus.InvokeAsync<PagedResult<AuditEntryDto>>(query with
        {
            Page = page,
            PageSize = pageSize,
            SortBy = sortBy,
            Descending = descending,
            Search = search,
        });

        return Results.Ok(ResponseBuilder.Success().AddData(result).Build());
    }
}
```

**Kural hatırlatması:** endpoint metotlarına `IQuerySession`/`IDocumentSession` inject etmek YASAKTIR; iş mantığı da içermezler. Yukarıdaki metotlar yalnız querystring'i sorgu record'una çevirir.

- [ ] **Step 2: `Program.cs`'e uç kaydını ekle**

`app.MapUserManagementEndpoints();` satırının hemen altına:

```csharp
    app.MapAuditEndpoints();
```

- [ ] **Step 3: Anonim uç sapma testini koştur**

```bash
dotnet test tests/MESNET.Security.UnitTests --filter "FullyQualifiedName~AnonymousEndpointDrift"
```

Beklenen: YEŞİL (grup `RequireAuthorization()` taşıyor).

- [ ] **Step 4: Derle ve tüm birim testlerini koştur**

```bash
dotnet build MESNET.slnx
dotnet test MESNET.slnx --filter "FullyQualifiedName!~MESNET.Api.Tests"
```

- [ ] **Step 5: Commit**

```bash
git add src/Modules/Audit/MESNET.Audit.Api src/MESNET.Presentation/Program.cs
git commit -m "feat(audit): GET /api/audit uçları"
```

---

### Task 11: Saklama — 24 ay, günlük temizlik

**Files:**
- Create: `src/Modules/Audit/MESNET.Audit.Application/Services/AuditRetentionService.cs`
- Modify: `src/Modules/Audit/MESNET.Audit.Application/ServiceRegistration.cs`
- Modify: `src/MESNET.Presentation/appsettings.json`

- [ ] **Step 1: Servisi yaz**

`src/Modules/Audit/MESNET.Audit.Application/Services/AuditRetentionService.cs`:

```csharp
using Marten;
using MESNET.Audit.Core.Entities;
using MESNET.Common.Infrastructure.Tenancy;
using MESNET.Common.Shared.Tenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MESNET.Audit.Application.Services;

/// <summary>
/// Yaşı geçen denetim satırlarını günlük olarak siler.
/// </summary>
/// <remarks>
/// <para><b>Süre yapılandırmadan gelir</b> (<c>Audit:RetentionMonths</c>), sabit kodlanmaz:
/// saklama süresi bir mevzuat kararıdır ve değiştiğinde yeni bir sürüm dağıtmak gerekmemeli.</para>
///
/// <para><b>Kiracı kiracı dolaşır.</b> Kiracı damgalı satırları silmek kiracı başına oturum
/// ister; kiracısız session yasaktır (<c>DefaultTenantUsageEnabled = false</c>).
/// <c>IDocumentSession</c> ENJEKTE EDİLMEZ — DI'dan gelen session kiracısızdır (proje kuralı:
/// arka plan işleri <c>IDocumentStore</c> alır).</para>
///
/// <para><b>Kaç satır silindiği kiracı başına loglanır.</b> Sessiz silme kabul edilemez: bir
/// denetim izinin kendi silinme kaydı olmadan çalışması, izin amacına aykırıdır.</para>
///
/// <para><c>platform</c> kiracısı da temizlenir — kurum üstü işlerin izi orada yaşar.</para>
/// </remarks>
public sealed class AuditRetentionService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<AuditRetentionService> logger) : BackgroundService
{
    private const int RunHourUtc = 3;
    private const int DefaultRetentionMonths = 24;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRun = CalculateNextRun(now);

            logger.LogInformation(
                "Denetim izi temizliği — sonraki çalışma: {NextRun:yyyy-MM-dd HH:mm} UTC ({Delay})",
                nextRun, nextRun - now);

            try
            {
                await Task.Delay(nextRun - now, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            try
            {
                await PurgeAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // Bir günlük koşu patlarsa servis ÖLMEMELİ — yarın tekrar denenir.
                logger.LogError(ex, "Denetim izi temizliği başarısız oldu.");
            }
        }
    }

    /// <summary>Her gün 03:00 UTC — maaş (01:00) ve rapor (00:30) koşularının dışında.</summary>
    private static DateTime CalculateNextRun(DateTime now)
    {
        var today = new DateTime(now.Year, now.Month, now.Day, RunHourUtc, 0, 0, DateTimeKind.Utc);
        return now < today ? today : today.AddDays(1);
    }

    private async Task PurgeAsync(CancellationToken cancellationToken)
    {
        var months = configuration.GetValue<int?>("Audit:RetentionMonths") ?? DefaultRetentionMonths;
        if (months <= 0)
        {
            logger.LogWarning(
                "Denetim izi temizliği atlandı — Audit:RetentionMonths geçersiz: {Months}", months);
            return;
        }

        var cutoff = DateTimeOffset.UtcNow.AddMonths(-months);

        await using var scope = scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();

        var tenants = await scope.ServiceProvider
            .GetRequiredService<ITenantDirectory>()
            .GetActiveTenantsAsync(cancellationToken);

        // Kurum üstü işlerin izi platform kiracısında yaşar; kiracı dizininde görünmez.
        var targets = tenants.Append(TenantResolution.Platform).Distinct(StringComparer.Ordinal);

        foreach (var tenantId in targets)
        {
            try
            {
                await using var session = store.LightweightSession(tenantId);

                var silinecek = await session.Query<AuditEntry>()
                    .CountAsync(e => e.OccurredAt < cutoff, cancellationToken);

                if (silinecek == 0) continue;

                session.DeleteWhere<AuditEntry>(e => e.OccurredAt < cutoff);
                await session.SaveChangesAsync(cancellationToken);

                logger.LogInformation(
                    "Denetim izi temizlendi — Kiracı: {TenantId}, Silinen: {Count}, Kesim: {Cutoff:yyyy-MM-dd}",
                    tenantId, silinecek, cutoff);
            }
            catch (Exception ex)
            {
                // Bir kiracının temizliği patlarsa diğerleri devam eder — tek okul yüzünden
                // bütün okulların izini büyütmek çok daha pahalıdır.
                logger.LogError(ex, "Denetim izi temizliği başarısız — Kiracı: {TenantId}", tenantId);
            }
        }
    }
}
```

**NOT:** `CountAsync` yalnız log için yapılan **ikinci** bir sorgudur ve bilinçlidir: "kaç satır silindi" sayısı olmadan sessiz silme yapmış oluruz. Bu servis günde bir kez çalışır; ikinci sorgunun bedeli ölçülemez.

- [ ] **Step 2: Kaydı ekle**

`src/Modules/Audit/MESNET.Audit.Application/ServiceRegistration.cs`:

```csharp
        services.AddHostedService<Services.AuditRetentionService>();
```

- [ ] **Step 3: Yapılandırma**

`src/MESNET.Presentation/appsettings.json` köküne:

```json
  "Audit": {
    "RetentionMonths": 24
  },
```

- [ ] **Step 4: Derle**

```bash
dotnet build MESNET.slnx
```

`DeleteWhere` imzası Marten 9'da `IDocumentOperations.DeleteWhere<T>(Expression<Func<T,bool>>)`'tir. Derlenmezse depodaki mevcut bir `DeleteWhere` kullanımını referans alın; **kendi silme SQL'inizi yazmayın**.

- [ ] **Step 5: Commit**

```bash
git add src/Modules/Audit/MESNET.Audit.Application src/MESNET.Presentation/appsettings.json
git commit -m "feat(audit): 24 aylık saklama süresi ve günlük temizlik"
```

---

### Task 12: Arayüz — denetim izi sayfası

**Files:**
- Create: `src/WebUI/src/api/audit.ts`
- Create: `src/WebUI/src/pages/audit/auditListQuery.ts`
- Create: `src/WebUI/src/pages/audit/AuditLogPage.vue`
- Create: `src/WebUI/src/pages/audit/AuditLogPage.spec.ts`
- Modify: `src/WebUI/src/router/index.ts`
- Modify: `src/WebUI/src/composables/useNavigation.ts`

**Referans desen (birebir izleyin):** `src/WebUI/src/pages/institution/InstitutionListPage.vue` + `institutionListQuery.ts` + `InstitutionListPage.spec.ts`. Sayfa ve test **aynı** sorgu dosyasını import eder; test kendi değerlerini YENİDEN YAZMAZ. Bu depoda ölçülmüş sahte-yeşil kalıbı budur: eski `InstitutionListPage.spec.ts` sayfayı hiç import etmiyordu ve sayfanın varsayılanı değiştirilip koşulduğunda 5/5 yeşil kalıyordu.

- [ ] **Step 1: API istemcisi**

`src/WebUI/src/api/audit.ts`:

```typescript
import { api } from 'boot/axios'
import type { PagedResponse, PaginationParams } from 'src/types/pagination'

/** Sunucudan gelen denetim satırı. `commandLabel` Türkçe etikettir — arayüz eşleme TUTMAZ. */
export interface AuditEntryDto {
  id: string
  occurredAt: string
  actorId: string
  actorName: string
  commandType: string
  commandLabel: string
  module: string
  subjectInstitutionId: string | null
  crossedTenantBoundary: boolean
  outcome: string
  outcomeSlug: string
  errorCode: string | null
  targetIds: Record<string, string>
  durationMs: number
}

export interface AuditListParams extends Record<string, unknown> {
  commandType?: string
  outcome?: string
  from?: string
  to?: string
  crossedTenantBoundary?: boolean
}

export const auditApi = {
  /** Aktörün kendi işlemleri. Ek izin gerektirmez. */
  listMine: (params?: AuditListParams & PaginationParams) =>
    api.get<PagedResponse<AuditEntryDto>>('/audit/mine', { params }),

  /** Kurum ağacının izi. `audit:view:institution` gerektirir; kapsam sunucudadır. */
  listForInstitution: (params?: AuditListParams & PaginationParams) =>
    api.get<PagedResponse<AuditEntryDto>>('/audit/institution', { params }),
}
```

`api` import yolunu depodaki diğer `api/*.ts` dosyalarından doğrulayın (`src/WebUI/src/api/institution.ts` ilk satırı).

- [ ] **Step 2: Sorgu sözleşmesi dosyası**

`src/WebUI/src/pages/audit/auditListQuery.ts`:

```typescript
/**
 * `AuditLogPage`'in sunucuya ne sorduğunu belirleyen SAF mantık.
 *
 * <p><b>Neden ayrı dosya:</b> sayfa VE testi aynı kaynağı okusun. Bu depoda ölçülmüş
 * sahte-yeşil kalıbı bunun yokluğundan doğdu: eski `InstitutionListPage.spec.ts` sayfayı hiç
 * import etmiyor, değerleri kendi yeniden yazıyordu; sayfanın varsayılanı değiştirilip
 * koşulduğunda test 5/5 yeşil kaldı.</p>
 */

/** Varsayılan kapsam. Herkesin izni olan tek kapsam — açılışta 403 riski yok. */
export const DEFAULT_SCOPE = 'mine'

/** Varsayılan sıralama alanı: en yeni işlem en üstte. */
export const DEFAULT_SORT_BY = 'occurredAt'

/** Denetim izinde varsayılan yön AZALANDIR — "az önce ne oldu" en sık sorulan sorudur. */
export const DEFAULT_DESCENDING = true

export type AuditScope = 'mine' | 'institution'

// Index imzası `useServerPagination<T, F extends Record<string, unknown>>` kısıtı için
// zorunludur — yalnız `{ outcome?: string }` bu kısıta uymaz (TS2322).
export interface AuditListFilters extends Record<string, unknown> {
  outcome?: string
  crossedTenantBoundary?: boolean
}

/** `useServerPagination`'a geçilecek filtre gövdesi. */
export function buildAuditListFilters(
  outcome: string | null,
  crossedOnly: boolean,
): AuditListFilters {
  const filters: AuditListFilters = {}
  // Boş süzgeç GÖNDERİLMEZ: sunucuda `outcome=""` hiçbir satırla eşleşmez ve liste sessizce
  // boşalırdı.
  if (outcome) filters.outcome = outcome
  if (crossedOnly) filters.crossedTenantBoundary = true
  return filters
}
```

- [ ] **Step 3: Sayfa**

`src/WebUI/src/pages/audit/AuditLogPage.vue`:

```vue
<template>
  <q-page padding>
    <PageHeader title="Son İşlemler" />

    <AppTable
      :rows="entries"
      :columns="columns"
      :loading="loading"
      :pagination="pagination"
      show-search
      :search="search"
      no-data-label="Bu aralıkta kayıtlı işlem yok."
      @request="onRequest"
      @search="onSearch"
    >
      <template #filters>
        <q-select
          v-model="scope"
          :options="scopeOptions"
          label="Kapsam"
          outlined
          dense
          emit-value
          map-options
          style="min-width: 220px"
        />

        <q-select
          v-model="outcomeFilter"
          :options="outcomeOptions"
          label="Sonuç"
          outlined
          dense
          clearable
          emit-value
          map-options
          style="min-width: 180px"
        />

        <q-toggle
          v-model="crossedOnly"
          label="Yalnız kurum sınırını aşanlar"
          dense
        />
      </template>

      <template #body-cell-occurredAt="{ row }">
        <q-td>{{ formatDateTime(row.occurredAt) }}</q-td>
      </template>

      <template #body-cell-commandLabel="{ row }">
        <q-td>
          <div class="text-weight-medium">{{ row.commandLabel }}</div>
          <div class="text-caption text-grey-7">{{ row.module }}</div>
        </q-td>
      </template>

      <template #body-cell-outcome="{ row }">
        <q-td>
          <q-badge :color="outcomeColor(row.outcome)">{{ row.outcomeSlug }}</q-badge>
          <q-badge
            v-if="row.crossedTenantBoundary"
            color="deep-orange"
            class="q-ml-xs"
          >
            Kurum dışı
          </q-badge>
          <div
            v-if="row.errorCode"
            class="text-caption text-grey-7"
          >
            {{ row.errorCode }}
          </div>
        </q-td>
      </template>

      <template #body-cell-targets="{ row }">
        <q-td>{{ formatTargets(row) }}</q-td>
      </template>
    </AppTable>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import type { QTableProps } from 'quasar'
import PageHeader from 'components/PageHeader.vue'
import AppTable from 'components/AppTable.vue'
import { auditApi, type AuditEntryDto } from 'src/api/audit'
import { useServerPagination } from 'src/composables/useServerPagination'
import { usePermissions } from 'src/utils/permissions'
import {
  DEFAULT_SCOPE,
  DEFAULT_SORT_BY,
  DEFAULT_DESCENDING,
  buildAuditListFilters,
  type AuditScope,
} from './auditListQuery'

const { hasPermission } = usePermissions()

/**
 * Kurum kapsamı YALNIZ izni olana gösterilir. Görünürlük bir kolaylıktır; asıl karar
 * sunucudadır (uç `audit:view:institution` ile korunur). İzin kontrolü ROL ADINA bakmaz.
 */
const canViewInstitution = computed(() => hasPermission('audit:view:institution'))

const scope = ref<AuditScope>(DEFAULT_SCOPE)
const outcomeFilter = ref<string | null>(null)
const crossedOnly = ref(false)

const scopeOptions = computed(() => {
  const options = [{ label: 'İşlemlerim', value: 'mine' }]
  if (canViewInstitution.value)
    options.push({ label: 'Kurumumdaki işlemler', value: 'institution' })
  return options
})

const outcomeOptions = [
  { label: 'Başarılı', value: 'Succeeded' },
  { label: 'Reddedildi', value: 'Rejected' },
  { label: 'Hata', value: 'Failed' },
]

const filters = computed(() => buildAuditListFilters(outcomeFilter.value, crossedOnly.value))

const { rows: entries, loading, pagination, search, onRequest, onSearch, load } =
  useServerPagination<AuditEntryDto>({
    // Kapsam URL'i DEĞİŞTİRİR, bir sorgu parametresi değildir: iki ucun izni farklıdır ve
    // yetki kararı sunucuda uç seviyesinde verilir.
    fetchFn: (params) =>
      scope.value === 'institution'
        ? auditApi.listForInstitution(params)
        : auditApi.listMine(params),
    filters,
    defaultSortBy: DEFAULT_SORT_BY,
    defaultDescending: DEFAULT_DESCENDING,
  })

// `filters` izleyicisi kapsam değişimini GÖRMEZ (kapsam gövdeye girmiyor, ucu değiştiriyor).
watch(scope, () => {
  load().catch(() => {})
})

const columns: QTableProps['columns'] = [
  { name: 'occurredAt', label: 'Tarih', field: 'occurredAt', align: 'left', sortable: true },
  { name: 'actorName', label: 'Kim', field: 'actorName', align: 'left', sortable: true },
  { name: 'commandLabel', label: 'İşlem', field: 'commandLabel', align: 'left' },
  { name: 'outcome', label: 'Sonuç', field: 'outcome', align: 'left' },
  { name: 'targets', label: 'Hedef Kayıt', field: 'targetIds', align: 'left' },
]

function outcomeColor(outcome: string): string {
  if (outcome === 'Succeeded') return 'positive'
  if (outcome === 'Rejected') return 'warning'
  return 'negative'
}

function formatDateTime(value: string): string {
  return new Date(value).toLocaleString('tr-TR')
}

/** Hedefsiz satır bir hata değildir — komut bilinen ad kümesini kullanmamıştır. */
function formatTargets(row: AuditEntryDto): string {
  const keys = Object.keys(row.targetIds ?? {})
  return keys.length > 0 ? keys.join(', ') : '—'
}

// `useServerPagination`'ın filtre izleyicisi `immediate` DEĞİLDİR; ilk yükleme burada
// tetiklenmezse sayfa kalıcı olarak boş görünür.
onMounted(() => {
  load().catch(() => {})
})
</script>
```

- [ ] **Step 4: Rota**

`src/WebUI/src/router/index.ts`, "Kurum" bloğunun altına:

```typescript
        // Denetim izi (C parçası). Rota "İşlemlerim" kapsamıyla açılır ve o kapsam EK İZİN
        // GEREKTİRMEZ — kullanıcının kendi geçmişini görmesi bir yetki sorusu değildir.
        // Kurum kapsamı sayfa içinde `audit:view:institution` ile açılır.
        {
          path: 'audit',
          name: 'AuditLog',
          component: () => import('pages/audit/AuditLogPage.vue'),
        },
```

- [ ] **Step 5: Menü**

`src/WebUI/src/composables/useNavigation.ts`, "Kurum Yönetimi" grubunun `children` dizisinin sonuna:

```typescript
      // İzin listesi BOŞ: "İşlemlerim" kapsamı herkese açıktır. Kurum kapsamı sayfa içinde
      // izinle açılır — menüyü izne bağlamak, kendi geçmişini göremeyen kullanıcılar üretirdi.
      { title: 'Son İşlemler', icon: 'history', to: { name: 'AuditLog' }, permissions: [] },
```

`useNavigation.upperNode.spec.ts` bu diziyi okuyorsa kırılabilir — kırılırsa testi yeni öğeyi kabul edecek şekilde güncelleyin, menüyü değil.

- [ ] **Step 6: Sözleşme testi**

`src/WebUI/src/pages/audit/AuditLogPage.spec.ts`:

```typescript
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { computed, ref } from 'vue'
import { useServerPagination } from 'src/composables/useServerPagination'
import type { PagedResponse } from 'src/types/pagination'
import type { AuditEntryDto } from 'src/api/audit'
import {
  DEFAULT_SORT_BY,
  DEFAULT_DESCENDING,
  DEFAULT_SCOPE,
  buildAuditListFilters,
} from './auditListQuery'

/**
 * Denetim listesinin SUNUCU SÖZLEŞMESİ.
 *
 * <p>Test bileşeni monte etmez, sayfanın sunucuya ne sorduğunu ölçer. Kırılgan olan kısım
 * şablon değil sözleşmedir: sıralama yönü gitmezse liste en ESKİ işlemle açılır ve "az önce
 * ne oldu" sorusu cevapsız kalır; boş `outcome` süzgeci gönderilirse liste sessizce boşalır.</p>
 *
 * <p>Değerler burada YENİDEN YAZILMAZ — `./auditListQuery`'den import edilir ve sayfa da AYNI
 * dosyayı kullanır. Bu depoda ölçülmüş sahte-yeşil kalıbının kapatılma biçimi budur.</p>
 *
 * <p><b>`useServerPagination` gerçek imzası:</b> `onSearch(term)` ve `onRequest(props)`
 * SENKRONDUR ve `load()`'u fire-and-forget tetikler — Promise DÖNDÜRMEZ. Bu yüzden `await`
 * ile değil `vi.useFakeTimers()` + `vi.runAllTimersAsync()` ile doğrulanır. Composable
 * BURADA DEĞİŞTİRİLMEZ.</p>
 */
describe('AuditLogPage — sunucu sözleşmesi', () => {
  const bosSayfa: PagedResponse<AuditEntryDto> = {
    items: [],
    totalCount: 0,
    page: 1,
    pageSize: 20,
    totalPages: 0,
    hasNextPage: false,
    hasPreviousPage: false,
  }

  type FetchFn = (
    params: Record<string, unknown>,
  ) => Promise<{ data: PagedResponse<AuditEntryDto> }>

  let fetchFn: ReturnType<typeof vi.fn<FetchFn>>

  const kur = (outcome: string | null = null, crossedOnly = false) =>
    useServerPagination<AuditEntryDto>({
      fetchFn,
      filters: computed(() => buildAuditListFilters(outcome, crossedOnly)),
      defaultSortBy: DEFAULT_SORT_BY,
      defaultDescending: DEFAULT_DESCENDING,
    })

  beforeEach(() => {
    vi.useFakeTimers()
    fetchFn = vi.fn(async () => ({ data: bosSayfa }))
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('varsayılan kapsam İŞLEMLERİM — açılışta izin duvarına çarpılmamalı', () => {
    expect(DEFAULT_SCOPE).toBe('mine')
  })

  it('varsayılan sıralama tarihe göre AZALANDIR — en yeni işlem üstte', async () => {
    const { load } = kur()

    await load()

    expect(fetchFn).toHaveBeenCalledWith(
      expect.objectContaining({ sortBy: 'occurredAt', descending: true }),
    )
  })

  it('sonuç süzgeci sunucuya gider', async () => {
    const { load } = kur('Rejected')

    await load()

    expect(fetchFn).toHaveBeenCalledWith(expect.objectContaining({ outcome: 'Rejected' }))
  })

  it('boş sonuç süzgeci GÖNDERİLMEZ — gönderilse liste sessizce boşalırdı', async () => {
    const { load } = kur(null)

    await load()

    const params = fetchFn.mock.calls[0]![0]
    expect(params).not.toHaveProperty('outcome')
  })

  it('kurum sınırı süzgeci yalnız açıkken gider', async () => {
    const { load } = kur(null, true)

    await load()

    expect(fetchFn).toHaveBeenCalledWith(
      expect.objectContaining({ crossedTenantBoundary: true }),
    )
  })

  it('arama terimi sunucuya gider — istemci tarafında süzülmez', async () => {
    const { onSearch } = kur()

    onSearch('Ayşe')
    await vi.runAllTimersAsync()

    expect(fetchFn).toHaveBeenCalledWith(expect.objectContaining({ search: 'Ayşe' }))
  })

  it('sayfa isteği sunucuya gider', async () => {
    const { onRequest } = kur()

    onRequest({ pagination: { page: 4, rowsPerPage: 20, sortBy: 'occurredAt', descending: true } })
    await vi.runAllTimersAsync()

    expect(fetchFn).toHaveBeenCalledWith(expect.objectContaining({ page: 4 }))
  })
})
```

- [ ] **Step 7: Koştur**

```bash
cd src/WebUI
pnpm test:run
pnpm vue-tsc --noEmit -p tsconfig.vitest.json
```

`vue-tsc` betiğinin adını `package.json`'dan doğrulayın (`pnpm lint` / `pnpm typecheck`). **`test:run` yeşil ama `vue-tsc` kırmızı olabilir** — A parçasında yaşandı: gevşek bir `fetchFn` tipi 4× TS2322 üretti. İkisi de yeşil olmadan görev bitmez.

- [ ] **Step 8: KANIT ADIMI (zorunlu)**

`auditListQuery.ts` içindeki `DEFAULT_DESCENDING` değerini `false` yapın, `pnpm test:run` koşun. Beklenen: `varsayılan sıralama tarihe göre AZALANDIR` KIRMIZI. Raporunuza yazın, geri alın.

İkinci kanıt: `buildAuditListFilters` içindeki `if (outcome)` koşulunu kaldırıp `filters.outcome = outcome` yapın. Beklenen: `boş sonuç süzgeci GÖNDERİLMEZ` KIRMIZI. Raporunuza yazın, geri alın.

- [ ] **Step 9: Commit**

```bash
git add src/WebUI/src/api/audit.ts src/WebUI/src/pages/audit src/WebUI/src/router/index.ts src/WebUI/src/composables/useNavigation.ts
git commit -m "feat(audit): denetim izi sayfası ve sunucu sözleşmesi testi"
```

---

## Dağıtım Notları

Bu plan bittiğinde çalıştırılması gereken **elle** adım YOKTUR — `AuditEntry` yeni bir belgedir ve geriye dönük veri kurmaz. İz **dağıtımdan sonraki** komutlardan itibaren dolar; geçmiş işlemler izde görünmez ve bu beklenen davranıştır.

İki not:

1. **Şema oluşumu.** `audit` şeması Marten'ın `AutoCreate` politikasıyla geliştirmede kendiliğinden doğar. Üretimde açılışta geçiş yapılmaz (`ApplyAllDatabaseChangesOnStartup()` bu depoda API'yi öldürüyor) — `src/Docs/docs/infrastructure/sql/` altındaki elle betik akışına `audit` şeması + `mt_doc_auditentry` tablosu eklenmelidir. Sırayı `dagitim-on-kosullari.md` belirler.
2. **Kurum yolu.** `SubjectInstitutionPath`, `POST /api/institutions/rebuild-hierarchy` koşmamış bir kurumda `null` kalır ve o satırlar yol önekiyle okuyan kullanıcıya görünmez (kimlik eşitliği dalına düşerler). Geçiş ucu zaten A parçasının dağıtım ön koşuludur.

---

## Kapsam Dışı (bilinçli)

- **HTTP 403 kaydı.** ASP.NET yetkilendirme reddi middleware'e ulaşmaz; kaydetmek ayrı bir ara katman ister.
- **`Commands/` klasöründeki sorguların taşınması.** On tip yanlış yerde duruyor; `AuditCommandFilter` onları ad kuralıyla eliyor ve `AuditCommandCoverageDriftTests` listeyi görünür tutuyor. Taşıma ayrı bir iştir.
- **Komut gövdesi ve alan-düzeyi değişiklik (before/after).** Olay deposunun işi.
- **İl/ilçe yetkilisine denetim izni.** B parçasında verilecek.
- **`PermissionClaimsTransformation`'daki yol araması kopyasının birleştirilmesi.** İki önbelleğin anahtarları ve geçersizleme yolları farklı; birleştirmek denetim yazmasını token yaşam döngüsüne bağlardı.
