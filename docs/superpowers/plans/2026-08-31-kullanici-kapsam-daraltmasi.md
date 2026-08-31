# Kullanıcı ve davet okumalarında kurum kapsamı — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `UserAccount` ve `UserInvitation` okumalarını aktörün kurum kapsamına daraltmak — kapsam istekten değil claim'den türetilerek, ve kurum bağı olmayan kayıtları bilerek dışarıda bırakarak.

**Architecture:** Kapsam kararı `InstitutionScopePolicy.VisibleScope` ile alınır, alt ağaç kimlikleri `IInstitutionSubtreeDirectory`'nin yeni metodundan gelir, çeviri saf bir `UserScopePolicy` fonksiyonunda yaşar ve Security'de tek bir çözücü hem kullanıcı hem davet sorgusunu besler. Yeni belge alanı, backfill ve dağıtım ön koşulu **yoktur**.

**Tech Stack:** .NET 10, Marten 9.11.0, Wolverine 6.15.0, xUnit + Shouldly, Vue 3 + Quasar (tek satırlık ön yüz düzeltmesi).

**Spec:** `docs/superpowers/specs/2026-08-31-kullanici-kapsam-daraltmasi-design.md`

**Dal:** `fix/kullanici-kapsam-daraltmasi`, `feat/mudurluk-panosu` (D2) üstünde. Merge sırası: D2 → bu düzeltme → D1.

## Global Constraints

- **Kapsam istekten ALINMAZ.** Her daraltma `InstitutionScopePolicy.VisibleScope(actorInstitutionId, actorPath, hasPlatformScope)` üzerinden aktörün claim'lerinden türer.
- **Platform muafiyeti EN ÖNDE değerlendirilir.** Aksi hâlde kendi kurumu olmayan platform aktörü `Guid.Empty`'ye düşer ve her zaman boş liste görür.
- **Kurum bağı olmayan kayıt (`InstitutionId == null`) daraltmanın DIŞINDADIR.** Bu opsiyonel değildir — bağı kuran tek arayüz bu listedir; düşerse hesap kalıcı kapsamsız kalır.
- **Elle kapsam karşılaştırması yazılmaz.** `InstitutionScopeDriftTests`'in `HandRolledScopeComparison` testi bunu yakalamak için vardır.
- Yetkilendirme permission bazlıdır, rol adına ASLA bakılmaz (ADR-0001).
- Kiracısız Marten session YASAKTIR; kiracı açıkça verilir.
- Bir modül başka modülün şemasına sorgu atmaz; iletişim `Common.Infrastructure`'daki sözleşmeyle olur.
- Endpoint metotları iş mantığı içermez, `IMessageBus` ile devreder.
- Commit'lere `Co-Authored-By` trailer'ı EKLENMEZ. Türkçe yorum, XML doc ve test adı; Türkçe karakterler doğru (ç, ş, ğ, ü, ö, ı, İ).
- **Sahte kapsama üretilmez.** Sadece durum kodu ölçen test eklemeyin; ölçemiyorsanız ölçemediğinizi yazın.

## Komutlar

```bash
dotnet build MESNET.slnx
dotnet test MESNET.slnx
dotnet test tests/MESNET.Security.UnitTests/MESNET.Security.UnitTests.csproj --filter "FullyQualifiedName~UserScopePolicyTests"
cd src/WebUI && pnpm test:run && pnpm exec vue-tsc --noEmit
```

**Temel çizgi:** backend 2064 geçen / 0 hata (12 proje), frontend 308 geçen.

**UYARI — Task 5 bu temel çizgiyi bilerek bozar.** `role-integrity` kararı verilmediği için yeni kilit o dosya için KIRMIZI kalır. Bu spec'in Karar 6'sıdır ve bilinçlidir; "düzeltmek" için muafiyet eklemeyin.

## Dosya Yapısı

| Dosya | Sorumluluk |
|---|---|
| `src/MESNET.Common.Infrastructure/Tenancy/IInstitutionSubtreeDirectory.cs` | **Değişir** — ikinci metot: alt ağacın TÜM kurum kimlikleri |
| `src/Modules/Institution/MESNET.Institution.Application/Services/InstitutionSubtreeDirectory.cs` | **Değişir** — yeni metodun uygulaması |
| `src/MESNET.Common.Shared/Security/UserScopePolicy.cs` | **Yeni** — saf karar: süzgeç yok / kimlik kümesi |
| `src/Modules/Security/MESNET.Security.Application/Services/UserScopeResolver.cs` | **Yeni** — claim + dizin → kimlik kümesi; Security'nin TEK kapsam kapısı |
| `src/Modules/Security/MESNET.Security.Application/ServiceRegistration.cs` | **Değişir** — çözücü kaydı |
| `src/Modules/Security/MESNET.Security.Application/Handlers/UserQueryHandler.cs` | **Değişir** — liste + tekil okuma daraltılır |
| `src/Modules/Security/MESNET.Security.Application/Handlers/InvitationHandler.cs` | **Değişir** — davet listesi daraltılır, `Metadata` DTO'dan çıkar |
| `src/MESNET.Common.Shared/Tenancy/DocumentTenancyMap.cs` | **Değişir** — `UserInvitation`'ın "KALAN BORÇ" notu silinir |
| `tests/MESNET.Security.UnitTests/UserScopePolicyTests.cs` | **Yeni** — saf kararın tüm hâlleri |
| `tests/MESNET.Security.UnitTests/IdentityDocumentScopeDriftTests.cs` | **Yeni** — Identity belgesi okuyan her dosya çözücüyü çağırmalı |
| `src/WebUI/src/composables/useDashboardStats.ts` | **Değişir** — ölü `'Pending'` süzgeci |

