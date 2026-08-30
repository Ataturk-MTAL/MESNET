# Müdürlük panosu (D2) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** İl/ilçe millî eğitim müdürlüğü bağlamındaki kullanıcıya `Ana Sayfa`'da okul panosu yerine üç kartlık bir müdürlük panosu göstermek: alt kurum ağacı, yöneticisi olmayan okullar, tıkanmış fesih onayları.

**Architecture:** Kart 1 mevcut kapsamlı kurum sorgusunun `TotalCount`'unu kullanır, yeni backend yoktur. Kart 2 Institution modülünde Security olaylarıyla beslenen `InstitutionAdminView` read-model'i üzerinden iki adımlı sorgu yapar. Kart 3 Marten `TenantIsOneOf` ile okul kiracılarını tek sorguda tarar; bu operatör `SubtreeTenantScope` adlı tek sarıcıda hapsedilir ve kaynak taraması testiyle kilitlenir.

**Tech Stack:** .NET 10, Marten 9.11.0 (document DB + conjoined multi-tenancy), Wolverine 6.15.0 (command bus + local queues), PostgreSQL, Vue 3 + Quasar + Pinia + TypeScript (pnpm, Vitest), xUnit + Shouldly.

**Spec:** `docs/superpowers/specs/2026-08-30-mudurluk-panosu-design.md`

## Global Constraints

- **Yetkilendirme permission bazlıdır, rol bazlı DEĞİLDİR.** `RequireRole` ve `IsInRole` kullanılmaz. İzin sabitleri `src/MESNET.Common.Shared/Security/Permissions.cs` içindedir.
- **Kapsam istekten ALINMAZ.** Kurum kapsamı her zaman `InstitutionScopePolicy.VisibleScope(actorInstitutionId, actorPath, hasPlatformScope)` ile aktörün claim'lerinden türer.
- **Kiracısız Marten session YASAKTIR.** `store.QuerySession()` argümansız çağrılmaz; kiracı açıkça verilir (`store.QuerySession(tenantId)` ya da hiçbir okula ait olmayan işler için `store.QuerySession(TenantResolution.Platform)`). `TenantlessSessionDriftTests` bunu kilitler.
- **`AnyTenant()` bu depoda tamamen yasaktır.** Kiracılar arası okuma yalnız `TenantIsOneOf(...)` ile ve yalnız `SubtreeTenantScope.cs` dosyasından yapılır.
- **Endpoint metotları iş mantığı içermez.** `IDocumentSession`/`IQuerySession` endpoint'e enjekte edilmez; her uç `IMessageBus` üzerinden handler'a devreder. Tek istisna `ICurrentUserService`.
- **Marten LINQ'te SmartEnum kullanılmaz.** `x.Status.Name` nested path'i her zaman `NULL` döner. Düz `bool`/`string`/`int` alanlar kullanılır.
- **Modüller arası doğrudan veri erişimi YASAKTIR.** Başka modülün şemasına sorgu atılmaz, başka modülün belgesi yazılmaz. İletişim olaylarla ya da `Common.Infrastructure`'da tanımlı bir sözleşmeyle yapılır.
- **Yeni Marten belgesi eklenirse `DocumentTenancyMap`'e sınıflandırma ZORUNLUDUR** (`src/MESNET.Common.Shared/Tenancy/DocumentTenancyMap.cs`), yoksa `DocumentTenancyVerificationHostedService` açılışta patlar.
- **Frontend arayüz dili Türkçedir** ve Türkçe karakterler doğru kullanılır (ç, ş, ğ, ü, ö, ı, İ). ASCII yaklaşık karakter kullanılmaz.
- **İkon-only butonlar `aria-label` + `<q-tooltip>` taşır**, `title` attribute'ü kullanılmaz.
- **Boş-durum bir hata değildir:** uyarı (⚠) ikonu yerine nötr ikon kullanılır.
- **Commit'lere `Co-Authored-By` trailer'ı EKLENMEZ.**
- **Eşik varsayılanı 14 gündür** ve `InternshipApprovalConfig.StuckApprovalDays` alanında yaşar.
- **Tekil yapılandırma belgesinin kimliği:** `8c62ac6c-a944-4eb6-b3b0-342fe7ffc3a6`.

## Sipariş kısıtı

Bu plan **D1'den sonra** uygulanır (`docs/superpowers/specs/2026-08-30-mudurluk-baglami-menu-ve-erisilebilirlik-design.md`), D1 de kullanıcı listesi kapsam düzeltmesinden sonra gelir. Gerekçe: Kart 2 ve Kart 3'ün eylem çağrıları D1'in düzelttiği rota ve buton izinlerinden geçer.

## Komutlar

```bash
# Backend — tüm testler
dotnet test MESNET.slnx

# Backend — tek proje
dotnet test tests/MESNET.Internship.UnitTests/MESNET.Internship.UnitTests.csproj

# Backend — tek test
dotnet test tests/MESNET.Internship.UnitTests/MESNET.Internship.UnitTests.csproj --filter "FullyQualifiedName~StuckApprovalPolicyTests"

# Frontend — tek seferlik koşum (izleme modu değil)
cd src/WebUI && pnpm test:run

# Frontend — tek dosya
cd src/WebUI && pnpm test:run src/utils/directorateContext.spec.ts

# Frontend — tip denetimi
cd src/WebUI && pnpm exec vue-tsc --noEmit
```

## Dosya Yapısı

**Yeni dosyalar — backend**

| Dosya | Sorumluluk |
|---|---|
| `src/MESNET.Common.Infrastructure/Tenancy/IInstitutionSubtreeDirectory.cs` | Sözleşme: yol öneki → okul kiracı kimlikleri |
| `src/MESNET.Common.Infrastructure/Tenancy/SubtreeTenantScope.cs` | `InstitutionVisibility` → kiracı kimlik listesi. **`TenantIsOneOf`'un tek kullanım yeri buradan beslenir** |
| `src/Modules/Institution/MESNET.Institution.Application/Services/InstitutionSubtreeDirectory.cs` | Sözleşmenin uygulaması — `Institution` belgesini okur |
| `src/Modules/Institution/MESNET.Institution.Core/ReadModels/InstitutionManagerLink.cs` | Kullanıcı başına: hangi kuruma bağlı, etkin mi, `institution:manage` taşıyor mu |
| `src/Modules/Institution/MESNET.Institution.Application/Consumers/InstitutionManagerLinkConsumer.cs` | Security kullanıcı olaylarını dinler, satırı mutlak olarak yazar (**sıralı kuyruk**) |
| `src/Modules/Institution/MESNET.Institution.Application/Queries/GetUnmanagedInstitutions.cs` | Yöneticisiz okul sorgusu (sayfalı) |
| `src/Modules/Institution/MESNET.Institution.Application/Handlers/GetUnmanagedInstitutionsHandler.cs` | İki adımlı sorgu |
| `src/Modules/Security/MESNET.Security.Application/Commands/ReplayUserAccounts.cs` + handler | Kullanıcı kayıtlarını `UserCreated` olarak yeniden yayınlar (backfill) |
| `src/Modules/Internship/MESNET.Internship.Core/Entities/InternshipApprovalConfig.cs` | Ulusal tekil eşik parametresi |
| `src/Modules/Internship/MESNET.Internship.Core/Policies/StuckApprovalPolicy.cs` | Tıkanmışlık kararı — saf, LINQ ifadesiyle eşleşmesi testle kilitli |
| `src/Modules/Internship/MESNET.Internship.Application/Queries/GetStuckApprovals.cs` | Sorgu kaydı |
| `src/Modules/Internship/MESNET.Internship.Application/Handlers/GetStuckApprovalsHandler.cs` | `TenantIsOneOf` sorgusu |
| `src/Modules/Internship/MESNET.Internship.Application/Dtos/StuckApprovalSummaryDto.cs` | Kart 3 çıktısı |
| `src/Modules/Internship/MESNET.Internship.Application/Queries/GetApprovalConfig.cs` + `Commands/UpdateApprovalConfig.cs` + iki handler | Eşik oku/yaz (doğrulama handler içinde, `DomainException` ile — `UpdateAbsenceLimitsHandler` emsali) |

**Yeni dosyalar — frontend**

| Dosya | Sorumluluk |
|---|---|
| `src/WebUI/src/utils/directorateContext.ts` | `isActingAsDirectorate(nodeType)` — saf karar |
| `src/WebUI/src/utils/directorateContext.spec.ts` | Kararın kilidi |
| `src/WebUI/src/composables/useDirectorateDashboard.ts` | Üç kartın verisi, yükleme/hata durumları |
| `src/WebUI/src/composables/useDirectorateDashboard.spec.ts` | Bir kart patlarsa diğerleri dolar |
| `src/WebUI/src/pages/dashboard/DirectorateDashboard.vue` | Üç kart |
| `src/WebUI/src/pages/admin/PlatformParametersPage.vue` | Ulusal parametre ekranı (yalnız eşik) |

**Yeni dosyalar — testler**

| Dosya | Sorumluluk |
|---|---|
| `tests/MESNET.Institution.UnitTests/CrossTenantQueryDriftTests.cs` | `AnyTenant(` hiç yok; `TenantIsOneOf(` yalnız sarıcıda |
| `tests/MESNET.Institution.UnitTests/SubtreeTenantScopeTests.cs` | Dört kapsam hâli |
| `tests/MESNET.Internship.UnitTests/StuckApprovalPolicyTests.cs` | Doğruluk tablosu + eşik + null |
| `tests/MESNET.Internship.UnitTests/ApprovalConfigValidationTests.cs` | 0/-1/366 reddedilir |
| `tests/MESNET.Institution.UnitTests/ManagerLinkPermissionTests.cs` | `institution:manage` rollerden türetiliyor, rol adına bakılmıyor |

**Değişen dosyalar**

| Dosya | Değişiklik |
|---|---|
| `src/MESNET.Common.Shared/Tenancy/DocumentTenancyMap.cs` | `InstitutionManagerLink` = `Identity`, `InternshipApprovalConfig` = `Shared` |
| `src/Modules/Institution/MESNET.Institution.Application/ServiceRegistration.cs` | `IInstitutionSubtreeDirectory` kaydı |
| `src/Modules/Institution/MESNET.Institution.Api/InstitutionEndpoints.cs` | `/unmanaged` |
| `src/Modules/Security/MESNET.Security.Api/UserManagementEndpoints.cs` | `/replay` |
| `src/Modules/Institution/MESNET.Institution.Persistence/InstitutionMartenConfig.cs` | `InstitutionManagerLink` şeması + indeks |
| `src/Modules/Internship/MESNET.Internship.Application/Sagas/InternshipSaga.cs` | `TerminationRequestedAt` alanı |
| `src/Modules/Internship/MESNET.Internship.Api/InternshipEndpoints.cs` | `/stuck-approvals`, `/approval-config` |
| `src/Modules/Internship/MESNET.Internship.Persistence/InternshipMartenConfig.cs` | `InternshipApprovalConfig` şeması |
| `src/WebUI/src/api/institution.ts` | `listUnmanaged` |
| `src/WebUI/src/api/internship.ts` | `getStuckApprovals`, `getApprovalConfig`, `updateApprovalConfig` |
| `src/WebUI/src/pages/DashboardPage.vue` | Müdürlük dalı |
| `src/WebUI/src/router/index.ts` | `/admin/parameters` rotası |
| `src/WebUI/src/composables/useNavigation.ts` | `Ulusal Parametreler` menü girdisi |
| `src/Docs/docs/infrastructure/dagitim-on-kosullari.md` | İki yeni ön koşul |

---

### Task 1: Alt ağaç kiracı dizini

Internship modülü `Institution` belgesini okuyamaz (şema izolasyonu). Depoda bunun için yerleşik desen var: **sözleşme `Common.Infrastructure`'da, uygulaması modülde** — `ITenantDirectory` ile birebir aynı.

**Files:**
- Create: `src/MESNET.Common.Infrastructure/Tenancy/IInstitutionSubtreeDirectory.cs`
- Create: `src/Modules/Institution/MESNET.Institution.Application/Services/InstitutionSubtreeDirectory.cs`
- Modify: `src/Modules/Institution/MESNET.Institution.Application/ServiceRegistration.cs`

**Interfaces:**
- Consumes: `TenantResolution.Platform` (sabit `"platform"`), `TenantResolution.ForInstitution(Guid) → string`, `InstitutionNodeType.School`, `IQueryableExtensions.OfNodeType(...)` — hepsi mevcut.
- Produces: `IInstitutionSubtreeDirectory.GetSchoolTenantsAsync(string pathPrefix, CancellationToken) → Task<IReadOnlyList<string>>` ve `GetAllSchoolTenantsAsync(CancellationToken) → Task<IReadOnlyList<string>>`. Task 2 bunları tüketir.

- [ ] **Step 1: Sözleşmeyi yaz**

`src/MESNET.Common.Infrastructure/Tenancy/IInstitutionSubtreeDirectory.cs`:

```csharp
namespace MESNET.Common.Infrastructure.Tenancy;

/// <summary>
/// Bir kurum alt ağacındaki OKUL kiracılarının listesi.
///
/// <para><b>Neden arayüz, doğrudan sorgu değil:</b> kiracı = okul (ADR-0003) ve okul kaydı
/// Institution modülünündür. Başka bir modülün <c>institution</c> şemasına sorgu atması şema
/// izolasyonunu kırardı. Uygulaması modülde, sözleşmesi burada —
/// <see cref="ITenantDirectory"/> ile aynı desen.</para>
///
/// <para><b>İl ve ilçe düğümleri kiracı DEĞİLDİR</b> ve kiracı damgalı hiçbir veri taşımazlar;
/// bu yüzden döndürülen liste yalnız okul düğümlerini içerir. Süzülmeselerdi çağıran hiçbir
/// verinin bulunmadığı "kiracılarda" arama yapardı — istisna değil, sessiz boş sonuç.</para>
///
/// <para><b>Boş liste hata değildir:</b> alt ağaçta okul yoksa çağıranın arayacağı bir şey de
/// yoktur.</para>
/// </summary>
public interface IInstitutionSubtreeDirectory
{
    /// <param name="pathPrefix">
    /// Aktörün kurum ağacındaki yolu (<c>InstitutionVisibility.PathPrefix</c>). Bu önekle
    /// başlayan okullar döner.
    /// </param>
    Task<IReadOnlyList<string>> GetSchoolTenantsAsync(
        string pathPrefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bütün okul kiracıları. <b>Yalnız kapsamsız (platform) aktör için</b> — kapsamlı aktörde
    /// çağrılırsa kapsam sessizce genişler.
    /// </summary>
    Task<IReadOnlyList<string>> GetAllSchoolTenantsAsync(
        CancellationToken cancellationToken = default);
}
```

- [ ] **Step 2: Uygulamayı yaz**

`src/Modules/Institution/MESNET.Institution.Application/Services/InstitutionSubtreeDirectory.cs`:

```csharp
using Marten;
using MESNET.Common.Infrastructure.Tenancy;
using MESNET.Common.Shared.Tenancy;
using MESNET.Institution.Application.Extensions;
using MESNET.Institution.Core.Enums;
// "Institution" hem ad alanı hem tip adı olduğu için doğrudan kullanılamaz (CS0118).
using InstitutionRecord = MESNET.Institution.Core.Entities.Institution;

namespace MESNET.Institution.Application.Services;

/// <summary>
/// <inheritdoc cref="IInstitutionSubtreeDirectory"/>
///
/// <para><b>Neden <see cref="TenantResolution.Platform"/> ile okunuyor:</b> <c>Institution</c>
/// <c>DocumentTenancyMap</c> içinde <b>kimlik katmanındadır</b> — kiracı damgası taşımaz, çünkü
/// kiracının kendisidir. Yine de bir ada ihtiyaç var: kiracısız session yasaktır.</para>
/// </summary>
public sealed class InstitutionSubtreeDirectory : IInstitutionSubtreeDirectory
{
    private readonly IDocumentStore _store;

    public InstitutionSubtreeDirectory(IDocumentStore store)
    {
        _store = store;
    }

    public async Task<IReadOnlyList<string>> GetSchoolTenantsAsync(
        string pathPrefix, CancellationToken cancellationToken = default)
    {
        // Boş önek "her şey" demek DEĞİLDİR. Marten string.StartsWith("") her satırı geçirirdi
        // ve kapsamlı bir aktör sessizce bütün okulları görürdü. Kapsamsız kalmak, kapsamı
        // aşmaktan iyidir.
        if (string.IsNullOrWhiteSpace(pathPrefix))
            return [];

        await using var session = _store.QuerySession(TenantResolution.Platform);

        // Marten string.StartsWith'i SQL'de LIKE 'önek%' çevirir; ham SQL ve WITH RECURSIVE
        // gerekmez. Yolu olmayan satır alt ağaçta DEĞİLDİR.
        var ids = await session.Query<InstitutionRecord>()
            .OfNodeType(InstitutionNodeType.School)
            .Where(i => i.Path != null && i.Path.StartsWith(pathPrefix))
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);

        return ToTenants(ids);
    }

    public async Task<IReadOnlyList<string>> GetAllSchoolTenantsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var session = _store.QuerySession(TenantResolution.Platform);

        var ids = await session.Query<InstitutionRecord>()
            .OfNodeType(InstitutionNodeType.School)
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);

        return ToTenants(ids);
    }

    // Çevrim burada TEKRARLANMAZ: 1:1 eşleşme TenantResolution'da tek noktada yaşar (#148).
    private static IReadOnlyList<string> ToTenants(IEnumerable<Guid> ids) =>
        ids.Select(TenantResolution.ForInstitution).ToList();
}
```

