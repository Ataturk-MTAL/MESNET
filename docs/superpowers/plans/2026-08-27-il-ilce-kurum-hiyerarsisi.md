# Kurum Hiyerarşisi ve İl/İlçe Kapsam Katmanı (A Parçası) — Uygulama Planı

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Kurumları ağaç yapar (il → ilçe → okul), kapsam kararını "hedefin yolu aktörün yoluyla başlıyor mu" sorusuna indirger ve il/ilçe yetkilisinin kendi alt ağacındaki okulları **listeleyip okuyabilmesini** sağlar.

**Architecture:** `Institution` belgesi düğüm hâline gelir: `ParentId`, `NodeTypeName`, `Path`. Kapsam kararı iki saf sınıfta toplanır (`InstitutionPath`, `InstitutionScopePolicy`) ve karar üç sonuçludur — `Allowed` / `Denied` / `NeedsPathCheck`. Aktörün yolu `institution_path` claim'i olarak sunucu tarafında üretilir (`institution_id` ile aynı disiplin: token'daki değer her istekte silinir); hedefin yolu yalnız aktör ≠ hedef olduğunda tek belge yüklemesiyle çözülür. Kiracılık değişmez — kiracı yine okuldur.

**Tech Stack:** .NET 10, Wolverine 6 (middleware + handler), Marten 9 (document store, LINQ `StartsWith` → SQL `LIKE 'önek%'`), Ardalis.SmartEnum, xUnit + Shouldly, Vue 3 + Quasar + Pinia + TypeScript, Vitest.

## Global Constraints

Aşağıdakiler **her görevin** gereksinimlerine dahildir; tekrar yazılmaz.

- **Yol biçimi:** `Path` daima `/` ile başlar **ve** `/` ile biter: `/{ilId}/{ilçeId}/{okulId}/`. Kimliklerden kurulur, adlardan **değil**.
- **Nullable, `required` DEĞİL:** yeni entity alanlarının hiçbiri `required` olamaz. Mevcut kayıtlar bu alanlar olmadan saklandı; `required` System.Text.Json'ı her eski kurumda `JsonException` ile durdurur.
- **Yeni izin tanımlanmaz.** Yeni roller düz `Permissions.Institution.View` alır. `institution:` önekli her yeni izin `institution:*` wildcard'ı üzerinden **her okul müdürüne** sessizce geçer (ADR-0002 önek tuzağı).
- **Kiracılık değişmez.** `DocumentTenancyMap` içindeki `Institution` → `DocumentTenancy.Identity` sınıflandırmasına **dokunulmaz**.
- **Marten SmartEnum LINQ yasağı:** LINQ `Where`/`Select` içinde SmartEnum özelliği kullanılmaz (`i.NodeType.Name` SQL'de `data->'nodeType'->>'Name'` üretir ve **NULL** döner). Sorgular daima düz `NodeTypeName` string alanına bakar.
- **Marten composite index adı:** her `Index(...)` çağrısında `x.Name = "..."` verilir. PostgreSQL tanımlayıcı sınırı 64 karakter, Marten'in otomatik adı bunu aşar.
- **Arayüz dili Türkçe**, Türkçe karakterler doğru yazılır (ç, ş, ğ, ü, ö, ı, İ). Backend enum `Name` değerleri İngilizce kalır.
- **Endpoint'te `IQuerySession`/`IDocumentSession` inject etmek YASAK.** Uçlar `IMessageBus` üzerinden handler çağırır.
- **Commit mesajı formatı:** `<type>: <description>` (feat/fix/refactor/docs/test/chore). `Co-Authored-By` trailer'ı **eklenmez**.
- **Her görev sonunda ilgili kapı koşar:** backend için `dotnet test`, ön yüz için `pnpm -C src/WebUI lint && pnpm -C src/WebUI test:run`.

## Spec'ten Bilinçli Sapmalar

Üçü de spec'in **kuralını** korur, uygulama ayrıntısını değiştirir. Uygulayıcı bunları kendi kararıyla geri çevirmemelidir.

1. **Yolu boş aktör kendi kurumunu görmeye devam eder.** Spec "yolu boş olan aktör hiçbir şey görür" diyor. Harfiyen uygulanırsa, geçiş ucu koşturulmadan yapılan bir dağıtım **her okul müdürünün kurum sayfasını** kırar — çünkü kimsenin yolu yoktur. Bunun yerine kimlik eşitliği (`aktör == hedef`) yol kontrolünden **önce** gelir ve yolu olmayan aktörün listesi bugünkü gibi tek kuruma daralır. Spec'in kuralı korunur: yolu boş aktör **hiçbir şey kazanmaz**; yalnız sahip olduğunu kaybetmez.
2. **`/auth/me` `institutionNodeType` kazanmaz.** Bunun yerine `InstitutionDto.nodeType` alanı eklenir ve menü kararı `institutionStore` üzerinden verilir. Gerekçe: `/auth/me` bir Minimal API ucudur, DI'dan gelen `IQuerySession` **kiracısızdır** ve kullanılırsa `DefaultTenantUsageDisabledException` atar; düğüm tipini oradan okumak ya yeni bir claim ya da yeni bir sorgu yolu ister. `institutionStore` aktörün kendi kurumunu `GET /api/institutions/{id}` ile zaten yüklüyor (MainLayout mount'ta çağırıyor) ve o yanıt guard'dan geçiyor.
3. **`InstitutionScopePolicy.CanAccess(Guid?, Guid, bool)` kaldırılır, yerine `Decide(...)` gelir.** Aynı adı yeni anlamla bırakmak, güncellenmemiş bir çağrı yerinin üst-düğüm aktörünü **sessizce reddetmesi** demekti. Üretimde yalnız iki çağrı yeri var; ikisi de bu planda güncelleniyor.

---

### Task 1: Yol biçimi ve kapsam kararı (saf çekirdek)

Ağacın tamamı bu iki saf sınıfın doğruluğuna dayanır. Veritabanı, HTTP ve Marten olmadan tek başına test edilir.

**Files:**
- Create: `src/MESNET.Common.Shared/Security/InstitutionPath.cs`
- Modify: `src/MESNET.Common.Shared/Security/InstitutionScopePolicy.cs` (tamamı yeniden yazılır)
- Test: `tests/MESNET.Security.UnitTests/InstitutionPathTests.cs` (yeni)
- Test: `tests/MESNET.Security.UnitTests/InstitutionScopePolicyTests.cs` (mevcut, yeniden yazılır)

**Interfaces:**
- Consumes: yok (ilk görev).
- Produces:
  - `MESNET.Common.Shared.Security.InstitutionPath` — `const char Separator = '/'`; `static string Root(Guid nodeId)`; `static string Child(string parentPath, Guid nodeId)`; `static string? Normalize(string? path)`; `static bool Contains(string? ancestorPath, string? descendantPath)`
  - `MESNET.Common.Shared.Security.InstitutionScopeOutcome` — `enum { Allowed, Denied, NeedsPathCheck }`
  - `MESNET.Common.Shared.Security.InstitutionScopePolicy` — `static InstitutionScopeOutcome Decide(Guid? actorInstitutionId, Guid targetInstitutionId, bool hasPlatformScope)`; `static bool CanAccessByPath(string? actorPath, string? targetPath)`; `static InstitutionVisibility VisibleScope(Guid? actorInstitutionId, string? actorPath, bool hasPlatformScope)`
  - `MESNET.Common.Shared.Security.InstitutionVisibility` — `sealed record InstitutionVisibility(bool Unrestricted, string? PathPrefix, Guid? InstitutionId)`

- [ ] **Step 1: Yol testlerini yaz (kırmızı)**

`tests/MESNET.Security.UnitTests/InstitutionPathTests.cs`:

```csharp
using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Materyalize yolun BİÇİMİ bir güvenlik kararıdır, süs değil.
///
/// <para>Sondaki ayraç olmadan <c>/33/1</c> öneki <c>/33/10...</c> yolunu da yakalar ve bir
/// ilçe yetkilisi KARDEŞ ilçenin okullarını görür. Kimlikler Guid olduğu için bugün segment
/// uzunlukları sabittir ve bu çakışma pratikte oluşamaz — ama biçim garantisi kimlik tipine
/// bağlı bırakılmaz: yarın kısa bir kod segmenti eklenirse kural sessizce çöker.</para>
/// </summary>
public sealed class InstitutionPathTests
{
    private static readonly Guid Il = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Ilce = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Okul = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public void Kok_yolu_iki_ayrac_arasinda_kimlik_tasir()
    {
        InstitutionPath.Root(Il).ShouldBe($"/{Il:D}/");
    }

    [Fact]
    public void Cocuk_yolu_ust_yolun_uzerine_eklenir()
    {
        var ilce = InstitutionPath.Child(InstitutionPath.Root(Il), Ilce);
        var okul = InstitutionPath.Child(ilce, Okul);

        okul.ShouldBe($"/{Il:D}/{Ilce:D}/{Okul:D}/");
    }

    [Fact]
    public void Ust_yolu_bos_olan_cocuk_yaratilamaz()
    {
        Should.Throw<ArgumentException>(() => InstitutionPath.Child("", Okul));
        Should.Throw<ArgumentException>(() => InstitutionPath.Child("   ", Okul));
    }

    [Theory]
    [InlineData("a", "/a/")]
    [InlineData("/a", "/a/")]
    [InlineData("a/", "/a/")]
    [InlineData("  /a/  ", "/a/")]
    public void Normalize_bastaki_ve_sondaki_ayraci_garanti_eder(string girdi, string beklenen)
    {
        InstitutionPath.Normalize(girdi).ShouldBe(beklenen);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_bos_degeri_null_dondurur(string? girdi)
    {
        InstitutionPath.Normalize(girdi).ShouldBeNull();
    }

    [Fact]
    public void Dugum_kendi_alt_agacindadir()
    {
        InstitutionPath.Contains("/a/b/", "/a/b/").ShouldBeTrue();
    }

    [Fact]
    public void Alt_ve_torun_dugum_kapsamdadir()
    {
        InstitutionPath.Contains("/a/", "/a/b/").ShouldBeTrue();
        InstitutionPath.Contains("/a/", "/a/b/c/").ShouldBeTrue();
    }

    [Fact]
    public void Ust_dugum_kapsam_DISIDIR()
    {
        InstitutionPath.Contains("/a/b/", "/a/").ShouldBeFalse(
            "Okul müdürü ilçe müdürlüğünün kaydını görmemeli — kapsam aşağı doğrudur.");
    }

    [Fact]
    public void Kardes_dugum_kapsam_disidir()
    {
        InstitutionPath.Contains("/a/b/", "/a/c/").ShouldBeFalse();
    }

    /// <summary>
    /// Biçimin var oluş nedeni. Ayraçla bitmeyen bir önek karşılaştırması burada
    /// <c>true</c> döner ve kardeş ilçe sızar.
    /// </summary>
    [Fact]
    public void Onek_benzerligi_kardes_dugumu_sizdirmaz()
    {
        InstitutionPath.Contains("/33/1/", "/33/10/").ShouldBeFalse();
    }

    [Theory]
    [InlineData(null, "/a/")]
    [InlineData("/a/", null)]
    [InlineData("", "/a/")]
    [InlineData("/a/", "")]
    public void Bos_yol_hicbir_seyi_kapsamaz(string? ata, string? torun)
    {
        InstitutionPath.Contains(ata, torun).ShouldBeFalse();
    }
}
```

- [ ] **Step 2: Testin başarısız olduğunu doğrula**

Run: `dotnet test tests/MESNET.Security.UnitTests --filter FullyQualifiedName~InstitutionPathTests`
Expected: FAIL — `error CS0246: The type or namespace name 'InstitutionPath' could not be found`

- [ ] **Step 3: `InstitutionPath` yaz**

`src/MESNET.Common.Shared/Security/InstitutionPath.cs`:

```csharp
namespace MESNET.Common.Shared.Security;

/// <summary>
/// Kurum ağacındaki materyalize yolun <b>tek</b> biçim otoritesi.
///
/// <para><b>Neden materyalize yol:</b> alt ağaç sorgusu <c>Path.StartsWith(aktörünYolu)</c>
/// olur ve Marten bunu <c>LIKE 'önek%'</c> çevirir. Ham SQL, <c>WITH RECURSIVE</c> ve her
/// istekte ağaç yürüyüşü gerekmez.</para>
///
/// <para><b>Neden sondaki ayraç biçimin parçası:</b> onsuz <c>/33/1</c> öneki <c>/33/10...</c>
/// yolunu da yakalar ve bir ilçe yetkilisi kardeş ilçeyi görür. Kimlikler Guid olduğu için
/// segmentler bugün sabit uzunluktadır ve çakışma oluşamaz; garanti yine de kimlik tipine
/// bırakılmaz.</para>
///
/// <para><b>Yol kimliklerden kurulur, adlardan DEĞİL.</b> İlçe adı düzeltildiğinde yolun
/// bozulmaması gerekir.</para>
/// </summary>
public static class InstitutionPath
{
    public const char Separator = '/';

    /// <summary>Kök (il) düğümünün yolu.</summary>
    public static string Root(Guid nodeId) => $"{Separator}{nodeId:D}{Separator}";

    /// <summary>Üst düğümün yoluna bir segment ekler.</summary>
    /// <exception cref="ArgumentException">Üst yol boş — kök için <see cref="Root"/> kullanın.</exception>
    public static string Child(string parentPath, Guid nodeId)
    {
        var normalized = Normalize(parentPath)
            ?? throw new ArgumentException(
                "Üst düğümün yolu boş olamaz; kök düğüm için Root(...) kullanın.", nameof(parentPath));

        return $"{normalized}{nodeId:D}{Separator}";
    }

    /// <summary>
    /// Baştaki ve sondaki ayracı garanti eder. Boş/boşluk girdi <c>null</c> döner —
    /// "yol yok" ile "kök yolu" birbirine karışmamalıdır.
    /// </summary>
    public static string? Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        var trimmed = path.Trim();

        if (trimmed[0] != Separator) trimmed = Separator + trimmed;
        if (trimmed[^1] != Separator) trimmed += Separator;

        return trimmed;
    }

    /// <summary>
    /// <paramref name="descendantPath"/>, <paramref name="ancestorPath"/>'in alt ağacında mı?
    /// Düğüm kendi alt ağacındadır; ÜST düğüm ve kardeşler değildir.
    /// </summary>
    public static bool Contains(string? ancestorPath, string? descendantPath)
    {
        if (Normalize(ancestorPath) is not { } ancestor) return false;
        if (Normalize(descendantPath) is not { } descendant) return false;

        return descendant.StartsWith(ancestor, StringComparison.Ordinal);
    }
}
```

- [ ] **Step 4: Yol testlerinin geçtiğini doğrula**

Run: `dotnet test tests/MESNET.Security.UnitTests --filter FullyQualifiedName~InstitutionPathTests`
Expected: PASS (14 test)

- [ ] **Step 5: Kapsam kararı testlerini yaz (kırmızı)**

`tests/MESNET.Security.UnitTests/InstitutionScopePolicyTests.cs` dosyasının **tamamını** şununla değiştir:

```csharp
using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Hangi kurumun verisine dokunulabileceği kararı (ADR-0003 adım 6 + kurum hiyerarşisi).
///
/// <para><b>Neden gerekti — ölçüldü.</b> İki okullu dev ortamında bu kontrol yokken B okulunun
/// müdürü A okulunun kaydını <b>okudu</b> (200, 7 kişilik personel listesiyle), <b>adını
/// değiştirdi</b> (200) ve personel listesine <b>kayıt ekledi</b> (201). Marten conjoined
/// kiracılığı bunu engelleyemez: <c>Institution</c> belgesi kiracının kendisidir ve kiracı
/// damgası taşımaz.</para>
///
/// <para><b>Karar neden üç sonuçlu:</b> yol karşılaştırması hedefin kaydını okumayı gerektirir.
/// Kararı "izin var / yok / yola bakmak gerekiyor" diye ayırmak, okul kullanıcısının (aktör ==
/// hedef) hiçbir isteğinde ek okuma yapılmamasını sağlar — ek maliyet yalnız yeni üst-düğüm
/// yeteneği kullanıldığında ödenir.</para>
/// </summary>
public sealed class InstitutionScopePolicyTests
{
    private static readonly Guid OkulA = Guid.Parse("efd57b88-2f47-471c-9f51-476f80fabfca");
    private static readonly Guid OkulB = Guid.Parse("a24ebbab-8c58-4373-b936-640fa3247e77");

    // ── Decide: kimlik aşaması ──

    [Fact]
    public void Kendi_kurumuna_erisir_ve_yol_okumasi_gerekmez()
    {
        InstitutionScopePolicy.Decide(OkulA, OkulA, hasPlatformScope: false)
            .ShouldBe(InstitutionScopeOutcome.Allowed);
    }

    [Fact]
    public void Baska_kurum_yol_kontrolune_dusurulur()
    {
        InstitutionScopePolicy.Decide(OkulA, OkulB, hasPlatformScope: false)
            .ShouldBe(InstitutionScopeOutcome.NeedsPathCheck);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Kapsamsiz_aktor_hicbir_kuruma_erisemez(string? actorId)
    {
        Guid? actor = actorId is null ? null : Guid.Parse(actorId);

        InstitutionScopePolicy.Decide(actor, OkulA, hasPlatformScope: false)
            .ShouldBe(InstitutionScopeOutcome.Denied);
    }

    /// <summary>Muafiyet ÖNCE bakılır; platform aktörünün kendi kurumu yoktur.</summary>
    [Fact]
    public void Kurum_ustu_aktor_her_kuruma_erisir()
    {
        InstitutionScopePolicy.Decide(null, OkulB, hasPlatformScope: true)
            .ShouldBe(InstitutionScopeOutcome.Allowed);
        InstitutionScopePolicy.Decide(OkulA, OkulB, hasPlatformScope: true)
            .ShouldBe(InstitutionScopeOutcome.Allowed);
    }

    [Fact]
    public void Bos_hedef_reddedilir()
    {
        InstitutionScopePolicy.Decide(OkulA, Guid.Empty, hasPlatformScope: false)
            .ShouldBe(InstitutionScopeOutcome.Denied);
    }

    // ── CanAccessByPath: ağaç aşaması ──

    [Fact]
    public void Il_yetkilisi_kendi_ilindeki_okulu_gorur()
    {
        InstitutionScopePolicy.CanAccessByPath("/il/", "/il/ilce/okul/").ShouldBeTrue();
    }

    [Fact]
    public void Il_yetkilisi_baska_ilin_okulunu_goremez()
    {
        InstitutionScopePolicy.CanAccessByPath("/il/", "/baskail/ilce/okul/").ShouldBeFalse();
    }

    [Fact]
    public void Okul_ust_dugumu_goremez()
    {
        InstitutionScopePolicy.CanAccessByPath("/il/ilce/okul/", "/il/ilce/").ShouldBeFalse();
    }

    [Fact]
    public void Yolu_olmayan_aktor_yol_kontrolunu_gecemez()
    {
        InstitutionScopePolicy.CanAccessByPath(null, "/il/ilce/okul/").ShouldBeFalse();
    }

    // ── VisibleScope: listeleme daraltması ──

    [Fact]
    public void Kurum_ustu_aktorun_listesi_daraltilmaz()
    {
        var scope = InstitutionScopePolicy.VisibleScope(null, null, hasPlatformScope: true);

        scope.Unrestricted.ShouldBeTrue();
        scope.PathPrefix.ShouldBeNull();
        scope.InstitutionId.ShouldBeNull();
    }

    [Fact]
    public void Yolu_olan_aktorun_listesi_alt_agaca_daraltilir()
    {
        var scope = InstitutionScopePolicy.VisibleScope(OkulA, "/il/", hasPlatformScope: false);

        scope.Unrestricted.ShouldBeFalse();
        scope.PathPrefix.ShouldBe("/il/");
        scope.InstitutionId.ShouldBeNull();
    }

    /// <summary>
    /// <b>Geçiş ucu koşturulmadan yapılan dağıtım kurum sayfasını KIRMAMALI.</b> Yolu olmayan
    /// aktör bugünkü davranışı korur: yalnız kendi kurumunu görür. Bu bir genişletme değildir —
    /// yolu olmayan aktör hiçbir şey KAZANMAZ, yalnız sahip olduğunu kaybetmez.
    /// </summary>
    [Fact]
    public void Yolu_olmayan_aktor_yalniz_kendi_kurumunu_gorur()
    {
        var scope = InstitutionScopePolicy.VisibleScope(OkulA, null, hasPlatformScope: false);

        scope.Unrestricted.ShouldBeFalse();
        scope.PathPrefix.ShouldBeNull();
        scope.InstitutionId.ShouldBe(OkulA);
    }

    /// <summary>Kapsamsızlık sınırsızlık değildir: hiçbir kurumla eşleşmeyen bir daraltma döner.</summary>
    [Fact]
    public void Kapsamsiz_aktorun_listesi_bos_gelir()
    {
        var scope = InstitutionScopePolicy.VisibleScope(null, null, hasPlatformScope: false);

        scope.Unrestricted.ShouldBeFalse();
        scope.PathPrefix.ShouldBeNull();
        scope.InstitutionId.ShouldBe(Guid.Empty);
    }
}
```

- [ ] **Step 6: Testin başarısız olduğunu doğrula**

Run: `dotnet test tests/MESNET.Security.UnitTests --filter FullyQualifiedName~InstitutionScopePolicyTests`
Expected: FAIL — `error CS0117: 'InstitutionScopePolicy' does not contain a definition for 'Decide'`

- [ ] **Step 7: `InstitutionScopePolicy` yeniden yaz**

`src/MESNET.Common.Shared/Security/InstitutionScopePolicy.cs` dosyasının **tamamını** şununla değiştir:

```csharp
namespace MESNET.Common.Shared.Security;

/// <summary>Kapsam kararının sonucu.</summary>
public enum InstitutionScopeOutcome
{
    /// <summary>Erişim var; hedefin kaydını okumaya gerek yok.</summary>
    Allowed,

    /// <summary>Erişim yok; hedefin kaydını okumaya gerek yok.</summary>
    Denied,

    /// <summary>
    /// Karar ağaca bakmadan verilemez. Çağıran hedefin <c>Path</c> değerini okur ve
    /// <see cref="InstitutionScopePolicy.CanAccessByPath"/> ile bitirir.
    /// </summary>
    NeedsPathCheck
}

/// <summary>
/// Bir listeleme sorgusunun nasıl daraltılacağı.
/// </summary>
/// <param name="Unrestricted">Daraltma yok — yalnız kurum üstü aktör.</param>
/// <param name="PathPrefix">Verilmişse <c>Path.StartsWith(prefix)</c> ile daraltılır.</param>
/// <param name="InstitutionId">
/// Verilmişse <c>Id == value</c> ile daraltılır. <see cref="Guid.Empty"/> hiçbir kurumla
/// eşleşmez, yani liste boş gelir — <b>her şeyi görmek</b> yerine hiçbir şey görmek.
/// </param>
public sealed record InstitutionVisibility(bool Unrestricted, string? PathPrefix, Guid? InstitutionId);

/// <summary>
/// Bir aktörün <b>hangi kurumun</b> verisine dokunabileceğine karar verir (ADR-0003 adım 6 +
/// kurum hiyerarşisi).
///
/// <para><b>Neden kiracılık yetmiyor:</b> Marten conjoined kiracılığı satırları süzer, ama
/// <c>Institution</c> belgesi <see cref="Tenancy.DocumentTenancy.Identity"/> sınıfındadır —
/// kiracının <i>kendisi</i> olduğu için damga taşımaz. Kiracılık onu korumaz; kurum kaydına
/// dokunan uçlar kimliği <b>istekten</b> alır ve karşılaştırma yapılmazsa kimse durdurmaz.</para>
///
/// <para><b>Ölçüldü (iki okullu dev ortamı):</b> bu kontrol yokken B okulunun müdürü A okulunun
/// kaydını okudu (200, <b>7 kişilik personel listesiyle</b>), adını değiştirdi (200) ve personel
/// listesine kayıt ekledi (201). Hiçbiri hata vermedi.</para>
///
/// <para><b>İzin erişimi açar, kapsamı belirlemez</b> (ADR-0001). Kapsam kararı burada ve
/// aktörün <c>institution_id</c> / <c>institution_path</c> claim'lerinden okunur — istekten
/// DEĞİL. İki claim de sunucu tarafında kullanıcı kaydından üretilir (ADR-0003 adım 2).</para>
/// </summary>
public static class InstitutionScopePolicy
{
    /// <summary>
    /// Kimlik aşaması. Yol okuması gerektirmeyen bütün durumları burada bitirir.
    /// </summary>
    /// <param name="actorInstitutionId">Aktörün kurum kapsamı — <c>institution_id</c> claim'i.</param>
    /// <param name="targetInstitutionId">İstekte geçen hedef kurum.</param>
    /// <param name="hasPlatformScope">
    /// <c>platform:tenant:manage</c> — kurum sınırının üstünde çalışma yetkisi. Okul rollerinin
    /// hiçbirinde yoktur.
    /// </param>
    public static InstitutionScopeOutcome Decide(
        Guid? actorInstitutionId, Guid targetInstitutionId, bool hasPlatformScope)
    {
        // Sıra önemli: muafiyet ÖNCE. Platform aktörünün kendi kurumu yoktur; kapsam
        // karşılaştırmasına girseydi hiçbir okula erişemezdi.
        if (hasPlatformScope)
            return InstitutionScopeOutcome.Allowed;

        if (targetInstitutionId == Guid.Empty)
            return InstitutionScopeOutcome.Denied;

        if (Normalize(actorInstitutionId) is not { } actor)
            return InstitutionScopeOutcome.Denied;

        // Kimlik eşitliği ağaçtan ÖNCE gelir. Okul kullanıcısının kendi kurumuna erişimi hiçbir
        // ek okuma yapmadan çözülür — ve geçiş ucu koşturulmamış bir kurulumda (yollar boş)
        // kurum sayfası çalışmaya devam eder.
        if (actor == targetInstitutionId)
            return InstitutionScopeOutcome.Allowed;

        return InstitutionScopeOutcome.NeedsPathCheck;
    }

    /// <summary>
    /// Ağaç aşaması. Hedef, aktörün alt ağacında mı? Aktörün kendi düğümü de alt ağacındadır;
    /// <b>üst düğüm ve kardeşler değildir</b>.
    /// </summary>
    public static bool CanAccessByPath(string? actorPath, string? targetPath) =>
        InstitutionPath.Contains(actorPath, targetPath);

    /// <summary>
    /// Bir listeleme sorgusunun nasıl daraltılacağı.
    ///
    /// <para><b>Yolu olmayan aktör kendi kurumuna daralır</b>, boşa değil. Spec harfiyen
    /// uygulansaydı (yolu boş aktör hiçbir şey görür), geçiş ucu koşturulmadan yapılan bir
    /// dağıtım her okul müdürünün kurum sayfasını kırardı. Bu daraltma bir genişletme değildir:
    /// yolu olmayan aktör hiçbir şey kazanmaz, yalnız bugünkü hakkını kaybetmez.</para>
    /// </summary>
    public static InstitutionVisibility VisibleScope(
        Guid? actorInstitutionId, string? actorPath, bool hasPlatformScope)
    {
        if (hasPlatformScope)
            return new InstitutionVisibility(Unrestricted: true, PathPrefix: null, InstitutionId: null);

        if (InstitutionPath.Normalize(actorPath) is { } path)
            return new InstitutionVisibility(Unrestricted: false, PathPrefix: path, InstitutionId: null);

        // Yol yok: kimliğe düş. Kapsamsız aktörde bu Guid.Empty'dir ve hiçbir kurumla eşleşmez.
        return new InstitutionVisibility(
            Unrestricted: false,
            PathPrefix: null,
            InstitutionId: Normalize(actorInstitutionId) ?? Guid.Empty);
    }

    /// <summary>Boş Guid ile <c>null</c> aynı anlama gelir: kapsam yok.</summary>
    private static Guid? Normalize(Guid? value) =>
        value is { } id && id != Guid.Empty ? id : null;
}
```