---

### Task 1: Alt ağacın tüm kurum kimlikleri

Mevcut `GetSchoolTenantsAsync` yalnız `School` düğümünü döndürür — bilerek, çünkü kiracı = okul. Kullanıcı daraltmasında o listeyi kullanmak **müdürlüğün kendi ekibini görememesine** yol açar: müdürlük personelinin `UserAccount.InstitutionId`'si müdürlük **düğümüdür**.

**Files:**
- Modify: `src/MESNET.Common.Infrastructure/Tenancy/IInstitutionSubtreeDirectory.cs`
- Modify: `src/Modules/Institution/MESNET.Institution.Application/Services/InstitutionSubtreeDirectory.cs`

**Interfaces:**
- Consumes: `TenantResolution.Platform`, `InstitutionNodeType`, `OfNodeType` uzantısı (`MESNET.Institution.Application.Extensions`).
- Produces: `IInstitutionSubtreeDirectory.GetSubtreeInstitutionIdsAsync(string pathPrefix, CancellationToken) → Task<IReadOnlyList<Guid>>`. Task 3 bunu tüketir.

- [ ] **Step 1: Sözleşmeye metodu ekle**

`IInstitutionSubtreeDirectory.cs` içinde, mevcut `GetSchoolTenantsAsync` bildiriminin altına:

```csharp
    /// <summary>
    /// Yol öneki altındaki <b>bütün</b> kurum kimlikleri — okul, ilçe ve il düğümleri dahil.
    ///
    /// <para><b>Neden <see cref="GetSchoolTenantsAsync"/> yetmez:</b> o metot bilerek yalnız
    /// okul düğümünü döndürür, çünkü kiracı = okul. Ama kullanıcı ve davet kayıtları müdürlük
    /// düğümüne de bağlanabilir: müdürlük personelinin <c>InstitutionId</c>'si il/ilçe
    /// düğümüdür. Okul listesiyle daraltılsaydı müdürlük <b>kendi ekibini</b> göremezdi —
    /// hata değil, sessiz boş liste.</para>
    ///
    /// <para><b>Kiracı kimliği DEĞİL kurum kimliği döner.</b> Çağıran bunları kiracı olarak
    /// kullanmamalıdır; müdürlük düğümleri kiracı değildir.</para>
    /// </summary>
    Task<IReadOnlyList<Guid>> GetSubtreeInstitutionIdsAsync(
        string pathPrefix, CancellationToken cancellationToken = default);
```

Ayrıca arayüzün sınıf düzeyi `<summary>`'sindeki "yalnız okul düğümlerini içerir" cümlesi artık **yalnız `GetSchoolTenantsAsync` için** geçerlidir; o cümleyi ilgili metodun kapsamına taşıyacak biçimde düzelt.

- [ ] **Step 2: Uygulamayı yaz**

`InstitutionSubtreeDirectory.cs` içine, `GetSchoolTenantsAsync`'in altına:

```csharp
    public async Task<IReadOnlyList<Guid>> GetSubtreeInstitutionIdsAsync(
        string pathPrefix, CancellationToken cancellationToken = default)
    {
        // Boş önek "her şey" demek DEĞİLDİR — GetSchoolTenantsAsync ile aynı gerekçe.
        // Marten string.StartsWith("") her satırı geçirir.
        if (string.IsNullOrWhiteSpace(pathPrefix))
            return [];

        await using var session = _store.QuerySession(TenantResolution.Platform);

        // Düğüm tipi süzgeci YOK: müdürlük düğümleri de dönmelidir, çünkü kullanıcı kaydı
        // onlara da bağlanır.
        return await session.Query<InstitutionRecord>()
            .Where(i => i.Path != null && i.Path.StartsWith(pathPrefix))
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);
    }
```

- [ ] **Step 3: Drift testinin tepkisini kontrol et**

Run: `dotnet build MESNET.slnx && dotnet test tests/MESNET.Security.UnitTests/MESNET.Security.UnitTests.csproj --filter "FullyQualifiedName~InstitutionScopeDriftTests"`

`InstitutionSubtreeDirectory.cs` `MayEnumerateAll` muafiyet listesinde **zaten var**, dolayısıyla yeşil kalmalı. Kırmızıysa mesajı oku ve raporla — muafiyeti körlemesine genişletme.

- [ ] **Step 4: Tüm testleri koş ve commit**

Run: `dotnet test MESNET.slnx`
Expected: 2064 geçen, 0 hata.

```bash
git add src/MESNET.Common.Infrastructure/Tenancy/IInstitutionSubtreeDirectory.cs \
        src/Modules/Institution/MESNET.Institution.Application/Services/InstitutionSubtreeDirectory.cs
git commit -m "feat(institution): alt ağacın tüm kurum kimlikleri — müdürlük düğümleri dahil"
```

---

### Task 2: Kapsamın saf çevirisi

**Files:**
- Create: `src/MESNET.Common.Shared/Security/UserScopePolicy.cs`
- Create: `tests/MESNET.Security.UnitTests/UserScopePolicyTests.cs`

**Interfaces:**
- Consumes: `InstitutionVisibility(bool Unrestricted, string? PathPrefix, Guid? InstitutionId)` (`MESNET.Common.Shared.Security`).
- Produces: `UserScopePolicy.VisibleInstitutionIds(InstitutionVisibility scope, IReadOnlyList<Guid> subtreeIds) → IReadOnlyList<Guid>?`. Task 3 bunu tüketir.

- [ ] **Step 1: Başarısız testi yaz**

`tests/MESNET.Security.UnitTests/UserScopePolicyTests.cs`:

```csharp
using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Kullanıcı ve davet okumalarında kapsamın hangi dala düştüğü.
///
/// <para><b>Neden saf fonksiyon:</b> gerçek daraltmayı ölçen uçtan uca test bu depoda
/// yazılamıyor — <c>MESNET.Api.Tests</c> çalışan yığına karşı koşar ve realm'de ikinci kuruma
/// bağlı kullanıcı yoktur. Karar buraya çıkarıldığı için DB'siz ve Keycloak'sız ölçülebilir.</para>
///
/// <para><b>En olası sessiz hata platform muafiyetinin sırasını kaçırmaktır:</b> kendi kurumu
/// olmayan platform aktörü <c>Guid.Empty</c>'ye düşerse HER ZAMAN boş liste görür ve bu hata
/// vermez.</para>
/// </summary>
public sealed class UserScopePolicyTests
{
    private static readonly Guid OkulA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OkulB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void Platform_kapsami_suzgec_uygulatmaz()
    {
        var scope = new InstitutionVisibility(Unrestricted: true, PathPrefix: null, InstitutionId: null);

        UserScopePolicy.VisibleInstitutionIds(scope, []).ShouldBeNull();
    }

    /// <summary>
    /// Sıra kritik: platform aktörünün kurumu OLMAYABİLİR. Muafiyet en önde
    /// değerlendirilmezse kimlik dalına düşer ve her zaman boş liste görür.
    /// </summary>
    [Fact]
    public void Platform_kapsami_kurum_kimligi_dolu_olsa_bile_en_onde()
    {
        var scope = new InstitutionVisibility(Unrestricted: true, PathPrefix: "/il-35", InstitutionId: OkulA);

        UserScopePolicy.VisibleInstitutionIds(scope, [OkulB]).ShouldBeNull();
    }

    [Fact]
    public void Yol_oneki_olan_aktor_alt_agac_kimliklerini_gorur()
    {
        var scope = new InstitutionVisibility(Unrestricted: false, PathPrefix: "/il-35/ilce-konak", InstitutionId: null);

        UserScopePolicy.VisibleInstitutionIds(scope, [OkulA, OkulB]).ShouldBe([OkulA, OkulB]);
    }

    /// <summary>
    /// Alt ağaç boş dönerse kapsam BOŞ kümedir — "her şey" değil. Boş kümede yalnız kurum
    /// bağı olmayan kayıtlar görünür; bu Karar 3'ün gereğidir.
    /// </summary>
    [Fact]
    public void Yol_oneki_var_ama_alt_agac_bossa_bos_kume_doner()
    {
        var scope = new InstitutionVisibility(Unrestricted: false, PathPrefix: "/il-35/ilce-konak", InstitutionId: null);

        UserScopePolicy.VisibleInstitutionIds(scope, []).ShouldBeEmpty();
    }

    [Fact]
    public void Yolu_olmayan_okul_aktoru_yalniz_kendi_kurumunu_gorur()
    {
        var scope = new InstitutionVisibility(Unrestricted: false, PathPrefix: null, InstitutionId: OkulA);

        UserScopePolicy.VisibleInstitutionIds(scope, []).ShouldBe([OkulA]);
    }

    [Fact]
    public void Kapsamsiz_aktor_bos_kume_alir()
    {
        var scope = new InstitutionVisibility(Unrestricted: false, PathPrefix: null, InstitutionId: Guid.Empty);

        UserScopePolicy.VisibleInstitutionIds(scope, []).ShouldBeEmpty();
    }

    [Fact]
    public void Kurum_kimligi_null_olan_kapsamsiz_aktor_de_bos_kume_alir()
    {
        var scope = new InstitutionVisibility(Unrestricted: false, PathPrefix: null, InstitutionId: null);

        UserScopePolicy.VisibleInstitutionIds(scope, []).ShouldBeEmpty();
    }

    /// <summary>
    /// <c>null</c> ile boş liste AYNI ŞEY DEĞİLDİR ve çağıran ikisini karıştırırsa sonuç ters
    /// döner: <c>null</c> "süzme", boş liste "yalnız bağsızları göster" demektir.
    /// </summary>
    [Fact]
    public void Null_ile_bos_liste_ayni_sey_degildir()
    {
        var platform = new InstitutionVisibility(Unrestricted: true, PathPrefix: null, InstitutionId: null);
        var kapsamsiz = new InstitutionVisibility(Unrestricted: false, PathPrefix: null, InstitutionId: Guid.Empty);

        UserScopePolicy.VisibleInstitutionIds(platform, []).ShouldBeNull();
        UserScopePolicy.VisibleInstitutionIds(kapsamsiz, []).ShouldNotBeNull();
    }
}
```

- [ ] **Step 2: Testin başarısız olduğunu doğrula**

Run: `dotnet test tests/MESNET.Security.UnitTests/MESNET.Security.UnitTests.csproj --filter "FullyQualifiedName~UserScopePolicyTests"`
Expected: derleme hatası — `UserScopePolicy` tipi yok.

- [ ] **Step 3: Politikayı yaz**

`src/MESNET.Common.Shared/Security/UserScopePolicy.cs`:

```csharp
namespace MESNET.Common.Shared.Security;

/// <summary>
/// Kurum kapsamının kullanıcı/davet sorgusuna nasıl çevrileceği — saf karar.
///
/// <para><b>Neden ayrı bir fonksiyon:</b> <c>UserAccount</c> ve <c>UserInvitation</c>
/// <c>DocumentTenancyMap</c>'te <b>kimlik katmanındadır</b>; conjoined kiracılık onları
/// SÜZMEZ. Kapsamın tamamı sorgu handler'ına aittir ve iki ayrı handler'da yaşayacağı için
/// karar tek yere çıkarılmıştır.</para>
/// </summary>
public static class UserScopePolicy
{
    /// <param name="scope">Aktörün görünürlüğü — <c>InstitutionScopePolicy.VisibleScope</c>'tan.</param>
    /// <param name="subtreeIds">
    /// <paramref name="scope"/> bir yol öneki taşıyorsa o önekin altındaki kurum kimlikleri;
    /// aksi hâlde boş liste. Çağıran bunu <c>IInstitutionSubtreeDirectory</c>'den alır.
    /// </param>
    /// <returns>
    /// <c>null</c> = süzgeç UYGULANMAZ (platform kapsamı).
    /// Boş liste = yalnız kurum bağı OLMAYAN kayıtlar görünür.
    /// Dolu liste = bu kimliklere bağlı VEYA kurum bağı olmayan kayıtlar görünür.
    ///
    /// <para><b><c>null</c> ile boş liste karıştırılırsa sonuç TERS döner</b> — biri her şeyi
    /// açar, öteki neredeyse her şeyi kapatır.</para>
    /// </returns>
    public static IReadOnlyList<Guid>? VisibleInstitutionIds(
        InstitutionVisibility scope, IReadOnlyList<Guid> subtreeIds)
    {
        // EN ÖNDE: platform aktörünün kurumu olmayabilir. Bu dal sonda olsaydı kapsamsız
        // sayılıp Guid.Empty'ye düşerdi ve HER ZAMAN boş liste görürdü — sessiz hata.
        if (scope.Unrestricted)
            return null;

        if (!string.IsNullOrWhiteSpace(scope.PathPrefix))
            return subtreeIds;

        // Yolu olmayan aktör kendi kurumuna daralır. Kapsamsız aktörde bu Guid.Empty'dir ve
        // hiçbir kurumla eşleşmez — her şeyi görmek yerine hiçbir şey görmek.
        return scope.InstitutionId is { } id && id != Guid.Empty ? [id] : [];
    }
}
```

- [ ] **Step 4: Testin geçtiğini doğrula**

Run: `dotnet test tests/MESNET.Security.UnitTests/MESNET.Security.UnitTests.csproj --filter "FullyQualifiedName~UserScopePolicyTests"`
Expected: 8 test PASS.

- [ ] **Step 5: Kilidin kilitlediğini kanıtla**

`UserScopePolicy` içindeki `if (scope.Unrestricted) return null;` bloğunu **geçici olarak** metodun en sonuna taşı (yani `PathPrefix` ve kimlik dallarından sonra).

Run: aynı filtre.
Expected: `Platform_kapsami_kurum_kimligi_dolu_olsa_bile_en_onde` **FAIL**.

Sırayı geri al, tekrar koş. Expected: 8 PASS. Kırmızı çıktıyı raporla.

- [ ] **Step 6: Tüm testleri koş ve commit**

Run: `dotnet test MESNET.slnx`

```bash
git add src/MESNET.Common.Shared/Security/UserScopePolicy.cs \
        tests/MESNET.Security.UnitTests/UserScopePolicyTests.cs
git commit -m "feat(security): kullanıcı kapsamının saf çevirisi — platform muafiyeti en önde"
```

---

### Task 3: Kullanıcı okumalarını daralt

**Files:**
- Create: `src/Modules/Security/MESNET.Security.Application/Services/UserScopeResolver.cs`
- Modify: `src/Modules/Security/MESNET.Security.Application/ServiceRegistration.cs`
- Modify: `src/Modules/Security/MESNET.Security.Application/Handlers/UserQueryHandler.cs`

**Interfaces:**
- Consumes: Task 1'in `IInstitutionSubtreeDirectory.GetSubtreeInstitutionIdsAsync`; Task 2'nin `UserScopePolicy.VisibleInstitutionIds`; `InstitutionScopePolicy.VisibleScope(Guid?, string?, bool)`; `ICurrentUserService.GetCurrentUser()?.InstitutionId`, `.GetInstitutionPath()`, `.HasPermission(...)`; `Permissions.Platform.TenantManage`.
- Produces: `UserScopeResolver.ResolveAsync(CancellationToken) → Task<IReadOnlyList<Guid>?>` — `null` = süzgeç yok. Task 4 aynı çözücüyü kullanır.

- [ ] **Step 1: Çözücüyü yaz**

`src/Modules/Security/MESNET.Security.Application/Services/UserScopeResolver.cs`:

```csharp
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Infrastructure.Tenancy;
using MESNET.Common.Shared.Security;

namespace MESNET.Security.Application.Services;

/// <summary>
/// Kullanıcı ve davet okumalarının kurum kapsamı — Security'nin <b>TEK</b> kapsam kapısı.
///
/// <para><b>Neden gerekli:</b> <c>UserAccount</c> ve <c>UserInvitation</c>
/// <c>DocumentTenancyMap</c>'te kimlik katmanındadır; conjoined kiracılık onları SÜZMEZ.
/// Kapsam kararının tamamı sorgu handler'ına aittir.</para>
///
/// <para><b>Kimlikler istekten HİÇ gelmez</b> — aktörün claim'lerinden türer. Kilitleyen test:
/// <c>IdentityDocumentScopeDriftTests</c>.</para>
/// </summary>
public sealed class UserScopeResolver
{
    private readonly ICurrentUserService _currentUser;
    private readonly IInstitutionSubtreeDirectory _subtree;

    public UserScopeResolver(ICurrentUserService currentUser, IInstitutionSubtreeDirectory subtree)
    {
        _currentUser = currentUser;
        _subtree = subtree;
    }

    /// <returns>
    /// <c>null</c> = süzgeç uygulanmaz (platform kapsamı). Aksi hâlde görünür kurum kimlikleri;
    /// boş liste geçerlidir ve "yalnız kurum bağı olmayan kayıtlar" demektir.
    /// </returns>
    public async Task<IReadOnlyList<Guid>?> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var scope = InstitutionScopePolicy.VisibleScope(
            _currentUser.GetCurrentUser()?.InstitutionId,
            _currentUser.GetInstitutionPath(),
            _currentUser.HasPermission(Permissions.Platform.TenantManage));

        // Alt ağaç sorgusu YALNIZ yol öneki varken yapılır — platform aktöründe gereksiz,
        // kimlik dalında anlamsız.
        var subtreeIds = string.IsNullOrWhiteSpace(scope.PathPrefix)
            ? []
            : await _subtree.GetSubtreeInstitutionIdsAsync(scope.PathPrefix, cancellationToken);

        return UserScopePolicy.VisibleInstitutionIds(scope, subtreeIds);
    }
}
```