- [ ] **Step 3: DI'a kaydet**

`src/Modules/Institution/MESNET.Institution.Application/ServiceRegistration.cs` içinde, mevcut `services.AddScoped<ITenantDirectory, InstitutionTenantDirectory>();` satırının hemen altına ekle:

```csharp
        // Alt ağaç kiracı listesi (D2). Aynı gerekçe: sözleşme altyapıda, uygulama burada.
        services.AddScoped<IInstitutionSubtreeDirectory, InstitutionSubtreeDirectory>();
```

- [ ] **Step 4: Derlendiğini doğrula**

Run: `dotnet build MESNET.slnx`
Expected: hatasız derleme.

- [ ] **Step 5: Commit**

```bash
git add src/MESNET.Common.Infrastructure/Tenancy/IInstitutionSubtreeDirectory.cs \
        src/Modules/Institution/MESNET.Institution.Application/Services/InstitutionSubtreeDirectory.cs \
        src/Modules/Institution/MESNET.Institution.Application/ServiceRegistration.cs
git commit -m "feat(institution): alt ağaç kiracı dizini — sözleşme altyapıda, uygulama modülde"
```

---

### Task 2: Kiracılar arası okumanın tek kapısı + kilidi

`TenantIsOneOf` kiracı yalıtımını **bilerek** deler. Serbest bırakılırsa bir gün biri onu istekten gelen kimliklerle çağırır ve kapsam sessizce açılır. Bu görev operatörü tek dosyaya hapseder ve kaynağı tarayan bir testle kilitler.

**Files:**
- Create: `src/MESNET.Common.Infrastructure/Tenancy/SubtreeTenantScope.cs`
- Create: `tests/MESNET.Institution.UnitTests/SubtreeTenantScopeTests.cs`
- Create: `tests/MESNET.Institution.UnitTests/CrossTenantQueryDriftTests.cs`
- Modify: `src/Modules/Institution/MESNET.Institution.Application/ServiceRegistration.cs`

**Interfaces:**
- Consumes: Task 1'in `IInstitutionSubtreeDirectory`'si; `InstitutionVisibility(bool Unrestricted, string? PathPrefix, Guid? InstitutionId)` (`MESNET.Common.Shared.Security`); `TenantResolution.ForInstitution(Guid) → string`.
- Produces: `SubtreeTenantScope.ResolveAsync(InstitutionVisibility scope, CancellationToken) → Task<IReadOnlyList<string>>`. Task 5 bunu tüketir.

- [ ] **Step 1: Başarısız testi yaz**

`tests/MESNET.Institution.UnitTests/SubtreeTenantScopeTests.cs`:

```csharp
using MESNET.Common.Infrastructure.Tenancy;
using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Institution.UnitTests;

/// <summary>
/// Kiracılar arası okumanın kapsamı. <b>Kimlikler istekten HİÇ gelmez</b>; bu testin varlık
/// nedeni o kuralı kod düzeyinde kilitlemektir.
/// </summary>
public sealed class SubtreeTenantScopeTests
{
    /// <summary>Dizini taklit eder — gerçek Marten gerekmez, karar saf.</summary>
    private sealed class FakeDirectory : IInstitutionSubtreeDirectory
    {
        public string? RequestedPrefix { get; private set; }
        public bool AllRequested { get; private set; }

        public Task<IReadOnlyList<string>> GetSchoolTenantsAsync(
            string pathPrefix, CancellationToken cancellationToken = default)
        {
            RequestedPrefix = pathPrefix;
            return Task.FromResult<IReadOnlyList<string>>(["okul-a", "okul-b"]);
        }

        public Task<IReadOnlyList<string>> GetAllSchoolTenantsAsync(
            CancellationToken cancellationToken = default)
        {
            AllRequested = true;
            return Task.FromResult<IReadOnlyList<string>>(["okul-a", "okul-b", "okul-c"]);
        }
    }

    [Fact]
    public async Task Yol_oneki_olan_aktor_alt_agacini_gorur()
    {
        // Arrange
        var directory = new FakeDirectory();
        var scope = new SubtreeTenantScope(directory);
        var visibility = new InstitutionVisibility(
            Unrestricted: false, PathPrefix: "/il-35/ilce-konak", InstitutionId: null);

        // Act
        var tenants = await scope.ResolveAsync(visibility);

        // Assert
        tenants.ShouldBe(["okul-a", "okul-b"]);
        directory.RequestedPrefix.ShouldBe("/il-35/ilce-konak");
        directory.AllRequested.ShouldBeFalse();
    }

    [Fact]
    public async Task Kapsamsiz_platform_aktoru_butun_okullari_gorur()
    {
        var directory = new FakeDirectory();
        var scope = new SubtreeTenantScope(directory);
        var visibility = new InstitutionVisibility(
            Unrestricted: true, PathPrefix: null, InstitutionId: null);

        var tenants = await scope.ResolveAsync(visibility);

        tenants.Count.ShouldBe(3);
        directory.AllRequested.ShouldBeTrue();
    }

    [Fact]
    public async Task Okul_aktoru_yalniz_kendi_kiracisini_gorur()
    {
        var directory = new FakeDirectory();
        var scope = new SubtreeTenantScope(directory);
        var institutionId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var visibility = new InstitutionVisibility(
            Unrestricted: false, PathPrefix: null, InstitutionId: institutionId);

        var tenants = await scope.ResolveAsync(visibility);

        // Dizine HİÇ gitmez: kendi kiracısını bilmek için sorguya gerek yok.
        tenants.ShouldBe([institutionId.ToString()]);
        directory.RequestedPrefix.ShouldBeNull();
        directory.AllRequested.ShouldBeFalse();
    }

    /// <summary>
    /// Kapsamsız aktör HER ŞEYİ değil HİÇBİR ŞEYİ görür. Boş liste dönmesi çağıranın sorguyu
    /// hiç kurmaması içindir — parametresiz <c>TenantIsOneOf()</c>'un SQL'de ne ürettiğine
    /// güvenilmez.
    /// </summary>
    [Fact]
    public async Task Kapsamsiz_aktor_bos_liste_alir()
    {
        var directory = new FakeDirectory();
        var scope = new SubtreeTenantScope(directory);
        var visibility = new InstitutionVisibility(
            Unrestricted: false, PathPrefix: null, InstitutionId: Guid.Empty);

        var tenants = await scope.ResolveAsync(visibility);

        tenants.ShouldBeEmpty();
        directory.AllRequested.ShouldBeFalse();
    }
}
```

- [ ] **Step 2: Testin başarısız olduğunu doğrula**

Run: `dotnet test tests/MESNET.Institution.UnitTests/MESNET.Institution.UnitTests.csproj --filter "FullyQualifiedName~SubtreeTenantScopeTests"`
Expected: derleme hatası — `SubtreeTenantScope` tipi yok.

- [ ] **Step 3: Sarıcıyı yaz**

`src/MESNET.Common.Infrastructure/Tenancy/SubtreeTenantScope.cs`:

```csharp
using MESNET.Common.Shared.Security;
using MESNET.Common.Shared.Tenancy;

namespace MESNET.Common.Infrastructure.Tenancy;

/// <summary>
/// Aktörün okuyabileceği okul kiracılarının listesi — <b>kiracılar arası okumanın TEK
/// kapısı</b>.
///
/// <para><b>Neden tek kapı:</b> Marten'in <c>TenantIsOneOf(...)</c> operatörü kiracı yalıtımını
/// bilerek deler; ürettiği SQL <c>tenant_id IN (...)</c>'dir. Serbest bırakılırsa bir gün biri
/// onu <b>istekten gelen</b> kimliklerle çağırır ve kapsam sessizce açılır — hata değil, fazla
/// veri. Bu sınıf listeyi yalnız <see cref="InstitutionVisibility"/>'den üretir; istekten gelen
/// hiçbir değer buraya giremez.</para>
///
/// <para><b><c>AnyTenant()</c> bu depoda YASAKTIR</b> — kapsamsız aktör için bile
/// kullanılmaz. Tek kod yolu, tek gözden geçirme noktası. Kilitleyen test:
/// <c>CrossTenantQueryDriftTests</c>.</para>
/// </summary>
public sealed class SubtreeTenantScope
{
    private readonly IInstitutionSubtreeDirectory _directory;

    public SubtreeTenantScope(IInstitutionSubtreeDirectory directory)
    {
        _directory = directory;
    }

    /// <summary>
    /// Kapsamı kiracı kimliklerine çevirir.
    /// </summary>
    /// <returns>
    /// Kiracı kimlikleri; kapsamsız aktörde <b>boş liste</b>. Çağıran boş listede sorguyu HİÇ
    /// kurmamalıdır — parametresiz <c>TenantIsOneOf()</c>'un davranışına güvenilmez.
    /// </returns>
    public async Task<IReadOnlyList<string>> ResolveAsync(
        InstitutionVisibility scope, CancellationToken cancellationToken = default)
    {
        if (scope.Unrestricted)
            return await _directory.GetAllSchoolTenantsAsync(cancellationToken);

        if (scope.PathPrefix is { } prefix && !string.IsNullOrWhiteSpace(prefix))
            return await _directory.GetSchoolTenantsAsync(prefix, cancellationToken);

        // Okul aktörü kendi kiracısını bilir; dizine gitmeye gerek yok.
        if (scope.InstitutionId is { } institutionId && institutionId != Guid.Empty)
            return [TenantResolution.ForInstitution(institutionId)];

        // Kapsamsız aktör: her şeyi görmek yerine hiçbir şey görmek.
        return [];
    }
}
```

- [ ] **Step 4: DI'a kaydet**

`src/Modules/Institution/MESNET.Institution.Application/ServiceRegistration.cs` içinde Task 1'de eklenen satırın altına:

```csharp
        // Kiracılar arası okumanın tek kapısı (D2).
        services.AddScoped<SubtreeTenantScope>();
```

`using MESNET.Common.Infrastructure.Tenancy;` zaten dosyanın başında var.

- [ ] **Step 5: Testin geçtiğini doğrula**

Run: `dotnet test tests/MESNET.Institution.UnitTests/MESNET.Institution.UnitTests.csproj --filter "FullyQualifiedName~SubtreeTenantScopeTests"`
Expected: 4 test PASS.

- [ ] **Step 6: Kaynak taraması kilidini yaz**

`tests/MESNET.Institution.UnitTests/CrossTenantQueryDriftTests.cs`:

```csharp
using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace MESNET.Institution.UnitTests;

/// <summary>
/// Kiracılar arası okuma tek kapıdan geçer (D2).
///
/// <para><b>Neden derleyici yakalayamaz:</b> <c>AnyTenant()</c> ve <c>TenantIsOneOf(...)</c>
/// geçerli Marten çağrılarıdır ve doğru derlenirler. Yeni bir handler <c>AnyTenant()</c>
/// yazarsa hiçbir davranış testi kırılmaz — kiracılar arası okuma <b>sessizce</b> açılır ve
/// kimse fark etmez. Tek savunma, çağrının kaynakta hiç bulunmamasıdır.</para>
///
/// <para><b>Doğrusu:</b> kapsam <c>SubtreeTenantScope.ResolveAsync</c> ile
/// <c>InstitutionVisibility</c>'den türetilir; sorgu o listeyle
/// <c>TenantIsOneOf(tenants.ToArray())</c> çağırır.</para>
/// </summary>
public sealed class CrossTenantQueryDriftTests
{
    /// <summary>Kapsamı tümden kaldıran operatör — hiçbir gerekçeyle kullanılmaz.</summary>
    private static readonly Regex AnyTenantCall = new(@"\bAnyTenant\s*\(", RegexOptions.Compiled);

    /// <summary>Kapsamı listeye daraltan operatör — yalnız izinli dosyalarda.</summary>
    private static readonly Regex TenantIsOneOfCall =
        new(@"\bTenantIsOneOf\s*\(", RegexOptions.Compiled);

    /// <summary>
    /// Operatörü kullanabilecek tek üretim dosyası. Sorgu handler'ı listeyi buradan alır ama
    /// operatörü kendisi çağırır; bu yüzden handler dosyası da izinlidir.
    /// </summary>
    private static readonly string[] AllowedFiles =
    [
        "SubtreeTenantScope.cs",
        "GetStuckApprovalsHandler.cs",
    ];

    [Fact]
    public void Kaynakta_AnyTenant_cagrisi_yok()
    {
        var violations = new List<string>();

        foreach (var file in SourceFiles())
        {
            var code = StripComments(File.ReadAllText(file));
            if (AnyTenantCall.IsMatch(code))
                violations.Add(Relative(file));
        }

        violations.ShouldBeEmpty(
            "AnyTenant() kiracı kapsamını TÜMDEN kaldırır ve bu depoda yasaktır — kapsamsız "
            + "aktör için bile. Kapsamı SubtreeTenantScope.ResolveAsync ile türetip "
            + $"TenantIsOneOf(...) kullanın. İhlaller: {string.Join(" | ", violations)}");
    }

    [Fact]
    public void TenantIsOneOf_yalniz_izinli_dosyalarda()
    {
        var violations = new List<string>();

        foreach (var file in SourceFiles())
        {
            var code = StripComments(File.ReadAllText(file));
            if (!TenantIsOneOfCall.IsMatch(code))
                continue;

            if (!AllowedFiles.Contains(Path.GetFileName(file), StringComparer.Ordinal))
                violations.Add(Relative(file));
        }

        violations.ShouldBeEmpty(
            "TenantIsOneOf(...) kiracı yalıtımını deler ve yalnız tek kapıdan kullanılır. "
            + "Kapsamı SubtreeTenantScope.ResolveAsync'ten alın; listeyi istekten ALMAYIN. "
            + $"İhlaller: {string.Join(" | ", violations)}");
    }

    /// <summary>
    /// Satır ve blok yorumlarını atar: bu kuralın NEDENİNİ anlatan XML doc'lar yasak çağrının
    /// adını geçirir. Yorumu koda saymak doğru yazılmış dosyayı ihlal gösterirdi.
    /// </summary>
    private static string StripComments(string source)
    {
        var withoutBlocks = Regex.Replace(source, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        return Regex.Replace(withoutBlocks, @"//.*$", string.Empty, RegexOptions.Multiline);
    }

    private static IEnumerable<string> SourceFiles()
    {
        var root = Path.Combine(RepoRoot(), "src");
        var obj = $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}";
        var bin = $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}";

        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(obj, StringComparison.Ordinal)
                     && !f.Contains(bin, StringComparison.Ordinal));
    }

    private static string Relative(string file) => Path.GetRelativePath(RepoRoot(), file);

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MESNET.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Depo kökü bulunamadı (MESNET.slnx aranıyordu).");
    }
}
```

- [ ] **Step 7: Kilidin GERÇEKTEN kilitlediğini kanıtla**

Bu adım atlanamaz. Bu oturumun tekrar eden başarısızlık kalıbı **içi boş kilit**: yeşil ama hiçbir şeyi korumuyor.

`src/MESNET.Common.Infrastructure/Tenancy/SubtreeTenantScope.cs` dosyasının `ResolveAsync` metodunun ilk satırına geçici olarak ekle:

```csharp
        var gecici = "AnyTenant(";  // GEÇİCİ — kilit denemesi
```

Bu bir dize; regex `\bAnyTenant\s*\(` yine eşleşir çünkü tarama sözdizimi çözümlemez.

Run: `dotnet test tests/MESNET.Institution.UnitTests/MESNET.Institution.UnitTests.csproj --filter "FullyQualifiedName~CrossTenantQueryDriftTests"`
Expected: `Kaynakta_AnyTenant_cagrisi_yok` **FAIL**, ihlal listesinde `SubtreeTenantScope.cs` görünür.

Sonra satırı **sil** ve tekrar koş.
Expected: 2 test PASS.

- [ ] **Step 8: Tüm testleri koş**

Run: `dotnet test MESNET.slnx`
Expected: hepsi yeşil.

- [ ] **Step 9: Commit**

```bash
git add src/MESNET.Common.Infrastructure/Tenancy/SubtreeTenantScope.cs \
        src/Modules/Institution/MESNET.Institution.Application/ServiceRegistration.cs \
        tests/MESNET.Institution.UnitTests/SubtreeTenantScopeTests.cs \
        tests/MESNET.Institution.UnitTests/CrossTenantQueryDriftTests.cs
git commit -m "feat(tenancy): kiracılar arası okuma tek kapıya alındı, AnyTenant yasaklandı"
```