- [ ] **Step 8: Testlerin geçtiğini doğrula (derleme hatası beklenir)**

Run: `dotnet test tests/MESNET.Security.UnitTests --filter FullyQualifiedName~InstitutionScopePolicy`
Expected: FAIL — çözüm derlenmez, çünkü `InstitutionScopeGuardMiddleware.cs:28` ve `GetInstitutionsHandler.cs:30` artık var olmayan `CanAccess` / `VisibleInstitutionFilter` çağırıyor. Bu **beklenen**; Task 3 ve Task 5 onları düzeltir. Bu görevin testlerini izole koşmak için geçici olarak o iki dosyayı derleyen projeler yerine yalnız `MESNET.Common.Shared` projesini derleyin:

Run: `dotnet build src/MESNET.Common.Shared`
Expected: BUILD SUCCEEDED

- [ ] **Step 9: Commit**

```bash
git add src/MESNET.Common.Shared/Security/InstitutionPath.cs \
        src/MESNET.Common.Shared/Security/InstitutionScopePolicy.cs \
        tests/MESNET.Security.UnitTests/InstitutionPathTests.cs \
        tests/MESNET.Security.UnitTests/InstitutionScopePolicyTests.cs
git commit -m "feat(institution): kurum kapsamı yol tabanlı ağaç kararına çevrildi

Kapsam artık kimlik eşitliği değil 'hedefin yolu aktörün yoluyla başlıyor mu'
sorusu. Karar üç sonuçlu: Allowed / Denied / NeedsPathCheck — okul kullanıcısının
kendi kurumuna erişimi hiçbir ek okuma yapmadan çözülür.

Sondaki ayraç biçimin parçasıdır: onsuz /33/1 öneki /33/10 yolunu da yakalar ve
ilçe yetkilisi kardeş ilçeyi görür.

Yolu olmayan aktör kendi kurumuna daralır, boşa değil — geçiş ucu koşturulmadan
yapılan bir dağıtım kurum sayfasını kırmasın diye."
```

---

### Task 2: `Institution` ağaç alanları, düğüm tipi ve DTO

Ağacın veri modeli. Tek stok alan (`NodeTypeName`) + hesaplanmış SmartEnum, çünkü Marten LINQ SmartEnum özelliğini sorgulayamaz.

**Files:**
- Create: `src/Modules/Institution/MESNET.Institution.Core/Enums/InstitutionNodeType.cs`
- Create: `src/Modules/Institution/MESNET.Institution.Application/Extensions/InstitutionQueryExtensions.cs`
- Modify: `src/Modules/Institution/MESNET.Institution.Core/Entities/Institution.cs`
- Modify: `src/Modules/Institution/MESNET.Institution.Persistence/InstitutionMartenConfig.cs`
- Modify: `src/Modules/Institution/MESNET.Institution.Application/Dtos/InstitutionDto.cs`
- Modify: `src/Modules/Institution/MESNET.Institution.Application/Extensions/InstitutionMappingExtensions.cs`
- Test: `tests/MESNET.Institution.UnitTests/InstitutionNodeTypeTests.cs` (yeni)

**Interfaces:**
- Consumes: `InstitutionPath` (Task 1).
- Produces:
  - `MESNET.Institution.Core.Enums.InstitutionNodeType` — SmartEnum; `Province` / `District` / `School`; `string Slug`; `static InstitutionNodeType Resolve(string? name)`
  - `Institution` entity yeni özellikleri: `Guid? ParentId`, `string? NodeTypeName`, `string? Path`, `[JsonIgnore] InstitutionNodeType NodeType` (get-only)
  - `MESNET.Institution.Application.Extensions.InstitutionQueryExtensions` — `static IQueryable<Institution> OfNodeType(this IQueryable<Institution> queryable, InstitutionNodeType nodeType)`
  - `InstitutionDto` yeni alanları (konum sırası önemlidir, aşağıdaki tam tanıma bakın): `string NodeType`, `string NodeTypeSlug`, `Guid? ParentId`, `string? ParentName`
  - `InstitutionMappingExtensions.ToDto(this Institution entity, string? parentName = null)`

- [ ] **Step 1: Testi yaz (kırmızı)**

`tests/MESNET.Institution.UnitTests/InstitutionNodeTypeTests.cs`:

```csharp
using MESNET.Institution.Application.Extensions;
using MESNET.Institution.Core.Enums;
using Shouldly;
using Xunit;
// "Institution" hem ad alanı hem tip adı olduğu için doğrudan kullanılamaz (CS0118).
// Depoda aynı kısayol InstitutionTenantDirectory içinde de var.
using InstitutionRecord = MESNET.Institution.Core.Entities.Institution;

namespace MESNET.Institution.UnitTests;

/// <summary>
/// Düğüm tipinin <b>çözümlenmesi</b> geriye dönük uyumluluğun tamamıdır.
///
/// <para>Mevcut kurum kayıtları bu alan olmadan saklandı ve hepsi okuldur. <c>Resolve(null)</c>
/// <c>School</c> döndürmezse, geçiş ucu koşturulana kadar okul listesi <b>boş</b> gelir — hata
/// değil, sessiz boşluk.</para>
///
/// <para><b>Neden entity'de SmartEnum saklanmıyor:</b> Marten LINQ'te <c>i.NodeType.Name</c>
/// SQL'e <c>data->'nodeType'->>'Name'</c> çevrilir; SmartEnum ise JSON'a düz string yazılır,
/// nesne değil. Sonuç HER ZAMAN NULL'dur ve sorgu hiçbir şey bulmaz. Bu yüzden stok alan tek
/// ve düzdür (<c>NodeTypeName</c>); SmartEnum ondan hesaplanır ve serialize EDİLMEZ.</para>
/// </summary>
public sealed class InstitutionNodeTypeTests
{
    [Fact]
    public void Uc_dugum_tipi_vardir()
    {
        InstitutionNodeType.List.Select(t => t.Name).ShouldBe(
            new[] { "Province", "District", "School" }, ignoreOrder: true);
    }

    [Fact]
    public void Her_tipin_turkce_etiketi_var()
    {
        foreach (var type in InstitutionNodeType.List)
            type.Slug.ShouldNotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("Province")]
    [InlineData("province")]
    [InlineData("PROVINCE")]
    public void Bilinen_ad_buyuk_kucuk_harfe_duyarsiz_cozulur(string name)
    {
        InstitutionNodeType.Resolve(name).ShouldBe(InstitutionNodeType.Province);
    }

    /// <summary>
    /// Geçiş koşturulmamış kayıt. Bu davranış olmadan okul listesi boş gelir.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Bos_deger_okul_sayilir(string? name)
    {
        InstitutionNodeType.Resolve(name).ShouldBe(InstitutionNodeType.School);
    }

    /// <summary>
    /// Tanınmayan değer de en DAR okumaya düşer. Province sayılsaydı, bozuk tek bir satır
    /// kendine bir alt ağaç uydururdu.
    /// </summary>
    [Fact]
    public void Taninmayan_deger_okul_sayilir()
    {
        InstitutionNodeType.Resolve("Bakanlik").ShouldBe(InstitutionNodeType.School);
    }

    [Fact]
    public void Entity_dugum_tipini_stok_alandan_hesaplar()
    {
        var entity = new InstitutionRecord { FullName = "Test" };

        entity.NodeType.ShouldBe(InstitutionNodeType.School);

        entity.NodeTypeName = InstitutionNodeType.Province.Name;
        entity.NodeType.ShouldBe(InstitutionNodeType.Province);
    }

    [Fact]
    public void Eski_kayit_dto_ya_okul_olarak_cikar()
    {
        var entity = new InstitutionRecord
        {
            Id = Guid.NewGuid(),
            InstitutionCode = 967523,
            FullName = "Atatürk MTAL"
        };

        var dto = entity.ToDto();

        dto.NodeType.ShouldBe("School");
        dto.NodeTypeSlug.ShouldBe(InstitutionNodeType.School.Slug);
        dto.ParentId.ShouldBeNull();
        dto.ParentName.ShouldBeNull();
    }

    [Fact]
    public void Ust_dugum_adi_disaridan_verilir()
    {
        var entity = new InstitutionRecord
        {
            Id = Guid.NewGuid(),
            InstitutionCode = 967523,
            FullName = "Atatürk MTAL",
            ParentId = Guid.NewGuid(),
            NodeTypeName = InstitutionNodeType.School.Name
        };

        entity.ToDto(parentName: "Yenimahalle İlçe Millî Eğitim Müdürlüğü")
            .ParentName.ShouldBe("Yenimahalle İlçe Millî Eğitim Müdürlüğü");
    }
}
```

- [ ] **Step 2: Testin başarısız olduğunu doğrula**

Run: `dotnet test tests/MESNET.Institution.UnitTests --filter FullyQualifiedName~InstitutionNodeTypeTests`
Expected: FAIL — `error CS0246: The type or namespace name 'InstitutionNodeType' could not be found`

- [ ] **Step 3: `InstitutionNodeType` yaz**

`src/Modules/Institution/MESNET.Institution.Core/Enums/InstitutionNodeType.cs`:

```csharp
using Ardalis.SmartEnum;

namespace MESNET.Institution.Core.Enums;

/// <summary>
/// Kurum ağacındaki düğümün tipi. İl müdürlüğü, ilçe müdürlüğü ve okul aynı belge tipinin
/// farklı tipleridir — kullanıcı–kurum bağı tek kural olarak kalsın diye (herkes bir kuruma
/// bağlanır, tipi ne olursa olsun).
///
/// <para><b>Bugün üretilen tip sayısı üçtür.</b> Ağacın sonsuz derinliği bedava bir yan
/// üründür, hedeflenen özellik değil: modellenen seviye il ve ilçedir (30.07.2026 kapsam
/// kararı — Bakanlık düzeyi aktör ve iller arası federasyon tasarlanmaz).</para>
/// </summary>
public sealed class InstitutionNodeType : SmartEnum<InstitutionNodeType>
{
    public static readonly InstitutionNodeType Province =
        new(nameof(Province), 1, "İl Millî Eğitim Müdürlüğü");

    public static readonly InstitutionNodeType District =
        new(nameof(District), 2, "İlçe Millî Eğitim Müdürlüğü");

    public static readonly InstitutionNodeType School =
        new(nameof(School), 3, "Okul");

    /// <summary>Türkçe arayüz etiketi. <see cref="SmartEnum{T}.Name"/> İngilizcedir ve serialize edilir.</summary>
    public string Slug { get; }

    private InstitutionNodeType(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }

    /// <summary>
    /// Saklanan adı düğüm tipine çevirir. Boş ve tanınmayan değer <see cref="School"/>'a düşer.
    ///
    /// <para><b>Boş neden okul:</b> mevcut kurum kayıtları bu alan olmadan saklandı ve hepsi
    /// okuldur. Başka bir şeye düşseydi, geçiş ucu koşturulana kadar okul listesi boş gelirdi —
    /// hata değil, sessiz boşluk.</para>
    ///
    /// <para><b>Tanınmayan da okul:</b> en dar okuma. <see cref="Province"/> sayılsaydı bozuk
    /// tek bir satır kendine bir alt ağaç uydururdu. Aynı gerekçe
    /// <c>InstitutionBrandPalette.Resolve</c> içinde de var.</para>
    /// </summary>
    public static InstitutionNodeType Resolve(string? name) =>
        !string.IsNullOrWhiteSpace(name)
        && TryFromName(name.Trim(), ignoreCase: true, out var found)
            ? found
            : School;
}
```

- [ ] **Step 4: `Institution` entity'sine ağaç alanlarını ekle**

`src/Modules/Institution/MESNET.Institution.Core/Entities/Institution.cs` — dosyanın en üstündeki `using` listesine ekle:

```csharp
using System.Text.Json.Serialization;
using MESNET.Institution.Core.Enums;
```

`public List<StaffMember> Staff { get; set; } = [];` satırının **hemen üstüne** şu bloğu ekle:

```csharp
    /// <summary>
    /// Üst düğüm. Kök (il müdürlüğü) için <c>null</c>. Okul için ilçe — ilçe bilgisi yoksa il.
    /// </summary>
    /// <remarks>
    /// Nullable, <c>required</c> DEĞİL: mevcut kayıtlar bu alan olmadan saklandı ve
    /// <c>required</c> System.Text.Json'ı her eski kurumda <c>JsonException</c> ile durdurur
    /// (aynı tuzak <see cref="ProvinceCode"/> ve <see cref="BrandPaletteName"/> yorumlarında).
    /// </remarks>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Düğüm tipinin <b>saklanan</b> hâli — <c>InstitutionNodeType.Name</c> değeri
    /// (<c>Province</c> / <c>District</c> / <c>School</c>).
    /// </summary>
    /// <remarks>
    /// <para><b>Neden düz string, neden SmartEnum değil:</b> Marten LINQ'te
    /// <c>i.NodeType.Name</c> SQL'e <c>data->'nodeType'->>'Name'</c> çevrilir; SmartEnum ise
    /// JSON'a düz string yazılır, nesne değil. Sorgu HER ZAMAN NULL döner ve hiçbir şey
    /// bulmaz — derleyici de test de bunu göremez. Bu yüzden stok alan tek ve düzdür; tip
    /// <see cref="NodeType"/> ile ondan hesaplanır.</para>
    ///
    /// <para><c>null</c> = geçiş koşturulmamış eski kayıt → <b>okul</b> sayılır.</para>
    /// </remarks>
    public string? NodeTypeName { get; set; }

    /// <summary>
    /// Kökten kendisine kimlik zinciri; <b>daima <c>/</c> ile başlar ve <c>/</c> ile biter</b>:
    /// <c>/{ilId}/{ilçeId}/{okulId}/</c>. Biçimin tek otoritesi
    /// <c>MESNET.Common.Shared.Security.InstitutionPath</c>'tir.
    /// </summary>
    /// <remarks>
    /// <para><b>Kimliklerden kurulur, adlardan DEĞİL</b> — ilçe adı düzeltildiğinde yol
    /// bozulmamalıdır.</para>
    ///
    /// <para><b>Sondaki ayraç süs değil:</b> onsuz <c>/33/1</c> öneki <c>/33/10...</c> yolunu
    /// da yakalar ve bir ilçe yetkilisi kardeş ilçeyi görür.</para>
    ///
    /// <para><c>null</c> = geçiş ucu (<c>POST /api/institutions/rebuild-hierarchy</c>) bu kayıt
    /// için henüz koşmadı. Kapsam kararı o durumda kimlik eşitliğine düşer, yani bugünkü
    /// davranış korunur.</para>
    /// </remarks>
    public string? Path { get; set; }

    /// <summary>
    /// Düğüm tipi. <see cref="NodeTypeName"/>'den hesaplanır ve <b>serialize edilmez</b> —
    /// tek stok alan olsun ki ikisi ayrışamasın.
    /// </summary>
    [JsonIgnore]
    public InstitutionNodeType NodeType => InstitutionNodeType.Resolve(NodeTypeName);

```

- [ ] **Step 5: Marten indekslerini ekle**

`src/Modules/Institution/MESNET.Institution.Persistence/InstitutionMartenConfig.cs` — `options.Schema.For<Core.Entities.Institution>().DatabaseSchemaName("institution");` satırının hemen ardına ekle:

```csharp
        // Kurum ağacı (#il/ilçe kapsam katmanı). İsimler ELLE verilir: PostgreSQL tanımlayıcı
        // sınırı 64 karakter ve Marten'in otomatik adı (mt_doc_institution_idx_...) bunu aşar.
        //
        // NOT — Path indeksi düz btree'dir. PostgreSQL bunu `LIKE 'önek%'` için ancak C
        // collation ya da text_pattern_ops opclass'ıyla kullanır; varsayılan collation'da
        // planlayıcı seq scan seçebilir. Kurum sayısı (okul + il + ilçe) bu ölçekte üç haneli
        // olduğu için A parçasında bedeli ölçülemez; opclass gerekirse elle DDL ile eklenir.
        options.Schema.For<Core.Entities.Institution>()
            .Index(x => x.Path, x => x.Name = "idx_institution_path");
        // ParentId nullable'dır (Guid?). Marten nullable alanda indeks kurmayı reddederse
        // (sürüm farkı) bu satırı kaldırıp yerine ham DDL koymayın — indeks A parçasında
        // ZORUNLU DEĞİL: ParentId yalnız isteğe bağlı ?parentId= süzgecinde kullanılır ve
        // kurum sayısı üç hanelidir. Kaldırdıysanız gerekçesini buraya yazın.
        options.Schema.For<Core.Entities.Institution>()
            .Index(x => x.ParentId, x => x.Name = "idx_institution_parent");
        options.Schema.For<Core.Entities.Institution>()
            .Index(x => x.NodeTypeName, x => x.Name = "idx_institution_node_type");
```

- [ ] **Step 6: DTO'ya ağaç alanlarını ekle**

`src/Modules/Institution/MESNET.Institution.Application/Dtos/InstitutionDto.cs` — `InstitutionDto` record'unda `string? DistrictName,` satırının hemen ardına ekle:

```csharp
    // Kurum ağacı. NodeType saklanan İngilizce anahtar (istemci mantığı buna bakar),
    // NodeTypeSlug Türkçe etiket (gösterim). ParentName üst düğümün adıdır ve sorgu tarafında
    // çözülür — entity onu bilmez, mapping saf kalır.
    string NodeType,
    string NodeTypeSlug,
    Guid? ParentId,
    string? ParentName,
```

- [ ] **Step 7: Mapping'i güncelle**

`src/Modules/Institution/MESNET.Institution.Application/Extensions/InstitutionMappingExtensions.cs` — `ToDto` metodunu şununla değiştir:

```csharp
    /// <param name="parentName">
    /// Üst düğümün adı. Entity onu bilmez (yalnız <c>ParentId</c> tutar) ve bu uzantı saf
    /// kalmalıdır — bir session açıp okumaz. Sorgu tarafı üst düğümleri <b>toplu</b> okur
    /// (<c>LoadManyAsync</c>) ve buraya geçirir; aksi hâlde her satır için bir okuma olurdu.
    /// </param>
    public static InstitutionDto ToDto(this Core.Entities.Institution entity, string? parentName = null)
    {
        // Saklanan anahtar burada palete çözülür. Null (hiç seçim yapılmamış) ve tanınmayan
        // değer aynı yere, varsayılana düşer — arayüz her zaman geçerli bir tema alır.
        var palette = InstitutionBrandPalette.Resolve(entity.BrandPaletteName);

        // Aynı disiplin düğüm tipinde de geçerli: null (geçiş koşmamış eski kayıt) ve tanınmayan
        // değer en dar okumaya, School'a düşer.
        var nodeType = entity.NodeType;

        return new InstitutionDto(
            entity.Id,
            entity.InstitutionCode,
            entity.FullName,
            entity.Address,
            entity.PhoneNumber,
            entity.Email,
            entity.WebUrl,
            entity.Location,
            entity.ProvinceCode,
            TurkishProvinces.GetName(entity.ProvinceCode),
            entity.DistrictName,
            nodeType.Name,
            nodeType.Slug,
            entity.ParentId,
            parentName,
            palette.Name,
            palette.Slug,
            palette.Primary,
            palette.Secondary,
            entity.Branches.Select(b => b.ToDto()).ToList(),
            entity.Staff.Select(s => s.ToDto()).ToList());
    }
```

- [ ] **Step 8: Sorgu süzgeci uzantısını yaz**

`src/Modules/Institution/MESNET.Institution.Application/Extensions/InstitutionQueryExtensions.cs`:

```csharp
using MESNET.Institution.Core.Enums;
using InstitutionRecord = MESNET.Institution.Core.Entities.Institution;

namespace MESNET.Institution.Application.Extensions;

/// <summary>
/// Kurum ağacında düğüm tipine göre süzme — <b>tek yer</b>.
///
/// <para><b>Neden uzantı, neden elle <c>Where</c> değil:</b> <c>Institution</c> artık "okul"
/// demek değil. Okul listesi üreten her sorgu tipe göre süzmek zorundadır ve süzmeyen sorgu
/// il/ilçe müdürlüğünü <b>okul sanar</b> — bu sessizce olur: açılır listede bir MEM adı belirir,
/// kimse hata görmez. Kuralı tek fonksiyona bağlamak
/// <c>InstitutionNodeTypeDriftTests</c>'in taranabilir bir hedefi olmasını sağlar.</para>
/// </summary>
public static class InstitutionQueryExtensions
{
    /// <summary>
    /// Verilen düğüm tipine daraltır.
    ///
    /// <para><b>Okul sorgusu boş <c>NodeTypeName</c>'i de kapsar</b> — geçiş ucu koşturulmamış
    /// kayıtların hepsi okuldur. Kapsamasaydı okul listesi dağıtımdan sonra boş gelirdi: hata
    /// değil, sessiz boşluk.</para>
    ///
    /// <para>Karşılaştırma düz <c>NodeTypeName</c> alanına yapılır. SmartEnum özelliği
    /// (<c>i.NodeType.Name</c>) Marten'de <c>data->'nodeType'->>'Name'</c> üretir ve HER ZAMAN
    /// NULL döner.</para>
    /// </summary>
    public static IQueryable<InstitutionRecord> OfNodeType(
        this IQueryable<InstitutionRecord> queryable, InstitutionNodeType nodeType)
    {
        var name = nodeType.Name;

        if (nodeType == InstitutionNodeType.School)
            return queryable.Where(i => i.NodeTypeName == null || i.NodeTypeName == name);

        return queryable.Where(i => i.NodeTypeName == name);
    }
}
```

- [ ] **Step 9: Testlerin geçtiğini doğrula**

Run: `dotnet build src/Modules/Institution/MESNET.Institution.Application && dotnet test tests/MESNET.Institution.UnitTests --filter FullyQualifiedName~InstitutionNodeTypeTests`
Expected: PASS (9 test)

Not: çözümün tamamı hâlâ derlenmez (Task 1'in kaldırdığı `CanAccess` / `VisibleInstitutionFilter` çağrıları Task 3 ve Task 5'te düzeltilir). Bu görevde yalnız yukarıdaki iki proje derlenir.

- [ ] **Step 10: Commit**