- [ ] **Step 2: DI'a kaydet**

`ServiceRegistration.cs` içinde `services.AddScoped<IUserPermissionProvider, UserPermissionProvider>();` satırının altına:

```csharp
        // Kullanıcı ve davet okumalarının tek kapsam kapısı. UserAccount/UserInvitation
        // kimlik katmanındadır; conjoined kiracılık onları süzmez.
        services.AddScoped<UserScopeResolver>();
```

- [ ] **Step 3: Liste sorgusunu daralt**

`UserQueryHandler.cs` — `GetUserAccountsHandler.Handle` imzasını ve gövdesinin başını değiştir:

```csharp
    public static async Task<PagedResult<UserAccountDto>> Handle(
        GetUserAccounts query,
        IQuerySession session,
        UserScopeResolver scopeResolver,
        CancellationToken cancellationToken)
    {
        // Mezar taşları yönetim yüzeyinde görünmez (#210).
        IQueryable<UserAccount> queryable = session.Query<UserAccount>().Where(u => u.DeletedAt == null);

        // KAPSAM — istekten gelen InstitutionId'den ÖNCE ve ondan bağımsız. İstekteki değer
        // bir kolaylık süzgecidir, yetki değildir; kapsamı genişletemez.
        //
        // Kurum bağı OLMAYAN kayıtlar bilerek GÖRÜNÜR KALIR (yüklemdeki `== null` dalı):
        // bağı kuran tek arayüz bu listedir (UserManagementPage → POST /users/{id}/institution).
        // Süzülüp düşselerdi o uç hiç çağrılamaz ve hesap kalıcı kapsamsız kalırdı — tek
        // yönlü kapı.
        var visibleIds = await scopeResolver.ResolveAsync(cancellationToken);
        if (visibleIds is { } ids)
            queryable = queryable.Where(u => u.InstitutionId == null || ids.Contains(u.InstitutionId.Value));
```

Gövdenin geri kalanı **değişmez**, ancak:
- `if (query.InstitutionId.HasValue)` dalı olduğu gibi kalır ve kapsam süzgecinin **altında** çalışır.
- `MissingBranchOnly` dalındaki `await queryable.ToListAsync()` çağrısına `cancellationToken` eklenir.
- Son satır `return await queryable.ToPagedResultAsync(query, ToDto);` — imzası `CancellationToken` alıyorsa onu da geç; almıyorsa dokunma.

**Kritik:** kapsam süzgeci `MissingBranchOnly` dalındaki `ToListAsync()` çağrısından **önce** eklenmiş olmalıdır. O dal tüm eşleşen satırları belleğe çeker; süzgeç sonra gelirse sızıntı tam da o yolda sürer.

- [ ] **Step 4: Tekil okumayı daralt**

Aynı dosyada `GetUserAccountHandler.Handle`:

```csharp
    public static async Task<UserAccountDto> Handle(
        GetUserAccount query,
        IQuerySession session,
        UserScopeResolver scopeResolver,
        CancellationToken cancellationToken)
    {
        var account = await session.LoadAsync<UserAccount>(query.UserAccountId, cancellationToken);
        // Mezar taşı yönetim yüzeyinde yok sayılır (#210).
        if (account is null || account.DeletedAt is not null)
            throw new DomainException(SecurityErrors.UserNotFound(query.UserAccountId));

        // Kapsam dışı kayıt "bulunamadı" döner, "yasak" DEĞİL: yasak yanıtı kaydın VAR
        // olduğunu doğrular ve kimliği tahmin edilebilir hâle gelmiş bir listede bu bilgi
        // sızıntıdır.
        var visibleIds = await scopeResolver.ResolveAsync(cancellationToken);
        if (visibleIds is { } ids
            && account.InstitutionId is { } institutionId
            && !ids.Contains(institutionId))
            throw new DomainException(SecurityErrors.UserNotFound(query.UserAccountId));

        return GetUserAccountsHandler.ToDto(account);
    }
```

`account.InstitutionId is { } institutionId` koşulu Karar 3'ün tekil karşılığıdır: kurum bağı olmayan kayıt kapsam dışı sayılmaz.

- [ ] **Step 5: Derle ve tüm testleri koş**

Run: `dotnet build MESNET.slnx && dotnet test MESNET.slnx`
Expected: 2064 geçen, 0 hata. `MESNET.Api.Tests` fixture kimliği `admin` hem `user:*` hem `platform:tenant:manage` taşır, yani `Unrestricted` dalına düşer ve mevcut testler etkilenmez. Etkilenirse **daraltma yanlıştır** — raporla, testi gevşetme.