---

### Task 3: Ulusal eşik parametresi

Eşik okul başına değişmez; `AttendanceLimitConfig` ile birebir aynı desen: `Shared` sınıfı tekil belge, yazma izni `platform:parameter:manage`.

**Files:**
- Create: `src/Modules/Internship/MESNET.Internship.Core/Entities/InternshipApprovalConfig.cs`
- Create: `src/Modules/Internship/MESNET.Internship.Application/Queries/GetApprovalConfig.cs`
- Create: `src/Modules/Internship/MESNET.Internship.Application/Commands/UpdateApprovalConfig.cs`
- Create: `src/Modules/Internship/MESNET.Internship.Application/Dtos/ApprovalConfigDto.cs`
- Create: `src/Modules/Internship/MESNET.Internship.Application/Handlers/GetApprovalConfigHandler.cs`
- Create: `src/Modules/Internship/MESNET.Internship.Application/Handlers/UpdateApprovalConfigHandler.cs`
- Create: `tests/MESNET.Internship.UnitTests/ApprovalConfigValidationTests.cs`
- Modify: `src/MESNET.Common.Shared/Tenancy/DocumentTenancyMap.cs`
- Modify: `src/Modules/Internship/MESNET.Internship.Persistence/InternshipMartenConfig.cs`
- Modify: `src/Modules/Internship/MESNET.Internship.Application/Errors/InternshipErrors.cs`
- Modify: `src/Modules/Internship/MESNET.Internship.Api/InternshipEndpoints.cs`

**Interfaces:**
- Consumes: `Error` (`MESNET.Common.Shared`), `DomainException` (`MESNET.Common.Shared`), `ICurrentUserService.GetUserId()`, `Permissions.Internship.ApprovalOverride`, `Permissions.Platform.ParameterManage`, `ResponseBuilder.Success(int code = 200).AddData(object).Build()`.
- Produces: `InternshipApprovalConfig.SingletonId` (Guid) ve `InternshipApprovalConfig.DefaultStuckApprovalDays` (int = 14); `ApprovalConfigDto(int StuckApprovalDays)`. Task 5 varsayılanı ve belgeyi okur.

- [ ] **Step 1: Başarısız doğrulama testini yaz**

`tests/MESNET.Internship.UnitTests/ApprovalConfigValidationTests.cs`:

```csharp
using MESNET.Internship.Core.Entities;
using Shouldly;
using Xunit;

namespace MESNET.Internship.UnitTests;

/// <summary>
/// Eşik doğrulaması. <b>Sıfır ve negatif</b> her açık zinciri tıkanmış yapar — kart anlamını
/// kaybeder. <b>Üst sınır</b> yazım hatasını (14 yerine 1400) kartı sessizce boşaltmadan
/// durdurur.
/// </summary>
public sealed class ApprovalConfigValidationTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(14)]
    [InlineData(365)]
    public void Gecerli_esikler_kabul_edilir(int days)
    {
        InternshipApprovalConfig.IsValidThreshold(days).ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(366)]
    [InlineData(1400)]
    public void Gecersiz_esikler_reddedilir(int days)
    {
        InternshipApprovalConfig.IsValidThreshold(days).ShouldBeFalse();
    }

    [Fact]
    public void Varsayilan_esik_14_gundur()
    {
        InternshipApprovalConfig.DefaultStuckApprovalDays.ShouldBe(14);
        new InternshipApprovalConfig().StuckApprovalDays.ShouldBe(14);
    }
}
```

- [ ] **Step 2: Testin başarısız olduğunu doğrula**

Run: `dotnet test tests/MESNET.Internship.UnitTests/MESNET.Internship.UnitTests.csproj --filter "FullyQualifiedName~ApprovalConfigValidationTests"`
Expected: derleme hatası — `InternshipApprovalConfig` tipi yok.

- [ ] **Step 3: Belgeyi yaz**

`src/Modules/Internship/MESNET.Internship.Core/Entities/InternshipApprovalConfig.cs`:

```csharp
namespace MESNET.Internship.Core.Entities;

/// <summary>
/// Fesih onay zincirinin "tıkanmış" sayılma eşiği — <b>ulusal parametre</b>.
///
/// <para>Kurum kimliği <b>taşımaz</b>: eşik bir işletim politikasıdır ve okul başına
/// değişmez. Yazma izni <c>platform:parameter:manage</c>'dir; hiçbir okul rolünde yoktur.
/// Emsal: <c>AttendanceLimitConfig</c> (#183).</para>
///
/// <para><b>Belge yoksa varsayılan kullanılır ve belge YAZILMAZ.</b> İlk okuma bir yazma
/// tetikleseydi okuma ucunun yan etkisi olurdu ve kiracı kararı okuma yoluna sızardı.</para>
/// </summary>
public sealed class InternshipApprovalConfig
{
    /// <summary>Tekil belge kimliği — sabittir, üretilmez.</summary>
    public static readonly Guid SingletonId = Guid.Parse("8c62ac6c-a944-4eb6-b3b0-342fe7ffc3a6");

    /// <summary>Eşik girilmemişse kullanılan gün sayısı.</summary>
    public const int DefaultStuckApprovalDays = 14;

    private const int MinThresholdDays = 1;
    private const int MaxThresholdDays = 365;

    public Guid Id { get; set; } = SingletonId;

    /// <summary>Açık onay zinciri kaç günden sonra tıkanmış sayılır.</summary>
    public int StuckApprovalDays { get; set; } = DefaultStuckApprovalDays;

    public Guid UpdatedById { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Karar saf ve tek yerdedir; handler bunu çağırır, kendi koşulunu yazmaz.
    /// </summary>
    public static bool IsValidThreshold(int days) =>
        days is >= MinThresholdDays and <= MaxThresholdDays;
}
```

- [ ] **Step 4: Testin geçtiğini doğrula**

Run: `dotnet test tests/MESNET.Internship.UnitTests/MESNET.Internship.UnitTests.csproj --filter "FullyQualifiedName~ApprovalConfigValidationTests"`
Expected: 8 test PASS.

- [ ] **Step 5: Kiracılık sınıflandırmasını ekle**

`src/MESNET.Common.Shared/Tenancy/DocumentTenancyMap.cs` — `["AttendanceLimitConfig"] = Shared,` satırının hemen altına:

```csharp
        // Fesih onay zinciri tıkanma eşiği (D2) de ulusal parametredir: bir işletim
        // politikasıdır, okul başına değişmez. Damgalanırsa her okul kendi eşiğini belirlerdi
        // ve müdürlük panosu okuldan okula başka sayı gösterirdi.
        ["InternshipApprovalConfig"] = Shared,
```

- [ ] **Step 6: Marten şemasına kaydet**

`src/Modules/Internship/MESNET.Internship.Persistence/InternshipMartenConfig.cs` — `Configure` metodunun sonuna:

```csharp
        // Ulusal tekil parametre (D2) — tek satır, indeks gerekmez.
        options.Schema.For<InternshipApprovalConfig>().DatabaseSchemaName("internship");
```

Dosyanın başına `using MESNET.Internship.Core.Entities;` zaten var (`InternshipSummary` için).

- [ ] **Step 7: Sorgu, komut ve DTO'yu yaz**

`src/Modules/Internship/MESNET.Internship.Application/Queries/GetApprovalConfig.cs`:

```csharp
namespace MESNET.Internship.Application.Queries;

/// <summary>Tıkanma eşiğini okur. Parametresizdir — ulusal tekil parametre.</summary>
public sealed record GetApprovalConfig;
```

`src/Modules/Internship/MESNET.Internship.Application/Commands/UpdateApprovalConfig.cs`:

```csharp
namespace MESNET.Internship.Application.Commands;

/// <summary>
/// Tıkanma eşiğini yazar — <b>ulusal parametre</b>. Kurum kimliği taşımaz; yazma izni
/// <c>platform:parameter:manage</c>'dir.
/// </summary>
public sealed record UpdateApprovalConfig(int StuckApprovalDays);
```

`src/Modules/Internship/MESNET.Internship.Application/Dtos/ApprovalConfigDto.cs`:

```csharp
namespace MESNET.Internship.Application.Dtos;

public sealed record ApprovalConfigDto(int StuckApprovalDays);
```

- [ ] **Step 8: Hata mesajını ekle**

`src/Modules/Internship/MESNET.Internship.Application/Errors/InternshipErrors.cs` içine, sınıfın sonuna:

```csharp
    /// <summary>
    /// Eşik aralık dışı. Sıfır ve negatif her açık zinciri tıkanmış yapar; üst sınır yazım
    /// hatasının kartı sessizce boşaltmasını engeller.
    /// </summary>
    public static Error InvalidStuckApprovalThreshold(int days) =>
        new("Internship.InvalidStuckApprovalThreshold",
            $"Tıkanma eşiği 1 ile 365 gün arasında olmalıdır. Girilen: {days}");
```

- [ ] **Step 9: Handler'ları yaz**

`src/Modules/Internship/MESNET.Internship.Application/Handlers/GetApprovalConfigHandler.cs`:

```csharp
using Marten;
using MESNET.Internship.Application.Dtos;
using MESNET.Internship.Application.Queries;
using MESNET.Internship.Core.Entities;

namespace MESNET.Internship.Application.Handlers;

/// <summary>
/// Eşiği okur. <b>Belge yoksa varsayılan döner ve belge YAZILMAZ</b> — okuma ucunun yan etkisi
/// olmaz.
/// </summary>
public static class GetApprovalConfigHandler
{
    public static async Task<ApprovalConfigDto> Handle(
        GetApprovalConfig query, IQuerySession session, CancellationToken cancellationToken)
    {
        var config = await session.LoadAsync<InternshipApprovalConfig>(
            InternshipApprovalConfig.SingletonId, cancellationToken);

        return new ApprovalConfigDto(
            config?.StuckApprovalDays ?? InternshipApprovalConfig.DefaultStuckApprovalDays);
    }
}
```

`src/Modules/Internship/MESNET.Internship.Application/Handlers/UpdateApprovalConfigHandler.cs`:

```csharp
using Marten;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using MESNET.Internship.Application.Commands;
using MESNET.Internship.Application.Errors;
using MESNET.Internship.Core.Entities;

namespace MESNET.Internship.Application.Handlers;

/// <summary>
/// Eşiği yazar. Tek satırlık ulusal parametre — sürüm geçmişi yok; eşik sorgu ANINDA
/// değerlendirilir, geriye dönük hesap yoktur.
/// </summary>
public static class UpdateApprovalConfigHandler
{
    public static async Task Handle(
        UpdateApprovalConfig command,
        IDocumentSession session,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (!InternshipApprovalConfig.IsValidThreshold(command.StuckApprovalDays))
            throw new DomainException(
                InternshipErrors.InvalidStuckApprovalThreshold(command.StuckApprovalDays));

        var config = await session.LoadAsync<InternshipApprovalConfig>(
                         InternshipApprovalConfig.SingletonId, cancellationToken)
                     ?? new InternshipApprovalConfig();

        config.StuckApprovalDays = command.StuckApprovalDays;
        config.UpdatedById = currentUser.GetUserId();
        config.UpdatedAt = DateTime.UtcNow;

        session.Store(config);
    }
}
```

- [ ] **Step 10: Uçları ekle**

`src/Modules/Internship/MESNET.Internship.Api/InternshipEndpoints.cs` — `MapInternshipEndpoints` içinde, `resync-sagas` satırından önce:

```csharp
        // Tıkanma eşiği. OKUMA müdahale yetkisine bağlıdır: eşiği yalnız kartı gören
        // kullanıcının bilmesi anlamlıdır. YAZMA ulusal parametre iznidir ve hiçbir okul
        // rolünde yoktur — okul kendi eşiğini belirleyemez.
        group.MapGet("/approval-config", GetApprovalConfiguration)
            .RequireAuthorization(Permissions.Internship.ApprovalOverride);
        group.MapPut("/approval-config", PutApprovalConfiguration)
            .RequireAuthorization(Permissions.Platform.ParameterManage);
```

Ve sınıfın metotları arasına:

```csharp
    private static async Task<IResult> GetApprovalConfiguration(IMessageBus bus)
    {
        var dto = await bus.InvokeAsync<ApprovalConfigDto>(new GetApprovalConfig());
        return Results.Ok(ResponseBuilder.Success().AddData(dto).Build());
    }

    private static async Task<IResult> PutApprovalConfiguration(
        UpdateApprovalConfig command, IMessageBus bus)
    {
        await bus.InvokeAsync(command);
        return Results.Ok(ResponseBuilder.Success().Build());
    }
```

- [ ] **Step 11: Derle ve tüm testleri koş**

Run: `dotnet build MESNET.slnx && dotnet test MESNET.slnx`
Expected: derleme temiz, tüm testler yeşil (`DocumentTenancyMap` sınıflandırma testi dahil — sınıflandırma eklenmezse o test kırmızı olurdu).

- [ ] **Step 12: Commit**

```bash
git add src/Modules/Internship src/MESNET.Common.Shared/Tenancy/DocumentTenancyMap.cs \
        tests/MESNET.Internship.UnitTests/ApprovalConfigValidationTests.cs
git commit -m "feat(internship): tıkanma eşiği ulusal parametre olarak eklendi (varsayılan 14 gün)"
```

---

### Task 4: Fesih talep zamanı

Saga bugün fesih talebinin **ne zaman** açıldığını hiç tutmuyor: `ApprovalChain` yalnız `OverriddenAt` ve `CompletedAt` taşır, ikisi de zincir **kapanınca** dolar.

**Files:**
- Modify: `src/Modules/Internship/MESNET.Internship.Application/Sagas/InternshipSaga.cs`
- Create: `tests/MESNET.Internship.UnitTests/TerminationRequestedAtTests.cs`

**Interfaces:**
- Consumes: mevcut `InternshipSaga.Handle(InternshipTerminationRequested, ILogger)`.
- Produces: `InternshipSaga.TerminationRequestedAt` (`DateTime?`). Task 5 ve Task 6 bu alanı okur.

- [ ] **Step 1: Başarısız testi yaz**

`tests/MESNET.Internship.UnitTests/TerminationRequestedAtTests.cs`:

```csharp
using MESNET.Enrollment.Shared.Events;
using MESNET.Internship.Application.Sagas;
using MESNET.Internship.Shared.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace MESNET.Internship.UnitTests;

/// <summary>
/// Fesih talebinin açılma zamanı kaydedilir. Bu alan olmadan "kaç gündür bekliyor"
/// hesaplanamaz — müdürlük panosunun tıkanmışlık kartı bütünüyle buna dayanır.
/// </summary>
public sealed class TerminationRequestedAtTests
{
    private static InternshipSaga StartedSaga()
    {
        var placed = new StudentPlaced(
            PlacementId: Guid.NewGuid(),
            StudentId: Guid.NewGuid(),
            StudentName: "Test Öğrenci",
            BusinessId: Guid.NewGuid(),
            BusinessName: "Test İşletme",
            InstitutionId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid());

        var (saga, _) = InternshipSaga.Start(placed);
        return saga;
    }

    [Fact]
    public void Yeni_saga_talep_zamani_tasimaz()
    {
        StartedSaga().TerminationRequestedAt.ShouldBeNull();
    }

    [Fact]
    public void Fesih_talebi_acildiginda_zaman_kaydedilir()
    {
        // Arrange
        var saga = StartedSaga();
        var before = DateTime.UtcNow;

        // Act
        saga.Handle(
            new InternshipTerminationRequested(saga.Id, "Gerekçe", "BusinessRequest"),
            NullLogger.Instance);

        // Assert
        saga.TerminationRequestedAt.ShouldNotBeNull();
        saga.TerminationRequestedAt!.Value.ShouldBeGreaterThanOrEqualTo(before);
    }

    /// <summary>
    /// İkinci talep yok sayılır (zincir zaten yürüyor) — zaman damgası da EZİLMEZ, yoksa her
    /// yinelenen talep sayacı sıfırlar ve tıkanmış zincir sonsuza kadar taze görünür.
    /// </summary>
    [Fact]
    public void Ikinci_talep_zamani_ezmez()
    {
        var saga = StartedSaga();
        saga.Handle(
            new InternshipTerminationRequested(saga.Id, "İlk", "BusinessRequest"),
            NullLogger.Instance);
        var first = saga.TerminationRequestedAt;

        saga.Handle(
            new InternshipTerminationRequested(saga.Id, "İkinci", "BusinessRequest"),
            NullLogger.Instance);

        saga.TerminationRequestedAt.ShouldBe(first);
    }
}
```

**Not:** `StudentPlaced` ve `InternshipTerminationRequested` kayıtlarının gerçek parametre listesi farklıysa testi derlenen imzaya uydur — alan adları değişmez, yalnız kurucu çağrısı uyarlanır.

- [ ] **Step 2: Testin başarısız olduğunu doğrula**