```bash
git add src/Modules/Institution/MESNET.Institution.Core/Enums/InstitutionNodeType.cs \
        src/Modules/Institution/MESNET.Institution.Core/Entities/Institution.cs \
        src/Modules/Institution/MESNET.Institution.Persistence/InstitutionMartenConfig.cs \
        src/Modules/Institution/MESNET.Institution.Application/Dtos/InstitutionDto.cs \
        src/Modules/Institution/MESNET.Institution.Application/Extensions/InstitutionMappingExtensions.cs \
        src/Modules/Institution/MESNET.Institution.Application/Extensions/InstitutionQueryExtensions.cs \
        tests/MESNET.Institution.UnitTests/InstitutionNodeTypeTests.cs
git commit -m "feat(institution): kurum belgesi ağaç düğümü oldu (ParentId, NodeTypeName, Path)

Düğüm tipi entity'de DÜZ STRING saklanır, SmartEnum ondan hesaplanır ve serialize
edilmez. Marten LINQ'te i.NodeType.Name SQL'e data->'nodeType'->>'Name' çevrilir;
SmartEnum JSON'a düz string yazıldığı için sorgu her zaman NULL döner ve hiçbir
şey bulmaz — derleyici de test de bunu görmez.

Resolve(null) = School: mevcut kayıtlar bu alan olmadan saklandı ve hepsi okul.
Başka bir şeye düşseydi geçiş ucu koşana kadar okul listesi boş gelirdi."
```

---

### Task 3: `institution_path` claim'i

Aktörün yolu neredeyse her kapsam kararında gerekir. Claim yapılırsa hiçbir sıcak yolda ek okuma olmaz; `institution_id` ile **birebir aynı disiplinden** geçer — token'daki değer her istekte silinir, kaynak yalnız kurum kaydıdır.

**Files:**
- Modify: `src/MESNET.Common.Shared/Security/UserContext.cs`
- Modify: `src/MESNET.Common.Infrastructure/Security/ICurrentUserService.cs`
- Modify: `src/MESNET.Common.Infrastructure/Security/CurrentUserService.cs`
- Modify: `src/MESNET.Common.Infrastructure/Security/PermissionClaimsTransformation.cs`
- Test: `tests/MESNET.Security.UnitTests/InstitutionClaimAuthorityTests.cs` (mevcut — yeni Fact eklenir)

**Interfaces:**
- Consumes: `InstitutionPath` (Task 1), `Institution.Path` alanı (Task 2).
- Produces:
  - `UserContext` yeni son parametresi: `string? InstitutionPath = null`
  - `ICurrentUserService.GetInstitutionPath()` → `string?`
  - Claim tipi sabiti: `"institution_path"`

- [ ] **Step 1: Testi yaz (kırmızı)**

`tests/MESNET.Security.UnitTests/InstitutionClaimAuthorityTests.cs` dosyasının **sonuna**, son `}` işaretinden önce ekle:

```csharp
    /// <summary>
    /// <c>institution_path</c> <b>kiracı anahtarının türevidir</b>, yani <c>institution_id</c>
    /// ile aynı disiplinden geçmelidir: token'dan gelen değer HİÇ kabul edilmez ve Keycloak'a
    /// hiçbir yerden YAZILMAZ.
    ///
    /// <para>Neden bu kadar katı: yol, aktörün göreceği ALT AĞACI belirler. Kullanıcının
    /// yazabildiği bir yol, kullanıcının kendi kapsamını seçmesi demektir — <c>/</c> yazan
    /// biri bütün okulları görürdü. Öznitelik Keycloak'ta <i>unmanaged</i>'dır; realm
    /// politikası yanlış kurulursa kullanıcı <c>manage-account</c> ile onu kendi yazar.</para>
    /// </summary>
    [Fact]
    public void Hicbir_kod_keycloaka_institution_path_oznitelig_yazmaz()
    {
        // Tarama deseni Hicbir_kod_keycloaka_institution_id_oznitelig_yazmaz ile AYNIDIR:
        // sözlük anahtarına ATAMA aranır, yorum satırları serbesttir (kararın nedenini
        // anlatan açıklamalar var).
        var sourceRoot = Path.Combine(RepoRoot(), "src");
        Directory.Exists(sourceRoot).ShouldBeTrue($"Kaynak klasörü bulunamadı: {sourceRoot}");

        var violations = new List<string>();

        foreach (var file in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                continue;

            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.TrimStart().StartsWith("//", StringComparison.Ordinal)) continue;

                var key = line.IndexOf("[\"institution_path\"]", StringComparison.Ordinal);
                if (key < 0) continue;
                if (line.IndexOf('=', key) < 0) continue;

                violations.Add($"{Path.GetRelativePath(RepoRoot(), file)}:{i + 1}");
            }
        }

        violations.ShouldBeEmpty(
            "institution_path Keycloak'a yazılıyor. Yol aktörün ALT AĞACINI belirler — "
            + "kullanıcının yazabildiği bir yol, kullanıcının kendi kapsamını seçmesi "
            + "demektir; kök yazan biri her okulu görürdü. Otorite Institution.Path "
            + "alanıdır.\n  " + string.Join("\n  ", violations));
    }

    /// <summary>
    /// Token'daki <c>institution_path</c> her istekte silinmelidir — kayıt boş olsa bile.
    /// "Kaynak yoksa token'a düş" davranışı, kaydı olmayan kullanıcıya kendi kapsamını
    /// seçtirirdi.
    /// </summary>
    [Fact]
    public void Tokendaki_institution_path_claimi_silinir()
    {
        var transformation = Path.Combine(
            RepoRoot(), "src", "MESNET.Common.Infrastructure", "Security",
            "PermissionClaimsTransformation.cs");

        File.Exists(transformation).ShouldBeTrue($"Dosya bulunamadı: {transformation}");

        File.ReadAllText(transformation).ShouldContain("RemoveInstitutionPathClaims",
            customMessage:
                "Token'daki institution_path claim'ini silen yol yok. Silinmezse "
                + "\"kaynak yoksa token'a düş\" davranışı geri gelir ve kaydı olmayan "
                + "kullanıcı kendi kapsamını seçer.");
    }
```

Not: `RepoRoot()` yardımcı metodu bu test sınıfında zaten vardır (dosyanın "Kurulum" bölümünde); yeniden **tanımlamayın**.

- [ ] **Step 2: Testin başarısız olduğunu doğrula**

Run: `dotnet test tests/MESNET.Security.UnitTests --filter FullyQualifiedName~InstitutionClaimAuthorityTests`
Expected: FAIL — `Token'daki institution_path claim'ini silen yol yok.`

- [ ] **Step 3: `UserContext`'e yol alanını ekle**

`src/MESNET.Common.Shared/Security/UserContext.cs` — son parametre olan `IReadOnlyList<Guid>? LinkedStudentIds = null);` satırını şununla değiştir:

```csharp
    IReadOnlyList<Guid>? LinkedStudentIds = null,
    /// <summary>
    /// Aktörün kurum ağacındaki yolu — <c>institution_path</c> claim'i. Kapsam kararının
    /// ağaç aşamasında kullanılır: hedefin yolu bununla başlıyorsa erişim vardır.
    ///
    /// <para><c>null</c> = geçiş ucu bu kullanıcının kurumu için henüz koşmadı. O durumda
    /// kapsam kimlik eşitliğine düşer, yani bugünkü davranış korunur.</para>
    ///
    /// <para><b>Kaynağı kurum kaydıdır, token DEĞİL</b> — <c>institution_id</c> ile aynı
    /// disiplin (ADR-0003 adım 2). Kullanıcının yazabildiği bir yol, kullanıcının kendi
    /// kapsamını seçmesi demektir.</para>
    /// </summary>
    string? InstitutionPath = null);
```

- [ ] **Step 4: `ICurrentUserService`'e okuyucu ekle**

`src/MESNET.Common.Infrastructure/Security/ICurrentUserService.cs` — `IReadOnlyList<Guid> GetLinkedStudentIds();` satırının ardına ekle:

```csharp

    /// <summary>
    /// Aktörün kurum ağacındaki yolu — <c>institution_path</c> claim'i.
    /// Kapsam kararı için kullanılır; erişim kararı için değil (o permission'ın işidir).
    /// Bilgi yoksa <c>null</c> döner ve kapsam kimlik eşitliğine düşer.
    /// </summary>
    string? GetInstitutionPath();
```

- [ ] **Step 5: `CurrentUserService`'i güncelle**

`src/MESNET.Common.Infrastructure/Security/CurrentUserService.cs`:

`var linkedStudentIds = LinkedStudentClaims.Read(user);` satırının ardına ekle:

```csharp
        var institutionPath = user.FindFirst("institution_path")?.Value;
```

`_cachedContext = new UserContext(...)` çağrısını şununla değiştir:

```csharp
        _cachedContext = new UserContext(
            userId, fullName, institutionId, businessId, studentId, roles, permissions,
            branchCodes, linkedStudentIds, institutionPath);
```

Sınıfın sonuna, `GetLinkedStudentIds()` metodunun ardına ekle:

```csharp
    public string? GetInstitutionPath()
    {
        return GetCurrentUser()?.InstitutionPath;
    }
```

- [ ] **Step 6: Claim'i sunucu tarafında üret**

`src/MESNET.Common.Infrastructure/Security/PermissionClaimsTransformation.cs`:

**(a)** `InstitutionLookupSql` sabitinin ardına yeni bir sorgu ekle:

```csharp
    /// <summary>
    /// Kurum ağacındaki yolu kurum kimliğinden bulan raw SQL.
    ///
    /// <para>Modül entity referansı kullanmaz — schema izolasyonuna uyar; aynı gerekçeyle
    /// <c>InstitutionLookupSql</c> de burada.</para>
    ///
    /// <para><c>NULL</c> dönebilir: geçiş ucu (<c>POST /api/institutions/rebuild-hierarchy</c>)
    /// bu kayıt için henüz koşmamıştır. Bu bir hata DEĞİLDİR; kapsam kimlik eşitliğine düşer.</para>
    /// </summary>
    private const string InstitutionPathLookupSql = """
        SELECT data->>'path' AS path
        FROM institution.mt_doc_institution
        WHERE data->>'id' = @institutionId
        LIMIT 1
        """;

    /// <summary>Kurum ağacındaki yol claim'i. Otoritesi <c>Institution.Path</c> alanıdır.</summary>
    private const string InstitutionPathClaimType = "institution_path";
```