- [ ] **Step 6: Commit**

```bash
git add src/Modules/Security
git commit -m "fix(security): kullanıcı listesi ve tekil okuma aktörün kurum kapsamına daraltıldı"
```

---

### Task 4: Davet okumasını daralt ve `Metadata`'yı listeden çıkar

**Files:**
- Modify: `src/Modules/Security/MESNET.Security.Application/Handlers/InvitationHandler.cs`
- Modify: `src/WebUI/src/api/security.ts`

**Interfaces:**
- Consumes: Task 3'ün `UserScopeResolver.ResolveAsync`.
- Produces: `InvitationDto` artık `Metadata` taşımaz.

- [ ] **Step 1: Kapsamı uygula**

`GetInvitationsHandler.Handle` imzasına `UserScopeResolver scopeResolver, CancellationToken cancellationToken` eklenir ve `queryable` kurulduktan hemen sonra:

```csharp
        // KAPSAM — kullanıcı listesiyle AYNI kapıdan. Kurum bağı olmayan davetler bilerek
        // GÖRÜNÜR KALIR (yüklemdeki `== null` dalı): CreateInvitation InstitutionId'yi isteğe
        // bağlı alır ve süzülüp düşen davet onaylanamaz/reddedilemez hâle gelirdi.
        var visibleIds = await scopeResolver.ResolveAsync(cancellationToken);
        if (visibleIds is { } ids)
            queryable = queryable.Where(i => i.InstitutionId == null || ids.Contains(i.InstitutionId.Value));
```

Mevcut `if (query.InstitutionId.HasValue)` dalı olduğu gibi kalır ve bunun altında çalışır.

- [ ] **Step 2: `Metadata`'yı liste DTO'sundan çıkar**

`InvitationDto` kaydından son parametreyi (`Dictionary<string, string> Metadata`) **kaldır** ve `GetInvitationsHandler` içindeki kurucu çağrısından `i.Metadata` argümanını çıkar.

Kayda şu XML doc'u ekle:

```csharp
/// <summary>
/// Davet listesi satırı.
///
/// <para><b><c>Metadata</c> bilerek YOKTUR.</b> Öğrenci davetinde T.C. kimlik numarası
/// taşıyordu ve liste ucu onu kendi okulunun her davetini gören herkese veriyordu. Bu bir
/// veri minimizasyonu kararıdır ve kurum kapsamından BAĞIMSIZDIR — kapsam daraltılsa bile
/// alan gerekmiyordu. Tüketicisi ölçüldü: ön yüz onu yalnız davet OLUŞTURURKEN gönderiyor,
/// listede hiç okumuyor.</para>
/// </summary>
```

- [ ] **Step 3: Ön yüz tipinden de çıkar**

`src/WebUI/src/api/security.ts` — davet **okuma** arayüzündeki `metadata: Record<string, string>` alanını kaldır (satır ~52). Davet **oluşturma** isteğindeki isteğe bağlı `metadata?: Record<string, string>` alanı (satır ~74) **KALIR** — o taşıma biçimidir ve `UserManagementPage.vue:826` onu gönderir.

Ölçüldü: okuma tarafındaki `metadata` alanının ön yüzde hiçbir tüketicisi yok. Yine de kaldırmadan önce doğrula:

```bash
grep -rn "metadata" src/WebUI/src --include="*.vue" --include="*.ts" | grep -v node_modules
```

Beklenen: yalnız oluşturma tarafı (`UserManagementPage.vue:823-826`) ve `security.ts`'teki iki bildirim.

- [ ] **Step 4: Derle, testleri ve tip denetimini koş**

Run: `dotnet build MESNET.slnx && dotnet test MESNET.slnx`
Run: `cd src/WebUI && pnpm test:run && pnpm exec vue-tsc --noEmit`
Expected: backend 2064 / 0 hata; frontend 308 geçen, tip hatası yok.

- [ ] **Step 5: Commit**

```bash
git add src/Modules/Security src/WebUI/src/api/security.ts
git commit -m "fix(security): davet listesi kapsama daraltıldı, Metadata liste yanıtından çıkarıldı"
```

---

### Task 5: Kilidi ekle — ve `role-integrity` için BİLEREK kırmızı bırak (#283)

**Files:**
- Create: `tests/MESNET.Security.UnitTests/IdentityDocumentScopeDriftTests.cs`

**Interfaces:**
- Consumes: yok (kaynak taraması).
- Produces: yok.

**Bu görev testleri KIRMIZI bırakır ve bu bilinçlidir** (spec Karar 6). `role-integrity` ucunun kurum düzeyinde mi platform düzeyinde mi olduğu bir ürün kararıdır ve verilmemiştir.

- [ ] **Step 1: Kilidi yaz**

`tests/MESNET.Security.UnitTests/IdentityDocumentScopeDriftTests.cs`:

Tarama şekli `CrossTenantQueryDriftTests` ve `TenantlessSessionDriftTests` ile aynı olmalı — o dosyaları oku ve `SourceFiles()`, `StripComments()`, `Relative()`, `RepoRoot()` yardımcılarını aynı biçimde kullan (`src/` ve `tests/` ağaçlarının ikisi de taranır, yorumlar önce ayıklanır, izin listesi **tam göreli yol** tutar).

Test şu kuralı dayatır: `Query<UserAccount>()` ya da `Query<UserInvitation>()` çağıran her dosya, ya `UserScopeResolver` adını da içermeli, ya da gerekçesi yazılı izin listesinde olmalı.

İzin listesi **ölçüldü** — `Query<UserAccount>()` / `Query<UserInvitation>()` çağıran tüm
dosyalar (hepsi `src/Modules/Security/MESNET.Security.Application/` altında):