Run: `dotnet test tests/MESNET.Internship.UnitTests/MESNET.Internship.UnitTests.csproj --filter "FullyQualifiedName~TerminationRequestedAtTests"`
Expected: derleme hatası — `TerminationRequestedAt` üyesi yok.

- [ ] **Step 3: Alanı ekle**

`src/Modules/Internship/MESNET.Internship.Application/Sagas/InternshipSaga.cs` — `ApprovalChain` özelliğinin hemen altına:

```csharp
    /// <summary>
    /// Fesih talebinin açıldığı an (D2). <b>Zincir kapanınca temizlenmez</b> — kapanmış zincir
    /// zaten tıkanmış sayılmaz, ayrıca geçmişi silmek denetim değerini yok ederdi.
    ///
    /// <para><c>null</c> iki şey olabilir: fesih hiç istenmedi (zincir de yok) ya da kayıt bu
    /// alan eklenmeden önce doğdu. İkinci hâlde saga <b>tıkanmış sayılır</b> — eksik veri
    /// sınırı gevşetemez (#252). Ters karar aylardır takılı duran eski kayıtları panodan
    /// sessizce silerdi.</para>
    /// </summary>
    public DateTime? TerminationRequestedAt { get; set; }
```

- [ ] **Step 4: Talep yolunda doldur**

Aynı dosyada `Handle(InternshipTerminationRequested e, ILogger logger)` metodunda, `ApprovalChain = new TerminationApprovalChain();` satırının hemen altına:

```csharp
        TerminationRequestedAt = DateTime.UtcNow;
```

Bu satır zincirin kurulduğu yerin **yanında** durur, ayrı bir yola konmaz: ikisi tek olaydır ve ayrılırlarsa biri diğeri olmadan çalışabilir.

Erken `return null` dalı (zincir zaten yürüyor) bu satırdan **önce**dir, dolayısıyla ikinci talep zamanı ezmez.

- [ ] **Step 5: Testin geçtiğini doğrula**

Run: `dotnet test tests/MESNET.Internship.UnitTests/MESNET.Internship.UnitTests.csproj --filter "FullyQualifiedName~TerminationRequestedAtTests"`
Expected: 3 test PASS.

- [ ] **Step 6: Tüm testleri koş**

Run: `dotnet test MESNET.slnx`
Expected: hepsi yeşil.

- [ ] **Step 7: Commit**

```bash
git add src/Modules/Internship/MESNET.Internship.Application/Sagas/InternshipSaga.cs \
        tests/MESNET.Internship.UnitTests/TerminationRequestedAtTests.cs
git commit -m "feat(internship): fesih talep zamanı saga'da kaydediliyor"
```

---

### Task 5: Tıkanmışlık kararı ve kiracılar arası sorgu

Kartın çekirdeği. Karar saf bir politikada yaşar; LINQ ifadesinin o politikayla aynı şeyi söylediği **doğruluk tablosuyla** kilitlenir.

**Files:**
- Create: `src/Modules/Internship/MESNET.Internship.Core/Policies/StuckApprovalPolicy.cs`
- Create: `src/Modules/Internship/MESNET.Internship.Application/Queries/GetStuckApprovals.cs`
- Create: `src/Modules/Internship/MESNET.Internship.Application/Dtos/StuckApprovalSummaryDto.cs`
- Create: `src/Modules/Internship/MESNET.Internship.Application/Handlers/GetStuckApprovalsHandler.cs`
- Create: `tests/MESNET.Internship.UnitTests/StuckApprovalPolicyTests.cs`
- Modify: `src/Modules/Internship/MESNET.Internship.Api/InternshipEndpoints.cs`

**Interfaces:**
- Consumes: Task 2'nin `SubtreeTenantScope.ResolveAsync(InstitutionVisibility, CancellationToken)`; Task 3'ün `InternshipApprovalConfig.SingletonId` / `DefaultStuckApprovalDays`; Task 4'ün `InternshipSaga.TerminationRequestedAt`; `TerminationApprovalChain` (`MESNET.Internship.Core.ValueObjects`) ve onun `IsCompleteOrOverridden()` metodu; `InstitutionScopePolicy.VisibleScope(Guid?, string?, bool)`; `ICurrentUserService.GetCurrentUser()?.InstitutionId`, `.GetInstitutionPath()`, `.HasPermission(...)`.
- Produces: `StuckApprovalSummaryDto(int TotalCount, int ThresholdDays, IReadOnlyList<StuckApprovalByInstitutionDto> ByInstitution)` ve `StuckApprovalByInstitutionDto(Guid InstitutionId, string? InstitutionName, int Count, int? OldestDays)`. Task 11 (frontend) bunları tüketir.

- [ ] **Step 1: Başarısız politika testini yaz**

`tests/MESNET.Internship.UnitTests/StuckApprovalPolicyTests.cs`:

```csharp
using MESNET.Internship.Core.Policies;
using MESNET.Internship.Core.ValueObjects;
using Shouldly;
using Xunit;

namespace MESNET.Internship.UnitTests;

/// <summary>
/// Tıkanmışlık kararı ve o kararın LINQ ikizinin doğruluğu.
///
/// <para><b>Neden doğruluk tablosu:</b> Marten <c>IsCompleteOrOverridden()</c> metodunu SQL'e
/// çeviremez, bu yüzden sorgu koşulu bayrakları AÇARAK yazmak zorunda. Aynı karar iki yerde
/// yaşayınca ayrışabilir — ve ayrışma sessiz olur: kart yanlış sayı gösterir, hiçbir test
/// kırılmaz. Tablo o ayrışmayı imkânsız kılar.</para>
/// </summary>
public sealed class StuckApprovalPolicyTests
{
    private static readonly DateTime Now = new(2026, 8, 30, 12, 0, 0, DateTimeKind.Utc);

    private static TerminationApprovalChain Chain(
        bool teacher = false, bool deputy = false, bool director = false, bool overridden = false) =>
        new()
        {
            TeacherApproved = teacher,
            DeputyApproved = deputy,
            DirectorApproved = director,
            IsOverridden = overridden,
        };

    /// <summary>
    /// 16 bayrak birleşiminin HEPSİNDE, sorguda kullanılan açık ifade politikanın kendisiyle
    /// aynı şeyi söylemeli. Zincir kuralı bir gün değişirse (dördüncü onaycı) bu test kırmızı
    /// olur ve ayrışma sessiz kalmaz.
    /// </summary>
    [Fact]
    public void Acik_LINQ_ifadesi_politikayla_ayni_seyi_soyler()
    {
        var mismatches = new List<string>();

        foreach (var teacher in new[] { false, true })
        foreach (var deputy in new[] { false, true })
        foreach (var director in new[] { false, true })
        foreach (var overridden in new[] { false, true })
        {
            var chain = Chain(teacher, deputy, director, overridden);

            // GetStuckApprovalsHandler içindeki Where koşulunun birebir kopyası.
            var linq = !chain.IsOverridden
                       && !(chain.TeacherApproved && chain.DeputyApproved && chain.DirectorApproved);

            var policy = !chain.IsCompleteOrOverridden();

            if (linq != policy)
                mismatches.Add($"T={teacher} D={deputy} Dir={director} Ovr={overridden}: "
                               + $"linq={linq} policy={policy}");
        }

        mismatches.ShouldBeEmpty(
            "GetStuckApprovalsHandler'daki açık LINQ koşulu ile "
            + "TerminationApprovalChain.IsCompleteOrOverridden() ayrışmış. Sorgu koşulunu "
            + $"düzeltin. Ayrışmalar: {string.Join(" | ", mismatches)}");
    }

    [Fact]
    public void Zincir_yoksa_tikanmis_degildir()
    {
        StuckApprovalPolicy.IsStuck(null, requestedAt: null, Now, thresholdDays: 14)
            .ShouldBeFalse();
    }

    [Fact]
    public void Kapanmis_zincir_tikanmis_degildir()
    {
        var chain = Chain(teacher: true, deputy: true, director: true);
        StuckApprovalPolicy.IsStuck(chain, Now.AddDays(-100), Now, thresholdDays: 14)
            .ShouldBeFalse();
    }

    [Fact]
    public void Override_edilmis_zincir_tikanmis_degildir()
    {
        var chain = Chain(overridden: true);
        StuckApprovalPolicy.IsStuck(chain, Now.AddDays(-100), Now, thresholdDays: 14)
            .ShouldBeFalse();
    }

    [Fact]
    public void Esigin_altindaki_acik_zincir_tikanmis_degildir()
    {
        var chain = Chain(teacher: true);
        StuckApprovalPolicy.IsStuck(chain, Now.AddDays(-13), Now, thresholdDays: 14)
            .ShouldBeFalse();
    }

    [Fact]
    public void Esigi_asan_acik_zincir_tikanmistir()
    {
        var chain = Chain(teacher: true);
        StuckApprovalPolicy.IsStuck(chain, Now.AddDays(-15), Now, thresholdDays: 14)
            .ShouldBeTrue();
    }

    /// <summary>
    /// EKSİK VERİ SINIRI GEVŞETEMEZ (#252). Talep zamanı bilinmeyen açık zincir tıkanmış
    /// SAYILIR. Ters karar aylardır takılı duran eski kayıtları panodan sessizce silerdi —
    /// tam olarak kartın var olma sebebi olan durum.
    /// </summary>
    [Fact]
    public void Talep_zamani_bilinmeyen_acik_zincir_tikanmistir()
    {
        var chain = Chain(teacher: true);
        StuckApprovalPolicy.IsStuck(chain, requestedAt: null, Now, thresholdDays: 14)
            .ShouldBeTrue();
    }

    [Fact]
    public void Yas_gun_olarak_hesaplanir_bilinmiyorsa_null()
    {
        StuckApprovalPolicy.AgeInDays(Now.AddDays(-15), Now).ShouldBe(15);
        StuckApprovalPolicy.AgeInDays(null, Now).ShouldBeNull();
    }
}
```

- [ ] **Step 2: Testin başarısız olduğunu doğrula**

Run: `dotnet test tests/MESNET.Internship.UnitTests/MESNET.Internship.UnitTests.csproj --filter "FullyQualifiedName~StuckApprovalPolicyTests"`
Expected: derleme hatası — `StuckApprovalPolicy` tipi yok.

- [ ] **Step 3: Politikayı yaz**

`src/Modules/Internship/MESNET.Internship.Core/Policies/StuckApprovalPolicy.cs`:

```csharp
using MESNET.Internship.Core.ValueObjects;

namespace MESNET.Internship.Core.Policies;

/// <summary>
/// Bir fesih onay zinciri "tıkanmış" mı (D2).
///
/// <para><b>Faz alanına BAKILMAZ.</b> <c>InternshipSaga.Phase</c> bir SmartEnum'dur ve Marten
/// LINQ'te nested path'i her zaman <c>NULL</c> döner. Düz bir <c>PhaseName</c> ikizi eklemek
/// <b>yanlış yöne</b> başarısız olurdu: alan yeni olduğu için mevcut satırlarda yoktur, o
/// satırlar süzgece takılmaz ve kart eskileri SESSİZCE hiç göstermez — aranan kayıtlar tam
/// olarak eskiler olduğu için kart işe yaramazdı. Faz zaten türetilebilir: zincir varsa ve
/// kapanmamışsa saga tanımı gereği <c>TerminationInProgress</c>'tedir.</para>
/// </summary>
public static class StuckApprovalPolicy
{
    /// <param name="chain">Fesih onay zinciri; <c>null</c> ise fesih hiç istenmemiştir.</param>
    /// <param name="requestedAt">
    /// Talebin açıldığı an. <c>null</c> <b>tıkanmış</b> demektir — eksik veri sınırı
    /// gevşetemez (#252).
    /// </param>
    public static bool IsStuck(
        TerminationApprovalChain? chain, DateTime? requestedAt, DateTime now, int thresholdDays)
    {
        if (chain is null)
            return false;

        if (chain.IsCompleteOrOverridden())
            return false;

        if (requestedAt is null)
            return true;

        return requestedAt.Value <= now.AddDays(-thresholdDays);
    }

    /// <summary>Zincirin yaşı gün olarak; talep zamanı bilinmiyorsa <c>null</c>.</summary>
    public static int? AgeInDays(DateTime? requestedAt, DateTime now) =>
        requestedAt is { } at ? (int)(now - at).TotalDays : null;
}
```

- [ ] **Step 4: Testin geçtiğini doğrula**

Run: `dotnet test tests/MESNET.Internship.UnitTests/MESNET.Internship.UnitTests.csproj --filter "FullyQualifiedName~StuckApprovalPolicyTests"`
Expected: 8 test PASS.

- [ ] **Step 5: Sorgu ve DTO'ları yaz**

`src/Modules/Internship/MESNET.Internship.Application/Queries/GetStuckApprovals.cs`:

```csharp
namespace MESNET.Internship.Application.Queries;

/// <summary>
/// Tıkanmış fesih onaylarının özeti. <b>Parametresizdir</b> — kapsam istekten alınmaz,
/// aktörün claim'lerinden türer.
/// </summary>
public sealed record GetStuckApprovals;
```

`src/Modules/Internship/MESNET.Internship.Application/Dtos/StuckApprovalSummaryDto.cs`:

```csharp
namespace MESNET.Internship.Application.Dtos;

/// <param name="TotalCount">Alt ağaçtaki tıkanmış zincir sayısı.</param>
/// <param name="ThresholdDays">Karar anındaki eşik — ön yüz boş-durum metnini bununla yazar.</param>
public sealed record StuckApprovalSummaryDto(
    int TotalCount,
    int ThresholdDays,
    IReadOnlyList<StuckApprovalByInstitutionDto> ByInstitution);

/// <param name="InstitutionName">
/// <b>Her zaman <c>null</c></b>: kurum adı Institution modülünündür ve buradan okunamaz (şema
/// izolasyonu). Ön yüz lookup map ile doldurur. Alan yine de durur ki istemci kendi tipini
/// uydurmasın.
/// </param>
/// <param name="OldestDays">
/// En eski zincirin yaşı. <c>null</c> = o kurumdaki tıkanmış zincirlerin hiçbirinde talep
/// zamanı bilinmiyor. Sıfır ya da sentinel yazmak sayıyı sessizce yanlışlardı.
/// </param>
public sealed record StuckApprovalByInstitutionDto(
    Guid InstitutionId,
    string? InstitutionName,
    int Count,
    int? OldestDays);
```

- [ ] **Step 6: Handler'ı yaz**

`src/Modules/Internship/MESNET.Internship.Application/Handlers/GetStuckApprovalsHandler.cs`:

```csharp
using Marten;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Infrastructure.Tenancy;
using MESNET.Common.Shared.Security;
using MESNET.Internship.Application.Dtos;
using MESNET.Internship.Application.Queries;
using MESNET.Internship.Application.Sagas;
using MESNET.Internship.Core.Entities;
using MESNET.Internship.Core.Policies;

namespace MESNET.Internship.Application.Handlers;

/// <summary>
/// Alt ağaçtaki tıkanmış fesih onaylarını sayar.
///
/// <para><b>Kiracı sınırı bilerek aşılır.</b> <c>InternshipSaga</c> kiracıya aittir ve
/// müdürlük düğümü kiracı DEĞİLDİR — müdürlüğün kendi kiracısında sorgu boş dönerdi.
/// Marten'in <c>TenantIsOneOf(...)</c> operatörü kapsamı okul kiracılarına açar; kimlikler
/// <b>istekten değil</b> <see cref="SubtreeTenantScope"/> üzerinden aktörün
/// claim'lerinden gelir.</para>
///
/// <para><b>Boş listede sorgu HİÇ kurulmaz</b> — parametresiz <c>TenantIsOneOf()</c>'un SQL'de
/// ne ürettiğine güvenilmez.</para>
///
/// <para><b>Belgeler tam çekilir, projeksiyon yapılmaz.</b> Süzgeç zaten "eşiği aşmış açık
/// zincir" olduğu için küme küçüktür; Marten'in projeksiyon çevirisine bağımlılık eklemeye
/// değmez.</para>
/// </summary>
public static class GetStuckApprovalsHandler
{
    public static async Task<StuckApprovalSummaryDto> Handle(
        GetStuckApprovals query,
        IQuerySession session,
        ICurrentUserService currentUser,
        SubtreeTenantScope tenantScope,
        CancellationToken cancellationToken)
    {
        var config = await session.LoadAsync<InternshipApprovalConfig>(
            InternshipApprovalConfig.SingletonId, cancellationToken);
        var thresholdDays =
            config?.StuckApprovalDays ?? InternshipApprovalConfig.DefaultStuckApprovalDays;

        var visibility = InstitutionScopePolicy.VisibleScope(
            currentUser.GetCurrentUser()?.InstitutionId,
            currentUser.GetInstitutionPath(),
            currentUser.HasPermission(Permissions.Platform.TenantManage));

        var tenants = await tenantScope.ResolveAsync(visibility, cancellationToken);

        if (tenants.Count == 0)
            return new StuckApprovalSummaryDto(0, thresholdDays, []);

        var now = DateTime.UtcNow;
        var cutoff = now.AddDays(-thresholdDays);
        var tenantIds = tenants.ToArray();

        // IsCompleteOrOverridden() bir metottur ve SQL'e çevrilemez; koşul AÇILARAK yazılır.
        // Bu açılımın politikayla aynı şeyi söylediği StuckApprovalPolicyTests içindeki
        // doğruluk tablosuyla kilitlidir.
        //
        // Talep zamanı NULL olan kayıt bilerek İÇERİDE bırakılır: eksik veri sınırı
        // gevşetemez (#252).
        var stuck = await session.Query<InternshipSaga>()
            .Where(x => x.TenantIsOneOf(tenantIds)
                        && x.ApprovalChain != null
                        && !x.ApprovalChain.IsOverridden
                        && !(x.ApprovalChain.TeacherApproved
                             && x.ApprovalChain.DeputyApproved
                             && x.ApprovalChain.DirectorApproved)
                        && (x.TerminationRequestedAt == null
                            || x.TerminationRequestedAt <= cutoff))
            .ToListAsync(cancellationToken);

        var byInstitution = stuck
            .GroupBy(x => x.InstitutionId)
            .Select(g => new StuckApprovalByInstitutionDto(
                InstitutionId: g.Key,
                InstitutionName: null,
                Count: g.Count(),
                OldestDays: g
                    .Select(x => StuckApprovalPolicy.AgeInDays(x.TerminationRequestedAt, now))
                    .Where(age => age is not null)
                    .DefaultIfEmpty(null)
                    .Max()))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.InstitutionId)
            .ToList();

        return new StuckApprovalSummaryDto(stuck.Count, thresholdDays, byInstitution);
    }
}
```