**(b)** `EnrichInstitutionClaimAsync` metodunun **tamamını** şununla değiştir (gövde aynı kalır, yalnız çözülen kimlik bir değişkende toplanır ve sonunda yol claim'i eklenir):

```csharp
    private async Task EnrichInstitutionClaimAsync(
        ClaimsPrincipal principal, string keycloakUserId, Guid? accountInstitutionId)
    {
        if (principal.Identity is not ClaimsIdentity identity)
            return;

        // Token'daki değer HER DURUMDA atılır — sunucu tarafı kaynak bulunamasa bile.
        // Aksi hâlde "kaynak yoksa token'a düş" davranışı geri gelirdi. Yol claim'i de aynı
        // kurala tabidir: yol aktörün göreceği ALT AĞACI belirler, yani kullanıcının
        // yazabildiği bir yol kendi kapsamını seçmesi demektir.
        RemoveInstitutionClaims(principal);
        RemoveInstitutionPathClaims(principal);

        var resolvedInstitutionId = await ResolveInstitutionIdAsync(
            identity, keycloakUserId, accountInstitutionId);

        if (resolvedInstitutionId is null)
            return;

        await EnrichInstitutionPathClaimAsync(identity, resolvedInstitutionId);
    }

    /// <summary>
    /// Kurum kimliğini çözer ve claim olarak ekler; çözülen değeri döndürür.
    /// Sıra: (1) kullanıcı kaydı — otorite, (2) personel kaydı yedeği — geçiş adımı.
    /// </summary>
    private async Task<string?> ResolveInstitutionIdAsync(
        ClaimsIdentity identity, string keycloakUserId, Guid? accountInstitutionId)
    {
        // (1) Kullanıcı kaydı — kiracı anahtarının otoritesi.
        if (accountInstitutionId is { } institution && institution != Guid.Empty)
        {
            var value = institution.ToString();
            identity.AddClaim(new Claim("institution_id", value));
            return value;
        }

        // (2) Personel kaydı yedeği — mevcut kullanıcılar için geçiş adımı.
        var cacheKey = $"user-institution:{keycloakUserId}";

        if (!_cache.TryGetValue(cacheKey, out string? institutionId))
        {
            institutionId = await LookupInstitutionIdAsync(keycloakUserId);

            // ── SONUÇSUZ ARAMA ÖNBELLEĞE ALINMAZ ──
            //
            // Eskiden boş sonuç da CacheDuration boyunca saklanıyordu ve bu, geçici bir
            // durumu 5 dakikalık bir kesintiye çeviriyordu: kullanıcı personel kaydına
            // eklendikten SONRA bile kapsamsız kalıyordu, çünkü önbellekte "kurumu yok"
            // yazıyordu.
            if (!string.IsNullOrEmpty(institutionId))
                _cache.Set(cacheKey, institutionId, CacheDuration);
            else
                WarnScopeless(keycloakUserId);
        }

        if (string.IsNullOrEmpty(institutionId))
            return null;

        identity.AddClaim(new Claim("institution_id", institutionId));
        return institutionId;
    }

    /// <summary>
    /// Kurum ağacındaki yolu claim olarak ekler.
    ///
    /// <para><b>Yolun boş olması hata değildir</b> — geçiş ucu o kurum için henüz koşmamış
    /// olabilir. Claim eklenmez ve kapsam kararı kimlik eşitliğine düşer, yani bugünkü
    /// davranış korunur. Uyarı da basılmaz: geçiş öncesi bu <b>normal</b> durumdur ve her
    /// istekte log üretmek uyarıyı görünmez yapardı.</para>
    /// </summary>
    private async Task EnrichInstitutionPathClaimAsync(ClaimsIdentity identity, string institutionId)
    {
        var cacheKey = $"institution-path:{institutionId}";

        if (!_cache.TryGetValue(cacheKey, out string? path))
        {
            path = await LookupInstitutionPathAsync(institutionId);

            // Boş sonuç önbelleğe ALINMAZ: geçiş ucu koşturulduğu anda yol doğar ve
            // kullanıcının 5 dakika daha kapsamsız kalması için bir neden yoktur
            // (institution_id yedeğiyle aynı gerekçe).
            if (!string.IsNullOrEmpty(path))
                _cache.Set(cacheKey, path, CacheDuration);
        }

        if (!string.IsNullOrEmpty(path))
            identity.AddClaim(new Claim(InstitutionPathClaimType, path));
    }

    private async Task<string?> LookupInstitutionPathAsync(string institutionId)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var store = scope.ServiceProvider.GetService<Marten.IDocumentStore>();
            if (store is null)
                return null;

            var conn = store.Storage.Database.CreateConnection();
            await conn.OpenAsync();
            await using (conn)
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = InstitutionPathLookupSql;
                cmd.Parameters.Add(new NpgsqlParameter("institutionId", institutionId));

                var result = await cmd.ExecuteScalarAsync();
                if (result is string path && !string.IsNullOrEmpty(path))
                    return path;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Institution path lookup hatası: {InstitutionId}", institutionId);
        }

        return null;
    }

    /// <summary>
    /// Token'dan gelen <c>institution_path</c> claim'lerini siler.
    ///
    /// <para><b>Tüm</b> identity'ler taranır, yalnız birincil olan değil: okuma tarafı
    /// <c>ClaimsPrincipal.FindFirst</c> kullanır ve bütün identity'lerdeki claim'leri görür
    /// (<see cref="RemoveInstitutionClaims"/> ile aynı gerekçe).</para>
    /// </summary>
    private void RemoveInstitutionPathClaims(ClaimsPrincipal principal)
    {
        foreach (var identity in principal.Identities)
        {
            var existing = identity.FindAll(InstitutionPathClaimType).ToList();

            foreach (var claim in existing)
            {
                if (identity.TryRemoveClaim(claim))
                    continue;

                _logger.LogWarning(
                    "Token'daki institution_path claim'i kaldırılamadı: {ClaimValue}. " +
                    "Kapsam kararı yine de kurum kaydından verilir.", claim.Value);
            }
        }
    }
```

- [ ] **Step 7: Testlerin geçtiğini doğrula**

Run: `dotnet build src/MESNET.Common.Infrastructure && dotnet test tests/MESNET.Security.UnitTests --filter FullyQualifiedName~InstitutionClaimAuthorityTests`
Expected: PASS

- [ ] **Step 8: Commit**

```bash
git add src/MESNET.Common.Shared/Security/UserContext.cs \
        src/MESNET.Common.Infrastructure/Security/ICurrentUserService.cs \
        src/MESNET.Common.Infrastructure/Security/CurrentUserService.cs \
        src/MESNET.Common.Infrastructure/Security/PermissionClaimsTransformation.cs \
        tests/MESNET.Security.UnitTests/InstitutionClaimAuthorityTests.cs
git commit -m "feat(security): institution_path claim'i kurum kaydından üretilir

Aktörün ağaçtaki yolu neredeyse her kapsam kararında gerekiyor; claim yapılınca
sıcak yolda ek okuma kalmıyor. institution_id ile aynı disiplin: token'daki değer
her istekte silinir, Keycloak'a yazılmaz.

Yol aktörün göreceği ALT AĞACI belirler — kullanıcının yazabildiği bir yol,
kullanıcının kendi kapsamını seçmesi demektir. Kök yazan biri her okulu görürdü.

Yolun boş olması hata değil: geçiş ucu o kurum için henüz koşmamıştır ve kapsam
kimlik eşitliğine düşer."
```

---

### Task 4: Guard middleware'i yola duyarlı yap

**Files:**
- Modify: `src/Modules/Institution/MESNET.Institution.Application/Security/InstitutionScopeGuardMiddleware.cs`
- Test: `tests/MESNET.Institution.UnitTests/InstitutionScopeGuardShapeTests.cs` (yeni)

**Interfaces:**
- Consumes: `InstitutionScopePolicy.Decide` / `CanAccessByPath` (Task 1), `Institution.Path` (Task 2), `ICurrentUserService.GetInstitutionPath()` (Task 3).
- Produces: `InstitutionScopeGuardMiddleware.BeforeAsync(IInstitutionScoped message, ICurrentUserService currentUser, IQuerySession session)`

- [ ] **Step 1: Testi yaz (kırmızı)**

`tests/MESNET.Institution.UnitTests/InstitutionScopeGuardShapeTests.cs`:

```csharp
using System.Reflection;
using MESNET.Institution.Application.Security;
using Shouldly;
using Xunit;

namespace MESNET.Institution.UnitTests;

/// <summary>
/// Guard'ın <b>şeklini</b> kilitler. Karar saf <c>InstitutionScopePolicy</c>'dedir ve orası
/// birim testiyle kapalı; burada kilitlenen şey, guard'ın o karara <b>ağaç aşamasını da</b>
/// sorabilecek girdiye sahip olması.
///
/// <para><b>Neden şekil testi:</b> guard bir Wolverine middleware'idir — bağımlılıklarını
/// imzasından alır. <c>IQuerySession</c> imzadan düşerse hedefin yolu okunamaz ve kod yine
/// derlenir: kapsam sessizce kimlik eşitliğine geriler, yani il yetkilisi hiçbir okulun
/// kaydını açamaz. Derleyici bunu görmez, entegrasyon testi olmadan da görülmez.</para>
/// </summary>
public sealed class InstitutionScopeGuardShapeTests
{
    private static MethodInfo GuardMethod() =>
        typeof(InstitutionScopeGuardMiddleware)
            .GetMethod("BeforeAsync", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException(
                "InstitutionScopeGuardMiddleware.BeforeAsync bulunamadı. Guard senkron "
                + "kaldıysa hedefin yolu okunamaz ve kapsam kimlik eşitliğine geriler.");

    [Fact]
    public void Guard_asenkrondur()
    {
        GuardMethod().ReturnType.ShouldBe(typeof(Task));
    }

    [Fact]
    public void Guard_hedefin_yolunu_okuyabilecek_girdiye_sahiptir()
    {
        var parameterTypes = GuardMethod().GetParameters().Select(p => p.ParameterType.Name).ToList();

        parameterTypes.ShouldContain("IInstitutionScoped");
        parameterTypes.ShouldContain("ICurrentUserService");
        parameterTypes.ShouldContain(
            "IQuerySession",
            customMessage: "Guard hedefin Path alanını okuyamıyor; il/ilçe yetkilisi kendi "
                         + "alt ağacındaki okulun kaydını açamaz.");
    }

    /// <summary>
    /// Eski senkron giriş noktası kalmamalı: Wolverine ikisini de bulursa hangisinin
    /// koşacağı belirsizleşir ve yanlışlıkla dar olan seçilebilir.
    /// </summary>
    [Fact]
    public void Eski_senkron_giris_noktasi_kalmaz()
    {
        typeof(InstitutionScopeGuardMiddleware)
            .GetMethod("Before", BindingFlags.Public | BindingFlags.Static)
            .ShouldBeNull();
    }
}
```

- [ ] **Step 2: Testin başarısız olduğunu doğrula**

Run: `dotnet test tests/MESNET.Institution.UnitTests --filter FullyQualifiedName~InstitutionScopeGuardShapeTests`
Expected: FAIL — `InstitutionScopeGuardMiddleware.BeforeAsync bulunamadı.`

- [ ] **Step 3: Guard'ı yeniden yaz**

`src/Modules/Institution/MESNET.Institution.Application/Security/InstitutionScopeGuardMiddleware.cs` dosyasının **tamamını** şununla değiştir:

```csharp
using Marten;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Institution.Application.Errors;

namespace MESNET.Institution.Application.Security;

/// <summary>
/// Kurum kapsamı guard'ı (ADR-0003 adım 6 + kurum hiyerarşisi).
/// <see cref="IInstitutionScoped"/> taşıyan her command/query'den önce çalışır: aktörün
/// kapsamı hedefi içermiyorsa <see cref="DomainException"/> fırlatır (HTTP 422).
///
/// <para><b>Karar burada değil, saf <see cref="InstitutionScopePolicy"/> içindedir</b>; burası
/// yalnız girdileri toplar ve gerekiyorsa hedefin yolunu okur. Aynı ayrım
/// <c>BranchScopeGuard</c>'da da var.</para>
///
/// <para><b>Sıcak yolda ek okuma YOKTUR.</b> Okul kullanıcısının kendi kurumuna erişiminde
/// aktör ve hedef kimlikleri eşittir; karar <see cref="InstitutionScopePolicy.Decide"/>
/// içinde biter ve veritabanına hiç gidilmez. Hedefin yolu yalnız kimlikler ayrıştığında —
/// yani yeni il/ilçe yeteneği kullanıldığında — okunur.</para>
///
/// <para><b>Okumada da çalışır</b> — alan kapsamının aksine. Alan şefinin başka alanın
/// dağıtımını görmesi bilinçli olarak açıktı; başka <i>okulun</i> kaydını görmek değildir.
/// Ölçüldü: kontrol yokken bir okul müdürü diğer okulun <b>personel listesini</b> okuyordu.</para>
/// </summary>
public static class InstitutionScopeGuardMiddleware
{
    public static async Task BeforeAsync(
        IInstitutionScoped message, ICurrentUserService currentUser, IQuerySession session)
    {
        var actor = currentUser.GetCurrentUser();
        var hasPlatformScope = currentUser.HasPermission(Permissions.Platform.TenantManage);

        var outcome = InstitutionScopePolicy.Decide(
            actor?.InstitutionId, message.InstitutionId, hasPlatformScope);

        if (outcome == InstitutionScopeOutcome.Allowed)
            return;

        if (outcome == InstitutionScopeOutcome.NeedsPathCheck)
        {
            var target = await session
                .LoadAsync<Core.Entities.Institution>(message.InstitutionId);

            // Var olmayan hedef reddedilir, "bulunamadı" DENMEZ: kapsamı olmayan bir aktöre
            // hangi kimliklerin var olduğunu doğrulatmak, kurum listesini tahminle taramanın
            // kapısını açar. Aynı gerekçe InstitutionErrors.InstitutionScopeDenied yorumunda.
            if (target is not null
                && InstitutionScopePolicy.CanAccessByPath(actor?.InstitutionPath, target.Path))
            {
                return;
            }
        }

        throw new DomainException(InstitutionErrors.InstitutionScopeDenied(message.InstitutionId));
    }
}
```

- [ ] **Step 4: Testlerin geçtiğini doğrula**

Run: `dotnet build src/Modules/Institution/MESNET.Institution.Application && dotnet test tests/MESNET.Institution.UnitTests --filter FullyQualifiedName~InstitutionScopeGuardShapeTests`
Expected: PASS (3 test)

- [ ] **Step 5: Commit**

```bash
git add src/Modules/Institution/MESNET.Institution.Application/Security/InstitutionScopeGuardMiddleware.cs \
        tests/MESNET.Institution.UnitTests/InstitutionScopeGuardShapeTests.cs
git commit -m "feat(institution): kapsam guard'ı ağaca bakar, sıcak yolda ek okuma yok

Karar üç sonuçlu: aktör == hedef ise veritabanına hiç gidilmez. Hedefin Path
alanı yalnız kimlikler ayrıştığında — yani yeni il/ilçe yeteneği kullanıldığında —
okunur.

Var olmayan hedef 'bulunamadı' değil kapsam reddi döner: kapsamı olmayan aktöre
hangi kimliklerin var olduğunu doğrulatmak listeyi tahminle taramanın kapısıdır."
```

---

### Task 5: `GET /api/institutions` sayfalı, süzgeçli ve ağaç kapsamlı

Liste ucunun sayfalı olması zorunlu: bugün sayfasız `List<InstitutionDto>` dönüyor; tek okullu dünyada sorun değildi, il kapsamında yüzlerce satır demek. Aynı değişiklik **eksik `ORDER BY`** hatasını da kapatır — sıralamasız sorgu, Postgres güncellenen satırı heap'te yerinden oynattığı için iki çağrı arasında farklı sıra döndürüyordu.

**Files:**
- Modify: `src/Modules/Institution/MESNET.Institution.Application/Queries/GetInstitutions.cs`
- Modify: `src/Modules/Institution/MESNET.Institution.Application/Handlers/GetInstitutionsHandler.cs`
- Modify: `src/Modules/Institution/MESNET.Institution.Api/InstitutionEndpoints.cs`
- Test: `tests/MESNET.Api.Tests/Institution/InstitutionApiTests.cs` (mevcut liste testi)

**Interfaces:**
- Consumes: `InstitutionScopePolicy.VisibleScope` (Task 1), `InstitutionQueryExtensions.OfNodeType` + `InstitutionDto` yeni alanları (Task 2), `ICurrentUserService.GetInstitutionPath()` (Task 3).
- Produces:
  - `GetInstitutions : PagedQuery` — `string? NodeType = null`, `Guid? ParentId = null`
  - `GetInstitutionsHandler.Handle(...)` → `PagedResult<InstitutionDto>`
  - `GET /api/institutions?page&pageSize&sortBy&descending&search&nodeType&parentId`

- [ ] **Step 1: Sorgu record'unu genişlet**

`src/Modules/Institution/MESNET.Institution.Application/Queries/GetInstitutions.cs` dosyasının **tamamını** şununla değiştir:

```csharp
using MESNET.Common.Shared.Pagination;

namespace MESNET.Institution.Application.Queries;

/// <summary>
/// Görünür kurumların sayfalı listesi.
///
/// <para><b>Bu sorgu <c>IInstitutionScoped</c> OLAMAZ</b> — hedef kurum istekte geçmez,
/// sorulan zaten "hangi kurumlar". Kapsam bu yüzden guard'la değil <b>süzmeyle</b> uygulanır
/// (<c>InstitutionScopePolicy.VisibleScope</c>).</para>
/// </summary>
/// <param name="NodeType">
/// Düğüm tipi süzgeci — <c>Province</c> / <c>District</c> / <c>School</c>. Verilmezse
/// <b>okullar</b> döner: çağıranların ezici çoğunluğu okul listesi bekler ve varsayılan
/// süzgeçsiz olsaydı il/ilçe müdürlükleri açılır listelerde okul gibi görünürdü.
/// </param>
/// <param name="ParentId">Belirli bir düğümün doğrudan çocukları. Verilmezse tüm alt ağaç.</param>
public sealed record GetInstitutions(string? NodeType = null, Guid? ParentId = null) : PagedQuery;
```

- [ ] **Step 2: Handler'ı yeniden yaz**

`src/Modules/Institution/MESNET.Institution.Application/Handlers/GetInstitutionsHandler.cs` dosyasının **tamamını** şununla değiştir:

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
using InstitutionRecord = MESNET.Institution.Core.Entities.Institution;

namespace MESNET.Institution.Application.Handlers;

/// <summary>
/// Kurum listesi aktörün <b>alt ağacıyla</b> sınırlıdır (ADR-0003 adım 6 + kurum hiyerarşisi).
///
/// <para>Bu sorgu <c>IInstitutionScoped</c> olamaz — hedef kurum istekte geçmez, sorulan zaten
/// "hangi kurumlar". Kapsam bu yüzden guard'la değil <b>süzmeyle</b> uygulanır.</para>
///
/// <para><b>Neden önemli:</b> <c>Institution</c> belgesi kiracının kendisidir ve kiracı damgası
/// taşımaz, yani conjoined kiracılık bu listeyi süzmez. Ölçüldü: süzme yokken bir okulun müdürü
/// diğer okulu listede görüyordu; kimlikle devam edip kaydını ve personel listesini de
/// okuyabiliyordu.</para>
///
/// <para><b>Sıralama artık ZORUNLU.</b> Bu sorgunun <c>ORDER BY</c>'ı yoktu ve Postgres
/// güncellenen satırı heap'te yerinden oynattığı için sıra iki çağrı arasında değişiyordu.
/// Ölçüldü (27.08.2026): kurumu olmayan platform aktörü için "listenin ilk satırı" her
/// yazmadan sonra başka bir okuldu; yönetim ekranı paleti yanlış okula yazdı.</para>
/// </summary>
public static class GetInstitutionsHandler
{
    public static async Task<PagedResult<InstitutionDto>> Handle(
        GetInstitutions query,
        IQuerySession session,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var scope = InstitutionScopePolicy.VisibleScope(
            currentUser.GetCurrentUser()?.InstitutionId,
            currentUser.GetInstitutionPath(),
            currentUser.HasPermission(Permissions.Platform.TenantManage));

        IQueryable<InstitutionRecord> queryable = session.Query<InstitutionRecord>();

        queryable = ApplyScope(queryable, scope);

        // Varsayılan OKUL: çağıranların çoğu okul listesi bekler. Süzgeçsiz bırakılsaydı
        // il/ilçe müdürlükleri açılır listelerde okul gibi görünürdü — sessizce.
        queryable = queryable.OfNodeType(InstitutionNodeType.Resolve(query.NodeType));

        if (query.ParentId is { } parentId)
            queryable = queryable.Where(i => i.ParentId == parentId);

        queryable = ApplySearchTerm(queryable, query.Search);
        queryable = queryable.ApplySort(query.SortBy, query.Descending, defaultSort: i => i.FullName);

        var page = await queryable.ToPagedResultAsync(query, cancellationToken);
        var parentNames = await ResolveParentNamesAsync(session, page.Items, cancellationToken);

        return PagedResult<InstitutionDto>.Create(
            page.Items
                .Select(i => i.ToDto(i.ParentId is { } id && parentNames.TryGetValue(id, out var name) ? name : null))
                .ToList(),
            page.TotalCount,
            page.Page,
            page.PageSize);
    }

    /// <summary>
    /// Kapsam daraltması. Üç hâl vardır ve üçü de <see cref="InstitutionVisibility"/>'den gelir;
    /// karar burada TEKRARLANMAZ.
    /// </summary>
    private static IQueryable<InstitutionRecord> ApplyScope(
        IQueryable<InstitutionRecord> queryable, InstitutionVisibility scope)
    {
        if (scope.Unrestricted)
            return queryable;

        if (scope.PathPrefix is { } prefix)
        {
            // Marten string.StartsWith'i SQL'de LIKE 'önek%' çevirir; ham SQL ve
            // WITH RECURSIVE gerekmez. Yolu olmayan satır alt ağaçta DEĞİLDİR.
            return queryable.Where(i => i.Path != null && i.Path.StartsWith(prefix));
        }

        // Yol yok: kimliğe düş. Kapsamsız aktörde bu Guid.Empty'dir ve hiçbir kurumla
        // eşleşmez — her şeyi görmek yerine hiçbir şey görmek.
        var institutionId = scope.InstitutionId ?? Guid.Empty;
        return queryable.Where(i => i.Id == institutionId);
    }

    /// <summary>
    /// Ad ve kurum kodu araması.
    ///
    /// <para>Kod <c>int</c> olduğu için <c>ApplySearch</c> ile aranamaz (o yalnız string
    /// alanlarda çalışır). Terim sayıya çevrilebiliyorsa kodda <b>tam eşleşme</b> aranır:
    /// kurum kodu tam girilen bir kimliktir, parçası anlamlı değildir.</para>
    /// </summary>
    private static IQueryable<InstitutionRecord> ApplySearchTerm(
        IQueryable<InstitutionRecord> queryable, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return queryable;

        var term = search.Trim();

        if (int.TryParse(term, out var code))
            return queryable.Where(i => i.InstitutionCode == code);

        return queryable.ApplySearch(term, i => i.FullName);
    }

    /// <summary>
    /// Üst düğüm adlarını <b>toplu</b> okur. Satır başına okuma yapılsaydı 20 satırlık bir
    /// sayfa 21 sorgu ederdi (N+1).
    /// </summary>
    private static async Task<Dictionary<Guid, string>> ResolveParentNamesAsync(
        IQuerySession session, IReadOnlyList<InstitutionRecord> items, CancellationToken cancellationToken)
    {
        var parentIds = items
            .Select(i => i.ParentId)
            .OfType<Guid>()
            .Distinct()
            .ToList();

        if (parentIds.Count == 0)
            return [];

        var parents = await session.LoadManyAsync<InstitutionRecord>(cancellationToken, parentIds);

        return parents.ToDictionary(p => p.Id, p => p.FullName);
    }
}
```

- [ ] **Step 3: Ucu sayfalı hâle getir**

`src/Modules/Institution/MESNET.Institution.Api/InstitutionEndpoints.cs` — `GetAll` metodunu şununla değiştir:

```csharp
    /// <summary>
    /// Görünür kurumların sayfalı listesi. Kapsam sorgunun İÇİNDE uygulanır (handler);
    /// uçta kimlik karşılaştırması yapılmaz.
    /// </summary>
    /// <param name="nodeType">
    /// <c>Province</c> / <c>District</c> / <c>School</c>. Verilmezse okullar döner.
    /// </param>
    /// <param name="parentId">Belirli bir düğümün doğrudan çocukları.</param>
    private static async Task<IResult> GetAll(
        string? nodeType = null,
        Guid? parentId = null,
        int page = 1,
        int pageSize = 20,
        string? sortBy = null,
        bool descending = false,
        string? search = null,
        IMessageBus bus = default!)
    {
        var result = await bus.InvokeAsync<PagedResult<InstitutionDto>>(
            new GetInstitutions(nodeType, parentId)
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

Dosyanın en üstündeki `using` listesine ekle:

```csharp
using MESNET.Common.Shared.Pagination;
```

- [ ] **Step 4: Derle ve tüm birim testlerini koş**

Run: `dotnet build MESNET.slnx`
Expected: BUILD SUCCEEDED — Task 1'de kaldırılan API'nin son çağrı yeri de kapandı.

Run: `dotnet test tests/MESNET.Security.UnitTests tests/MESNET.Institution.UnitTests`
Expected: PASS

- [ ] **Step 5: API testini sayfalı yanıta uyarla**

`tests/MESNET.Api.Tests/Institution/InstitutionApiTests.cs` — `GET /api/institutions/` listesini doğrulayan mevcut testi bulun (dosyada 37. satır civarı). Yanıt gövdesi artık çıplak dizi değil `PagedResult` sarmalayıcısıdır. Testin gövde iddiasını şu şekilde güncelleyin (durum kodu iddiası **değişmez**):

```csharp
        // Yanıt artık PagedResult sarmalayıcısıdır: data.items + data.totalCount.
        // Çıplak dizi bekleyen bir iddia burada sessizce boş listeye düşerdi.
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("\"items\"");
        body.ShouldContain("\"totalCount\"");
```

Ayrıca aynı dosyada, düğüm tipi süzgecinin çalıştığını gösteren yeni bir test ekleyin:

```csharp
    /// <summary>
    /// Düğüm tipi süzgeci. <c>Institution</c> artık "okul" demek değil; süzgeç çalışmazsa
    /// il/ilçe müdürlükleri okul listesinde belirir ve bu SESSİZCE olur.
    /// </summary>
    [Fact]
    public async Task Kurum_listesi_dugum_tipine_gore_suzulur()
    {
        var response = await _fixture.Client.GetAsync("/api/institutions/?nodeType=Province");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("\"items\"");
    }
```

- [ ] **Step 6: Commit**

```bash
git add src/Modules/Institution/MESNET.Institution.Application/Queries/GetInstitutions.cs \
        src/Modules/Institution/MESNET.Institution.Application/Handlers/GetInstitutionsHandler.cs \
        src/Modules/Institution/MESNET.Institution.Api/InstitutionEndpoints.cs \
        tests/MESNET.Api.Tests/Institution/InstitutionApiTests.cs
git commit -m "feat(institution): kurum listesi sayfalı, düğüm tipine göre süzülüyor ve alt ağaçla sınırlı

Kapsam artık Path.StartsWith(aktörünYolu) — Marten bunu LIKE 'önek%' çevirir,
ham SQL ve WITH RECURSIVE gerekmez.

Sayfalama zorunluydu: uç sayfasız List<InstitutionDto> dönüyordu ve il kapsamında
bu yüzlerce satır demek. Aynı değişiklik eksik ORDER BY'ı da kapatır — sıralamasız
sorgu, Postgres güncellenen satırı heap'te oynattığı için iki çağrı arasında farklı
sıra döndürüyordu ve yönetim ekranı paleti yanlış okula yazmıştı.

Üst düğüm adları toplu okunur (LoadManyAsync), satır başına değil."
```

---

### Task 6: Geçiş — `POST /api/institutions/rebuild-hierarchy`

Ağacı mevcut okulların `ProvinceCode` / `DistrictName` alanlarından üretir. **Dağıtım ön koşuludur ve atlanırsa sessizdir:** yollar boş kalır, il yetkilisi hata değil **boş liste** görür.

Karar mantığı saf bir planlayıcıya alınır; handler yalnız planı uygular. İdempotanlık böylece veritabanı olmadan test edilir.

**Files:**
- Create: `src/Modules/Institution/MESNET.Institution.Core/Services/InstitutionHierarchyPlanner.cs`
- Create: `src/Modules/Institution/MESNET.Institution.Application/Commands/RebuildInstitutionHierarchy.cs`
- Create: `src/Modules/Institution/MESNET.Institution.Application/Handlers/RebuildInstitutionHierarchyHandler.cs`
- Modify: `src/Modules/Institution/MESNET.Institution.Api/InstitutionEndpoints.cs`
- Modify: `src/Modules/Institution/MESNET.Institution.Application/Services/InstitutionTenantDirectory.cs`
- Modify: `tests/MESNET.Security.UnitTests/InstitutionScopeDriftTests.cs` (muafiyet listesi)
- Modify: `src/Docs/docs/infrastructure/dagitim-on-kosullari.md`
- Test: `tests/MESNET.Institution.UnitTests/InstitutionHierarchyPlannerTests.cs` (yeni)

**Interfaces:**
- Consumes: `InstitutionPath` (Task 1), `InstitutionNodeType` + `InstitutionQueryExtensions.OfNodeType` (Task 2).
- Produces:
  - `MESNET.Institution.Core.Services.HierarchyNodeToCreate(Guid Id, Guid? ParentId, string NodeTypeName, string Path, string FullName, string ProvinceCode, string? DistrictName)`
  - `MESNET.Institution.Core.Services.HierarchyAssignment(Guid Id, Guid? ParentId, string NodeTypeName, string Path)`
  - `MESNET.Institution.Core.Services.HierarchyPlan(IReadOnlyList<HierarchyNodeToCreate> Created, IReadOnlyList<HierarchyAssignment> Assignments, IReadOnlyList<Guid> SkippedNoProvince)`
  - `MESNET.Institution.Core.Services.InstitutionHierarchyPlanner.Plan(IReadOnlyList<Institution> all, Func<Guid> newId)` → `HierarchyPlan`
  - `MESNET.Institution.Application.Commands.RebuildInstitutionHierarchy()`
  - `RebuildInstitutionHierarchyResult(int ProvincesCreated, int DistrictsCreated, int NodesUpdated, int SkippedNoProvince)`

- [ ] **Step 1: Planlayıcı testini yaz (kırmızı)**

`tests/MESNET.Institution.UnitTests/InstitutionHierarchyPlannerTests.cs`:

```csharp
using MESNET.Institution.Core.Enums;
using MESNET.Institution.Core.Services;
using Shouldly;
using Xunit;
using InstitutionRecord = MESNET.Institution.Core.Entities.Institution;

namespace MESNET.Institution.UnitTests;

/// <summary>
/// Ağacı mevcut okul künyelerinden kuran geçiş kararı.
///
/// <para><b>Neden saf bir planlayıcı, neden handler'ın içinde değil:</b> bu geçişin tek
/// kritik özelliği <b>idempotanlık</b>tır — ikinci koşu aynı ağacı üretmeli, düğüm
/// ÇOĞALTMAMALIDIR. Mantık handler'ın içinde kalsaydı bunu ancak veritabanına iki kez
/// yazarak sınayabilirdik; burada iki kez plan üretip karşılaştırmak yeter.</para>
/// </summary>
public sealed class InstitutionHierarchyPlannerTests
{
    private static int _counter;

    /// <summary>Deterministik kimlik üreteci — plan iki kez koşturulduğunda karşılaştırılabilsin.</summary>
    private static Func<Guid> Ids()
    {
        var n = 0;
        return () => Guid.Parse($"{++n:D8}-0000-0000-0000-000000000000");
    }

    private static InstitutionRecord Okul(
        string ad, string? il = "06", string? ilce = "Yenimahalle", Guid? id = null) =>
        new()
        {
            Id = id ?? Guid.Parse($"{++_counter:D8}-1111-1111-1111-111111111111"),
            InstitutionCode = 900000 + _counter,
            FullName = ad,
            ProvinceCode = il,
            DistrictName = ilce
        };

    [Fact]
    public void Bos_girdi_bos_plan_uretir()
    {
        var plan = InstitutionHierarchyPlanner.Plan([], Ids());

        plan.Created.ShouldBeEmpty();
        plan.Assignments.ShouldBeEmpty();
        plan.SkippedNoProvince.ShouldBeEmpty();
    }

    [Fact]
    public void Tek_okul_icin_il_ve_ilce_dugumu_uretilir()
    {
        var okul = Okul("Atatürk MTAL");

        var plan = InstitutionHierarchyPlanner.Plan([okul], Ids());

        plan.Created.Count.ShouldBe(2);
        plan.Created.Count(c => c.NodeTypeName == InstitutionNodeType.Province.Name).ShouldBe(1);
        plan.Created.Count(c => c.NodeTypeName == InstitutionNodeType.District.Name).ShouldBe(1);
    }

    [Fact]
    public void Okulun_yolu_uc_segmentlidir_ve_ayracla_baslar_biter()
    {
        var okul = Okul("Atatürk MTAL");

        var plan = InstitutionHierarchyPlanner.Plan([okul], Ids());
        var atama = plan.Assignments.Single(a => a.Id == okul.Id);

        atama.Path.ShouldStartWith("/");
        atama.Path.ShouldEndWith("/");
        atama.Path.Trim('/').Split('/').Length.ShouldBe(3);
        atama.NodeTypeName.ShouldBe(InstitutionNodeType.School.Name);
    }

    [Fact]
    public void Ayni_ilcedeki_iki_okul_tek_il_ve_tek_ilce_dugumu_paylasir()
    {
        var plan = InstitutionHierarchyPlanner.Plan(
            [Okul("Atatürk MTAL"), Okul("Cumhuriyet MTAL")], Ids());

        plan.Created.Count.ShouldBe(2);
    }

    [Fact]
    public void Ilcesiz_okul_dogrudan_il_altina_baglanir()
    {
        var okul = Okul("Merkez MTAL", ilce: null);

        var plan = InstitutionHierarchyPlanner.Plan([okul], Ids());

        plan.Created.Count.ShouldBe(1);
        plan.Created.Single().NodeTypeName.ShouldBe(InstitutionNodeType.Province.Name);
        plan.Assignments.Single(a => a.Id == okul.Id).Path.Trim('/').Split('/').Length.ShouldBe(2);
    }

    /// <summary>
    /// İl kodu olmayan okul <b>köke bağlanmaz</b>. Bağlansaydı, herhangi bir il yetkilisinin
    /// alt ağacına düşen sahipsiz bir kayıt olurdu. Kapsamsız kalır ve sayılır — sayı
    /// boşluğu görünür kılar.
    /// </summary>
    [Fact]
    public void Il_kodu_olmayan_okul_kapsamsiz_kalir_ve_sayilir()
    {
        var okul = Okul("Künyesiz MTAL", il: null, ilce: null);

        var plan = InstitutionHierarchyPlanner.Plan([okul], Ids());

        plan.Created.ShouldBeEmpty();
        plan.Assignments.ShouldBeEmpty();
        plan.SkippedNoProvince.ShouldBe([okul.Id]);
    }

    /// <summary>
    /// <b>İdempotanlık — bu geçişin tek kritik özelliği.</b> İlk planı uygulanmış gibi kabul
    /// edip ikinci kez planlarsak hiçbir düğüm ÜRETİLMEMELİ ve atamalar birebir aynı olmalı.
    /// </summary>
    [Fact]
    public void Ikinci_kosu_dugum_cogaltmaz_ve_ayni_agaci_uretir()
    {
        var okul = Okul("Atatürk MTAL");
        var ilkPlan = InstitutionHierarchyPlanner.Plan([okul], Ids());

        // İlk planı diske yazılmış gibi uygula.
        var uygulanmis = new List<InstitutionRecord> { okul };

        foreach (var yeni in ilkPlan.Created)
        {
            uygulanmis.Add(new InstitutionRecord
            {
                Id = yeni.Id,
                InstitutionCode = 0,
                FullName = yeni.FullName,
                ParentId = yeni.ParentId,
                NodeTypeName = yeni.NodeTypeName,
                Path = yeni.Path,
                ProvinceCode = yeni.ProvinceCode,
                DistrictName = yeni.DistrictName
            });
        }

        foreach (var atama in ilkPlan.Assignments)
        {
            var kayit = uygulanmis.Single(i => i.Id == atama.Id);
            kayit.ParentId = atama.ParentId;
            kayit.NodeTypeName = atama.NodeTypeName;
            kayit.Path = atama.Path;
        }

        var ikinciPlan = InstitutionHierarchyPlanner.Plan(uygulanmis, Ids());

        ikinciPlan.Created.ShouldBeEmpty("İkinci koşu düğüm çoğaltmamalı — geçiş idempotenttir.");
        ikinciPlan.Assignments.OrderBy(a => a.Id).ShouldBe(
            ilkPlan.Assignments.OrderBy(a => a.Id));
    }

    /// <summary>
    /// Bozulmuş bir yol ikinci koşuda ONARILIR. Atamalar yalnız "eksik" satırlara değil,
    /// bütün düğümlere yazılır; aksi hâlde elle bozulmuş tek bir satır kalıcı olurdu.
    /// </summary>
    [Fact]
    public void Bozulmus_yol_yeniden_kosuda_onarilir()
    {
        var okul = Okul("Atatürk MTAL");
        okul.NodeTypeName = InstitutionNodeType.School.Name;
        okul.Path = "/bozuk/";

        var plan = InstitutionHierarchyPlanner.Plan([okul], Ids());

        plan.Assignments.Single(a => a.Id == okul.Id).Path.ShouldNotBe("/bozuk/");
    }
}
```

- [ ] **Step 2: Testin başarısız olduğunu doğrula**

Run: `dotnet test tests/MESNET.Institution.UnitTests --filter FullyQualifiedName~InstitutionHierarchyPlannerTests`
Expected: FAIL — `error CS0246: The type or namespace name 'InstitutionHierarchyPlanner' could not be found`

- [ ] **Step 3: Planlayıcıyı yaz**

`src/Modules/Institution/MESNET.Institution.Core/Services/InstitutionHierarchyPlanner.cs`:

```csharp
using MESNET.Common.Shared.Reference;
using MESNET.Common.Shared.Security;
using MESNET.Institution.Core.Enums;
using InstitutionRecord = MESNET.Institution.Core.Entities.Institution;

namespace MESNET.Institution.Core.Services;

/// <summary>Kurulacak yeni üst düğüm (il ya da ilçe müdürlüğü).</summary>
public sealed record HierarchyNodeToCreate(
    Guid Id,
    Guid? ParentId,
    string NodeTypeName,
    string Path,
    string FullName,
    string ProvinceCode,
    string? DistrictName);

/// <summary>Var olan bir kayda yazılacak ağaç alanları.</summary>
public sealed record HierarchyAssignment(Guid Id, Guid? ParentId, string NodeTypeName, string Path);

/// <summary>
/// Geçiş planı.
/// </summary>
/// <param name="Created">Kurulacak yeni üst düğümler.</param>
/// <param name="Assignments">
/// <b>Bütün</b> düğümlere yazılacak ağaç alanları — yenilere de, var olanlara da. Yalnız eksik
/// satırlara yazılsaydı, elle bozulmuş bir yol kalıcı olurdu.
/// </param>
/// <param name="SkippedNoProvince">İl kodu olmadığı için kapsamsız bırakılan okullar.</param>
/// <remarks>
/// <b>Bilinen sınır:</b> hiçbir okulun referans vermediği bir üst düğüm (ör. son okulu
/// kapanmış bir ilçe müdürlüğü) atama listesine girmez ve yolu olduğu gibi kalır. Zararsızdır —
/// altında kimse yoktur — ama yolu bozulmuşsa bu koşu onu onarmaz.
/// </remarks>
public sealed record HierarchyPlan(
    IReadOnlyList<HierarchyNodeToCreate> Created,
    IReadOnlyList<HierarchyAssignment> Assignments,
    IReadOnlyList<Guid> SkippedNoProvince);

/// <summary>
/// Mevcut okul künyelerinden (<c>ProvinceCode</c> / <c>DistrictName</c>) kurum ağacını üretir.
///
/// <para><b>Saf — veritabanı bilmez.</b> Bu geçişin tek kritik özelliği idempotanlıktır ve
/// mantık handler'ın içinde kalsaydı bunu ancak iki kez yazarak sınayabilirdik.</para>
///
/// <para><b>İl kodu olmayan okul köke BAĞLANMAZ.</b> Bağlansaydı, herhangi bir il yetkilisinin
/// alt ağacına düşen sahipsiz bir kayıt olurdu. Kapsamsız kalır ve sayılır — sonuçtaki sayı
/// boşluğu görünür kılar (aynı desen <c>SyncUsersFromKeycloak</c>'un <c>WithoutInstitution</c>
/// sayısında da var).</para>
/// </summary>
public static class InstitutionHierarchyPlanner
{
    /// <summary>
    /// Üst düğümlerin kurum kodu. MEB müdürlüklerinin kendi kodları vardır ama bu geçişin
    /// elinde o veri yoktur ve <b>uydurulmuş bir kod gerçek veri gibi görünürdü</b> (aynı
    /// gerekçe <c>Institution.DistrictName</c> yorumunda ilçe kodu için de yazılı). Sıfır,
    /// "girilmedi" demektir; B parçasında bu düğümler düzenlenebilir olacak.
    /// </summary>
    public const int UnknownInstitutionCode = 0;

    public static HierarchyPlan Plan(IReadOnlyList<InstitutionRecord> all, Func<Guid> newId)
    {
        var created = new List<HierarchyNodeToCreate>();
        var assignments = new List<HierarchyAssignment>();
        var skipped = new List<Guid>();

        // Var olan üst düğümler. Anahtarlar künyeden gelir, addan değil: ilçe adı düzeltilse
        // bile aynı düğüm bulunur.
        var provinces = all
            .Where(i => i.NodeType == InstitutionNodeType.Province && !string.IsNullOrWhiteSpace(i.ProvinceCode))
            .GroupBy(i => i.ProvinceCode!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.OrderBy(i => i.Id).First().Id, StringComparer.OrdinalIgnoreCase);

        var districts = all
            .Where(i => i.NodeType == InstitutionNodeType.District
                        && !string.IsNullOrWhiteSpace(i.ProvinceCode)
                        && !string.IsNullOrWhiteSpace(i.DistrictName))
            .GroupBy(i => DistrictKey(i.ProvinceCode!, i.DistrictName!), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.OrderBy(i => i.Id).First().Id, StringComparer.OrdinalIgnoreCase);

        var schools = all
            .Where(i => i.NodeType == InstitutionNodeType.School)
            .OrderBy(i => i.Id)
            .ToList();

        // Yol, kimlikler çözüldükten SONRA kurulur; bu yüzden düğüm kimlikleri önce toplanır.
        var provincePaths = new Dictionary<string, (Guid Id, string Path)>(StringComparer.OrdinalIgnoreCase);
        var districtPaths = new Dictionary<string, (Guid Id, string Path)>(StringComparer.OrdinalIgnoreCase);

        foreach (var school in schools)
        {
            if (string.IsNullOrWhiteSpace(school.ProvinceCode))
            {
                skipped.Add(school.Id);
                continue;
            }

            var provinceCode = school.ProvinceCode.Trim();
            var province = EnsureProvince(provinceCode);

            var parent = province;

            if (!string.IsNullOrWhiteSpace(school.DistrictName))
                parent = EnsureDistrict(provinceCode, school.DistrictName.Trim(), province);

            assignments.Add(new HierarchyAssignment(
                school.Id,
                parent.Id,
                InstitutionNodeType.School.Name,
                InstitutionPath.Child(parent.Path, school.Id)));
        }

        return new HierarchyPlan(created, assignments, skipped);

        // ── yerel yardımcılar ──

        (Guid Id, string Path) EnsureProvince(string code)
        {
            if (provincePaths.TryGetValue(code, out var known))
                return known;

            var id = provinces.TryGetValue(code, out var existingId) ? existingId : newId();
            var path = InstitutionPath.Root(id);

            if (!provinces.ContainsKey(code))
            {
                created.Add(new HierarchyNodeToCreate(
                    id, null, InstitutionNodeType.Province.Name, path,
                    $"{TurkishProvinces.GetName(code) ?? code} İl Millî Eğitim Müdürlüğü",
                    code, null));
            }

            assignments.Add(new HierarchyAssignment(id, null, InstitutionNodeType.Province.Name, path));

            var node = (id, path);
            provincePaths[code] = node;
            return node;
        }

        (Guid Id, string Path) EnsureDistrict(string code, string name, (Guid Id, string Path) province)
        {
            var key = DistrictKey(code, name);

            if (districtPaths.TryGetValue(key, out var known))
                return known;

            var id = districts.TryGetValue(key, out var existingId) ? existingId : newId();
            var path = InstitutionPath.Child(province.Path, id);

            if (!districts.ContainsKey(key))
            {
                created.Add(new HierarchyNodeToCreate(
                    id, province.Id, InstitutionNodeType.District.Name, path,
                    $"{name} İlçe Millî Eğitim Müdürlüğü", code, name));
            }

            assignments.Add(new HierarchyAssignment(
                id, province.Id, InstitutionNodeType.District.Name, path));

            var node = (id, path);
            districtPaths[key] = node;
            return node;
        }

        // İlçe adı tek başına benzersiz DEĞİLDİR ("Merkez" 81 ilde var); anahtar daima
        // (il, ilçe) ikilisidir.
        static string DistrictKey(string provinceCode, string districtName) =>
            $"{provinceCode.Trim()}|{districtName.Trim()}";
    }
}
```

- [ ] **Step 4: Planlayıcı testlerinin geçtiğini doğrula**

Run: `dotnet test tests/MESNET.Institution.UnitTests --filter FullyQualifiedName~InstitutionHierarchyPlannerTests`
Expected: PASS (8 test)

- [ ] **Step 5: Komut ve handler'ı yaz**

`src/Modules/Institution/MESNET.Institution.Application/Commands/RebuildInstitutionHierarchy.cs`:

```csharp
namespace MESNET.Institution.Application.Commands;

/// <summary>
/// Kurum ağacını mevcut okul künyelerinden yeniden kurar. <b>İdempotent</b>: ikinci koşu aynı
/// ağacı üretir, düğüm çoğaltmaz.
///
/// <para>Kurum kimliği <b>taşımaz</b> — kurum üstü bir iştir ve kapsamı izniyle sınırlıdır
/// (<c>platform:tenant:manage</c>). <c>IInstitutionScoped</c> uygulanamaz: karşılaştırılacak
/// tek bir hedef yoktur.</para>
/// </summary>
public sealed record RebuildInstitutionHierarchy;

/// <param name="ProvincesCreated">Yeni kurulan il müdürlüğü düğümü sayısı.</param>
/// <param name="DistrictsCreated">Yeni kurulan ilçe müdürlüğü düğümü sayısı.</param>
/// <param name="NodesUpdated">Ağaç alanları yazılan düğüm sayısı (yeniler dahil).</param>
/// <param name="SkippedNoProvince">
/// İl kodu olmadığı için <b>kapsamsız</b> bırakılan okul sayısı. Sıfırdan büyükse o okullar
/// hiçbir il yetkilisinin listesinde görünmez — künyeleri tamamlanıp uç yeniden çağrılmalıdır.
/// </param>
public sealed record RebuildInstitutionHierarchyResult(
    int ProvincesCreated,
    int DistrictsCreated,
    int NodesUpdated,
    int SkippedNoProvince);
```

`src/Modules/Institution/MESNET.Institution.Application/Handlers/RebuildInstitutionHierarchyHandler.cs`:

```csharp
using Marten;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Core.Enums;
using MESNET.Institution.Core.Services;
using Microsoft.Extensions.Logging;
using InstitutionRecord = MESNET.Institution.Core.Entities.Institution;

namespace MESNET.Institution.Application.Handlers;

/// <summary>
/// Kurum ağacını kurar (dağıtım ön koşulu).
///
/// <para><b>Atlanırsa sessizdir.</b> Yollar boş kalır, <c>StartsWith</c> hiçbir şeyle
/// eşleşmez ve il yetkilisi hata değil <b>boş liste</b> görür. Bu yüzden
/// <c>src/Docs/docs/infrastructure/dagitim-on-kosullari.md</c> içinde zorunlu adım olarak
/// yazılıdır.</para>
///
/// <para><b>Olay yayınlamaz.</b> Ağaç alanları hiçbir modülün görünümünü beslemiyor; yayın
/// yalnız bütün tüketicileri boşuna uyandırırdı. B parçasında düğüm taşıma geldiğinde bu
/// karar yeniden değerlendirilir.</para>
///
/// <para><b>Kurum belgesini filtresiz dolaşır</b> — bu bilinçlidir ve
/// <c>InstitutionScopeDriftTests.MayEnumerateAll</c> listesinde gerekçesiyle yazılıdır:
/// ağacı kurmak tanımı gereği bütün düğümleri görmeyi gerektirir ve uç kurum üstü izinle
/// korunur.</para>
/// </summary>
public static class RebuildInstitutionHierarchyHandler
{
    public static async Task<RebuildInstitutionHierarchyResult> Handle(
        RebuildInstitutionHierarchy command,
        IDocumentSession session,
        ILogger<RebuildInstitutionHierarchy> logger,
        CancellationToken cancellationToken)
    {
        var all = await session.Query<InstitutionRecord>().ToListAsync(cancellationToken);
        var plan = InstitutionHierarchyPlanner.Plan(all, Guid.NewGuid);

        var byId = all.ToDictionary(i => i.Id);

        foreach (var node in plan.Created)
        {
            var record = new InstitutionRecord
            {
                Id = node.Id,
                InstitutionCode = InstitutionHierarchyPlanner.UnknownInstitutionCode,
                FullName = node.FullName,
                ProvinceCode = node.ProvinceCode,
                DistrictName = node.DistrictName
            };

            byId[node.Id] = record;
            session.Store(record);
        }

        foreach (var assignment in plan.Assignments)
        {
            if (!byId.TryGetValue(assignment.Id, out var record))
                continue;

            record.ParentId = assignment.ParentId;
            record.NodeTypeName = assignment.NodeTypeName;
            record.Path = assignment.Path;

            session.Store(record);
        }

        await session.SaveChangesAsync(cancellationToken);

        if (plan.SkippedNoProvince.Count > 0)
        {
            // Sessiz kalmaz: bu okullar hiçbir il yetkilisinin listesinde görünmez ve bunu
            // fark ettirecek başka bir sinyal yok (hata değil, BOŞ SONUÇ üretirler).
            logger.LogWarning(
                "Kurum ağacı kuruldu ama {Count} okulun il kodu yok; kapsamsız kaldılar ve "
                + "hiçbir il/ilçe yetkilisinin listesinde görünmezler. Kimlikler: {Ids}",
                plan.SkippedNoProvince.Count, string.Join(", ", plan.SkippedNoProvince));
        }

        return new RebuildInstitutionHierarchyResult(
            plan.Created.Count(c => c.NodeTypeName == InstitutionNodeType.Province.Name),
            plan.Created.Count(c => c.NodeTypeName == InstitutionNodeType.District.Name),
            plan.Assignments.Count,
            plan.SkippedNoProvince.Count);
    }
}
```

- [ ] **Step 6: Ucu ekle**

`src/Modules/Institution/MESNET.Institution.Api/InstitutionEndpoints.cs` — `group.MapPost("/staff/resync-branch-codes", ...)` satırının hemen ardına ekle:

```csharp
        // Kurum ağacı geçişi — DAĞITIM ÖN KOŞULU, idempotent. Atlanırsa sessizdir: yollar boş
        // kalır ve il/ilçe yetkilisi hata değil BOŞ LİSTE görür. Kurum üstü bir iştir:
        // institution:manage "kurum yönetebilir" der, "bütün ağacı yeniden kurabilir" demez.
        group.MapPost("/rebuild-hierarchy", PostRebuildHierarchy)
            .RequireAuthorization(Permissions.Platform.TenantManage);
```

Ve endpoint metodunu sınıfa ekle:

```csharp
    /// <summary>
    /// Kurum ağacını mevcut okul künyelerinden yeniden kurar. İdempotent — birden çok kez
    /// çağrılabilir.
    /// </summary>
    private static async Task<IResult> PostRebuildHierarchy(IMessageBus bus)
    {
        var result = await bus.InvokeAsync<RebuildInstitutionHierarchyResult>(
            new RebuildInstitutionHierarchy());

        var uyari = result.SkippedNoProvince > 0
            ? $" {result.SkippedNoProvince} okulun il kodu yok; kapsamsız kaldılar ve hiçbir "
              + "il/ilçe yetkilisinin listesinde görünmezler."
            : string.Empty;

        return Results.Ok(ResponseBuilder.Success()
            .AddData(result)
            .AddMessage(
                $"Kurum ağacı kuruldu: {result.ProvincesCreated} il, {result.DistrictsCreated} ilçe "
                + $"müdürlüğü açıldı, {result.NodesUpdated} düğümün ağaç bilgisi yazıldı.{uyari}")
            .Build());
    }
```

- [ ] **Step 7: Kiracı listesini okullara daralt**

`src/Modules/Institution/MESNET.Institution.Application/Services/InstitutionTenantDirectory.cs` — sorguyu şununla değiştir:

```csharp
        // KİRACI = OKUL. İl ve ilçe müdürlüğü düğümleri kiracı DEĞİLDİR ve kiracı damgalı
        // hiçbir veri taşımazlar. Süzülmeselerdi arka plan işleri hiçbir verinin bulunmadığı
        // "kiracılarda" koşardı — istisna değil, sessiz boş geçiş.
        var ids = await session.Query<InstitutionRecord>()
            .OfNodeType(InstitutionNodeType.School)
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);
```

Dosyanın `using` listesine ekle:

```csharp
using MESNET.Institution.Application.Extensions;
using MESNET.Institution.Core.Enums;
```

- [ ] **Step 8: Drift testinin muafiyet listesine geçiş handler'ını ekle**

`tests/MESNET.Security.UnitTests/InstitutionScopeDriftTests.cs` — `MayEnumerateAll` kümesine ekle:

```csharp
        // Ağacı kurmak tanımı gereği bütün düğümleri görmeyi gerektirir; uç kurum üstü izinle
        // korunur (platform:tenant:manage) ve komut hiçbir kurum kimliği taşımaz.
        "RebuildInstitutionHierarchyHandler.cs",
```

Aynı dosyadaki `MayEnumerateAll` XML yorumuna bu gerekçeyi bir cümleyle ekleyin.

- [ ] **Step 9: Dağıtım ön koşulu belgesini güncelle**

`src/Docs/docs/infrastructure/dagitim-on-kosullari.md` — resync uçları tablosuna satır ekle (tablonun sonuna):

```markdown
| `POST /api/institutions/rebuild-hierarchy` | Kurum **ağacını** mevcut okul künyelerinden (`ProvinceCode` / `DistrictName`) kurar: il ve ilçe müdürlüğü düğümlerini açar, `ParentId` ve `Path` yazar. `platform:tenant:manage` ister. **İdempotent** — ikinci koşu düğüm çoğaltmaz, bozulmuş yolu onarır |
```

Ve aynı dosyaya, "Personel backfill'i tek okulludur (#131)" bölümünün hemen ardına şu uyarı bölümünü ekle:

```markdown
### `rebuild-hierarchy` ZORUNLUDUR — atlanırsa hata değil boş liste

Kurum kapsamı artık ağaçtan geliyor: bir aktörün göreceği kurumlar `Path.StartsWith(aktörünYolu)`
ile bulunur. Geçiş koşturulmazsa **hiçbir kaydın yolu yoktur**, `StartsWith` hiçbir şeyle
eşleşmez ve il/ilçe yetkilisi **boş liste** görür — istek 200 döner, log temiz kalır.

Okul kullanıcıları etkilenmez: kapsam kararı kimlik eşitliğini yol kontrolünden **önce**
sorar, yani herkes kendi kurumunu yolsuz da görür. Kaybolan yalnız **yeni** il/ilçe
yeteneğidir.

Uç kurum üstü izinle korunur ve tüm ağacı bir kerede kurar; kiracı başına çağırmak gerekmez:

```bash
curl -X POST http://localhost:5270/api/institutions/rebuild-hierarchy \
  -H "Authorization: Bearer $TOKEN"
```

Yanıttaki `skippedNoProvince` sıfırdan büyükse, o okulların **il kodu yoktur** ve kapsamsız
kalmışlardır — hiçbir il yetkilisinin listesinde görünmezler. Künyeleri tamamlayıp ucu yeniden
çağırın.
```

- [ ] **Step 10: Derle ve testleri koş**

Run: `dotnet build MESNET.slnx && dotnet test tests/MESNET.Institution.UnitTests tests/MESNET.Security.UnitTests`
Expected: PASS

- [ ] **Step 11: Commit**

```bash
git add src/Modules/Institution/MESNET.Institution.Core/Services/InstitutionHierarchyPlanner.cs \
        src/Modules/Institution/MESNET.Institution.Application/Commands/RebuildInstitutionHierarchy.cs \
        src/Modules/Institution/MESNET.Institution.Application/Handlers/RebuildInstitutionHierarchyHandler.cs \
        src/Modules/Institution/MESNET.Institution.Application/Services/InstitutionTenantDirectory.cs \
        src/Modules/Institution/MESNET.Institution.Api/InstitutionEndpoints.cs \
        tests/MESNET.Institution.UnitTests/InstitutionHierarchyPlannerTests.cs \
        tests/MESNET.Security.UnitTests/InstitutionScopeDriftTests.cs \
        src/Docs/docs/infrastructure/dagitim-on-kosullari.md
git commit -m "feat(institution): kurum ağacı geçişi (rebuild-hierarchy) — idempotent

Karar saf bir planlayıcıda: bu geçişin tek kritik özelliği idempotanlık ve mantık
handler'da kalsaydı ancak veritabanına iki kez yazarak sınanabilirdi.

Atamalar YALNIZ eksik satırlara değil bütün düğümlere yazılır — elle bozulmuş bir
yol yoksa kalıcı olurdu.

İl kodu olmayan okul köke BAĞLANMAZ: bağlansaydı herhangi bir il yetkilisinin alt
ağacına düşen sahipsiz bir kayıt olurdu. Kapsamsız kalır, sayılır ve loglanır.

Kiracı listesi okullara daraltıldı: il/ilçe düğümleri kiracı değildir ve kiracı
damgalı veri taşımaz; süzülmeselerdi arka plan işleri boş 'kiracılarda' koşardı.

Dağıtım ön koşulu belgesine zorunlu adım olarak yazıldı — atlanırsa hata değil
boş liste."
```

---

### Task 7: `ProvincialAdmin` ve `DistrictAdmin` rolleri

**Yeni izin tanımlanmaz.** İkisi de düz `institution:view` alır; farkı izin değil, **ağaçtaki yeri** yaratır. `institution:` önekli her yeni izin `institution:*` wildcard'ı üzerinden her okul müdürüne sessizce geçerdi (ADR-0002 önek tuzağı).

**Files:**
- Modify: `src/MESNET.Common.Shared/Security/MesnetRoles.cs`
- Modify: `src/MESNET.Common.Shared/Security/RolePermissionMap.cs`
- Modify: `src/MESNET.Common.Shared/Security/AssignablePermissionScope.cs`
- Modify: `src/MESNET.AppHost/keycloak/mesnet-realm.json`
- Modify: `src/Docs/docs/architecture/adr-0002-izin-agaci-ve-onek-secimi.md` (üretilmiş matris)
- Modify: `src/Docs/docs/actors/actors.md`
- Modify: `src/Docs/docs/actors/permissions.md`
- Test: `tests/MESNET.Security.UnitTests/RoleModelDriftTests.cs` (mevcut — değişiklik gerektirmez, kırmızıya döner)
- Test: `tests/MESNET.Security.UnitTests/UpperNodeRoleMappingTests.cs` (yeni)

**Interfaces:**
- Consumes: yok (bağımsız).
- Produces: `MesnetRoles.ProvincialAdmin` = `"ProvincialAdmin"`, `MesnetRoles.DistrictAdmin` = `"DistrictAdmin"`

- [ ] **Step 1: Testi yaz (kırmızı)**

`tests/MESNET.Security.UnitTests/UpperNodeRoleMappingTests.cs`:

```csharp
using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// İl/ilçe yetkilisi rollerinin izin demeti.
///
/// <para><b>Neden yeni izin tanımlanmadı:</b> <c>InstitutionManager</c> <c>institution:*</c>
/// taşır. <c>institution:</c> önekli her yeni izin — adı ne olursa olsun — o wildcard
/// üzerinden <b>her okul müdürüne</b> geçer ve bunu kimse fark etmez (ADR-0002 önek tuzağı,
/// #126'da alan muafiyeti izninde bire bir yaşandı). İl yetkilisinin farkı izinde değil
/// ağaçtaki YERİNDEDİR.</para>
/// </summary>
public sealed class UpperNodeRoleMappingTests
{
    [Fact]
    public void Il_ve_ilce_yetkilisi_kurum_okuma_iznine_sahiptir()
    {
        foreach (var role in new[] { MesnetRoles.ProvincialAdmin, MesnetRoles.DistrictAdmin })
        {
            RolePermissionMap.GetPermissionsForRoles([role])
                .ShouldContain(Permissions.Institution.View, $"{role} kurum listesini görmeli.");
        }
    }

    /// <summary>
    /// <b>A parçasında yazma YOKTUR.</b> Yazma izni verilseydi, arayüzdeki butonlar açılır ve
    /// denetim izi (C parçası) daha yazılmadan bir kişi bütün okulların verisini
    /// değiştirebilirdi — sıra bağlayıcıdır: C, B'den önce.
    /// </summary>
    [Fact]
    public void Il_ve_ilce_yetkilisinin_kurum_yazma_izni_yoktur()
    {
        foreach (var role in new[] { MesnetRoles.ProvincialAdmin, MesnetRoles.DistrictAdmin })
        {
            RolePermissionMap.GetPermissionsForRoles([role])
                .ShouldNotContain(Permissions.Institution.Manage,
                    $"{role} A parçasında yazamaz — denetim izi (C) henüz yok.");
        }
    }

    /// <summary>
    /// Wildcard verilmez. <c>institution:*</c> verilseydi bu roller kurum yazma, personel
    /// yönetimi ve alan kapsamı muafiyeti dahil <b>her</b> institution iznini alırdı.
    /// </summary>
    [Fact]
    public void Il_ve_ilce_yetkilisine_wildcard_verilmez()
    {
        foreach (var role in new[] { MesnetRoles.ProvincialAdmin, MesnetRoles.DistrictAdmin })
        {
            RolePermissionMap.GetPermissionsForRoles([role])
                .ShouldNotContain(Permissions.Institution.AllBranches,
                    $"{role} kapsam muafiyeti izni almamalı.");
        }
    }

    /// <summary>
    /// Kurum üstü izin de verilmez: bu roller yeni okul AÇAMAZ ve başka bir ağaca yazamaz.
    /// Kapsamları ağaçtaki yerleriyle sınırlıdır, izinle genişletilmez.
    /// </summary>
    [Fact]
    public void Il_ve_ilce_yetkilisi_kurum_ustu_degildir()
    {
        foreach (var role in new[] { MesnetRoles.ProvincialAdmin, MesnetRoles.DistrictAdmin })
        {
            RolePermissionMap.GetPermissionsForRoles([role])
                .ShouldNotContain(Permissions.Platform.TenantManage,
                    $"{role} kurum sınırının üstünde çalışamaz — kapsamı ağaçtan gelir.");
        }
    }
}
```

- [ ] **Step 2: Testin başarısız olduğunu doğrula**

Run: `dotnet test tests/MESNET.Security.UnitTests --filter FullyQualifiedName~UpperNodeRoleMappingTests`
Expected: FAIL — `error CS0117: 'MesnetRoles' does not contain a definition for 'ProvincialAdmin'`

- [ ] **Step 3: Rol sabitlerini ve kataloğu ekle**

`src/MESNET.Common.Shared/Security/MesnetRoles.cs` — `public const string SystemAdmin = "SystemAdmin";` bloğunun ardına ekle:

```csharp
    /// <summary>
    /// İl millî eğitim yetkilisi. Kendi <b>ilinin</b> okullarını listeler ve okur.
    ///
    /// <para><b>Kapsamı izinden gelmez, ağaçtaki YERİNDEN gelir.</b> İzni okul müdürününkinden
    /// dar (<c>institution:view</c>, wildcard yok); farkı, bağlı olduğu kurumun bir il
    /// müdürlüğü düğümü olması ve alt ağacındaki okulları görmesi.</para>
    ///
    /// <para><b>A parçasında YAZMA YOKTUR.</b> Tam yetki, denetim izi (C parçası) yazılmadan
    /// verilmez: izi olmayan tam yetkide bir kişi bütün okulların kiracı sınırını taşır ve
    /// hiçbir kayıt kalmaz.</para>
    /// </summary>
    public const string ProvincialAdmin = "ProvincialAdmin";

    /// <summary>
    /// İlçe millî eğitim yetkilisi. <see cref="ProvincialAdmin"/> ile aynı izin demetine
    /// sahiptir; farkı yalnız ağaçtaki yeridir — alt ağacı kendi ilçesiyle sınırlıdır.
    /// </summary>
    public const string DistrictAdmin = "DistrictAdmin";
```

`Catalog` listesine, `SystemAdmin` satırının **öncesine** ekle:

```csharp
        new(ProvincialAdmin, "İl MEM Yetkilisi",
            "Kendi ilindeki okulların bilgilerini listeler ve görüntüler."),
        new(DistrictAdmin, "İlçe MEM Yetkilisi",
            "Kendi ilçesindeki okulların bilgilerini listeler ve görüntüler."),
```

- [ ] **Step 4: İzin haritasına ekle**

`src/MESNET.Common.Shared/Security/RolePermissionMap.cs` — `Mappings` sözlüğünde `[MesnetRoles.SystemAdmin]` girdisinin **öncesine** ekle:

```csharp
        // İl / ilçe millî eğitim yetkilisi — kurum hiyerarşisi A parçası.
        //
        // YENİ İZİN YOK, WILDCARD YOK. institution:* InstitutionManager'a aittir ve
        // "institution:" önekli her yeni izin o wildcard üzerinden her okul müdürüne sessizce
        // geçer (ADR-0002 önek tuzağı — #126'da alan muafiyeti izninde bire bir yaşandı).
        //
        // Bu rollerin farkı izinde DEĞİL, ağaçtaki yerlerindedir: kapsam
        // InstitutionScopePolicy'de "hedefin yolu benim yolumla başlıyor mu" sorusuna iner.
        //
        // A parçasında YAZMA YOKTUR: institution:manage verilmez. Tam yetki, denetim izi
        // (C parçası) yazılmadan verilmez — sıra bağlayıcıdır, C önce gelir.
        [MesnetRoles.ProvincialAdmin] =
        [
            Permissions.Institution.View
        ],
        // İlçe yetkilisi il yetkilisiyle AYNI demeti alır; farkı yalnız ağaçtaki yeridir.
        // Demetleri ayrıştırmak, ikisinin arasındaki tek gerçek farkı (kapsam) izne taşıma
        // isteğini doğururdu — o da yeni bir "institution:" önekli izin demekti.
        [MesnetRoles.DistrictAdmin] =
        [
            Permissions.Institution.View
        ],
```

- [ ] **Step 5: Atanabilir kapsam varsayılanlarını ekle**

`src/MESNET.Common.Shared/Security/AssignablePermissionScope.cs` — `Defaults` sözlüğünde `[MesnetRoles.SystemAdmin]` girdisinin **öncesine** ekle:

```csharp
        // İl / ilçe yetkilisi: yalnız kurum domaini. Yetki DAĞITAMASINLAR diye başka domain
        // verilmez — kapsamları ağaçtan gelir, izinle genişletilmez.
        [MesnetRoles.ProvincialAdmin] =
        [
            "institution:",
        ],
        [MesnetRoles.DistrictAdmin] =
        [
            "institution:",
        ],
```

- [ ] **Step 6: Realm tanımına rolleri ekle**

`src/MESNET.AppHost/keycloak/mesnet-realm.json` — `roles.realm` dizisine, `SystemAdmin` girdisinin öncesine ekle:

```json
  {
    "name": "ProvincialAdmin",
    "description": "İl MEM yetkilisi — kendi ilindeki okulları listeler ve görüntüler"
  },
  {
    "name": "DistrictAdmin",
    "description": "İlçe MEM yetkilisi — kendi ilçesindeki okulları listeler ve görüntüler"
  },
```

- [ ] **Step 7: Rol testlerinin geçtiğini doğrula**

Run: `dotnet test tests/MESNET.Security.UnitTests --filter FullyQualifiedName~UpperNodeRoleMappingTests`
Expected: PASS (4 test)

Run: `dotnet test tests/MESNET.Security.UnitTests --filter FullyQualifiedName~RoleModelDriftTests`
Expected: PASS — rol listesi, izin haritası, atanabilir kapsam ve realm tanımı artık dört yerde de aynı. Kırmızı kalan bir iddia varsa hangisinin eksik olduğunu hata metni söyler; eksik yeri **doldurun**, testi gevşetmeyin.

- [ ] **Step 8: ADR-0002 izin matrisini yeniden üret**

Run: `dotnet test tests/MESNET.Security.UnitTests --filter FullyQualifiedName~PermissionMatrixDocTests`
Expected: FAIL — matris kodla uyuşmuyor; test **doğru metni diske yazar** ve yolunu hata mesajında verir (`permission-matrix.generated.md`).

Hata mesajındaki dosyanın içeriğini `src/Docs/docs/architecture/adr-0002-izin-agaci-ve-onek-secimi.md` içindeki işaretçilerin arasına **olduğu gibi** yapıştırın. Elle yeniden yazmayın — elle yazım bu testin çözdüğü sorunun ta kendisidir.

Run: `dotnet test tests/MESNET.Security.UnitTests --filter FullyQualifiedName~PermissionMatrixDocTests`
Expected: PASS

- [ ] **Step 9: Aktör ve izin belgelerini güncelle**

`src/Docs/docs/actors/actors.md` — aktör listesine iki satır ekle:

```markdown
### İl MEM Yetkilisi (`ProvincialAdmin`)

İl millî eğitim müdürlüğünde görevli yetkili. Kendi **ilindeki** okulların bilgilerini
listeler ve görüntüler.

Kapsamı **izinden değil, kurum ağacındaki yerinden** gelir: bağlı olduğu kurum bir il
müdürlüğü düğümüdür ve alt ağacındaki okulları görür. İzni okul müdürününkinden **dardır**
(`institution:view`; wildcard yok).

**Yazma yetkisi yoktur** (A parçası). Tam yetki denetim izi yazıldıktan sonra verilecektir.

### İlçe MEM Yetkilisi (`DistrictAdmin`)

İl yetkilisiyle aynı izin demetine sahiptir; farkı yalnız ağaçtaki yeridir — alt ağacı kendi
ilçesiyle sınırlıdır.
```

`src/Docs/docs/actors/permissions.md` — "Alan (Branş) Kapsamı Kontrolü" bölümünün ardına yeni bir bölüm ekle:

```markdown
## Kurum Kapsamı Ağaçtan Gelir

Kurumlar bir ağaçtır: il müdürlüğü → ilçe müdürlüğü → okul. Kapsam kararı tek soruya iner —
**hedefin yolu aktörün yoluyla başlıyor mu** (`InstitutionScopePolicy`).

- Aktörün kendi düğümü kapsamındadır
- **Üst düğüm kapsam DIŞIDIR** — okul müdürü ilçe müdürlüğünün kaydını göremez
- Kardeş düğümler kapsam dışıdır
- `platform:tenant:manage` taşıyan aktör ağacın tamamını görür

`ProvincialAdmin` ve `DistrictAdmin` rolleri **yeni izin almaz**: ikisi de düz
`institution:view` taşır. Yeni bir `institution:` önekli izin tanımlansaydı, o izin
`institution:*` wildcard'ı üzerinden **her okul müdürüne** sessizce geçerdi (ADR-0002 önek
tuzağı). Bu rollerin farkı izinde değil, ağaçtaki yerindedir.

Aktörün yolu `institution_path` claim'inden okunur ve claim **kurum kaydından** üretilir;
token'daki değer her istekte silinir. Kullanıcının yazabildiği bir yol, kullanıcının kendi
kapsamını seçmesi demektir — kök yazan biri her okulu görürdü.
```

- [ ] **Step 10: Commit**

```bash
git add src/MESNET.Common.Shared/Security/MesnetRoles.cs \
        src/MESNET.Common.Shared/Security/RolePermissionMap.cs \
        src/MESNET.Common.Shared/Security/AssignablePermissionScope.cs \
        src/MESNET.AppHost/keycloak/mesnet-realm.json \
        src/Docs/docs/architecture/adr-0002-izin-agaci-ve-onek-secimi.md \
        src/Docs/docs/actors/actors.md \
        src/Docs/docs/actors/permissions.md \
        tests/MESNET.Security.UnitTests/UpperNodeRoleMappingTests.cs
git commit -m "feat(security): ProvincialAdmin ve DistrictAdmin rolleri — yeni izin YOK

İkisi de düz institution:view alır. Yeni bir institution: önekli izin
tanımlansaydı institution:* wildcard'ı üzerinden her okul müdürüne sessizce
geçerdi (ADR-0002 önek tuzağı; #126'da alan muafiyetinde bire bir yaşandı).

Bu rollerin farkı izinde değil ağaçtaki yerinde. Yazma izni verilmedi: tam yetki
denetim izi (C parçası) yazılmadan verilmez — sıra bağlayıcı, C önce."
```

**Dağıtım notu (uygulayıcıya):** Keycloak realm import **tek seferliktir**. Bu iki rol mevcut bir dev kabına **hiç ulaşmaz** — `RealmVerificationHostedService` Development ortamında sapmayı görüp **açılışı durdurur**. Rolleri çalışan realm'e elle ekleyin (Keycloak admin konsolu → Realm roles → Create role) ya da Keycloak volume'ünü sıfırlayın.

---

### Kapsam atama neden A'da bir görev DEĞİL

Spec'in "Kapsam atama" bölümü il yetkilisinin kendi ilinin ilçe yetkililerini atayabilmesini
tarif ediyor. Bu A parçasında **uygulanamaz ve gerekmez**:

- Kullanıcı–kurum bağını yazan uç (`POST /api/security/users/{id}/institution`)
  `user:roles:manage` ister. `ProvincialAdmin`'in izin demeti yalnız `institution:view`'dir —
  **yazma yoktur** (A parçasının kuralı). Yani il yetkilisi bu ucu hiç çağıramaz.
- İzni vermek, kuralı uygulamadan önce yeteneği açmak olurdu: yol kontrolü olmayan
  `UserInstitutionScopePolicy` "kendi kurumu" der ve il yetkilisi ilçe müdürlüğüne
  **yazamaz** — yani izin verilseydi özellik zaten çalışmazdı, sessizce.
- A'da atamayı `SystemAdmin` yapar; kurum üstü muafiyet (`platform:tenant:manage`) bu yolu
  bugün de açıyor.

Kural **B parçasına** aittir ve orada `UserInstitutionScopePolicy` yol farkındalığı kazanır.
Bunu yaparken gereken kurum yolu okuması Security modülünden yapılamaz (modüller arası
Application/Core referansı ve başka modülün şemasına doğrudan SQL yasak) — çözüm B'nin
tasarımında ele alınmalıdır.

---

### Task 8: Ön yüz — sayfalı liste sözleşmesi ve düzenlenecek kurumun çözümü

Ucun sözleşmesi değişti: `GET /api/institutions` artık `PagedResponse<InstitutionDto>` döndürüyor. İki çağrı yeri de `institutions[0]` kullanıyor — biri 27.08.2026'da düzeltildi, **diğeri hâlâ aynı hatayı taşıyor**.

**Files:**
- Modify: `src/WebUI/src/api/institution.ts`
- Modify: `src/WebUI/src/utils/institutionScope.ts`
- Modify: `src/WebUI/src/utils/institutionScope.spec.ts`
- Modify: `src/WebUI/src/pages/institution/InstitutionPage.vue`
- Modify: `src/WebUI/src/pages/institution/InstitutionFormPage.vue`

**Interfaces:**
- Consumes: `InstitutionDto` yeni alanları (Task 2), sayfalı uç (Task 5).
- Produces:
  - `InstitutionDto` TS tipi yeni alanları: `nodeType: string`, `nodeTypeSlug: string`, `parentId: string | null`, `parentName: string | null`
  - `institutionApi.list(params?: InstitutionListParams & PaginationParams)` → `Promise<{ data: PagedResponse<InstitutionDto> }>`
  - `InstitutionListParams` = `{ nodeType?: string; parentId?: string }`
  - `resolveEditableInstitutionId(routeInstitutionId, ownInstitutionId, institutions)` → `string | null`

- [ ] **Step 1: Testi yaz (kırmızı)**

`src/WebUI/src/utils/institutionScope.spec.ts` dosyasının **tamamını** şununla değiştir:

```typescript
import { describe, it, expect } from 'vitest'
import { resolveEditableInstitutionId } from './institutionScope'

/**
 * Bu testin varlık nedeni ÖLÇÜLMÜŞ bir hatadır (27.08.2026):
 *
 * `InstitutionPage` düzenlenecek kurumu `institutions[0].id` ile seçiyordu. Okul rollerinde
 * liste zaten tek elemanlı olduğu için hata görünmüyordu; `platform:tenant:manage` taşıyan
 * aktörde (SystemAdmin) liste bütün okulları döndürür ve sorgunun `ORDER BY`'ı yoktu —
 * Postgres güncellenen satırı heap'te yerinden oynattığı için sıra HER YAZMADAN SONRA
 * değişebiliyordu.
 *
 * Sonuç: admin, ekranda "Cumhuriyet" yazarken kendi okulu "Atatürk" olan bir oturumda
 * Cumhuriyet'in paletini kaydetti; tema ise `institutionStore` üzerinden kendi okulundan
 * uygulandığı için ilk sayfa geçişinde eski renge döndü. Kayıp veri yoktu — yazma YANLIŞ
 * OKULA gitti.
 *
 * Kurum ağacıyla birlikte fonksiyon ÜÇÜNCÜ bir girdi kazandı: rota parametresi. İl yetkilisi
 * `/institutions/:id` ile alt ağacındaki bir okulu açtığında hedef O OKULDUR — kendi kurumu
 * (İl MEM) değil.
 */
describe('resolveEditableInstitutionId', () => {
  const list = [{ id: 'cumhuriyet-id' }, { id: 'gazi-id' }, { id: 'ataturk-id' }]

  it('rota parametresi her şeyden önce gelir — alt ağaçtaki okul açılıyordur', () => {
    // Arrange: aktörün kendi kurumu İl MEM, rota bir okulu işaret ediyor
    // Act
    const id = resolveEditableInstitutionId('okul-id', 'il-mem-id', list)
    // Assert
    expect(id).toBe('okul-id')
  })

  it('rota parametresi yoksa aktörün kendi kurumunu seçer — liste başka okulla başlasa bile', () => {
    expect(resolveEditableInstitutionId(null, 'ataturk-id', list)).toBe('ataturk-id')
  })

  it('kendi kurumu listede yoksa yine kendi kurumunu seçer — yetki kararı sunucunun', () => {
    expect(resolveEditableInstitutionId(null, 'baska-okul-id', list)).toBe('baska-okul-id')
  })

  it('kurumu olmayan platform aktöründe listeye düşer ama SIRAYA BAĞLI KALMAZ', () => {
    // Aynı içerik, farklı sıra → aynı sonuç. Sıra bağımlılığı hatanın kendisiydi.
    const karisik = [{ id: 'gazi-id' }, { id: 'ataturk-id' }, { id: 'cumhuriyet-id' }]
    expect(resolveEditableInstitutionId(null, null, list)).toBe(
      resolveEditableInstitutionId(null, null, karisik),
    )
  })

  it('kurum yok ve liste boşsa null döner — çağıran hata mesajı gösterir', () => {
    expect(resolveEditableInstitutionId(null, null, [])).toBeNull()
  })

  it('boş string kurum kimliği yokmuş sayılır', () => {
    expect(resolveEditableInstitutionId('', '', [{ id: 'gazi-id' }])).toBe('gazi-id')
  })
})
```

- [ ] **Step 2: Testin başarısız olduğunu doğrula**

Run: `pnpm -C src/WebUI test:run -- institutionScope`
Expected: FAIL — ilk test `'il-mem-id'` döner (`'okul-id'` beklenirken), çünkü fonksiyon henüz rota parametresi almıyor.

- [ ] **Step 3: `resolveEditableInstitutionId`'i genişlet**

`src/WebUI/src/utils/institutionScope.ts` — fonksiyon imzasını ve gövdesini şununla değiştir (dosyanın başındaki blok yorum korunur, aşağıdaki paragraf ona **eklenir**):

```typescript
/**
 * ...(mevcut blok yorum aynen kalır)...
 *
 * <p><b>Kurum ağacıyla gelen üçüncü girdi (27.08.2026):</b> rota parametresi. İl/ilçe
 * yetkilisi `/institutions/:id` ile alt ağacındaki bir okulu açtığında hedef O OKULDUR —
 * kendi kurumu (İl MEM) değil. Sıra bu yüzden <b>rota → kendi kurumu → liste</b>'dir; en
 * belirgin niyet en önde.</p>
 *
 * @param routeInstitutionId Rota parametresi (`/institutions/:id`). Yoksa `null`.
 * @param ownInstitutionId Aktörün kendi kurumu (`/auth/me` → `authStore.user.institutionId`).
 *   Token'dan GELMEZ; sunucu kullanıcı kaydından üretir (ADR-0003 adım 2).
 * @param institutions Sunucudan gelen görünür kurum listesi.
 * @returns Düzenlenecek kurum kimliği; hiçbiri yoksa `null`.
 */
export function resolveEditableInstitutionId(
  routeInstitutionId: string | null | undefined,
  ownInstitutionId: string | null | undefined,
  institutions: readonly { id: string }[],
): string | null {
  // Rota parametresi en belirgin niyettir: kullanıcı BU kurumu açmak istedi. Yetki kararı
  // sunucunundur (InstitutionScopeGuard); rota, yetkinin ikinci bir kopyası değildir.
  if (routeInstitutionId) return routeInstitutionId

  // Kendi kurumu VARSA tartışma yok: listede görünmese bile hedef odur.
  if (ownInstitutionId) return ownInstitutionId

  if (institutions.length === 0) return null

  // Kurumu olmayan platform aktörü. Listeye düşülür ama SIRAYA BAĞLI KALINMAZ: sunucu sıra
  // garantisi vermiyordu ve "ilk satır" her yazmadan sonra başka bir okul olabiliyordu.
  // Kimliğe göre kararlı seçim, aynı kümede her zaman aynı okulu verir.
  return [...institutions].sort((a, b) => a.id.localeCompare(b.id))[0]!.id
}
```

- [ ] **Step 4: Testin geçtiğini doğrula**

Run: `pnpm -C src/WebUI test:run -- institutionScope`
Expected: PASS (6 test)

- [ ] **Step 5: API sözleşmesini güncelle**

`src/WebUI/src/api/institution.ts`:

**(a)** `InstitutionDto` arayüzünde `districtName: string | null` satırının ardına ekle:

```typescript
  /**
   * Kurum ağacındaki düğüm tipi — `Province` / `District` / `School`.
   * İstemci mantığı BUNA bakar; `nodeTypeSlug` yalnız gösterim içindir.
   * Geçiş ucu koşturulmamış eski kayıtlar `School` döner (sunucu çözer).
   */
  nodeType: string
  /** Türkçe etiket — "İl Millî Eğitim Müdürlüğü" / "İlçe Millî Eğitim Müdürlüğü" / "Okul". */
  nodeTypeSlug: string
  /** Üst düğüm kimliği. Kök (il müdürlüğü) için `null`. */
  parentId: string | null
  /** Üst düğümün adı — sunucuda toplu çözülür, istemci ikinci istek atmaz. */
  parentName: string | null
```

**(b)** `institutionApi` içindeki `list` fonksiyonunu şununla değiştir ve tipini dosyada tanımla:

```typescript
/** `GET /api/institutions` süzgeçleri. */
export interface InstitutionListParams {
  /** `Province` / `District` / `School`. Verilmezse sunucu OKULLARI döndürür. */
  nodeType?: string
  /** Belirli bir düğümün doğrudan çocukları. */
  parentId?: string
}
```

```typescript
  /**
   * Görünür kurumların SAYFALI listesi.
   *
   * Kapsam sunucudadır: aktörün kurum ağacındaki alt ağacı döner. Kurum üstü aktör
   * (`platform:tenant:manage`) tüm ağacı görür.
   *
   * Varsayılan süzgeç OKUL'dur — il/ilçe müdürlüğü düğümleri açılır listelerde okul gibi
   * görünmesin diye. Üst düğümleri istemek için `nodeType` verin.
   */
  list: (params?: InstitutionListParams & PaginationParams) =>
    api.get<PagedResponse<InstitutionDto>>('/institutions', { params }),
```

- [ ] **Step 6: `InstitutionPage`'i rota parametresine bağla**

`src/WebUI/src/pages/institution/InstitutionPage.vue`:

`<script setup>` içinde `useRoute` import edilmemişse ekleyin ve örneği oluşturun:

```typescript
import { useRoute } from 'vue-router'

const route = useRoute()
```

`load()` fonksiyonundaki kurum çözümleme bloğunu şununla değiştir:

```typescript
    if (!institutionId.value) {
      // Sıra: rota parametresi → aktörün kendi kurumu → liste. Liste yalnız kurumu olmayan
      // platform aktörü için yedektir. "Listenin ilk satırı" demek, sıralaması olmayan bir
      // sorguya güvenmekti ve platform aktöründe her yazmadan sonra başka bir okulu
      // düzenletiyordu — bkz. utils/institutionScope.ts.
      const routeId = typeof route.params.id === 'string' ? route.params.id : null
      const ownId = authStore.user?.institutionId ?? null
      const listRes = routeId || ownId ? null : await institutionApi.list({ pageSize: 100 })
      const resolved = resolveEditableInstitutionId(routeId, ownId, listRes?.data?.items ?? [])
      if (!resolved) {
        error.value = 'Kayıtlı kurum bulunamadı.'
        return
      }
      institutionId.value = resolved
    }
```

- [ ] **Step 7: `InstitutionFormPage`'deki aynı hatayı kapat**

`src/WebUI/src/pages/institution/InstitutionFormPage.vue` — `loadInstitution()` içindeki şu bloğu:

```typescript
    const { data: institutions } = await institutionApi.list()
    if (!institutions || institutions.length === 0) {
      goBack()
      return
    }
    institutionId.value = institutions[0].id
```

şununla değiştir:

```typescript
    // AYNI HATA burada da vardı: "listenin ilk satırı" sıralaması olmayan bir sorguya
    // güvenmekti ve platform aktöründe her yazmadan sonra başka bir okulu düzenletiyordu.
    // InstitutionPage 27.08.2026'da düzeltilmişti; bu çağrı yeri gözden kaçmıştı.
    const routeId = typeof route.params.id === 'string' ? route.params.id : null
    const ownId = authStore.user?.institutionId ?? null
    const listRes = routeId || ownId ? null : await institutionApi.list({ pageSize: 100 })
    const resolved = resolveEditableInstitutionId(routeId, ownId, listRes?.data?.items ?? [])
    if (!resolved) {
      goBack()
      return
    }
    institutionId.value = resolved
```

Ve dosyanın `<script setup>` bloğuna gereken import/örnekleri ekleyin (zaten varsa tekrarlamayın):

```typescript
import { useRoute } from 'vue-router'
import { useAuthStore } from 'stores/auth'
import { resolveEditableInstitutionId } from 'src/utils/institutionScope'

const route = useRoute()
const authStore = useAuthStore()
```

- [ ] **Step 8: Ön yüz kapılarını koş**

Run: `pnpm -C src/WebUI lint && pnpm -C src/WebUI test:run`
Expected: PASS — lint temiz, tüm testler yeşil.

- [ ] **Step 9: Commit**

```bash
git add src/WebUI/src/api/institution.ts \
        src/WebUI/src/utils/institutionScope.ts \
        src/WebUI/src/utils/institutionScope.spec.ts \
        src/WebUI/src/pages/institution/InstitutionPage.vue \
        src/WebUI/src/pages/institution/InstitutionFormPage.vue
git commit -m "feat(webui): kurum listesi sayfalı sözleşmeye geçti, hedef kurum rotadan çözülüyor

resolveEditableInstitutionId üçüncü girdiyi aldı: rota parametresi. İl yetkilisi
/institutions/:id ile alt ağacındaki okulu açtığında hedef O OKULDUR, kendi
kurumu (İl MEM) değil.

InstitutionFormPage hâlâ institutions[0].id kullanıyordu — InstitutionPage'de
27.08.2026'da düzeltilen hatanın ikinci kopyası, gözden kaçmıştı."
```

---

### Task 9: Ön yüz — kurum listesi sayfası, rotalar ve menü kapısı

**Detay için yeni sayfa yazılmaz.** Satıra tıklama `/institutions/:id` rotasına gider ve mevcut `InstitutionPage` açılır; yazma butonları zaten `institution:manage` ile sarılı olduğundan sayfa il yetkilisinde **kendiliğinden salt okunur** açılır.

**Files:**
- Create: `src/WebUI/src/pages/institution/InstitutionListPage.vue`
- Modify: `src/WebUI/src/router/index.ts`
- Modify: `src/WebUI/src/composables/useNavigation.ts`
- Test: `src/WebUI/src/composables/useNavigation.upperNode.spec.ts` (yeni)

**Interfaces:**
- Consumes: `institutionApi.list` sayfalı hâli + `InstitutionDto.nodeType` (Task 8), `useInstitutionStore().institution` (mevcut).
- Produces:
  - Rota `InstitutionList` → `/institutions`
  - Rota `InstitutionDetail` → `/institutions/:id`
  - `NavItem.visibleWhen?: (ctx: NavVisibilityContext) => boolean`
  - `NavVisibilityContext` = `{ isUpperNode: boolean }`

- [ ] **Step 1: Menü kapısı testini yaz (kırmızı)**

`src/WebUI/src/composables/useNavigation.upperNode.spec.ts`:

```typescript
import { describe, it, expect } from 'vitest'
import { isNavItemVisible, type NavItem } from './useNavigation'

/**
 * "Kurumlar" menü girdisi okul kullanıcısına GÖSTERİLMEZ.
 *
 * İzin kapısı bunu yapamaz: okul müdürü de `institution:view` taşır (hatta `institution:*`).
 * Ayrım izinde değil, kullanıcının bağlı olduğu düğümün TİPİNDEDİR. Kapı olmasaydı okul
 * müdürü tek satırlık bir "liste" görürdü — bilgi taşımayan, tıklandığında zaten açık olan
 * sayfaya giden bir menü girdisi.
 *
 * NOT: bu bir GÖRÜNÜRLÜK kararıdır, yetki kararı değil. Yetki sunucudadır
 * (`InstitutionScopePolicy`); okul kullanıcısı ucu elle çağırsa da kendi kurumundan
 * fazlasını göremez.
 */
describe('isNavItemVisible', () => {
  const izinliOkuyucu = (perms: string[]) => (required: string[]) =>
    required.length === 0 || required.some((p) => perms.includes(p))

  const kurumlar: NavItem = {
    title: 'Kurumlar',
    icon: 'account_tree',
    to: { name: 'InstitutionList' },
    permissions: ['institution:view'],
    visibleWhen: (ctx) => ctx.isUpperNode,
  }

  it('il/ilçe kullanıcısına gösterilir', () => {
    expect(
      isNavItemVisible(kurumlar, izinliOkuyucu(['institution:view']), { isUpperNode: true }),
    ).toBe(true)
  })

  it('okul kullanıcısına gösterilmez — izni olsa bile', () => {
    expect(
      isNavItemVisible(kurumlar, izinliOkuyucu(['institution:view']), { isUpperNode: false }),
    ).toBe(false)
  })

  it('izni olmayana gösterilmez — düğüm tipi üst düğüm olsa bile', () => {
    expect(isNavItemVisible(kurumlar, izinliOkuyucu([]), { isUpperNode: true })).toBe(false)
  })

  it('koşulu olmayan girdi yalnız izne bakar', () => {
    const kurumBilgileri: NavItem = {
      title: 'Kurum Bilgileri',
      icon: 'account_balance',
      to: { name: 'Institution' },
      permissions: ['institution:view'],
    }

    expect(
      isNavItemVisible(kurumBilgileri, izinliOkuyucu(['institution:view']), { isUpperNode: false }),
    ).toBe(true)
  })
})
```

- [ ] **Step 2: Testin başarısız olduğunu doğrula**

Run: `pnpm -C src/WebUI test:run -- useNavigation.upperNode`
Expected: FAIL — `isNavItemVisible` dışa aktarılmamış.

- [ ] **Step 3: Menü kapısını uygula**

`src/WebUI/src/composables/useNavigation.ts`:

**(a)** `NavItem` arayüzünü ve yeni bağlam tipini şununla değiştir:

```typescript
/**
 * Menü görünürlüğü için izin DIŞI bağlam.
 *
 * `isUpperNode`: kullanıcının bağlı olduğu kurum bir il/ilçe müdürlüğü düğümü mü?
 * İzinle çözülemez — okul müdürü de `institution:view` taşır (hatta `institution:*`).
 */
export interface NavVisibilityContext {
  isUpperNode: boolean
}

export interface NavItem {
  title: string
  icon: string
  to: { name: string }
  permissions: string[]
  /**
   * İzne EK koşul. Verilmezse yalnız izne bakılır.
   *
   * Bu bir GÖRÜNÜRLÜK kararıdır, yetki kararı değil — yetki sunucudadır. Menüden gizlemek,
   * bilgi taşımayan bir girdiyi (okul kullanıcısına tek satırlık "liste") saklamak içindir.
   */
  visibleWhen?: (ctx: NavVisibilityContext) => boolean
}
```

**(b)** Dosyaya saf bir görünürlük fonksiyonu ekle (menü tanımının hemen ardına):

```typescript
/**
 * Bir menü girdisi görünür mü? Saf fonksiyon — store'a dokunmaz, testte tek başına koşar.
 */
export function isNavItemVisible(
  item: NavItem,
  hasAnyPermission: (permissions: string[]) => boolean,
  ctx: NavVisibilityContext,
): boolean {
  if (item.permissions.length > 0 && !hasAnyPermission(item.permissions)) return false
  if (item.visibleWhen && !item.visibleWhen(ctx)) return false
  return true
}
```

**(c)** "Kurum Yönetimi" grubunun `children` listesinde, `Kurum Bilgileri` satırının **öncesine** ekle:

```typescript
      {
        title: 'Kurumlar',
        icon: 'account_tree',
        to: { name: 'InstitutionList' },
        permissions: ['institution:view'],
        // Okul kullanıcısına gösterilmez: onun "listesi" tek satırdır ve tıklandığında zaten
        // açık olan sayfaya gider. Ayrım izinle yapılamaz — okul müdürü de institution:view
        // taşır; fark bağlı olduğu düğümün TİPİNDEDİR.
        visibleWhen: (ctx) => ctx.isUpperNode,
      },
```

**(d)** `useNavigation()` içinde bağlamı kur ve süzmeyi fonksiyona devret:

```typescript
export function useNavigation() {
  const authStore = useAuthStore()
  const institutionStore = useInstitutionStore()
  const route = useRoute()

  /**
   * Kullanıcının kurumu bir üst düğüm mü?
   *
   * Kaynak `institutionStore` — aktörün kendi kurumunu `GET /api/institutions/{id}` ile
   * zaten yükler (MainLayout mount'ta çağırır). Store dolmadan önce `false`'tur, yani menü
   * girdisi biraz geç belirir; alternatifi `/auth/me`'ye yeni bir claim eklemekti ve o
   * kapsam anahtarı olmayan bir görünürlük kararı için fazla ağır bir yol.
   */
  const visibilityContext = computed<NavVisibilityContext>(() => ({
    isUpperNode:
      institutionStore.institution?.nodeType === 'Province' ||
      institutionStore.institution?.nodeType === 'District',
  }))

  const filteredMenu = computed(() => {
    const ctx = visibilityContext.value
    const hasAny = (permissions: string[]) => authStore.hasAnyPermission(permissions)

    return menuDefinition
      .map((group) => {
        // Top-level link (children yok)
        if (group.to && group.children.length === 0) {
          const visible = group.permissions.length === 0 || hasAny(group.permissions)
          return visible ? group : null
        }

        const visibleChildren = group.children.filter((item) =>
          isNavItemVisible(item, hasAny, ctx),
        )

        if (visibleChildren.length === 0) return null

        // Tek child → düz link'e terfi ettir
        if (visibleChildren.length === 1) {
          return { ...group, to: visibleChildren[0].to, children: [] as NavItem[] }
        }

        return { ...group, children: visibleChildren }
      })
      .filter(Boolean) as NavGroup[]
  })
```

Dosyanın `import` bloğuna ekle:

```typescript
import { useInstitutionStore } from 'stores/institution'
```

- [ ] **Step 4: Menü testinin geçtiğini doğrula**

Run: `pnpm -C src/WebUI test:run -- useNavigation.upperNode`
Expected: PASS (4 test)

- [ ] **Step 5: Liste sayfasını yaz**

`src/WebUI/src/pages/institution/InstitutionListPage.vue`:

```vue
<template>
  <q-page padding>
    <PageHeader title="Kurumlar" />

    <AppTable
      :rows="institutions"
      :columns="columns"
      :loading="loading"
      :pagination="pagination"
      show-search
      :search="search"
      no-data-label="Kapsamınızda kurum bulunamadı."
      @request="onRequest"
      @search="onSearch"
    >
      <template #filters>
        <q-select
          v-model="nodeTypeFilter"
          :options="nodeTypeOptions"
          label="Kurum Türü"
          outlined
          dense
          emit-value
          map-options
          style="min-width: 220px"
          @update:model-value="load"
        />
      </template>

      <template #body-cell-fullName="{ row }">
        <q-td>
          <div class="text-weight-medium">{{ row.fullName }}</div>
          <div
            v-if="row.parentName"
            class="text-caption text-grey-7"
          >
            {{ row.parentName }}
          </div>
        </q-td>
      </template>

      <template #body-cell-location="{ row }">
        <q-td>{{ formatLocation(row) }}</q-td>
      </template>

      <template #body-cell-actions="{ row }">
        <q-td class="text-right">
          <q-btn
            flat
            dense
            round
            icon="visibility"
            aria-label="Kurum bilgilerini görüntüle"
            @click="openInstitution(row.id)"
          >
            <q-tooltip>Kurum Bilgilerini Görüntüle</q-tooltip>
          </q-btn>
        </q-td>
      </template>
    </AppTable>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import type { QTableProps } from 'quasar'
import PageHeader from 'components/PageHeader.vue'
import AppTable from 'components/AppTable.vue'
import { institutionApi, type InstitutionDto } from 'src/api/institution'
import { useServerPagination } from 'src/composables/useServerPagination'

const router = useRouter()

/**
 * Kurum türü süzgeci. Varsayılan OKUL: il yetkilisinin aradığı şey neredeyse her zaman bir
 * okuldur; ilçe müdürlükleri listesi ayrı bir sorudur ve karıştırılırsa okul sayısı yanlış
 * okunur.
 */
const nodeTypeFilter = ref<string>('School')

const nodeTypeOptions = [
  { label: 'Okullar', value: 'School' },
  { label: 'İlçe Müdürlükleri', value: 'District' },
  { label: 'İl Müdürlükleri', value: 'Province' },
]

const filters = computed(() => ({ nodeType: nodeTypeFilter.value }))

const { rows: institutions, loading, pagination, search, onRequest, onSearch, load } =
  useServerPagination<InstitutionDto>({
    fetchFn: (params) => institutionApi.list(params),
    filters,
    defaultSortBy: 'fullName',
  })

const columns: QTableProps['columns'] = [
  { name: 'fullName', label: 'Kurum Adı', field: 'fullName', align: 'left', sortable: true },
  { name: 'institutionCode', label: 'Kurum Kodu', field: 'institutionCode', align: 'left', sortable: true },
  { name: 'location', label: 'İl / İlçe', field: 'provinceName', align: 'left' },
  { name: 'nodeTypeSlug', label: 'Tür', field: 'nodeTypeSlug', align: 'left' },
  { name: 'actions', label: '', field: 'id', align: 'right' },
]

/** İl ve ilçe tek hücrede; ikisi de boş olabilir (künyesi tamamlanmamış kayıt). */
function formatLocation(row: InstitutionDto): string {
  const parcalar = [row.provinceName, row.districtName].filter(Boolean)
  return parcalar.length > 0 ? parcalar.join(' / ') : '—'
}

/**
 * Detay için ayrı sayfa YOK: mevcut kurum sayfası açılır. Yazma butonları orada
 * `institution:manage` ile sarılı olduğundan sayfa il yetkilisinde kendiliğinden salt okunur
 * açılır — ikinci bir yetki kopyası yazılmaz.
 */
function openInstitution(id: string) {
  router.push(`/institutions/${id}`).catch(() => {})
}
</script>
```

- [ ] **Step 6: Rotaları ekle**

`src/WebUI/src/router/index.ts` — "Kurum" bloğundaki `institution/edit` rotasının ardına ekle:

```typescript
        // Kurum ağacı listesi (il/ilçe yetkilisi). Menüde yalnız üst düğüm kullanıcısına
        // görünür; rota izinle korunur, kapsam sunucudadır (InstitutionScopePolicy).
        {
          path: 'institutions',
          name: 'InstitutionList',
          component: () => import('pages/institution/InstitutionListPage.vue'),
          meta: { permissions: ['institution:view'] },
        },
        // Detay için ayrı sayfa YOK: mevcut kurum sayfası rota parametresiyle açılır.
        // Yazma butonları orada institution:manage ile sarılı olduğundan sayfa il
        // yetkilisinde kendiliğinden salt okunur açılır.
        {
          path: 'institutions/:id',
          name: 'InstitutionDetail',
          component: () => import('pages/institution/InstitutionPage.vue'),
          meta: { permissions: ['institution:view'] },
        },
```

- [ ] **Step 7: Ön yüz kapılarını koş**

Run: `pnpm -C src/WebUI lint && pnpm -C src/WebUI test:run && pnpm -C src/WebUI build`
Expected: PASS — lint temiz, testler yeşil, `vue-tsc` derlemesi hatasız.

- [ ] **Step 8: Commit**

```bash
git add src/WebUI/src/pages/institution/InstitutionListPage.vue \
        src/WebUI/src/router/index.ts \
        src/WebUI/src/composables/useNavigation.ts \
        src/WebUI/src/composables/useNavigation.upperNode.spec.ts
git commit -m "feat(webui): kurum listesi sayfası ve üst düğüm menü kapısı

Detay için yeni sayfa yazılmadı: satır /institutions/:id ile mevcut kurum
sayfasını açar ve yazma butonları orada institution:manage ile sarılı olduğu için
sayfa il yetkilisinde kendiliğinden salt okunur açılır — ikinci bir yetki kopyası
yok.

Menü kapısı izinle yapılamıyordu: okul müdürü de institution:view taşır (hatta
institution:*). Ayrım bağlı olunan düğümün tipinde. Bu bir görünürlük kararıdır,
yetki kararı değil — yetki sunucuda."
```

---

### Task 10: `InstitutionNodeTypeDriftTests` — okul listesi kilidi

`Institution` artık "okul" demek değil. Okul listesi üreten ve düğüm tipine göre süzmeyen bir sorgu, il/ilçe müdürlüğünü **okul sanar** — ve bu sessizce olur: açılır listede bir MEM adı belirir, kimse hata görmez.

**Files:**
- Test: `tests/MESNET.Security.UnitTests/InstitutionNodeTypeDriftTests.cs` (yeni)

**Interfaces:**
- Consumes: `InstitutionQueryExtensions.OfNodeType` (Task 2), `GetInstitutionsHandler` + `InstitutionTenantDirectory` + `RebuildInstitutionHierarchyHandler` (Task 5, Task 6).
- Produces: yok (kilit).

- [ ] **Step 1: Testi yaz**

`tests/MESNET.Security.UnitTests/InstitutionNodeTypeDriftTests.cs`:

```csharp
using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// <b><c>Institution</c> artık "okul" demek değil.</b>
///
/// <para>Kurum belgesi ağacın düğümüdür: il müdürlüğü, ilçe müdürlüğü ve okul aynı tiptir.
/// Okul listesi üreten ve düğüm tipine göre süzmeyen bir sorgu, il/ilçe müdürlüğünü okul
/// sanar — ve bu <b>sessizce</b> olur: bir açılır listede "Ankara İl Millî Eğitim Müdürlüğü"
/// belirir, istek 200 döner, log temiz kalır. Ne derleyici ne de mevcut testler görür.</para>
///
/// <para><b>Neden kaynak taraması, neden çalışma zamanı testi değil:</b> tehlike bir davranış
/// hatası değil, <b>unutulmuş bir süzgeç</b>. Unutulan süzgeci ancak "her sorgu şu
/// fonksiyondan geçmeli" kuralını tarayarak yakalayabilirsiniz; davranış testi yalnız
/// yazdığınız senaryoları görür ve unutulan yeni sorgu tanımı gereği yazılmamış olandır.
/// Aynı gerekçe <c>InstitutionScopeDriftTests</c>'te de var.</para>
/// </summary>
public sealed class InstitutionNodeTypeDriftTests
{
    private const string InstitutionApplicationPath = "Modules/Institution/MESNET.Institution.Application";

    /// <summary>Kurum belgesini koleksiyon olarak sorgulayan çağrılar.</summary>
    private static readonly Regex QueriesInstitutions = new(
        @"Query<(Core\.Entities\.)?Institution(Record)?>\(\)", RegexOptions.Compiled);

    /// <summary>Düğüm tipi süzgeci — tek ve taranabilir hedef.</summary>
    private static readonly Regex FiltersNodeType = new(@"\.OfNodeType\(", RegexOptions.Compiled);

    /// <summary>
    /// Düğüm tipine göre süzmesi <b>beklenmeyen</b> yerler.
    ///
    /// <para><c>RebuildInstitutionHierarchyHandler</c>: ağacı kurmak tanımı gereği bütün
    /// düğümleri görmeyi gerektirir. Uç kurum üstü izinle korunur
    /// (<c>platform:tenant:manage</c>) ve komut hiçbir kurum kimliği taşımaz.</para>
    /// </summary>
    private static readonly HashSet<string> MayIgnoreNodeType = new(StringComparer.Ordinal)
    {
        "RebuildInstitutionHierarchyHandler.cs",
    };

    [Fact]
    public void Kurum_sorgusu_dugum_tipine_gore_suzer()
    {
        var offenders = new List<string>();

        foreach (var file in SourceFilesUnder(Path.Combine(RepoRoot(), "src", InstitutionApplicationPath)))
        {
            var name = Path.GetFileName(file);
            if (MayIgnoreNodeType.Contains(name)) continue;

            var text = File.ReadAllText(file);
            if (!QueriesInstitutions.IsMatch(text)) continue;
            if (FiltersNodeType.IsMatch(text)) continue;

            offenders.Add(name);
        }

        offenders.ShouldBeEmpty(
            "Kurum belgesi düğüm tipi süzülmeden sorgulanıyor. Institution artık 'okul' demek "
            + "değil: il ve ilçe müdürlüğü düğümleri de aynı belgedir. Süzmeyen sorgu onları "
            + "OKUL SANAR ve bu sessizce olur — açılır listede bir MEB müdürlüğü adı belirir, "
            + "istek 200 döner, log temiz kalır. Sorguya .OfNodeType(...) ekleyin; gerçekten "
            + "bütün düğümleri görmesi gereken bir işse muafiyet listesine gerekçesiyle yazın. "
            + $"İhlaller: {string.Join(", ", offenders)}");
    }

    /// <summary>
    /// Muafiyet listesi <b>küçük kalmalı</b>. Büyümesi, kuralın kural olmaktan çıkıp
    /// istisnalar tablosuna dönüştüğünün işaretidir (<c>InstitutionScopeDriftTests</c> ile
    /// aynı gerekçe).
    /// </summary>
    [Fact]
    public void Muafiyet_listesi_kucuk_kalir()
    {
        MayIgnoreNodeType.Count.ShouldBeLessThanOrEqualTo(2);
    }

    /// <summary>
    /// <b>Liste bayatlamaz.</b> Muafiyet verilen dosya silinirse satır da silinmelidir;
    /// yoksa liste zamanla gerçekle ilgisini kaybeder.
    /// </summary>
    [Fact]
    public void Muafiyet_listesinde_olu_satir_kalmaz()
    {
        var existing = SourceFilesUnder(Path.Combine(RepoRoot(), "src", InstitutionApplicationPath))
            .Select(Path.GetFileName)
            .ToHashSet(StringComparer.Ordinal);

        var stale = MayIgnoreNodeType.Where(f => !existing.Contains(f)).ToList();

        stale.ShouldBeEmpty(
            $"Muafiyet listesinde artık var olmayan dosya var: {string.Join(", ", stale)}");
    }

    private static IEnumerable<string> SourceFilesUnder(string root)
    {
        var obj = $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}";
        var bin = $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}";

        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(obj, StringComparison.Ordinal)
                     && !f.Contains(bin, StringComparison.Ordinal));
    }

    /// <summary>
    /// Test derlemesi depo içinde değil <c>bin/</c> altında koşar; göreli yol doğrudan
    /// kullanılamaz — çözüm dosyası (<c>MESNET.slnx</c>) işaretçi olarak aranır.
    /// </summary>
    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MESNET.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Depo kökü bulunamadı (MESNET.slnx aranıyordu): {AppContext.BaseDirectory}");
    }
}
```

- [ ] **Step 2: Testi koş — yeşil olmalı**

Run: `dotnet test tests/MESNET.Security.UnitTests --filter FullyQualifiedName~InstitutionNodeTypeDriftTests`
Expected: PASS (3 test)

Kırmızıysa ihlal eden dosya adı hata metnindedir; **sorguya `.OfNodeType(...)` ekleyin**, testi gevşetmeyin. Bugün beklenen iki sorgu yeri `GetInstitutionsHandler` ve `InstitutionTenantDirectory`'dir; ikisi de Task 5 ve Task 6'da süzgeci aldı.

- [ ] **Step 3: Kilidin gerçekten kilitlediğini doğrula**

Kuralı geçici olarak boz: `InstitutionTenantDirectory.cs` içindeki `.OfNodeType(InstitutionNodeType.School)` satırını yorum satırına alın.

Run: `dotnet test tests/MESNET.Security.UnitTests --filter FullyQualifiedName~InstitutionNodeTypeDriftTests`
Expected: FAIL — `İhlaller: InstitutionTenantDirectory.cs`

Satırı geri alın ve testin yeniden yeşile döndüğünü doğrulayın. **Bu adımı atlamayın:** kaynak tarayan bir test yanlış yazıldığında hiçbir şey eşleşmez ve daima yeşil kalır — yani hiçbir şey korumaz.

- [ ] **Step 4: Commit**

```bash
git add tests/MESNET.Security.UnitTests/InstitutionNodeTypeDriftTests.cs
git commit -m "test(institution): okul listesi üreten sorgu düğüm tipine göre süzmek zorunda

Institution artık 'okul' demek değil: il ve ilçe müdürlüğü düğümleri de aynı
belge. Süzmeyen sorgu onları okul sanar ve bu sessizce olur — açılır listede bir
MEB müdürlüğü adı belirir, istek 200 döner, log temiz kalır.

Kaynak taraması seçildi çünkü tehlike bir davranış hatası değil, unutulmuş bir
süzgeç; davranış testi yalnız yazılmış senaryoları görür ve unutulan sorgu tanımı
gereği yazılmamış olandır."
```

---

### Task 11: `POST /api/institutions` gövdesi `nodeType` + `parentId` alır

Geçiş ucu var olan okullardan ağacı kurar; **yeni** bir il/ilçe müdürlüğü ya da yeni bir okul açmanın yolu bu uçtur. Task 2'den sonra herhangi bir noktada yapılabilir.

**Files:**
- Modify: `src/Modules/Institution/MESNET.Institution.Application/Commands/CreateInstitution.cs`
- Modify: `src/Modules/Institution/MESNET.Institution.Application/Handlers/CreateInstitutionHandler.cs`
- Modify: `src/Modules/Institution/MESNET.Institution.Application/Validators/CreateInstitutionValidator.cs`
- Modify: `src/Modules/Institution/MESNET.Institution.Application/Errors/InstitutionErrors.cs`
- Test: `tests/MESNET.Institution.UnitTests/CreateInstitutionNodeValidationTests.cs` (yeni)

**Interfaces:**
- Consumes: `InstitutionPath` (Task 1), `InstitutionNodeType` + `Institution.ParentId/NodeTypeName/Path` (Task 2).
- Produces:
  - `CreateInstitution` yeni son parametreleri: `string? NodeType = null`, `Guid? ParentId = null`
  - `InstitutionErrors.ParentNotFound(Guid parentId)`, `InstitutionErrors.ParentHasNoPath(Guid parentId)`, `InstitutionErrors.ProvinceCannotHaveParent()`

- [ ] **Step 1: Testi yaz (kırmızı)**

`tests/MESNET.Institution.UnitTests/CreateInstitutionNodeValidationTests.cs`:

```csharp
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Application.Validators;
using MESNET.Institution.Core.Enums;
using Shouldly;
using Xunit;

namespace MESNET.Institution.UnitTests;

/// <summary>
/// Yeni kurum açarken ağaçtaki yerin doğrulanması.
///
/// <para><b>Neden validator seviyesinde:</b> tanınmayan bir <c>nodeType</c> handler'a
/// ulaşırsa <c>InstitutionNodeType.Resolve</c> onu sessizce <c>School</c> yapar — kullanıcı
/// il müdürlüğü açtığını sanırken bir okul doğar ve bunu hiçbir hata bildirmez. Çözümleyicinin
/// hoşgörüsü OKUMA tarafı içindir; yazma sınırında küme kapalıdır.</para>
/// </summary>
public sealed class CreateInstitutionNodeValidationTests
{
    private static readonly CreateInstitutionValidator Validator = new();

    private static CreateInstitution Komut(
        string? nodeType = null, Guid? parentId = null, int code = 967523) =>
        new(code, "Test Kurumu", null, null, null, null, null,
            ProvinceCode: "06", DistrictName: "Yenimahalle",
            Id: null, NodeType: nodeType, ParentId: parentId);

    [Theory]
    [InlineData(null)]
    [InlineData("School")]
    [InlineData("District")]
    [InlineData("Province")]
    public void Bilinen_dugum_tipleri_kabul_edilir(string? nodeType)
    {
        Validator.Validate(Komut(nodeType, parentId: nodeType == "Province" ? null : Guid.NewGuid()))
            .Errors.ShouldNotContain(e => e.PropertyName == nameof(CreateInstitution.NodeType));
    }

    /// <summary>
    /// Tanınmayan tip REDDEDİLİR. Resolve onu sessizce School yapardı ve kullanıcı il
    /// müdürlüğü açtığını sanırken bir okul doğardı.
    /// </summary>
    [Fact]
    public void Taninmayan_dugum_tipi_reddedilir()
    {
        Validator.Validate(Komut("Bakanlik"))
            .Errors.ShouldContain(e => e.PropertyName == nameof(CreateInstitution.NodeType));
    }

    /// <summary>
    /// İl müdürlüğü kökündür; üstü olamaz. İzin verilseydi ağaç modellenen üç seviyeyi aşar
    /// ve "il yetkilisinin üstündeki il yetkilisi" gibi anlamsız bir kapsam doğardı.
    /// </summary>
    [Fact]
    public void Il_dugumunun_ustu_olamaz()
    {
        Validator.Validate(Komut(InstitutionNodeType.Province.Name, parentId: Guid.NewGuid()))
            .Errors.ShouldContain(e => e.PropertyName == nameof(CreateInstitution.ParentId));
    }

    /// <summary>
    /// Üst düğümlerin MEB kurum kodu bu geçişin elinde yok; sıfır "girilmedi" demektir.
    /// Okul için kural değişmez — kod zorunludur.
    /// </summary>
    [Fact]
    public void Ust_dugum_kurum_kodusuz_acilabilir_okul_acilamaz()
    {
        Validator.Validate(Komut(InstitutionNodeType.Province.Name, code: 0))
            .Errors.ShouldNotContain(e => e.PropertyName == nameof(CreateInstitution.InstitutionCode));

        Validator.Validate(Komut(InstitutionNodeType.School.Name, parentId: Guid.NewGuid(), code: 0))
            .Errors.ShouldContain(e => e.PropertyName == nameof(CreateInstitution.InstitutionCode));
    }
}
```

- [ ] **Step 2: Testin başarısız olduğunu doğrula**

Run: `dotnet test tests/MESNET.Institution.UnitTests --filter FullyQualifiedName~CreateInstitutionNodeValidationTests`
Expected: FAIL — `error CS1739: The best overload for 'CreateInstitution' does not have a parameter named 'NodeType'`

- [ ] **Step 3: Komutu genişlet**

`src/Modules/Institution/MESNET.Institution.Application/Commands/CreateInstitution.cs` — `Guid? Id = null);` satırını şununla değiştir:

```csharp
    Guid? Id = null,
    // Ağaçtaki tip — Province / District / School. Verilmezse OKUL: bugüne kadarki bütün
    // çağrılar okul açıyordu ve varsayılanı değiştirmek onları sessizce başka bir şey yapardı.
    string? NodeType = null,
    // Üst düğüm. İl müdürlüğü için verilmez (kök). Okul ve ilçe için verilmezse yol boş kalır
    // ve geçiş ucu (rebuild-hierarchy) doldurur.
    Guid? ParentId = null);
```

- [ ] **Step 4: Doğrulayıcıyı genişlet**

`src/Modules/Institution/MESNET.Institution.Application/Validators/CreateInstitutionValidator.cs` — yapıcının içine, mevcut kuralların ardına ekle:

```csharp
        // Küme YAZMA sınırında kapalıdır. InstitutionNodeType.Resolve tanınmayan değeri
        // sessizce School yapar — o hoşgörü OKUMA tarafı içindir (eski kayıtlar). Burada
        // sessiz kalsaydı kullanıcı il müdürlüğü açtığını sanırken bir okul doğardı.
        RuleFor(x => x.NodeType)
            .Must(name => InstitutionNodeType.TryFromName(name, ignoreCase: true, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.NodeType))
            .WithMessage("Geçerli bir kurum türü seçiniz (Province / District / School).");

        // İl müdürlüğü köktür; üstü olamaz. İzin verilseydi ağaç modellenen üç seviyeyi aşar
        // ve "il yetkilisinin üstündeki il yetkilisi" gibi anlamsız bir kapsam doğardı.
        RuleFor(x => x.ParentId)
            .Empty()
            .When(x => string.Equals(x.NodeType, InstitutionNodeType.Province.Name,
                StringComparison.OrdinalIgnoreCase))
            .WithMessage("İl müdürlüğü kök düğümdür, üst kurumu olamaz.");
```

Ve mevcut kurum kodu kuralını şununla değiştir:

```csharp
        // Kurum kodu OKUL için zorunludur. İl/ilçe müdürlüklerinin kendi MEB kodları vardır
        // ama sistem onları bilmiyor ve uydurulmuş bir kod gerçek veri gibi görünürdü; sıfır
        // "girilmedi" demektir (aynı gerekçe InstitutionHierarchyPlanner içinde de yazılı).
        RuleFor(x => x.InstitutionCode)
            .GreaterThan(0)
            .When(x => IsSchool(x.NodeType))
            .WithMessage("Kurum kodu sıfırdan büyük olmalıdır.");
```

Sınıfa yardımcı ekle:

```csharp
    /// <summary>Tip verilmediğinde varsayılan okuldur — bugüne kadarki bütün çağrılar okul açıyordu.</summary>
    private static bool IsSchool(string? nodeType) =>
        string.IsNullOrWhiteSpace(nodeType)
        || string.Equals(nodeType, InstitutionNodeType.School.Name, StringComparison.OrdinalIgnoreCase);
```

Dosyanın `using` listesine ekle:

```csharp
using MESNET.Institution.Core.Enums;
```

- [ ] **Step 5: Hataları ekle**

`src/Modules/Institution/MESNET.Institution.Application/Errors/InstitutionErrors.cs` — sınıfa ekle:

```csharp
    /// <summary>
    /// Verilen üst düğüm yok. Ağaç bağı kurulamaz; kayıt <b>yaratılmaz</b> — yolsuz bir düğüm
    /// yaratıp "sonra düzeltiriz" demek, kimsenin göremediği bir kayıt bırakmaktır.
    /// </summary>
    public static Error ParentNotFound(Guid parentId) =>
        new("Institution.ParentNotFound",
            $"Üst kurum bulunamadı (kimlik: {parentId}).");

    /// <summary>
    /// Üst düğümün yolu yok — geçiş ucu (<c>POST /api/institutions/rebuild-hierarchy</c>) o
    /// kayıt için henüz koşmamış. Yolsuz bir üstün altına düğüm eklenirse çocuğun yolu da
    /// kurulamaz ve ikisi de hiçbir kapsamda görünmez.
    /// </summary>
    public static Error ParentHasNoPath(Guid parentId) =>
        new("Institution.ParentHasNoPath",
            $"Üst kurumun ağaç bilgisi eksik (kimlik: {parentId}). "
            + "Önce POST /api/institutions/rebuild-hierarchy çalıştırılmalıdır.");
```

- [ ] **Step 6: Handler'ı güncelle**

`src/Modules/Institution/MESNET.Institution.Application/Handlers/CreateInstitutionHandler.cs` — `Handle` metodunu şununla değiştir:

```csharp
    public static async Task<Guid> Handle(
        CreateInstitution command, IDocumentSession session, IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var id = command.Id ?? Guid.NewGuid();
        var nodeType = InstitutionNodeType.Resolve(command.NodeType);

        // Yol üst düğümden türetilir. Üst yoksa yalnız İL düğümü kök olarak yol alır; okul ve
        // ilçe yolsuz doğar ve geçiş ucu doldurur (bugünkü kayıtlarla aynı durum).
        string? path = null;

        if (command.ParentId is { } parentId)
        {
            var parent = await session
                .LoadAsync<Core.Entities.Institution>(parentId, cancellationToken)
                ?? throw new DomainException(InstitutionErrors.ParentNotFound(parentId));

            // Yolsuz bir üstün altına düğüm eklenirse çocuğun yolu da kurulamaz ve İKİSİ de
            // hiçbir kapsamda görünmez — hata değil, sessiz boşluk. Bu yüzden reddedilir.
            if (string.IsNullOrWhiteSpace(parent.Path))
                throw new DomainException(InstitutionErrors.ParentHasNoPath(parentId));

            path = InstitutionPath.Child(parent.Path, id);
        }
        else if (nodeType == InstitutionNodeType.Province)
        {
            path = InstitutionPath.Root(id);
        }

        var institution = new Core.Entities.Institution
        {
            Id = id,
            InstitutionCode = command.InstitutionCode,
            FullName = command.FullName,
            Address = command.Address,
            PhoneNumber = command.PhoneNumber,
            Email = command.Email,
            WebUrl = command.WebUrl,
            Location = command.Location,
            ProvinceCode = command.ProvinceCode,
            DistrictName = command.DistrictName,
            ParentId = command.ParentId,
            NodeTypeName = nodeType.Name,
            Path = path
        };

        session.Store(institution);

        await bus.PublishAsync(new InstitutionUpdated(
            institution.Id, institution.FullName, institution.Location,
            institution.ScheduleConfig?.DailyPeriodCount ?? 0));

        return institution.Id;
    }
```

Dosyanın `using` listesine ekle:

```csharp
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Institution.Application.Errors;
using MESNET.Institution.Core.Enums;
```

- [ ] **Step 7: Testleri koş**

Run: `dotnet build MESNET.slnx && dotnet test tests/MESNET.Institution.UnitTests`
Expected: PASS

- [ ] **Step 8: Commit**

```bash
git add src/Modules/Institution/MESNET.Institution.Application/Commands/CreateInstitution.cs \
        src/Modules/Institution/MESNET.Institution.Application/Handlers/CreateInstitutionHandler.cs \
        src/Modules/Institution/MESNET.Institution.Application/Validators/CreateInstitutionValidator.cs \
        src/Modules/Institution/MESNET.Institution.Application/Errors/InstitutionErrors.cs \
        tests/MESNET.Institution.UnitTests/CreateInstitutionNodeValidationTests.cs
git commit -m "feat(institution): yeni kurum ağaçtaki yeriyle açılır (nodeType + parentId)

Küme YAZMA sınırında kapalı: tanınmayan nodeType reddedilir. Resolve'un hoşgörüsü
OKUMA tarafı içindir (eski kayıtlar); yazmada sessiz kalsaydı kullanıcı il
müdürlüğü açtığını sanırken bir okul doğardı.

Yolsuz bir üstün altına düğüm eklenmez: çocuğun yolu da kurulamaz ve İKİSİ de
hiçbir kapsamda görünmez — hata değil, sessiz boşluk."
```

---

### Task 12: `InstitutionScopeDriftTests` — elle kimlik karşılaştırması kalmasın

Kapsam kararı artık iki aşamalıdır (kimlik + yol). Kararı **kopyalayan** bir uç, ağaç aşamasını atlar: kimlikleri karşılaştırır, eşit değilse reddeder — yani il yetkilisi hiçbir okulun kaydını açamaz. Bu **sessizdir**: kod derlenir, mevcut testler yeşil kalır.

**Files:**
- Modify: `tests/MESNET.Security.UnitTests/InstitutionScopeDriftTests.cs`

**Interfaces:**
- Consumes: `InstitutionScopePolicy` (Task 1), guard (Task 4), liste handler'ı (Task 5).
- Produces: yok (kilit).

- [ ] **Step 1: Yeni kilidi ekle**

`tests/MESNET.Security.UnitTests/InstitutionScopeDriftTests.cs` — sınıfa ekle:

```csharp
    /// <summary>
    /// Aktörün kurum claim'ini <b>elle</b> bir hedefle karşılaştıran kod.
    ///
    /// <para>Kalıp: <c>...InstitutionId ==</c> / <c>!=</c> — kapsam kararının kopyası.</para>
    /// </summary>
    private static readonly Regex HandRolledScopeComparison = new(
        @"InstitutionId\s*(==|!=)\s*\w*[Ii]nstitutionId", RegexOptions.Compiled);

    /// <summary>
    /// Kapsam kararını <b>kopyalayan</b> uç kalmamalı; karar politikadan geçmeli.
    ///
    /// <para><b>Neden kilitleniyor:</b> karar artık iki aşamalıdır — kimlik eşitliği, sonra
    /// gerekiyorsa ağaçtaki yol. Kararı kopyalayan bir yer <b>yalnız birinci aşamayı</b>
    /// yapar: kimlikler eşit değilse reddeder. Sonuç, il yetkilisinin kendi ilindeki hiçbir
    /// okulun kaydını açamaması — ve bu sessizdir: kod derlenir, mevcut testler yeşil kalır,
    /// istek 422 döner ve mesajı "yalnız kendi kurumunuz" der (doğru görünür).</para>
    ///
    /// <para><b>Muafiyet <see cref="InstitutionScopePolicy"/>'nin kendisidir</b> — karar orada
    /// yaşar. <c>UserInstitutionScopePolicy</c> de muaftır: o KİRACI BAĞI yazmayı yönetir,
    /// veri erişimini değil, ve kuralı bilinçli olarak kimlik eşitliğidir (bkz. planın
    /// "Kapsam atama neden A'da bir görev DEĞİL" bölümü).</para>
    /// </summary>
    [Fact]
    public void Kapsam_karari_kopyalanmaz()
    {
        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            "InstitutionScopePolicy.cs",
            "UserInstitutionScopePolicy.cs",
        };

        var offenders = new List<string>();

        foreach (var file in SourceFilesUnder(Path.Combine(RepoRoot(), "src", GuardableApplicationPath)))
        {
            var name = Path.GetFileName(file);
            if (allowed.Contains(name)) continue;

            if (HandRolledScopeComparison.IsMatch(File.ReadAllText(file)))
                offenders.Add(name);
        }

        offenders.ShouldBeEmpty(
            "Kapsam kararı elle kopyalanmış. Karar artık iki aşamalı: kimlik eşitliği, sonra "
            + "gerekiyorsa ağaçtaki yol. Kopya YALNIZ birinci aşamayı yapar ve il/ilçe "
            + "yetkilisi alt ağacındaki hiçbir kaydı açamaz — sessizce: kod derlenir, testler "
            + "yeşil kalır, istek 422 döner ve mesajı doğru görünür. Kararı "
            + $"InstitutionScopePolicy'ye devredin. İhlaller: {string.Join(", ", offenders)}");
    }
```

- [ ] **Step 2: Testin yeşil olduğunu doğrula**

Run: `dotnet test tests/MESNET.Security.UnitTests --filter FullyQualifiedName~InstitutionScopeDriftTests`
Expected: PASS

Kırmızıysa ihlal eden dosya adı hata metnindedir; o dosyadaki karşılaştırmayı `InstitutionScopePolicy.Decide(...)` çağrısına çevirin.

- [ ] **Step 3: Kilidin gerçekten kilitlediğini doğrula**

`GetInstitutionHandler.cs` dosyasının başına geçici olarak şu satırı ekleyin (derlenmesi gerekmez, tarama kaynak metnine bakar):

```csharp
// if (actorInstitutionId == query.InstitutionId) { }
```

Run: `dotnet test tests/MESNET.Security.UnitTests --filter FullyQualifiedName~Kapsam_karari_kopyalanmaz`
Expected: FAIL — `İhlaller: GetInstitutionHandler.cs`

Satırı geri alın ve testin yeşile döndüğünü doğrulayın. **Bu adımı atlamayın:** kaynak tarayan bir test yanlış yazıldığında hiçbir şey eşleşmez ve daima yeşil kalır — yani hiçbir şey korumaz.

- [ ] **Step 4: Commit**

```bash
git add tests/MESNET.Security.UnitTests/InstitutionScopeDriftTests.cs
git commit -m "test(institution): kapsam kararı kopyalanamaz — karar politikadan geçmeli

Karar artık iki aşamalı: kimlik eşitliği, sonra gerekiyorsa ağaçtaki yol. Kararı
kopyalayan bir yer YALNIZ birinci aşamayı yapar ve il/ilçe yetkilisi alt
ağacındaki hiçbir kaydı açamaz — sessizce: kod derlenir, testler yeşil kalır,
istek 422 döner ve mesajı doğru görünür."
```

---

### Task 13: Ön yüz — kurum listesi sayfası sayfalama ve arama testi

**Files:**
- Test: `src/WebUI/src/pages/institution/InstitutionListPage.spec.ts` (yeni)

**Interfaces:**
- Consumes: `InstitutionListPage.vue` (Task 9), `institutionApi.list` (Task 8).
- Produces: yok (kilit).

- [ ] **Step 1: Testi yaz**

`src/WebUI/src/pages/institution/InstitutionListPage.spec.ts`:

```typescript
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { ref, computed } from 'vue'
import { useServerPagination } from 'src/composables/useServerPagination'
import type { PagedResponse } from 'src/types/pagination'
import type { InstitutionDto } from 'src/api/institution'

/**
 * Liste sayfasının sunucu sözleşmesi.
 *
 * <p>Test bileşeni monte etmez, <b>sayfanın sunucuya ne sorduğunu</b> ölçer. Kırılgan olan
 * kısım şablon değil sözleşmedir: `nodeType` süzgeci gitmezse liste il/ilçe müdürlüklerini
 * okul gibi gösterir, `page`/`search` gitmezse sayfalama ve arama sessizce istemci tarafına
 * düşer ve yalnız ilk 20 satır aranır.</p>
 */
describe('InstitutionListPage — sunucu sözleşmesi', () => {
  const bosSayfa: PagedResponse<InstitutionDto> = {
    items: [],
    totalCount: 0,
    page: 1,
    pageSize: 20,
    totalPages: 0,
    hasNextPage: false,
    hasPreviousPage: false,
  }

  let fetchFn: ReturnType<typeof vi.fn>

  beforeEach(() => {
    fetchFn = vi.fn().mockResolvedValue({ data: bosSayfa })
  })

  it('varsayılan süzgeç OKUL — üst düğümler okul listesinde görünmemeli', async () => {
    // Arrange
    const nodeType = ref('School')
    const { load } = useServerPagination<InstitutionDto>({
      fetchFn,
      filters: computed(() => ({ nodeType: nodeType.value })),
      defaultSortBy: 'fullName',
    })

    // Act
    await load()

    // Assert
    expect(fetchFn).toHaveBeenCalledWith(expect.objectContaining({ nodeType: 'School' }))
  })

  it('kurum türü süzgeci sunucuya gider', async () => {
    const nodeType = ref('District')
    const { load } = useServerPagination<InstitutionDto>({
      fetchFn,
      filters: computed(() => ({ nodeType: nodeType.value })),
      defaultSortBy: 'fullName',
    })

    await load()

    expect(fetchFn).toHaveBeenCalledWith(expect.objectContaining({ nodeType: 'District' }))
  })

  it('arama terimi sunucuya gider — istemci tarafında süzülmez', async () => {
    const { onSearch } = useServerPagination<InstitutionDto>({
      fetchFn,
      filters: computed(() => ({ nodeType: 'School' })),
      defaultSortBy: 'fullName',
    })

    await onSearch('Atatürk')

    expect(fetchFn).toHaveBeenCalledWith(expect.objectContaining({ search: 'Atatürk' }))
  })

  it('sayfa isteği sunucuya gider', async () => {
    const { onRequest } = useServerPagination<InstitutionDto>({
      fetchFn,
      filters: computed(() => ({ nodeType: 'School' })),
      defaultSortBy: 'fullName',
    })

    await onRequest({ pagination: { page: 3, rowsPerPage: 20, sortBy: 'fullName', descending: false } })

    expect(fetchFn).toHaveBeenCalledWith(expect.objectContaining({ page: 3 }))
  })

  it('varsayılan sıralama kurum adıdır — sıralamasız liste her yazmadan sonra kayardı', async () => {
    const { load } = useServerPagination<InstitutionDto>({
      fetchFn,
      filters: computed(() => ({ nodeType: 'School' })),
      defaultSortBy: 'fullName',
    })

    await load()

    expect(fetchFn).toHaveBeenCalledWith(expect.objectContaining({ sortBy: 'fullName' }))
  })
})
```

- [ ] **Step 2: Testi koş**

Run: `pnpm -C src/WebUI test:run -- InstitutionListPage`
Expected: PASS (5 test)

`onSearch` / `onRequest` imzaları `useServerPagination.spec.ts` içindeki mevcut kullanımla eşleşmiyorsa **o dosyadaki imzayı** referans alın; composable'ı değiştirmeyin.

- [ ] **Step 3: Ön yüz kapılarını koş**

Run: `pnpm -C src/WebUI lint && pnpm -C src/WebUI test:run`
Expected: PASS

- [ ] **Step 4: Commit**

```bash
git add src/WebUI/src/pages/institution/InstitutionListPage.spec.ts
git commit -m "test(webui): kurum listesi sunucu sözleşmesi kilitlendi

Test bileşeni monte etmez, sayfanın sunucuya NE SORDUĞUNU ölçer: kırılgan olan
şablon değil sözleşme. nodeType gitmezse liste il/ilçe müdürlüklerini okul gibi
gösterir; page/search gitmezse sayfalama ve arama sessizce istemciye düşer ve
yalnız ilk 20 satır aranır."
```

---

## Bitirme kapısı

Bütün görevler bittikten sonra, ayrı bir commit gerektirmeyen doğrulama turu.

- [ ] **Adım 1: Tüm CI işlerini yerelde koş**

Run: `./scripts/ci-local.sh backend`
Expected: PASS — tüm birim testleri yeşil.

Run: `./scripts/ci-local.sh frontend`
Expected: PASS — `pnpm install` → `lint` → `test:run` → `build` zinciri temiz.

Run: `./scripts/ci-local.sh docs`
Expected: PASS

- [ ] **Adım 2: Yığını ayağa kaldır ve geçişi koştur**

```bash
ASPIRE_CONTAINER_RUNTIME=podman dotnet run --project src/MESNET.AppHost -c Debug
```

Yığın ayağa kalktıktan sonra token alın ve geçişi çalıştırın:

```bash
TOKEN=$(curl -s -X POST 'http://localhost:8080/realms/mesnet/protocol/openid-connect/token' \
  -d 'grant_type=password&client_id=mesnet-api&client_secret=dev-secret&username=admin&password=admin' \
  | python3 -c 'import sys,json; print(json.load(sys.stdin)["access_token"])')

curl -s -X POST http://localhost:5270/api/institutions/rebuild-hierarchy \
  -H "Authorization: Bearer $TOKEN" | python3 -m json.tool
```

Expected: `provincesCreated` ve `districtsCreated` sıfırdan büyük, `skippedNoProvince` sıfır (dev tohum verisindeki okulların künyeleri doludur). Sıfırdan büyükse hangi okulların il kodu eksik olduğu API log'undadır.

- [ ] **Adım 3: İdempotanlığı canlıda doğrula**

Aynı `curl` komutunu **ikinci kez** çalıştırın.

Expected: `provincesCreated: 0`, `districtsCreated: 0`, `nodesUpdated` aynı sayı. Sıfırdan büyük bir "created" değeri, geçişin düğüm çoğalttığını gösterir — planlayıcının anahtarlama mantığı bozuk demektir.

- [ ] **Adım 4: Kapsamı canlıda ölç**

```bash
curl -s "http://localhost:5270/api/institutions?nodeType=Province" \
  -H "Authorization: Bearer $TOKEN" | python3 -m json.tool
```

Expected: il müdürlüğü düğümleri döner, adları `... İl Millî Eğitim Müdürlüğü`, `nodeType: "Province"`, `parentId: null`.

```bash
curl -s "http://localhost:5270/api/institutions" \
  -H "Authorization: Bearer $TOKEN" | python3 -m json.tool
```

Expected: **yalnız okullar** döner (varsayılan süzgeç). Listede bir MEM adı görünüyorsa `OfNodeType` süzgeci çalışmıyordur.

- [ ] **Adım 5: Arayüzü gözle doğrula**

Tarayıcıda `http://localhost:5173` açın ve giriş yapın (**parolayı siz girin**).

Doğrulanacaklar:
1. Okul kullanıcısında **"Kurumlar" menü girdisi görünmemeli** — "Kurum Bilgileri" görünmeli.
2. Kurum sayfası kendi okulunu açmalı; başlıktaki ad `/auth/me` içindeki `institutionId` ile aynı okulun adı olmalı.
3. Bir il yetkilisi hesabı oluşturup (Keycloak konsolu → kullanıcı → `ProvincialAdmin` rolü, ardından `POST /api/security/users/{id}/institution` ile il müdürlüğü düğümüne bağlayın) giriş yapın:
   - "Kurumlar" menü girdisi görünmeli
   - Liste kendi ilindeki okulları göstermeli, **başka ilin okulunu göstermemeli**
   - Bir satıra tıklayınca kurum sayfası açılmalı ve **düzenle/kaydet butonları görünmemeli** (salt okunur)

- [ ] **Adım 6: Yığını kapat**

```bash
kill $(pgrep -f MESNET.AppHost)
```