| Dosya | Listede mi | Gerekçe |
|---|---|---|
| `Handlers/UserQueryHandler.cs` | **HAYIR** | Task 3 ile çözücüyü çağırır |
| `Handlers/InvitationHandler.cs` | **HAYIR** | Task 4 ile çözücüyü çağırır |
| `Handlers/RoleIntegrityHandler.cs` | **HAYIR — bilerek** | Kapsamı ürün kararıdır, verilmedi — **#283**. Test bu yüzden kırmızı kalır |
| `Handlers/GuardianLinkGapHandler.cs` | evet | Çıktı kiracı damgalı `GuardianLinkView` ile sınırlı; yalnız üyelik sorulmuş — gerekçe dosyada yazılı |
| `Handlers/ReplayUserAccountsHandler.cs` | evet | Dağıtım ön koşulu; `platform:tenant:manage` ile korunuyor |
| `Handlers/UserManagementHandler.cs` | evet | Yazma komutları hedefi kimlikle alır; ayrıca Keycloak senkronizasyonu doğası gereği realm geneli ve kurum bağı KURMAZ |
| `Handlers/SetActiveInstitutionHandler.cs` | evet | Aktörün KENDİ kaydını okur |
| `Services/UserPermissionProvider.cs` | evet | Claim dönüşümü; aktörün KENDİ kaydını okur, istek bağlamı yok |
| `Consumers/AbsenceNotificationEmailConsumer.cs` | evet | Olay tüketicisi; alıcıyı olayın taşıdığı kimlikten çözer |
| `Consumers/StaffBranchSyncConsumer.cs` | evet | Olay tüketicisi; kadro olayından alan kapsamı yazar |
| `Consumers/StudentAccountSyncConsumer.cs` | evet | Olay tüketicisi; öğrenci olayından bağ kurar |

**Her gerekçeyi dosyayı okuyarak DOĞRULA.** Yukarıdakiler tarama sonucundan çıkarıldı;
biri tutmuyorsa gerekçeyi düzelt ve raporla — yanlış gerekçeyle verilen muafiyet, muafiyetin
kendisinden kötüdür.

**Listenin uzunluğu bilinçlidir.** Kilidin değeri mevcut on dosyayı yasaklamak değil, bu iki
belgeyi sorgulayan **yeni** bir dosyanın kendini gerekçelendirmeye zorlanmasıdır — aynı şekil
`InstitutionScopeDriftTests.MayEnumerateAll`'da da böyle çalışır.

`RoleIntegrityHandler.cs` **listeye KONMAZ.**

Testin başarısızlık mesajı **kendini savunmalıdır** — şu üçünü söylemeli: (1) bu bir gözden kaçma değil açık bir ürün kararıdır; (2) karar nedir (uç kurum düzeyinde mi platform düzeyinde mi olmalı); (3) dosyayı izin listesine eklemek **kararı vermek anlamına gelir**, testi susturmak değil. Türkçe, diğer drift testlerinin sesiyle.

- [ ] **Step 2: Kilidin gerçekten kilitlediğini kanıtla**

İzin listesindeki dosyalardan birinin yolunu geçici olarak boz (ör. bir harfini değiştir).

Run: `dotnet test tests/MESNET.Security.UnitTests/MESNET.Security.UnitTests.csproj --filter "FullyQualifiedName~IdentityDocumentScopeDriftTests"`
Expected: ihlal listesinde o dosya görünerek FAIL.

Yolu düzelt, tekrar koş. Expected: **yalnız `RoleIntegrityHandler.cs` ihlali kalır** — yani test hâlâ kırmızıdır ve bu beklenen sonuçtur. Gerçek çıktıyı raporla.

- [ ] **Step 3: Tüm testleri koş ve sonucu DÜRÜSTÇE raporla**

Run: `dotnet test MESNET.slnx`
Expected: **1 başarısız test** — `IdentityDocumentScopeDriftTests`, tek ihlali `RoleIntegrityHandler.cs`. Başka hiçbir test kırmızı olmamalı; olursa o gerçek bir gerilemedir.

- [ ] **Step 4: Commit**

```bash
git add tests/MESNET.Security.UnitTests/IdentityDocumentScopeDriftTests.cs
git commit -m "test(security): kimlik katmanı belgelerinde kapsam kilidi — role-integrity kararı bekliyor"
```

---

### Task 6: Pano davet sayacındaki ölü süzgeç

**Files:**
- Modify: `src/WebUI/src/composables/useDashboardStats.ts`

**Interfaces:** yok.

- [ ] **Step 1: Ölçümü doğrula**

Run: `grep -n "PendingApproval" src/Modules/Security/MESNET.Security.Core/Enums/InvitationStatus.cs` (dosya yolu farklıysa `find src -name "InvitationStatus.cs"` ile bul)

Geçerli adlar: `PendingApproval`, `Approved`, `Rejected`, `Completed`, `Expired`. **`Pending` diye bir ad YOKTUR.**

- [ ] **Step 2: Düzelt**

`useDashboardStats.ts` içinde `securityApi.listInvitations({ status: 'Pending', ... })` çağrısındaki değeri `'PendingApproval'` yap ve yanına Türkçe bir yorum ekle:

```typescript
      // 'Pending' geçerli bir InvitationStatus adı DEĞİLDİR; TryFromName başarısız olur ve
      // durum süzgeci SESSİZCE düşerdi — kart tüm durumların davetini sayıyordu.
```

- [ ] **Step 3: Doğrula ve commit**

Run: `cd src/WebUI && pnpm test:run && pnpm exec vue-tsc --noEmit`