**Not:** `TenantIsOneOf` uzantısı `Marten.LinqExtensions` sınıfındadır ve `Marten` ad alanındadır — dosyanın başındaki `using Marten;` yeterlidir (doğrulandı: Marten 9.11.0, `M:Marten.LinqExtensions.TenantIsOneOf``1(``0,System.String[])`).

**Not:** `OrderByDescending(...).ThenBy(x => x.InstitutionId)` **zorunludur**. Yalnız sayıya göre sıralamak eşit sayılı kurumlarda kararsız sıra bırakır; Postgres güncellenen satırı heap'te oynattığı için liste iki çağrı arasında değişir ve ön yüz "ilk 5" gösterirken her yenilemede başka okulları gösterir.

- [ ] **Step 7: Ucu ekle**

`src/Modules/Internship/MESNET.Internship.Api/InternshipEndpoints.cs` — Task 3'te eklenen `approval-config` satırlarının hemen üstüne:

```csharp
        // Tıkanmış onaylar kartı (D2). İzin BİLEREK internship:view değil approval:override:
        // kart yalnız müdahale edebilecek aktöre bilgi taşır; görüp müdahale edemeyen
        // kullanıcı için bilgi değil gürültüdür.
        group.MapGet("/stuck-approvals", GetStuckApprovalSummary)
            .RequireAuthorization(Permissions.Internship.ApprovalOverride);
```

Ve metotlar arasına:

```csharp
    private static async Task<IResult> GetStuckApprovalSummary(IMessageBus bus)
    {
        var dto = await bus.InvokeAsync<StuckApprovalSummaryDto>(new GetStuckApprovals());
        return Results.Ok(ResponseBuilder.Success().AddData(dto).Build());
    }
```

- [ ] **Step 8: Kilidin sorgu koşulunu koruduğunu kanıtla**

`GetStuckApprovalsHandler` içindeki `!x.ApprovalChain.IsOverridden &&` parçasını **geçici olarak sil**, sonra `StuckApprovalPolicyTests` içindeki doğruluk tablosunun LINQ kopyasından da aynı parçayı sil.

Run: `dotnet test tests/MESNET.Internship.UnitTests/MESNET.Internship.UnitTests.csproj --filter "FullyQualifiedName~StuckApprovalPolicyTests"`
Expected: `Acik_LINQ_ifadesi_politikayla_ayni_seyi_soyler` **FAIL** — override edilmiş zincirlerde ayrışma raporlanır.

İki değişikliği de **geri al** ve tekrar koş.
Expected: 8 test PASS.

- [ ] **Step 9: Derle ve tüm testleri koş**

Run: `dotnet build MESNET.slnx && dotnet test MESNET.slnx`
Expected: derleme temiz (`CrossTenantQueryDriftTests` `GetStuckApprovalsHandler.cs`'i izinli dosya listesinde bulur), tüm testler yeşil.

- [ ] **Step 10: Commit**

```bash
git add src/Modules/Internship tests/MESNET.Internship.UnitTests/StuckApprovalPolicyTests.cs
git commit -m "feat(internship): tıkanmış fesih onayları alt ağaç kiracılarında tek sorguyla sayılıyor"
```

---


### Task 6: Yönetici bağı read-model'i ve tüketicisi

`Institution` belgesi kimin yöneticisi olduğunu bilmez; bilgi Security modülünün `UserAccount`'undadır ve o şemaya sorgu atmak yasaktır. Bilgi olaylarla taşınır.

**Files:**
- Create: `src/Modules/Institution/MESNET.Institution.Core/ReadModels/InstitutionManagerLink.cs`
- Create: `src/Modules/Institution/MESNET.Institution.Application/Consumers/InstitutionManagerLinkConsumer.cs`
- Create: `tests/MESNET.Institution.UnitTests/ManagerLinkPermissionTests.cs`
- Modify: `src/MESNET.Common.Shared/Tenancy/DocumentTenancyMap.cs`
- Modify: `src/Modules/Institution/MESNET.Institution.Persistence/InstitutionMartenConfig.cs`

**Interfaces:**
- Consumes: `MESNET.Security.Shared.Events` içindeki `UserCreated(Guid UserAccountId, string KeycloakUserId, string Username, string FullName, string Email, IReadOnlyList<string> Roles, Guid? InstitutionId, Guid? BusinessId, Dictionary<string,string> Metadata)`, `UserInstitutionChanged(Guid UserAccountId, string KeycloakUserId, Guid? PreviousInstitutionId, Guid? InstitutionId)`, `UserRolesChanged(Guid UserAccountId, string KeycloakUserId, IReadOnlyList<string> PreviousRoles, IReadOnlyList<string> NewRoles)`, `UserActivated(Guid UserAccountId, string KeycloakUserId)`, `UserDeactivated(Guid UserAccountId, string KeycloakUserId, string Reason)`, `UserDeleted(Guid UserAccountId, string KeycloakUserId)`; `RolePermissionMap.GetPermissionsForRoles(IEnumerable<string>) → IReadOnlyList<string>`; `Permissions.Institution.Manage`.
- Produces: `InstitutionManagerLink { Guid Id; Guid? InstitutionId; bool IsEnabled; bool HasManagePermission; DateTime UpdatedAt }` ve `InstitutionManagerLink.HasManage(IEnumerable<string> roles) → bool`. Task 7 ve Task 8 bunları okur.

- [ ] **Step 0: Proje referansını doğrula**

Run: `grep -n "Security.Shared" src/Modules/Institution/MESNET.Institution.Application/MESNET.Institution.Application.csproj`

Satır yoksa ekle (`.Shared` katmanına referans **serbesttir**, `.Core`/`.Application`/`.Persistence` yasaktır):

```xml
    <ProjectReference Include="../../Security/MESNET.Security.Shared/MESNET.Security.Shared.csproj" />
```

- [ ] **Step 1: Başarısız testi yaz**

`tests/MESNET.Institution.UnitTests/ManagerLinkPermissionTests.cs`:

```csharp
using MESNET.Institution.Core.ReadModels;
using Shouldly;
using Xunit;

namespace MESNET.Institution.UnitTests;

/// <summary>
/// "Yöneticisi var mı" sorusu <b>izne</b> bakar, rol adına değil (ADR-0001).
///
/// <para>Rol adına bakan bir kontrol yazılsaydı, yeni bir rol (ör. bir müdür vekili rolü)
/// <c>institution:manage</c> taşımasına rağmen listede görünmez ve okul sonsuza kadar
/// "yöneticisiz" kalırdı — hata değil, yanlış liste.</para>
/// </summary>
public sealed class ManagerLinkPermissionTests
{
    [Fact]
    public void Kurum_yoneticisi_rolu_manage_izni_tasir()
    {
        InstitutionManagerLink.HasManage(["InstitutionManager"]).ShouldBeTrue();
    }

    [Fact]
    public void Ogretmen_rolu_manage_izni_tasimaz()
    {
        InstitutionManagerLink.HasManage(["Teacher"]).ShouldBeFalse();
    }

    [Fact]
    public void Rol_yoksa_manage_izni_yoktur()
    {
        InstitutionManagerLink.HasManage([]).ShouldBeFalse();
    }

    [Fact]
    public void Taninmayan_rol_izin_vermez()
    {
        InstitutionManagerLink.HasManage(["BöyleBirRolYok"]).ShouldBeFalse();
    }

    /// <summary>
    /// Rollerden biri yetiyorsa yeter — kullanıcı birden çok rol taşıyabilir.
    /// </summary>
    [Fact]
    public void Rollerden_biri_yetiyorsa_izin_vardir()
    {
        InstitutionManagerLink.HasManage(["Teacher", "InstitutionManager"]).ShouldBeTrue();
    }
}
```

**Not:** Rol adlarını yazmadan önce gerçek değerleri ölç:

```bash
grep -n "\"InstitutionManager\"\|\"Teacher\"" src/MESNET.Common.Shared/Security/RolePermissionMap.cs | head
```

Eşleşmiyorsa testteki adları ölçülen adlarla değiştir. **Testte rol adı geçmesi kural ihlali değildir** — test, izin türetmesinin doğru çalıştığını kanıtlıyor; kararın kendisi hâlâ izne bakıyor.

- [ ] **Step 2: Testin başarısız olduğunu doğrula**

Run: `dotnet test tests/MESNET.Institution.UnitTests/MESNET.Institution.UnitTests.csproj --filter "FullyQualifiedName~ManagerLinkPermissionTests"`
Expected: derleme hatası — `InstitutionManagerLink` tipi yok.

- [ ] **Step 3: Read-model'i yaz**

`src/Modules/Institution/MESNET.Institution.Core/ReadModels/InstitutionManagerLink.cs`:

```csharp
using MESNET.Common.Shared.Security;

namespace MESNET.Institution.Core.ReadModels;

/// <summary>
/// Bir kullanıcının kurum bağı ve yönetme yetkisi — Institution modülünün kendi şemasındaki
/// yerel görünüm (D2).
///
/// <para><b>Neden KULLANICI başına, kurum başına sayaç değil:</b> sayacı azaltması gereken
/// olaylar (<c>UserRolesChanged</c>, <c>UserDeactivated</c>, <c>UserDeleted</c>) kurum kimliği
/// <b>taşımaz</b>. Institution modülü rolü değişen kullanıcının hangi okula bağlı olduğunu
/// bilemez, dolayısıyla hangi satırdan düşeceğini de bilemez. Kullanıcı başına satırda her
/// olay tek bir kullanıcının durumunu <b>mutlak</b> olarak yazar; artırma/azaltma yoktur,
/// kayan sayaç da yoktur.</para>
///
/// <para><b>Neden Security'ye sorulmuyor:</b> <c>UserAccount</c> Security modülünün
/// şemasındadır ve başka modülün oraya sorgu atması yasaktır (şema izolasyonu). Bilgi
/// olaylarla taşınır.</para>
/// </summary>
public sealed class InstitutionManagerLink
{
    /// <summary>Kullanıcı hesabı kimliği — belge kimliği olarak kullanılır.</summary>
    public Guid Id { get; set; }

    /// <summary><c>null</c> = kullanıcı hiçbir kuruma bağlı değil.</summary>
    public Guid? InstitutionId { get; set; }

    public bool IsEnabled { get; set; } = true;

    public bool HasManagePermission { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Roller <c>institution:manage</c> veriyor mu.
    ///
    /// <para><b>Rol adına BAKILMAZ</b> (ADR-0001): karar izne bakar.
    /// <c>RolePermissionMap.GetPermissionsForRoles</c> wildcard'ları
    /// (<c>institution:*</c>) zaten genişletir.</para>
    /// </summary>
    public static bool HasManage(IEnumerable<string> roles) =>
        RolePermissionMap.GetPermissionsForRoles(roles)
            .Contains(Permissions.Institution.Manage, StringComparer.OrdinalIgnoreCase);
}
```

- [ ] **Step 4: Testin geçtiğini doğrula**

Run: `dotnet test tests/MESNET.Institution.UnitTests/MESNET.Institution.UnitTests.csproj --filter "FullyQualifiedName~ManagerLinkPermissionTests"`
Expected: 5 test PASS.

- [ ] **Step 5: Kiracılık sınıflandırması ve Marten şeması**

`src/MESNET.Common.Shared/Tenancy/DocumentTenancyMap.cs` — `["UserAccount"] = Identity,` satırının hemen altına:

```csharp
        // Kullanıcının kurum bağı ve yönetme yetkisinin Institution modülündeki yerel görünümü
        // (D2). Kaynağı UserAccount, hedefi Institution — ikisi de kimlik katmanında ve kiracı
        // damgası taşımıyor; damgalansaydı müdürlük onu hiç göremezdi.
        ["InstitutionManagerLink"] = Identity,
```

`src/Modules/Institution/MESNET.Institution.Persistence/InstitutionMartenConfig.cs` — `Configure` metodunun sonuna:

```csharp
        // Yöneticisiz okul sorgusu bu alanla süzer; indeks olmadan her çağrıda tam tarama olur.
        options.Schema.For<InstitutionManagerLink>().DatabaseSchemaName("institution");
        options.Schema.For<InstitutionManagerLink>().Index(x => x.InstitutionId);
```

Dosyanın başına gerekiyorsa `using MESNET.Institution.Core.ReadModels;` ekle.

- [ ] **Step 6: Tüketiciyi yaz**

`src/Modules/Institution/MESNET.Institution.Application/Consumers/InstitutionManagerLinkConsumer.cs`:

```csharp
using Marten;
using MESNET.Institution.Core.ReadModels;
using MESNET.Security.Shared.Events;
using Wolverine.Configuration;
using Wolverine.Transports.Local;

namespace MESNET.Institution.Application.Consumers;

/// <summary>
/// Security kullanıcı olaylarından <see cref="InstitutionManagerLink"/> satırını besler (D2).
///
/// <para><b>Neden <c>static class</c> DEĞİL (#262):</b> kuyruk yapılandırması
/// <c>IConfigureLocalQueue</c> ile yapılıyor ve statik sınıf arayüz uygulayamaz. Metotlar
/// statik kalır; Wolverine statik handler metotlarını örnek oluşturmadan çağırır.</para>
/// </summary>
public sealed class InstitutionManagerLinkConsumer : IConfigureLocalQueue
{
    /// <summary>
    /// Bu tüketicinin yerel kuyruğu <b>sıralı</b> çalışır (#262).
    ///
    /// <para><c>MultipleHandlerBehavior.Separated</c> her handler tipine ayrı bir "sticky" yerel
    /// kuyruk verir, ama o kuyruk varsayılan olarak <b>paralel ve sırasızdır</b>. Bu sınıfın
    /// metotlarının hepsi aynı kuyruğa düşer, yani aynı kullanıcıya ait olaylar birbirini
    /// geçebilir.</para>
    ///
    /// <para><b>Kırılma:</b> <c>UserRolesChanged</c>, satırı <b>kuran</b> <c>UserCreated</c>'ı
    /// geçerse satır henüz yoktur; load-modify-store sessizce düşer ve kullanıcı yönetici
    /// sayılmaz — okul sonsuza kadar "yöneticisiz" görünür.
    /// <c>UseDurableLocalQueues()</c> dayanıklılık verir, <b>sıra vermez</b>.</para>
    /// </summary>
    public static void Configure(LocalQueueConfiguration configuration)
    {
        configuration.Sequential();
    }

    public static void Consume(UserCreated e, IDocumentSession session)
    {
        session.Store(new InstitutionManagerLink
        {
            Id = e.UserAccountId,
            InstitutionId = e.InstitutionId,
            IsEnabled = true,
            HasManagePermission = InstitutionManagerLink.HasManage(e.Roles),
            UpdatedAt = DateTime.UtcNow,
        });
    }

    public static async Task Consume(UserInstitutionChanged e, IDocumentSession session)
    {
        var link = await LoadOrCreate(e.UserAccountId, session);
        link.InstitutionId = e.InstitutionId;
        link.UpdatedAt = DateTime.UtcNow;
        session.Store(link);
    }

    public static async Task Consume(UserRolesChanged e, IDocumentSession session)
    {
        var link = await LoadOrCreate(e.UserAccountId, session);
        link.HasManagePermission = InstitutionManagerLink.HasManage(e.NewRoles);
        link.UpdatedAt = DateTime.UtcNow;
        session.Store(link);
    }

    public static async Task Consume(UserActivated e, IDocumentSession session)
    {
        var link = await LoadOrCreate(e.UserAccountId, session);
        link.IsEnabled = true;
        link.UpdatedAt = DateTime.UtcNow;
        session.Store(link);
    }

    public static async Task Consume(UserDeactivated e, IDocumentSession session)
    {
        var link = await LoadOrCreate(e.UserAccountId, session);
        link.IsEnabled = false;
        link.UpdatedAt = DateTime.UtcNow;
        session.Store(link);
    }

    public static void Consume(UserDeleted e, IDocumentSession session)
    {
        session.Delete<InstitutionManagerLink>(e.UserAccountId);
    }

    /// <summary>
    /// Satır yoksa boş bir tane kurar. <b>Sessizce vazgeçmez:</b> satırsız bir kullanıcı için
    /// güncellemeyi düşürmek, o kullanıcının bağlı olduğu okulu kalıcı olarak "yöneticisiz"
    /// gösterirdi. Eksik alanlar (kurum, roller) sonraki olayla ya da resync ile dolar.
    /// </summary>
    private static async Task<InstitutionManagerLink> LoadOrCreate(
        Guid userAccountId, IDocumentSession session) =>
        await session.LoadAsync<InstitutionManagerLink>(userAccountId)
        ?? new InstitutionManagerLink { Id = userAccountId };
}
```

- [ ] **Step 7: Derle ve tüm testleri koş**

Run: `dotnet build MESNET.slnx && dotnet test MESNET.slnx`
Expected: derleme temiz, tüm testler yeşil (`DocumentTenancyMap` sınıflandırma testi dahil).

- [ ] **Step 8: Commit**

```bash
git add src/Modules/Institution src/MESNET.Common.Shared/Tenancy/DocumentTenancyMap.cs \
        tests/MESNET.Institution.UnitTests/ManagerLinkPermissionTests.cs
git commit -m "feat(institution): kullanıcı kurum bağı yerel görünümü — Security olaylarıyla beslenir"
```

---

### Task 7: Yöneticisiz okul sorgusu ve yeniden yayın ucu

**Files:**
- Create: `src/Modules/Institution/MESNET.Institution.Application/Queries/GetUnmanagedInstitutions.cs`
- Create: `src/Modules/Institution/MESNET.Institution.Application/Handlers/GetUnmanagedInstitutionsHandler.cs`
- Create: `src/Modules/Security/MESNET.Security.Application/Commands/ReplayUserAccounts.cs`
- Create: `src/Modules/Security/MESNET.Security.Application/Handlers/ReplayUserAccountsHandler.cs`
- Modify: `src/Modules/Institution/MESNET.Institution.Api/InstitutionEndpoints.cs`
- Modify: `src/Modules/Security/MESNET.Security.Api/UserManagementEndpoints.cs`

**Interfaces:**
- Consumes: Task 6'nın `InstitutionManagerLink`'i; `InstitutionScopePolicy.VisibleScope`; mevcut `InstitutionDto` ve `ToDto(string? parentName)` uzantısı; `PagedQuery` (Page, PageSize, SortBy, Descending, Search); `PagedResult<T>.Create(items, totalCount, page, pageSize)`; `QueryableExtensions.ApplySort/ApplySearch/ToPagedResultAsync`; `IQueryableExtensions.OfNodeType(InstitutionNodeType)`.
- Produces: `GetUnmanagedInstitutions : PagedQuery` → `PagedResult<InstitutionDto>` (uç: `GET /api/institutions/unmanaged`); `ReplayUserAccounts` → `int` (uç: `POST /api/security/users/replay`). Task 9 (frontend) yalnız ilkini tüketir.

- [ ] **Step 1: Sorgu kaydını yaz**

`src/Modules/Institution/MESNET.Institution.Application/Queries/GetUnmanagedInstitutions.cs`:

```csharp
using MESNET.Common.Shared.Pagination;

namespace MESNET.Institution.Application.Queries;

/// <summary>
/// Yöneticisi olmayan okullar — bootstrap iş listesi (D2).
///
/// <para><b>Parametresizdir</b> (sayfalama dışında): kapsam istekten alınmaz, aktörün
/// claim'lerinden türer.</para>
/// </summary>
public sealed record GetUnmanagedInstitutions : PagedQuery;
```

- [ ] **Step 2: Handler'ı yaz**

`src/Modules/Institution/MESNET.Institution.Application/Handlers/GetUnmanagedInstitutionsHandler.cs`:

```csharp
using Marten;
using MESNET.Common.Infrastructure.Pagination;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared.Pagination;
using MESNET.Common.Shared.Security;
using MESNET.Institution.Application.Dtos;
using MESNET.Institution.Application.Extensions;
using MESNET.Institution.Application.Queries;
using MESNET.Institution.Core.Enums;
using MESNET.Institution.Core.ReadModels;
using InstitutionRecord = MESNET.Institution.Core.Entities.Institution;

namespace MESNET.Institution.Application.Handlers;

/// <summary>
/// Alt ağaçta <c>institution:manage</c> taşıyan etkin kullanıcısı olmayan okullar.
///
/// <para><b>Sorgu iki adımlı ve NEGATİF yöndedir.</b> Marten join yapmaz. Önce YÖNETİLEN kurum
/// kimlikleri toplanır, sonra kurum listesi o kümenin DIŞINDA kalanlara daraltılır.</para>
///
/// <para><b>Neden pozitif yön değil:</b> "yöneticisiz kurumların kimliklerini topla" demek her
/// kurum için bir read-model satırının var olmasını gerektirirdi; hiç kullanıcı olayı görmemiş
/// kurum o listede hiç doğmazdı — aranan kurum tam olarak o.</para>
///
/// <para><b>Neden sayfalama ikinci adımda:</b> önce kurumları sayfalayıp sonra bellekte süzmek
/// sayfa boyutlarını yanlışlardı — 20 satırlık sayfadan 3'ü kalırsa istemci "3 sonuç var"
/// sanır.</para>
/// </summary>
public static class GetUnmanagedInstitutionsHandler
{
    public static async Task<PagedResult<InstitutionDto>> Handle(
        GetUnmanagedInstitutions query,
        IQuerySession session,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        // 1. adım: yönetilen kurumlar.
        var managedIds = await session.Query<InstitutionManagerLink>()
            .Where(l => l.IsEnabled && l.HasManagePermission && l.InstitutionId != null)
            .Select(l => l.InstitutionId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var scope = InstitutionScopePolicy.VisibleScope(
            currentUser.GetCurrentUser()?.InstitutionId,
            currentUser.GetInstitutionPath(),
            currentUser.HasPermission(Permissions.Platform.TenantManage));

        IQueryable<InstitutionRecord> queryable = session.Query<InstitutionRecord>();

        queryable = ApplyScope(queryable, scope);

        // Yalnız OKUL: il/ilçe müdürlüğünün "yöneticisi" bu kartın konusu değildir.
        queryable = queryable.OfNodeType(InstitutionNodeType.School);

        // 2. adım: negatif süzgeç. Boş kümede Contains her satırı geçirir (doğru davranış:
        // hiçbir okul yönetilmiyorsa hepsi listelenir), ayrıca ele alınmasına gerek yok.
        if (managedIds.Count > 0)
            queryable = queryable.Where(i => !managedIds.Contains(i.Id));

        queryable = queryable.ApplySearch(query.Search, i => i.FullName);
        queryable = queryable.ApplySort(query.SortBy, query.Descending, defaultSort: i => i.FullName);

        var page = await queryable.ToPagedResultAsync(query, cancellationToken);

        return PagedResult<InstitutionDto>.Create(
            page.Items.Select(i => i.ToDto(null)).ToList(),
            page.TotalCount,
            page.Page,
            page.PageSize);
    }

    /// <summary>
    /// Kapsam daraltması — <c>GetInstitutionsHandler</c> ile AYNI karardan (<see
    /// cref="InstitutionVisibility"/>) beslenir; karar burada TEKRARLANMAZ.
    /// </summary>
    private static IQueryable<InstitutionRecord> ApplyScope(
        IQueryable<InstitutionRecord> queryable, InstitutionVisibility scope)
    {
        if (scope.Unrestricted)
            return queryable;

        if (scope.PathPrefix is { } prefix)
            return queryable.Where(i => i.Path != null && i.Path.StartsWith(prefix));

        var institutionId = scope.InstitutionId ?? Guid.Empty;
        return queryable.Where(i => i.Id == institutionId);
    }
}
```

**Not:** `ToDto` uzantısının imzası `ToDto(string? parentName)` değilse, `GetInstitutionsHandler`'daki çağrıyı okuyup aynı imzayı kullan.

- [ ] **Step 3: Backfill yolunu yaz — Security tarafında**

Burada **doğrudan modüller arası okuma yapılmaz**: Institution modülü `UserAccount` belgesini okuyamaz. Bunun yerine mevcut desen kullanılır — Security modülü kendi kayıtlarını **yeniden yayınlar**, Institution tüketir. Depoda birebir emsali var: `POST /api/institutions/staff/resync-branch-codes` olayı yeniden yayınlar, Security tüketir.

Bu yüzden backfill ucu **Security modülünde** açılır ve `UserCreated` olaylarını yeniden yayınlar; Task 6'nın tüketicisi onları zaten işler.

`src/Modules/Security/MESNET.Security.Application/Commands/ReplayUserAccounts.cs`:

```csharp
namespace MESNET.Security.Application.Commands;

/// <summary>
/// Bütün kullanıcı hesaplarını <c>UserCreated</c> olarak yeniden yayınlar (D2) — diğer
/// modüllerin yerel görünümlerini geriye dönük doldurmak için. İdempotenttir: tüketiciler
/// satırı mutlak olarak yazar.
///
/// <para><b>Neden yeniden yayın, doğrudan yazma değil:</b> bir modülün başka modülün
/// belgesine yazması yasaktır. Emsal: <c>POST /api/institutions/staff/resync-branch-codes</c>.</para>
/// </summary>
public sealed record ReplayUserAccounts;
```

`src/Modules/Security/MESNET.Security.Application/Handlers/ReplayUserAccountsHandler.cs`:

```csharp
using Marten;
using MESNET.Security.Application.Commands;
using MESNET.Security.Core.Entities;
using MESNET.Security.Shared.Events;
using Wolverine;

namespace MESNET.Security.Application.Handlers;

/// <summary>
/// <inheritdoc cref="ReplayUserAccounts"/>
/// </summary>
public static class ReplayUserAccountsHandler
{
    public static async Task<int> Handle(
        ReplayUserAccounts command,
        IQuerySession session,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var accounts = await session.Query<UserAccount>().ToListAsync(cancellationToken);

        foreach (var account in accounts)
        {
            await bus.PublishAsync(new UserCreated(
                account.Id,
                account.KeycloakUserId,
                account.Username,
                account.FullName,
                account.Email,
                account.Roles,
                account.InstitutionId,
                account.BusinessId,
                new Dictionary<string, string>()));
        }

        return accounts.Count;
    }
}
```

**Not:** `UserAccount` alan adlarını yazmadan önce ölç:

```bash
grep -n "public " src/Modules/Security/MESNET.Security.Core/Entities/UserAccount.cs
```

Alan adları farklıysa (`Username`, `FullName`, `Email`, `Roles`, `InstitutionId`, `BusinessId`) ölçülen adları kullan. `UserCreated` kaydının parametre sırası bu plandaki imzayla aynıdır.

- [ ] **Step 4: Uçları ekle**

`src/Modules/Institution/MESNET.Institution.Api/InstitutionEndpoints.cs` — `rebuild-hierarchy` satırının üstüne:

```csharp
        // Yöneticisi olmayan okullar (D2) — müdürlük panosu bootstrap iş listesi.
        group.MapGet("/unmanaged", GetUnmanaged).RequireAuthorization(Permissions.Institution.View);
```

Ve metotlar arasına:

```csharp
    private static async Task<IResult> GetUnmanaged(
        int page = 1,
        int pageSize = 20,
        string? sortBy = null,
        bool descending = false,
        string? search = null,
        IMessageBus bus = default!)
    {
        var result = await bus.InvokeAsync<PagedResult<InstitutionDto>>(
            new GetUnmanagedInstitutions
            {
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                Descending = descending,
                Search = search
            });

        return Results.Ok(ResponseBuilder.Success().AddData(result).Build());
    }
```

`src/Modules/Security/MESNET.Security.Api/UserManagementEndpoints.cs` — mevcut `sync` ucunun yanına:

```csharp
        // Kullanıcı kayıtlarını olay olarak yeniden yayınlar (D2) — diğer modüllerin yerel
        // görünümlerini doldurur. DAĞITIM ÖN KOŞULU, idempotent.
        group.MapPost("/replay", PostReplayUserAccounts)
            .RequireAuthorization(Permissions.Platform.TenantManage);
```

Ve metotlar arasına:

```csharp
    private static async Task<IResult> PostReplayUserAccounts(IMessageBus bus)
    {
        var count = await bus.InvokeAsync<int>(new ReplayUserAccounts());
        return Results.Ok(ResponseBuilder.Success().AddData(new { replayed = count }).Build());
    }
```

**Not:** Grup yolunu ve mevcut uçların imzasını yazmadan önce dosyayı oku; `group` değişkeninin yolu `/api/security/users` ise uç `POST /api/security/users/replay` olur.

- [ ] **Step 5: Derle ve tüm testleri koş**

Run: `dotnet build MESNET.slnx && dotnet test MESNET.slnx`
Expected: derleme temiz, tüm testler yeşil.

- [ ] **Step 6: Commit**

```bash
git add src/Modules/Institution src/Modules/Security
git commit -m "feat(institution): yöneticisiz okul listesi + kullanıcı kayıtlarını yeniden yayınlama ucu"
```

---

### Task 8: "Müdürlük olarak mı davranıyorum" kararı

Kod tabanında buna çok benzeyen ama **aktif bağlam açıkken ayrışan** bir fonksiyon zaten var. Bu görevin asıl işi o ikisini birbirinden ayırmak.

**Files:**
- Create: `src/WebUI/src/utils/directorateContext.ts`
- Create: `src/WebUI/src/utils/directorateContext.spec.ts`

**Interfaces:**
- Consumes: hiçbir şey — saf fonksiyon.
- Produces: `isActingAsDirectorate(nodeType: string | null | undefined) → boolean`. Task 10 bunu tüketir.

- [ ] **Step 1: Başarısız testi yaz**

`src/WebUI/src/utils/directorateContext.spec.ts`:

```typescript
import { describe, it, expect } from 'vitest'
import { isActingAsDirectorate } from './directorateContext'
import { resolveIsUpperNode } from 'src/composables/useNavigation'

/**
 * Kod tabanında birbirine çok benzeyen ama AKTİF BAĞLAM açıkken ayrışan iki soru var:
 *
 * - "Aktör üst düğüm mü?"  → resolveIsUpperNode(nodeType, activeInstitutionId)
 *   Aktif bağlam doluyken TRUE: `Kurumlar` ağacı okula geçince de görünmeli.
 *
 * - "Şu an müdürlük olarak mı davranıyorum?" → isActingAsDirectorate(nodeType)
 *   Aktif bağlam doluyken FALSE: kiracı o okuldur, okul panosu doğrudur.
 *
 * Buradaki bariz hata `resolveIsUpperNode`'u kopyalamak olurdu: il yetkilisi bir okula
 * geçtiğinde müdürlük panosunu görür, o pano da okul kiracısında alt ağaç sorar ve okul kendi
 * altında hiçbir şey bulamaz — HATA DEĞİL, BOŞ PANO.
 */
describe('isActingAsDirectorate', () => {
  it('il müdürlüğü bağlamında true döner', () => {
    expect(isActingAsDirectorate('Province')).toBe(true)
  })

  it('ilçe müdürlüğü bağlamında true döner', () => {
    expect(isActingAsDirectorate('District')).toBe(true)
  })

  it('okul bağlamında false döner', () => {
    expect(isActingAsDirectorate('School')).toBe(false)
  })

  it('düğüm tipi bilinmiyorsa false döner — okul panosu güvenli varsayılandır', () => {
    expect(isActingAsDirectorate(null)).toBe(false)
    expect(isActingAsDirectorate(undefined)).toBe(false)
    expect(isActingAsDirectorate('')).toBe(false)
  })

  it('resolveIsUpperNode ile AYNI ŞEY DEĞİLDİR — aktif bağlam açıkken ayrışırlar', () => {
    // İl yetkilisi bir okula geçti: institutionStore aktif bağlama bağlı olduğu için
    // nodeType artık 'School', activeInstitutionId ise dolu.
    const nodeType = 'School'
    const activeInstitutionId = 'ataturk-id'

    // Aktör hâlâ üst düğümdür (Kurumlar ağacı görünmeli)...
    expect(resolveIsUpperNode(nodeType, activeInstitutionId)).toBe(true)
    // ...ama müdürlük olarak DAVRANMIYOR (okul panosu görmeli).
    expect(isActingAsDirectorate(nodeType)).toBe(false)
  })
})
```

- [ ] **Step 2: Testin başarısız olduğunu doğrula**

Run: `cd src/WebUI && pnpm test:run src/utils/directorateContext.spec.ts`
Expected: FAIL — `Failed to resolve import "./directorateContext"`.

- [ ] **Step 3: Fonksiyonu yaz**

`src/WebUI/src/utils/directorateContext.ts`:

```typescript
/**
 * Kullanıcı ŞU AN müdürlük (il/ilçe millî eğitim) olarak mı davranıyor?
 *
 * <p><b>`resolveIsUpperNode` ile karıştırmayın.</b> O fonksiyon "aktör üst düğüm mü" sorusuna
 * cevap verir ve `activeInstitutionId`'yi OR'lar — çünkü `Kurumlar` ağacı, il yetkilisi bir
 * okula geçtiğinde de görünmelidir (geri dönebilmeli). Bu fonksiyonun sorusu farklıdır: il
 * yetkilisi bir okula geçtiğinde <b>kiracısı o okuldur</b> ve okul panosunu görmelidir.</p>
 *
 * <p>`institutionStore.institution?.nodeType` aktif bağlama bağlıdır, dolayısıyla tek girdi
 * olarak doğru cevabı verir.</p>
 *
 * @param nodeType `InstitutionDto.nodeType` — `'Province'`, `'District'` ya da `'School'`.
 *   Kurum henüz yüklenmemişse `null`/`undefined` olabilir; o hâlde okul panosu gösterilir
 *   (güvenli varsayılan: müdürlük panosu okul kiracısında boş çıkardı, tersi çıkmaz).
 */
export function isActingAsDirectorate(nodeType: string | null | undefined): boolean {
  return nodeType === 'Province' || nodeType === 'District'
}
```

- [ ] **Step 4: Testin geçtiğini doğrula**

Run: `cd src/WebUI && pnpm test:run src/utils/directorateContext.spec.ts`
Expected: 5 test PASS.

- [ ] **Step 5: Kilidin GERÇEKTEN kilitlediğini kanıtla**

`isActingAsDirectorate` gövdesini geçici olarak `resolveIsUpperNode`'un kopyasına çevir — yani ikinci bir parametre alıp OR'lasın. Bunu yapmak için imzayı değiştirmek gerekmez; sadece gövdeyi şuna çevir:

```typescript
  return nodeType === 'Province' || nodeType === 'District' || nodeType === 'School'
```

Run: `cd src/WebUI && pnpm test:run src/utils/directorateContext.spec.ts`
Expected: `okul bağlamında false döner` ve son test **FAIL**.

Gövdeyi **geri al** ve tekrar koş.
Expected: 5 test PASS.

- [ ] **Step 6: Tip denetimi ve commit**

```bash
cd src/WebUI && pnpm exec vue-tsc --noEmit
cd ../.. && git add src/WebUI/src/utils/directorateContext.ts src/WebUI/src/utils/directorateContext.spec.ts
git commit -m "feat(webui): müdürlük bağlamı kararı — aktif bağlam OR'lanmaz"
```

---

### Task 9: Pano verisi composable'ı

**Files:**
- Modify: `src/WebUI/src/api/institution.ts`
- Modify: `src/WebUI/src/api/internship.ts`
- Create: `src/WebUI/src/composables/useDirectorateDashboard.ts`
- Create: `src/WebUI/src/composables/useDirectorateDashboard.spec.ts`

**Interfaces:**
- Consumes: `boot/axios` (`api`), `PagedResponse<T>` / `PaginationParams` (`src/types/pagination`), `useNotify()` (`{ success, error, apiError, warning, info }`), Task 5'in `StuckApprovalSummaryDto`, Task 7'nin `GET /api/institutions/unmanaged`.
- Produces: `useDirectorateDashboard(options)` → `{ districtCount, schoolCount, unmanagedCount, unmanagedNames, stuckCount, stuckThresholdDays, stuckByInstitution, loading, load }`. Task 10 bunları tüketir.

- [ ] **Step 1: API istemcilerini ekle**

`src/WebUI/src/api/institution.ts` — `institutionApi` nesnesine, `list` girdisinin hemen altına:

```typescript
  /** Yöneticisi olmayan okullar (D2) — kapsam sunucuda, istekten geçmez. */
  listUnmanaged: (params?: PaginationParams) =>
    api.get<PagedResponse<InstitutionDto>>('/institutions/unmanaged', { params }),
```

`src/WebUI/src/api/internship.ts` — dosyanın tip bildirimleri arasına:

```typescript
/** Tıkanmış onayların kurum kırılımı (D2). */
export interface StuckApprovalByInstitutionDto {
  institutionId: string
  /** Sunucu HER ZAMAN null döndürür — ad ön yüzde lookup ile doldurulur (şema izolasyonu). */
  institutionName: string | null
  count: number
  /** null = o kurumdaki tıkanmış zincirlerin hiçbirinde talep zamanı bilinmiyor. */
  oldestDays: number | null
}

export interface StuckApprovalSummaryDto {
  totalCount: number
  /** Karar anındaki eşik — boş-durum metni bununla yazılır. */
  thresholdDays: number
  byInstitution: StuckApprovalByInstitutionDto[]
}

export interface ApprovalConfigDto {
  stuckApprovalDays: number
}
```

ve `internshipApi` nesnesine:

```typescript
  getStuckApprovals: () =>
    api.get<StuckApprovalSummaryDto>('/internships/stuck-approvals'),

  getApprovalConfig: () =>
    api.get<ApprovalConfigDto>('/internships/approval-config'),

  updateApprovalConfig: (payload: ApprovalConfigDto) =>
    api.put('/internships/approval-config', payload),
```

**Not:** Axios interceptor'ı `body.data`'yı açar, bu yüzden `api.get<T>` doğrudan `T` verir; `PagedResponse<T>` de aynı yoldan gelir.

- [ ] **Step 2: Başarısız testi yaz**

`src/WebUI/src/composables/useDirectorateDashboard.spec.ts`:

```typescript
import { describe, it, expect, vi } from 'vitest'
import { useDirectorateDashboard } from './useDirectorateDashboard'

function makeNotify() {
  return {
    success: vi.fn(),
    error: vi.fn(),
    apiError: vi.fn(),
    warning: vi.fn(),
    info: vi.fn(),
  }
}

const okDistricts = () => Promise.resolve(12)
const okSchools = () => Promise.resolve(148)
const okUnmanaged = () =>
  Promise.resolve({ total: 3, names: ['Atatürk MTAL', 'Gazi MTAL', 'Cumhuriyet MTAL'] })
const okStuck = () =>
  Promise.resolve({
    totalCount: 7,
    thresholdDays: 14,
    byInstitution: [
      { institutionId: 'a', institutionName: null, count: 5, oldestDays: 40 },
      { institutionId: 'b', institutionName: null, count: 2, oldestDays: null },
    ],
  })

describe('useDirectorateDashboard', () => {
  it('üç kart da dolduğunda değerleri yayar', async () => {
    // Arrange
    const dash = useDirectorateDashboard({
      fetchDistrictCount: okDistricts,
      fetchSchoolCount: okSchools,
      fetchUnmanaged: okUnmanaged,
      fetchStuck: okStuck,
      notify: makeNotify(),
    })

    // Act
    await dash.load()

    // Assert
    expect(dash.districtCount.value).toBe(12)
    expect(dash.schoolCount.value).toBe(148)
    expect(dash.unmanagedCount.value).toBe(3)
    expect(dash.unmanagedNames.value).toEqual([
      'Atatürk MTAL',
      'Gazi MTAL',
      'Cumhuriyet MTAL',
    ])
    expect(dash.stuckCount.value).toBe(7)
    expect(dash.stuckThresholdDays.value).toBe(14)
    expect(dash.loading.value).toBe(false)
  })

  /**
   * Bir kartın verisi gelmezse pano TÜMDEN boşalmamalı. Aksi hâlde tek bir uç arızası üç
   * kartı birden söndürür ve kullanıcı hiçbir şey göremez.
   */
  it('bir çağrı patlarsa diğer kartlar yine dolar', async () => {
    const notify = makeNotify()
    const dash = useDirectorateDashboard({
      fetchDistrictCount: okDistricts,
      fetchSchoolCount: okSchools,
      fetchUnmanaged: okUnmanaged,
      fetchStuck: () => Promise.reject(new Error('403')),
      notify,
    })

    await dash.load()

    expect(dash.districtCount.value).toBe(12)
    expect(dash.unmanagedCount.value).toBe(3)
    expect(dash.stuckCount.value).toBe(0)
    expect(notify.apiError).toHaveBeenCalled()
  })

  it('yükleme bittiğinde loading kapanır — hata olsa bile', async () => {
    const dash = useDirectorateDashboard({
      fetchDistrictCount: () => Promise.reject(new Error('500')),
      fetchSchoolCount: () => Promise.reject(new Error('500')),
      fetchUnmanaged: () => Promise.reject(new Error('500')),
      fetchStuck: () => Promise.reject(new Error('500')),
      notify: makeNotify(),
    })

    await dash.load()

    expect(dash.loading.value).toBe(false)
  })
})
```

- [ ] **Step 3: Testin başarısız olduğunu doğrula**

Run: `cd src/WebUI && pnpm test:run src/composables/useDirectorateDashboard.spec.ts`
Expected: FAIL — `Failed to resolve import "./useDirectorateDashboard"`.

- [ ] **Step 4: Composable'ı yaz**

`src/WebUI/src/composables/useDirectorateDashboard.ts`:

```typescript
import { ref } from 'vue'
import type { useNotify } from 'src/composables/useNotify'
import type { StuckApprovalByInstitutionDto } from 'src/api/internship'

/** Yöneticisiz okul özeti: toplam sayı + gösterilecek ilk adlar. */
export interface UnmanagedSummary {
  total: number
  names: string[]
}

export interface StuckSummary {
  totalCount: number
  thresholdDays: number
  byInstitution: StuckApprovalByInstitutionDto[]
}

/**
 * Veri kaynakları DIŞARIDAN verilir (CLAUDE.md: composable store/service'e doğrudan erişmek
 * yerine parametre alır). Böylece test axios'u taklit etmeden koşar.
 */
export interface UseDirectorateDashboardOptions {
  fetchDistrictCount: () => Promise<number>
  fetchSchoolCount: () => Promise<number>
  fetchUnmanaged: () => Promise<UnmanagedSummary>
  fetchStuck: () => Promise<StuckSummary>
  notify: ReturnType<typeof useNotify>
}

/** Eşik belgesi hiç yazılmamışsa sunucunun kullandığı varsayılan (backend ile aynı sayı). */
const DEFAULT_THRESHOLD_DAYS = 14

export function useDirectorateDashboard(options: UseDirectorateDashboardOptions) {
  const { fetchDistrictCount, fetchSchoolCount, fetchUnmanaged, fetchStuck, notify } = options

  const districtCount = ref(0)
  const schoolCount = ref(0)
  const unmanagedCount = ref(0)
  const unmanagedNames = ref<string[]>([])
  const stuckCount = ref(0)
  const stuckThresholdDays = ref(DEFAULT_THRESHOLD_DAYS)
  const stuckByInstitution = ref<StuckApprovalByInstitutionDto[]>([])
  const loading = ref(false)

  /**
   * Dört çağrı BİRBİRİNDEN BAĞIMSIZ yürür ve her biri kendi hatasını yutar. Tek bir
   * `Promise.all` kullanılsaydı ilk reddedilen çağrı diğer üçünün sonucunu da düşürürdü ve
   * bir ucun 403'ü panoyu tümden söndürürdü.
   */
  async function load() {
    loading.value = true

    await Promise.all([
      run(async () => {
        districtCount.value = await fetchDistrictCount()
      }, 'İlçe sayısı alınamadı.'),
      run(async () => {
        schoolCount.value = await fetchSchoolCount()
      }, 'Okul sayısı alınamadı.'),
      run(async () => {
        const summary = await fetchUnmanaged()
        unmanagedCount.value = summary.total
        unmanagedNames.value = summary.names
      }, 'Yöneticisi olmayan okullar alınamadı.'),
      run(async () => {
        const summary = await fetchStuck()
        stuckCount.value = summary.totalCount
        stuckThresholdDays.value = summary.thresholdDays
        stuckByInstitution.value = summary.byInstitution
      }, 'Tıkanmış onaylar alınamadı.'),
    ])

    loading.value = false
  }

  async function run(action: () => Promise<void>, message: string) {
    try {
      await action()
    } catch (e) {
      notify.apiError(e, message)
    }
  }

  return {
    districtCount,
    schoolCount,
    unmanagedCount,
    unmanagedNames,
    stuckCount,
    stuckThresholdDays,
    stuckByInstitution,
    loading,
    load,
  }
}
```

- [ ] **Step 5: Testin geçtiğini doğrula**

Run: `cd src/WebUI && pnpm test:run src/composables/useDirectorateDashboard.spec.ts`
Expected: 3 test PASS.

- [ ] **Step 6: Tip denetimi ve commit**

```bash
cd src/WebUI && pnpm exec vue-tsc --noEmit
cd ../.. && git add src/WebUI/src/api src/WebUI/src/composables/useDirectorateDashboard.ts \
        src/WebUI/src/composables/useDirectorateDashboard.spec.ts
git commit -m "feat(webui): müdürlük panosu verisi — kartlar birbirinin hatasından etkilenmez"
```

---

### Task 10: Pano bileşeni ve Ana Sayfa dallanması

**Files:**
- Create: `src/WebUI/src/pages/dashboard/DirectorateDashboard.vue`
- Modify: `src/WebUI/src/pages/DashboardPage.vue`

**Interfaces:**
- Consumes: Task 8'in `isActingAsDirectorate`; Task 9'un `useDirectorateDashboard` ve API istemcileri; `useInstitutionStore()` (`institution` → `InstitutionDto | null`, alan `nodeType: string`); `useAuthStore().hasPermission(...)`; `Permissions` (`src/utils/permissions`); `StatCard` bileşeni (`icon`, `value`, `label`, `color`).
- Produces: kullanıcıya görünen pano. Sonraki görev bağımlı değildir.

- [ ] **Step 1: Eksik izin sabitini ekle**

`Permissions.Internship.ApprovalOverride` ön yüzde **yok** — ölçüldü, `src/WebUI/src/utils/permissions.ts` içindeki `Internship` bloğu `Apply, View, Review, Approve, ViewOwn, Manage, Contract, Report` taşıyor, `ApprovalOverride` yok. Bileşen onsuz derlenmez.

`src/WebUI/src/utils/permissions.ts` — `Internship` bloğunda `Manage` satırının altına:

```typescript
    /** Onay zinciri müdahalesi — müdürlük yetkisi, okul rollerinde de bulunur. */
    ApprovalOverride: 'internship:approval:override',
```

D1 bu satırı zaten eklemiş olabilir (`TerminationsPage` düzeltmesi aynı sabite ihtiyaç duyar). Varsa **tekrar ekleme**, doğrulayıp geç.

- [ ] **Step 2: Bileşeni yaz**

`src/WebUI/src/pages/dashboard/DirectorateDashboard.vue`:

```vue
<template>
  <div>
    <div class="row q-col-gutter-md q-mb-lg">
      <div class="col-12 col-sm-6 col-md-4">
        <StatCard
          icon="account_tree"
          :value="schoolCount"
          label="Okul"
          color="primary"
          :loading="loading"
        />
      </div>
      <div
        v-if="districtCount > 0"
        class="col-12 col-sm-6 col-md-4"
      >
        <StatCard
          icon="location_city"
          :value="districtCount"
          label="İlçe"
          color="secondary"
          :loading="loading"
        />
      </div>
      <div class="col-12 col-sm-6 col-md-4">
        <StatCard
          icon="person_off"
          :value="unmanagedCount"
          label="Yöneticisi Olmayan Okul"
          color="warning"
          :loading="loading"
        />
      </div>
    </div>

    <div class="row q-col-gutter-md">
      <!-- Yöneticisi olmayan okullar -->
      <div class="col-12 col-md-6">
        <q-card
          flat
          bordered
        >
          <q-card-section class="row items-center q-pb-none">
            <div class="text-subtitle1 text-weight-medium col">
              Yöneticisi Olmayan Okullar
            </div>
            <q-btn
              flat
              dense
              no-caps
              color="primary"
              label="Kullanıcı bağla"
              :to="{ name: 'UserManagement' }"
            />
          </q-card-section>

          <q-card-section v-if="unmanagedCount === 0">
            <div class="row items-center text-grey-7">
              <q-icon
                name="verified"
                size="sm"
                class="q-mr-sm"
              />
              <span>Tüm okulların yöneticisi var.</span>
            </div>
          </q-card-section>

          <q-list
            v-else
            separator
          >
            <q-item
              v-for="name in unmanagedNames"
              :key="name"
            >
              <q-item-section>{{ name }}</q-item-section>
            </q-item>
            <q-item v-if="unmanagedCount > unmanagedNames.length">
              <q-item-section class="text-grey-7">
                ve {{ unmanagedCount - unmanagedNames.length }} okul daha
              </q-item-section>
            </q-item>
          </q-list>
        </q-card>
      </div>

      <!-- Tıkanmış onaylar — yalnız müdahale edebilene gösterilir -->
      <div
        v-if="canOverride"
        class="col-12 col-md-6"
      >
        <q-card
          flat
          bordered
        >
          <q-card-section class="row items-center q-pb-none">
            <div class="text-subtitle1 text-weight-medium col">
              Tıkanmış Fesih Onayları
            </div>
            <q-btn
              flat
              dense
              no-caps
              color="primary"
              label="Fesihlere git"
              :to="{ name: 'InstitutionTerminations' }"
            />
          </q-card-section>

          <q-card-section v-if="stuckCount === 0">
            <div class="row items-center text-grey-7">
              <q-icon
                name="task_alt"
                size="sm"
                class="q-mr-sm"
              />
              <span>{{ stuckThresholdDays }} günden uzun bekleyen onay yok.</span>
            </div>
          </q-card-section>

          <q-list
            v-else
            separator
          >
            <q-item
              v-for="row in stuckByInstitution"
              :key="row.institutionId"
            >
              <q-item-section>
                {{ institutionName(row.institutionId) }}
              </q-item-section>
              <q-item-section side>
                <div class="text-right">
                  <div class="text-weight-medium">
                    {{ row.count }}
                  </div>
                  <div class="text-caption text-grey-7">
                    {{ row.oldestDays === null ? 'süre bilinmiyor' : `en eski ${row.oldestDays} gün` }}
                  </div>
                </div>
              </q-item-section>
            </q-item>
          </q-list>
        </q-card>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAuthStore } from 'stores/auth'
import { useNotify } from 'src/composables/useNotify'
import { Permissions } from 'utils/permissions'
import { institutionApi, type InstitutionDto } from 'src/api/institution'
import { internshipApi } from 'src/api/internship'
import { useDirectorateDashboard } from 'src/composables/useDirectorateDashboard'
import StatCard from 'components/StatCard.vue'

/** Kartta gösterilecek okul adı sayısı — geri kalanı "ve N okul daha" olarak özetlenir. */
const NAME_PREVIEW_COUNT = 5

const authStore = useAuthStore()
const notify = useNotify()

const canOverride = authStore.hasPermission(Permissions.Internship.ApprovalOverride)

/**
 * Kurum adları Internship modülünde YOKTUR (şema izolasyonu) — sunucu institutionName alanını
 * her zaman null döndürür. Ad burada, alt ağaç listesinden kurulan lookup map ile çözülür;
 * depo deseni ContractListPage zenginleştirmesiyle aynıdır.
 */
const institutionNames = ref<Map<string, string>>(new Map())

function institutionName(id: string): string {
  return institutionNames.value.get(id) ?? 'Bilinmeyen kurum'
}

const {
  districtCount,
  schoolCount,
  unmanagedCount,
  unmanagedNames,
  stuckCount,
  stuckThresholdDays,
  stuckByInstitution,
  loading,
  load,
} = useDirectorateDashboard({
  fetchDistrictCount: async () => {
    const { data } = await institutionApi.list({ nodeType: 'District', page: 1, pageSize: 1 })
    return data.totalCount
  },
  fetchSchoolCount: async () => {
    // pageSize okul adı lookup'ını da besler: sayı ve adlar tek çağrıdan gelir.
    const { data } = await institutionApi.list({ nodeType: 'School', page: 1, pageSize: 200 })
    institutionNames.value = new Map(
      data.items.map((i: InstitutionDto) => [i.id, i.fullName]),
    )
    return data.totalCount
  },
  fetchUnmanaged: async () => {
    const { data } = await institutionApi.listUnmanaged({
      page: 1,
      pageSize: NAME_PREVIEW_COUNT,
    })
    return {
      total: data.totalCount,
      names: data.items.map((i: InstitutionDto) => i.fullName),
    }
  },
  fetchStuck: async () => {
    // Müdahale yetkisi yoksa uç 403 döner; boş özetle geç, kart zaten gizli.
    if (!canOverride) return { totalCount: 0, thresholdDays: 14, byInstitution: [] }
    const { data } = await internshipApi.getStuckApprovals()
    return data
  },
  notify,
})

onMounted(() => {
  load().catch(() => {})
})
</script>
```

**Not:** `institutionApi.list` parametre tipi (`InstitutionListParams`) `nodeType` alanını taşımıyorsa dosyayı okuyup mevcut alan adını kullan.

**Not:** `InstitutionTerminations` rotası D1'de müdürlük rollerine açılır. D1 uygulanmadan bu buton 403 duvarına gider — plan sırası bu yüzden bağlayıcıdır.

- [ ] **Step 3: Ana Sayfa'yı dallandır**

`src/WebUI/src/pages/DashboardPage.vue` — `<template>` içindeki `<q-page padding>` açılışının hemen altına, `PageHeader`'dan **sonra** gelecek şekilde mevcut tüm içeriği `<template v-if>` altına almak yerine **tek bir erken dal** kurulur:

```vue
<template>
  <q-page padding>
    <!-- Hoş Geldin -->
    <PageHeader
      :title="greeting"
      :subtitle="headerSubtitle"
    />

    <!--
      Müdürlük bağlamında okul panosu YANLIŞ veri değil, BOŞ veri gösterir: il/ilçe düğümü
      kiracı değildir ve kiracı damgalı belgelere attığı her sorgu boş döner. Bu yüzden
      dallanma bir tercih değil, doğruluk gereğidir.
    -->
    <DirectorateDashboard v-if="isDirectorate" />

    <template v-else>
      <!--
        MEVCUT İÇERİK BURAYA TAŞINIR — hiçbiri silinmez, değiştirilmez:
        `PageHeader`'dan SONRA gelen her şey, yani "Özet Kartları" yorumuyla başlayan
        `<div class="row q-col-gutter-md q-mb-lg">` bloğundan `</q-page>` kapanışına kadarki
        tüm kardeş elemanlar. Girinti bir seviye artar; başka değişiklik yoktur.
      -->
    </template>
  </q-page>
</template>
```

`<script setup>` bölümüne ekle:

```typescript
import { useInstitutionStore } from 'stores/institution'
import { isActingAsDirectorate } from 'utils/directorateContext'
import DirectorateDashboard from 'pages/dashboard/DirectorateDashboard.vue'

const institutionStore = useInstitutionStore()

/**
 * `resolveIsUpperNode` DEĞİL: il yetkilisi bir okula geçtiğinde kiracısı o okuldur ve okul
 * panosunu görmelidir. Fark `directorateContext.spec.ts` içinde kilitlidir.
 */
const isDirectorate = computed(() => isActingAsDirectorate(institutionStore.institution?.nodeType))
```

Mevcut `onMounted` içindeki okul verisi çağrıları müdürlük bağlamında gereksizdir; başına ekle:

```typescript
  if (isDirectorate.value) return
```

- [ ] **Step 4: Tip denetimi ve testler**

Run: `cd src/WebUI && pnpm exec vue-tsc --noEmit && pnpm test:run`
Expected: tip hatası yok, tüm testler yeşil.

- [ ] **Step 5: Tarayıcıda doğrula**

Dev yığınını çalıştır ve müdürlük yetkisi olan bir hesapla giriş yap. Sırayla:

1. `Ana Sayfa` müdürlük panosunu gösteriyor (okul grafikleri yok).
2. Üst bardan bir okula geç (aktif bağlam). `Ana Sayfa` **okul panosuna** dönüyor.
3. Kendi bağlamına dön. Müdürlük panosu geri geliyor.

2. adım bu görevin asıl kanıtıdır: `resolveIsUpperNode` kopyalanmış olsaydı orada müdürlük panosu kalır ve boş çıkardı.

- [ ] **Step 6: Commit**

```bash
git add src/WebUI/src/utils/permissions.ts src/WebUI/src/pages/dashboard/DirectorateDashboard.vue src/WebUI/src/pages/DashboardPage.vue
git commit -m "feat(webui): müdürlük panosu — Ana Sayfa bağlama göre dallanıyor"
```

---

### Task 11: Ulusal parametre ekranı

Ulusal parametre katmanının bugün **hiç ön yüzü yok** (`AttendanceLimitConfig` uçları var, sayfası yok). Eşik bu katmanın ilk ekranı olur.

**YAGNI:** sayfa yalnız eşiği taşır. Devamsızlık sınırları buraya taşınmaz — ayrı iş, ayrı doğrulama, ayrı mevzuat gerekçesi.

**Files:**
- Create: `src/WebUI/src/pages/admin/PlatformParametersPage.vue`
- Modify: `src/WebUI/src/router/index.ts`
- Modify: `src/WebUI/src/composables/useNavigation.ts`

**Interfaces:**
- Consumes: Task 9'un `internshipApi.getApprovalConfig()` / `updateApprovalConfig({ stuckApprovalDays })`; `useNotify()`.
- Produces: `/admin/parameters` rotası, adı `PlatformParameters`.

- [ ] **Step 1: Sayfayı yaz**

`src/WebUI/src/pages/admin/PlatformParametersPage.vue`:

```vue
<template>
  <q-page padding>
    <PageHeader
      title="Ulusal Parametreler"
      subtitle="Bu değerler tüm kurumlar için geçerlidir."
    />

    <q-card
      flat
      bordered
      style="max-width: 640px"
      class="relative-position"
    >
      <q-inner-loading :showing="loading" />
      <q-card-section class="q-gutter-md">
        <q-input
          v-model.number="stuckApprovalDays"
          label="Tıkanmış onay eşiği (gün)"
          type="number"
          outlined
          hint="Bir fesih onay zinciri kaç günden sonra müdürlük panosunda tıkanmış sayılsın."
          :rules="[thresholdRule]"
          lazy-rules
        >
          <template #prepend>
            <q-icon name="hourglass_bottom" />
          </template>
        </q-input>
      </q-card-section>

      <q-separator />
      <q-card-actions
        align="right"
        class="q-pa-md"
      >
        <q-btn
          unelevated
          color="primary"
          label="Kaydet"
          :loading="saving"
          @click="save"
        />
      </q-card-actions>
    </q-card>
  </q-page>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { internshipApi } from 'src/api/internship'
import { useNotify } from 'src/composables/useNotify'
import PageHeader from 'components/PageHeader.vue'

/** Backend ile AYNI aralık — sunucu da 1..365 dışını reddeder (422). */
const MIN_DAYS = 1
const MAX_DAYS = 365

const notify = useNotify()

const stuckApprovalDays = ref<number>(14)
const loading = ref(false)
const saving = ref(false)

function thresholdRule(value: number): true | string {
  if (value >= MIN_DAYS && value <= MAX_DAYS) return true
  return `Eşik ${MIN_DAYS} ile ${MAX_DAYS} gün arasında olmalıdır.`
}

async function loadConfig() {
  loading.value = true
  try {
    const { data } = await internshipApi.getApprovalConfig()
    stuckApprovalDays.value = data.stuckApprovalDays
  } catch (e) {
    notify.apiError(e, 'Parametreler yüklenemedi.')
  } finally {
    loading.value = false
  }
}

async function save() {
  // Sayfada q-form yok; :rules kaydetmeyi kendiliğinden engellemez.
  if (thresholdRule(stuckApprovalDays.value) !== true) {
    notify.error(`Eşik ${MIN_DAYS} ile ${MAX_DAYS} gün arasında olmalıdır.`)
    return
  }

  saving.value = true
  try {
    await internshipApi.updateApprovalConfig({ stuckApprovalDays: stuckApprovalDays.value })
    notify.success('Parametreler güncellendi.')
  } catch (e) {
    notify.apiError(e, 'Güncelleme sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

onMounted(() => {
  loadConfig().catch(() => {})
})
</script>
```

- [ ] **Step 2: Rotayı ekle**

`src/WebUI/src/router/index.ts` — `admin` çocukları arasına, `permission-scopes` girdisinin altına:

```typescript
            {
              path: 'parameters',
              name: 'PlatformParameters',
              component: () => import('pages/admin/PlatformParametersPage.vue'),
              meta: { permissions: ['platform:parameter:manage'] },
            },
```

**Not:** `formRoute: true` **konmaz** — bu bir sekmesiz ayar sayfasıdır, entity oluşturma/düzenleme formu değil; yönlü kayma geçişi anlamsız olurdu.

- [ ] **Step 3: Menü girdisini ekle**

`src/WebUI/src/composables/useNavigation.ts` — `institution` grubunun `children` dizisinde, `Yetki Kapsamı` girdisinin altına:

```typescript
      { title: 'Ulusal Parametreler', icon: 'public', to: { name: 'PlatformParameters' }, permissions: ['platform:parameter:manage'] },
```

İzin listesi rota `meta.permissions` ile **aynıdır**: menüde görünüp rotada 403 yiyen ya da rotaya girip menüde görünmeyen bir girdi bırakılmaz.

- [ ] **Step 4: Tip denetimi ve testler**

Run: `cd src/WebUI && pnpm exec vue-tsc --noEmit && pnpm test:run`
Expected: tip hatası yok, tüm testler yeşil.

- [ ] **Step 5: Uçtan uca doğrula**

`platform:parameter:manage` taşıyan bir hesapla:

1. `Kurum Yönetimi → Ulusal Parametreler` menüde görünüyor, sayfa açılıyor, mevcut değer (14) yükleniyor.
2. Değeri 30 yap, kaydet, sayfayı yenile — 30 geliyor.
3. Değeri 0 yapıp kaydet — istemci uyarısı çıkıyor; istemci doğrulaması atlansa bile sunucu 422 döndürür.
4. Müdürlük panosundaki boş-durum metni "30 günden uzun bekleyen onay yok." diyor.

Bu izni taşımayan bir hesapla menü girdisi **görünmüyor**.

- [ ] **Step 6: Commit**

```bash
git add src/WebUI/src/pages/admin/PlatformParametersPage.vue src/WebUI/src/router/index.ts \
        src/WebUI/src/composables/useNavigation.ts
git commit -m "feat(webui): ulusal parametre ekranı — tıkanma eşiği yönetilebilir"
```

---

### Task 12: Dağıtım ön koşulu belgesi

**Files:**
- Modify: `src/Docs/docs/infrastructure/dagitim-on-kosullari.md`

**Interfaces:**
- Consumes: Task 7'nin `POST /api/security/users/replay` ucu.
- Produces: yok (belge).

- [ ] **Step 1: Mevcut biçimi oku**

Run: `sed -n '1,60p' src/Docs/docs/infrastructure/dagitim-on-kosullari.md`

Dosya sekiz resync/backfill ucunu tek yerde topluyor. Yeni satır **aynı biçimde** yazılır: uç, izin, ne zaman koşulur, atlanırsa ne olur.

- [ ] **Step 2: Satırı ekle**

Listeye ekle:

```markdown
### `POST /api/security/users/replay` — kullanıcı kayıtlarını yeniden yayınla

- **İzin:** `platform:tenant:manage`
- **Ne zaman:** müdürlük panosu (D2) dağıtıldıktan sonra, bir kez. İdempotenttir.
- **Ne yapar:** her `UserAccount` kaydını `UserCreated` olayı olarak yeniden yayınlar;
  Institution modülü bunları tüketip `InstitutionManagerLink` yerel görünümünü doldurur.
- **Atlanırsa:** yerel görünüm boş kalır, yönetilen kurum kümesi boş olur ve müdürlük
  panosu **her okulu** "yöneticisi yok" olarak listeler. Hata değil, **yanlış liste** —
  sessizdir.
- **Not:** olaylar asenkron kuyruğa düşer; uç 200 döndüğünde işlem henüz bitmemiş olabilir.
  Panoyu birkaç saniye sonra kontrol edin.
```

- [ ] **Step 3: Commit**

```bash
git add src/Docs/docs/infrastructure/dagitim-on-kosullari.md
git commit -m "docs(infra): müdürlük panosu dağıtım ön koşulu eklendi"
```

---

## Uygulama sonrası kontrol listesi

- [ ] `dotnet test MESNET.slnx` — hepsi yeşil
- [ ] `cd src/WebUI && pnpm test:run && pnpm exec vue-tsc --noEmit` — temiz
- [ ] `CrossTenantQueryDriftTests` kırılabildiği kanıtlandı (Task 2, Step 7)
- [ ] `StuckApprovalPolicyTests` doğruluk tablosu kırılabildiği kanıtlandı (Task 5, Step 8)
- [ ] `directorateContext.spec.ts` kırılabildiği kanıtlandı (Task 8, Step 5)
- [ ] `POST /api/security/users/replay` dağıtımda koşuldu
- [ ] Müdürlük hesabıyla: `Ana Sayfa` müdürlük panosu; okula geçince **okul** panosu