```bash
git add src/WebUI/src/composables/useDashboardStats.ts
git commit -m "fix(webui): pano davet sayacındaki durum süzgeci sessizce düşüyordu"
```

---

### Task 7: Kapsam hakkında YANLIŞ olan iki yorumu düzelt

Bu görev kod davranışını değiştirmez; tenancy hakkında gerçeğe aykırı iki yorumu düzeltir.
İkisi de aynı sınıftan kusurdur: **bir yorumun, koruma varmış gibi konuşup gerçekte olmayan
bir güvenceyi anlatması.** Bu dalda aynı kusurun üçüncüsü Task 3'te yakalandı.

**Files:**
- Modify: `src/MESNET.Common.Shared/Tenancy/DocumentTenancyMap.cs`

- [ ] **Step 1: Kapanan borcun notunu sil**

`["UserInvitation"] = Identity,` girdisinin üstündeki yorumda "KALAN BORÇ: davet listeleme kapsamı isteğe bağlı InstitutionId filtresiyle çalışıyor … kapsam kararı sunucuda verilmeli" cümleleri **silinir** — borç kapandı.

Girdinin geri kalanı (anonim davet tamamlamanın neden `Identity` gerektirdiği, ölçülen 500 hatası) **AYNEN KALIR**; o gerekçe hâlâ geçerlidir ve silinirse biri belgeyi `Tenant`'a çevirip anonim akışı kırar.

`["UserAccount"] = Identity,` girdisine bu borç notu hiç yazılmamıştı; oraya bir şey eklenmez.

- [ ] **Step 2: Yanlış tenancy önermesini düzelt**

`src/Modules/Security/MESNET.Security.Application/Consumers/AbsenceNotificationEmailConsumer.cs`
satır 79-80'de şu yorum var:

```csharp
        // Kiracı süzgeci gerekmiyor: UserAccount kiracı damgalıdır ve oturum istek/mesaj
        // bağlamındaki kiracıyla açılır.
```

**Önerme yanlış.** `UserAccount` `DocumentTenancyMap`'te `Identity`'dir (`:151`), `Tenant`
değil — kiracı damgası **yoktur** ve conjoined kiracılık bu sorguyu **süzmez**. Yorum, süzgeç
yokluğunu var olmayan bir güvenceyle gerekçelendiriyor; bir sonraki okuyan aynı yanlış
önermeye dayanarak gerçekten sızdıran bir sorgu yazabilir.

**Sızıntı yok — ölçüldü.** Alıcılar bellekte olayın kendi tanımlayıcılarıyla sınırlanıyor:
veli bağı (`LinkedStudentIds.Contains(@event.StudentId)`), işletme (`u.BusinessId ==
@event.BusinessId`) ve öğrencinin kendisi (`u.StudentId == @event.StudentId`). Yani davranış
doğru, **gerekçe** yanlış.

Yorumu gerçeğe uyacak biçimde yeniden yaz. Şunları söylemeli: `UserAccount` kimlik
katmanındadır ve kiracı damgası taşımaz, dolayısıyla conjoined kiracılık burada **süzmez**;
daraltmayı yapan şey olayın taşıdığı tanımlayıcılardır (veli bağı / işletme / öğrenci), bu
yüzden ek bir kapsam süzgeci gerekmez. Davranışı DEĞİŞTİRME — yalnız gerekçeyi doğru yaz.

- [ ] **Step 3: Derle, testleri koş ve commit**

Run: `dotnet build MESNET.slnx && dotnet test MESNET.slnx`
Expected: Task 5'in bilerek kırmızı bıraktığı tek test dışında hepsi yeşil.

```bash
git add src/MESNET.Common.Shared/Tenancy/DocumentTenancyMap.cs \
        src/Modules/Security/MESNET.Security.Application/Consumers/AbsenceNotificationEmailConsumer.cs
git commit -m "docs(tenancy): kapsam hakkında yanlış konuşan iki yorum düzeltildi"
```

---

## Uygulama sonrası kontrol listesi

- [ ] `UserScopePolicyTests` kırılabildiği kanıtlandı (Task 2, Step 5)
- [ ] `IdentityDocumentScopeDriftTests` kırılabildiği kanıtlandı (Task 5, Step 2)
- [ ] `dotnet test MESNET.slnx` — **yalnız** `IdentityDocumentScopeDriftTests` kırmızı, tek ihlali `RoleIntegrityHandler.cs`
- [ ] `cd src/WebUI && pnpm test:run && pnpm exec vue-tsc --noEmit` — temiz
- [ ] Yeni belge alanı, backfill ya da dağıtım ön koşulu **eklenmedi**

## Devredilen işler — sessizce düşürülmez

1. **`role-integrity` kapsam kararı — #283.** Uç kurum düzeyinde mi platform düzeyinde mi? Üç seçenek ve bedelleri issue'da yazılı. Karar verilene kadar kilit kırmızı. Dal bu karar alınmadan merge edilmemelidir.
2. **İkinci kuruma bağlı aktörle uçtan uca API testi.** `mesnet-realm.json`'a ikinci kurum + `user:view` taşıyan kullanıcı gerekir; realm import tek seferliktir (#195), yani dev ortamının yeniden kurulmasını gerektirir.
3. **Marten çeviri doğrulaması (elle, canlı yığında).** `ids.Contains(u.InstitutionId.Value)` ifadesinin `Guid?` üzerinde ürettiği SQL birim testle kanıtlanamaz. Doğrulanacaklar: ikinci okulun aktörü birinci okulun kullanıcısını görmüyor; kurum bağı olmayan hesap her iki aktöre de görünüyor; platform aktörü hepsini görüyor.
