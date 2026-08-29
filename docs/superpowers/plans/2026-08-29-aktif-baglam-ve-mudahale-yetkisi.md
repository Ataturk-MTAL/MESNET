# Aktif Bağlam Değiştirme ve Müdahale Yetkisi (B parçası) — Uygulama Planı

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** İl/ilçe yetkilisinin alt ağacındaki bir okulun bağlamına geçip o okulu görebilmesi ve dört adlı müdahaleyi yapabilmesi — kiracı anahtarı istekten alınmadan, her eylem izli.

**Architecture:** Aktif bağlam `UserAccount`'ta saklanır ve `PermissionClaimsTransformation` tarafından istek başına `active_institution_id` claim'ine dönüştürülür. `TenantResolution` geçerli bağlamı kiracı olarak tercih eder. `institution_id` claim'ine DOKUNULMAZ — denetim izinin "kim olduğun" / "nerede davrandığın" ayrımı ona bağlıdır. Yazma yetkisi mevcut izinlerle karşılanır; yalnız "okula ilk yöneticiyi bağlama" yeni ve koşullu bir izin alır.

**Tech Stack:** .NET 10, Wolverine 6.15.0, Marten 9, PostgreSQL, Keycloak, Vue 3 + Quasar + Pinia, Vitest.

**Spec:** `docs/superpowers/specs/2026-08-29-aktif-baglam-ve-mudahale-yetkisi.md`

---

## Global Constraints

Her görevin gereksinimleri bu bölümü **örtük olarak içerir**.

### Ölçülmüş Keycloak davranışı (29.08.2026, canlı realm)

| Ölçüm | Sonuç |
|---|---|
| Kullanıcı access token'ında `sid` | **Var** — mapper YOK, realm değişikliği YOK |
| `session_state` | Yok (yeni Keycloak'ta `sid` onun yerini aldı) — **kullanmayın** |
| Token yenilemede `sid` | **Sabit** |
| Yeni girişte `sid` | **Değişiyor** |

`mesnet-realm.json` dosyasına oturum claim eşleyicisi **EKLENMEYECEK**.

### Kiracılık ve kapsam (ADR-0003)

- `institution_id` claim'i **ev kurumudur** ve aktif bağlamla **EZİLMEZ**. Ezilirse denetim izinde `CrossedTenantBoundary` her zaman `false` olur ve B'nin izli verilmesinin tek sebebi ortadan kalkar.
- Token'dan gelen `active_institution_id` **her istekte koşulsuz silinir** — kayıt boş olsa bile. "Kaynak yoksa token'a düş" davranışı, kaydı olmayan kullanıcıya kendi bağlamını seçtirirdi.
- `active_institution_id` Keycloak'a **YAZILMAZ** — ne bağlam değiştirmede, ne `SyncUsersFromKeycloak`'ta.
- Kapsam doğrulaması **iki yerde**: bağlam değiştirme anında VE her çözümlemede. Ağaç değişebilir; yalnız yazma anında doğrulanan bağlam sessizce yetki taşımaya devam ederdi.
- Argümansız Marten session açmak yasaktır (`DefaultTenantUsageEnabled = false`).

### Yetkilendirme

- Yetkilendirme **permission bazlıdır**; `RequireRole` KULLANILMAZ, rol adına bakan yeni kontrol yazılmaz.
- `AssignablePermissionScope.Defaults[ProvincialAdmin]` ve `[DistrictAdmin]` **BOŞ KALIR**. O liste "bu rol başkasına hangi izinleri dağıtabilir" sorusudur; açılırsa il yetkilisi kendi verdiği izinlerle kapsamını genişletir.
- Yeni izinler `NeverDirectlyAssignable` kümesine girer.

### Mimari sınırlar

- Endpoint metodu iş mantığı içermez, `IQuerySession`/`IDocumentSession` enjekte etmez; `IMessageBus` ile handler'a devreder. Tek istisna `ICurrentUserService`.
- Anonim uç EKLENMEZ (`AnonymousEndpointDriftTests`).
- Modüller arası doğrudan veri yazma yasaktır.

### Arayüz

- Tüm metin **Türkçe**, doğru Türkçe karakterlerle (ç ş ğ ü ö ı İ). ASCII yaklaşımı yasak.
- `<script setup>` zorunlu. Mutable state `ref()` ile; düz `let` yasak. Fire-and-forget çağrılara `.catch(() => {})`.
- Yalnız ikon içeren `q-btn` hem `aria-label` hem `<q-tooltip>` taşır; `title` attribute KULLANILMAZ.
- Sayfa ve testi **aynı** saf kaynağı okur (`utils/institutionScope.ts`, `pages/audit/auditListQuery.ts` deseni).

### Test disiplini

- **Her kilitleyen testin gerçekten kilitlediği KANITLANIR:** korunan şeyi **sil** (yorum satırına almak yetmez — kaynak tarayan testler yorumu da okur), testin **kırmızıya döndüğünü** ve **hangi ismi verdiğini** rapora yaz, sonra geri al.
- Bu depoda A ve C parçalarında **altı** kez "yeşil ama hiçbir şeyi korumayan kilit" ölçüldü. Kanıt adımı isteğe bağlı değildir.
- Testler AAA yapısında, açıklayıcı Türkçe adlarla. Test projelerinde `using Xunit;` gerekir (`ImplicitUsings` Xunit'i eklemiyor).

### Bilinen ve KABUL EDİLEN bedeller (düzeltmeye çalışmayın)

1. **Bağlamın ömrü SSO oturumunun ömrüdür**, sekmeninki değil. Sekmeyi kapatıp dönen kullanıcı, SSO oturumu canlıysa aynı bağlamda devam eder.
2. **İki sekme aynı bağlamı paylaşır** — sunucuda saklamanın doğrudan sonucu.
3. **Bağlam değişimi izin önbelleğini geçersiz kılar** — kullanıcının bütün claim'leri yeniden hesaplanır.
4. **`institution:manage` müdahale sınırını aşar** — marka paleti ve ders programı yapılandırması da açılır. Üçü de denetlenir.
5. **Yetki reddi (403) ize girmez** (C'den devralındı).
6. **İl geneli sayılar YOK** — sonraki sürüme bırakıldı. Sayı ekleme işi bu planın kapsamı dışındadır.

---

## Dosya Yapısı

**Backend — yeni**

| Dosya | Sorumluluk |
|---|---|
| `src/Modules/Security/MESNET.Security.Application/Commands/SetActiveInstitution.cs` | Bağlam değiştirme komutu |
| `src/Modules/Security/MESNET.Security.Application/Handlers/SetActiveInstitutionHandler.cs` | Kapsam doğrulaması + yazma + önbellek geçersizleme |
| `src/MESNET.Common.Shared/Security/ActiveContextPolicy.cs` | Saf: bağlam geçerli mi (`sid` + alt ağaç) |
| `src/MESNET.Common.Shared/Security/InstitutionBootstrapPolicy.cs` | Saf: okula ilk yönetici bağlanabilir mi |

**Backend — değişiklik**

| Dosya | Değişiklik |
|---|---|
| `src/Modules/Security/MESNET.Security.Core/Entities/UserAccount.cs` | `ActiveInstitutionId`, `ActiveContextSessionId` |
| `src/MESNET.Common.Infrastructure/Security/PermissionClaimsTransformation.cs` | `active_institution_id` üret/sil |
| `src/MESNET.Common.Shared/Tenancy/TenantResolution.cs` | Aktif bağlamı tercih et |
| `src/MESNET.Common.Shared/Security/UserContext.cs` | `ActiveInstitutionId` |
| `src/MESNET.Common.Infrastructure/Security/ICurrentUserService.cs` + `CurrentUserService.cs` | `GetActiveInstitutionId()` |
| `src/MESNET.Common.Infrastructure/Tenancy/TenantResolutionMiddleware.cs` | Aktif bağlamı geçir |
| `src/Modules/Audit/MESNET.Audit.Application/Auditing/AuditMiddleware.cs` + `AuditContext.cs` | Aktif bağlamı taşı |
| `src/Modules/Audit/MESNET.Audit.Core/Services/AuditEntryFactory.cs` | `ResolveSubject` düşme sırası |
| `src/MESNET.Common.Shared/Security/Permissions.cs` | `Directorate` sınıfı + `Internship.ApprovalOverride` |
| `src/MESNET.Common.Shared/Security/RolePermissionMap.cs` | İki role izin demeti |
| `src/MESNET.Common.Shared/Security/AssignablePermissionScope.cs` | `NeverDirectlyAssignable` |
| `src/Modules/Security/MESNET.Security.Api/UserManagementEndpoints.cs` | Bağlam ucu |
| `src/Modules/Security/MESNET.Security.Application/Handlers/ChangeUserInstitutionHandler.cs` | Bootstrap dalı |
| `src/Modules/Internship/MESNET.Internship.Api/InternshipEndpoints.cs` | Override izni |
| `src/MESNET.Presentation/AuthEndpoint.cs` | `/me` aktif bağlam |

**Ön yüz**

| Dosya | |
|---|---|
| `src/WebUI/src/api/security.ts` (varsa) ya da `api/auth.ts` | `setActiveInstitution` |
| `src/WebUI/src/stores/auth.ts` | `activeInstitutionId`, `currentInstitutionId` |
| `src/WebUI/src/composables/useInstitutionContext.ts` | **Bağlam değişiminin TEK yolu** |
| `src/WebUI/src/stores/academicPeriod.ts` | `loadedInstitutionId` koruması |
| `src/WebUI/src/pages/institution/ContextSelectPage.vue` | Bağlamsız ağaç listesi |
| `src/WebUI/src/pages/institution/contextSelectQuery.ts` | Sayfa+test ortak kaynağı |
| `src/WebUI/src/layouts/MainLayout.vue` | Üst bar seçici + gösterge |
| `src/WebUI/src/router/index.ts` | `/context` rotası |

---

## Görev Sırası

1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9 → 10. Hiçbir görev derlemeyi kırık bırakmaz.

---

### Task 1: `ActiveContextPolicy` (saf) + `UserAccount` alanları

Aktif bağlamın **kullanılabilir olup olmadığı** kararının tamamı tek bir saf fonksiyonda. Karar burada, girdi toplama çağıranlarda — `InstitutionScopePolicy` / `InstitutionScopeGuardMiddleware` ile aynı idiom.

**Files:**
- Create: `src/MESNET.Common.Shared/Security/ActiveContextPolicy.cs`
- Modify: `src/Modules/Security/MESNET.Security.Core/Entities/UserAccount.cs`
- Test: `tests/MESNET.Security.UnitTests/ActiveContextPolicyTests.cs`

**Interfaces:**
- Consumes: `InstitutionScopePolicy.CanAccessByPath(string? ancestorPath, string? descendantPath)` (A parçası)
- Produces: `ActiveContextPolicy.Resolve(Guid? activeInstitutionId, string? storedSessionId, string? currentSessionId, string? actorPath, string? targetPath)` → `Guid?`

- [ ] **Step 1: Başarısız testi yaz**

`tests/MESNET.Security.UnitTests/ActiveContextPolicyTests.cs`:

```csharp
using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Aktif bağlamın kullanılabilirlik kararı.
///
/// <para><b>Neden saf bir fonksiyon:</b> aynı karar iki yerde veriliyor — kiracı
/// çözümlemesinde ve izin dönüşümünde. İki kopya olsaydı biri değişip diğeri kalırdı ve
/// bayat bir bağlam yalnız birinde düşerdi: kullanıcı bir ekranda A okulunu, diğerinde B
/// okulunu görürdü.</para>
///
/// <para><b>Geçersiz bağlam HATA DEĞİLDİR.</b> <c>null</c> döner ve çağıran ev kurumuna
/// düşer. Bayat bağlam bir yetki ihlali değil, bir zamanaşımıdır; kullanıcı okulu yeniden
/// seçer.</para>
/// </summary>
public sealed class ActiveContextPolicyTests
{
    private static readonly Guid Okul = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string IlYolu = "/il/";
    private const string OkulYolu = "/il/ilce/okul/";
    private const string BaskaIlinOkuluYolu = "/baska-il/ilce/okul/";

    [Fact]
    public void Gecerli_baglam_kurum_kimligini_dondurur()
    {
        var sonuc = ActiveContextPolicy.Resolve(
            activeInstitutionId: Okul,
            storedSessionId: "oturum-1",
            currentSessionId: "oturum-1",
            actorPath: IlYolu,
            targetPath: OkulYolu);

        sonuc.ShouldBe(Okul);
    }

    [Fact]
    public void Baglam_yoksa_null_doner()
    {
        ActiveContextPolicy.Resolve(null, "oturum-1", "oturum-1", IlYolu, OkulYolu)
            .ShouldBeNull();
    }

    [Fact]
    public void Bos_Guid_baglam_sayilmaz()
    {
        ActiveContextPolicy.Resolve(Guid.Empty, "oturum-1", "oturum-1", IlYolu, OkulYolu)
            .ShouldBeNull();
    }

    [Fact]
    public void Bayat_oturumda_baglam_dusurulur()
    {
        // Yeni girişte sid değişir (ölçüldü). Dünkü seçim bugün geçerli değildir.
        ActiveContextPolicy.Resolve(Okul, "oturum-1", "oturum-2", IlYolu, OkulYolu)
            .ShouldBeNull();
    }

    [Fact]
    public void Saklanmis_oturum_kimligi_yoksa_baglam_dusurulur()
    {
        // Kimliksiz saklanmış bağlam hiçbir oturuma ait değildir; süresiz yaşamamalı.
        ActiveContextPolicy.Resolve(Okul, null, "oturum-1", IlYolu, OkulYolu)
            .ShouldBeNull();
    }

    [Fact]
    public void Istekte_oturum_kimligi_yoksa_baglam_dusurulur()
    {
        // Token'da sid gelmiyorsa bağlamın hangi oturumda kurulduğu doğrulanamaz.
        ActiveContextPolicy.Resolve(Okul, "oturum-1", null, IlYolu, OkulYolu)
            .ShouldBeNull();
    }

    [Fact]
    public void Alt_agac_disindaki_hedef_dusurulur()
    {
        // AĞAÇ DEĞİŞEBİLİR: okul başka ilçeye taşınabilir. Yalnız yazma anında doğrulanan
        // bir bağlam sessizce yetki taşımaya devam ederdi.
        ActiveContextPolicy.Resolve(Okul, "oturum-1", "oturum-1", IlYolu, BaskaIlinOkuluYolu)
            .ShouldBeNull();
    }

    [Fact]
    public void Aktorun_yolu_yoksa_baglam_dusurulur()
    {
        // Geçiş ucu koşmamış aktör alt ağaç iddiasında bulunamaz.
        ActiveContextPolicy.Resolve(Okul, "oturum-1", "oturum-1", null, OkulYolu)
            .ShouldBeNull();
    }

    [Fact]
    public void Hedefin_yolu_yoksa_baglam_dusurulur()
    {
        ActiveContextPolicy.Resolve(Okul, "oturum-1", "oturum-1", IlYolu, null)
            .ShouldBeNull();
    }

    [Fact]
    public void Oturum_kimligi_karsilastirmasi_buyuk_kucuk_harfe_duyarlidir()
    {
        // sid rastgele üretilmiş bir dizedir; harf katlaması iki ayrı oturumu eşitleyebilir.
        ActiveContextPolicy.Resolve(Okul, "AbC", "abc", IlYolu, OkulYolu)
            .ShouldBeNull();
    }
}
```

- [ ] **Step 2: Testi koştur, kırmızı gör**

```bash
dotnet test tests/MESNET.Security.UnitTests --filter "FullyQualifiedName~ActiveContextPolicy"
```

Beklenen: `ActiveContextPolicy` tipi yok — derleme hatası.

- [ ] **Step 3: Politikayı yaz**

`src/MESNET.Common.Shared/Security/ActiveContextPolicy.cs`:

```csharp
namespace MESNET.Common.Shared.Security;

/// <summary>
/// Saklanmış aktif bağlamın bu istekte kullanılabilir olup olmadığına karar verir (B parçası).
/// </summary>
/// <remarks>
/// <para><b>İki koşul birlikte aranır:</b> bağlam bu oturumda kurulmuş olmalı (<c>sid</c>
/// eşleşmesi) ve hedef hâlâ aktörün alt ağacında olmalı.</para>
///
/// <para><b>Alt ağaç kontrolü neden HER çözümlemede tekrarlanır:</b> ağaç değişebilir — okul
/// başka ilçeye taşınabilir, kullanıcının kendi kurumu değişebilir. Yalnız bağlam kurulurken
/// doğrulansaydı, sonradan alt ağaçtan çıkan bir okula erişim sessizce sürerdi.</para>
///
/// <para><b>Geçersizlik hata değildir.</b> <c>null</c> dönülür ve çağıran ev kurumuna düşer;
/// bayat bağlam bir yetki ihlali değil, bir zamanaşımıdır.</para>
///
/// <para><b><c>sid</c> yetki kararında kullanılmaz</b>, yalnız bağlamı düşürmek için. En kötü
/// hâlde yanlış karşılaştırır ve kullanıcı okulu yeniden seçer; kapsam sızdırmaz.</para>
/// </remarks>
public static class ActiveContextPolicy
{
    /// <param name="activeInstitutionId">Kayıttaki aktif bağlam; <c>null</c> = bağlam yok.</param>
    /// <param name="storedSessionId">Bağlamı kuran token'ın <c>sid</c>'i.</param>
    /// <param name="currentSessionId">Bu isteğin token'ındaki <c>sid</c>.</param>
    /// <param name="actorPath">Aktörün kurum ağacındaki yolu (<c>institution_path</c>).</param>
    /// <param name="targetPath">Hedef kurumun yolu.</param>
    /// <returns>Kullanılabilir bağlamın kurum kimliği; kullanılamıyorsa <c>null</c>.</returns>
    public static Guid? Resolve(
        Guid? activeInstitutionId,
        string? storedSessionId,
        string? currentSessionId,
        string? actorPath,
        string? targetPath)
    {
        if (activeInstitutionId is not { } target || target == Guid.Empty)
            return null;

        // Ordinal ve büyük/küçük harfe DUYARLI: sid rastgele üretilmiş bir dizedir, harf
        // katlaması iki ayrı oturumu eşitleyebilirdi.
        if (string.IsNullOrEmpty(storedSessionId)
            || string.IsNullOrEmpty(currentSessionId)
            || !string.Equals(storedSessionId, currentSessionId, StringComparison.Ordinal))
        {
            return null;
        }

        return InstitutionScopePolicy.CanAccessByPath(actorPath, targetPath) ? target : null;
    }
}
```

**NOT:** `InstitutionScopePolicy.CanAccessByPath` A parçasından gelir ve yollardan biri boşsa `false` döner — bu yüzden "aktörün yolu yok" ve "hedefin yolu yok" dalları için ayrı kontrol YAZILMAZ.

- [ ] **Step 4: Testi koştur, yeşil gör**

```bash
dotnet test tests/MESNET.Security.UnitTests --filter "FullyQualifiedName~ActiveContextPolicy"
```

Beklenen: 10/10 PASS. `CanAccessByPath`'in boş yol davranışı beklentiyle uyuşmazsa **o politikayı değiştirmeyin**; testin gerekçesini gerçeğe uydurup raporunuza yazın.

- [ ] **Step 5: `UserAccount` alanlarını ekle**

`src/Modules/Security/MESNET.Security.Core/Entities/UserAccount.cs`, `LinkedStudentIds` alanının hemen altına:

```csharp
    /// <summary>
    /// Aktif bağlam — il/ilçe yetkilisinin adına davrandığı okul (B parçası).
    /// <c>null</c> = kendi kurumunda çalışıyor.
    /// </summary>
    /// <remarks>
    /// <b>Kiracı anahtarıdır ve istekten ALINMAZ.</b> Yalnız
    /// <c>POST /api/security/users/me/context</c> ucundan yazılır; o uç hedefin aktörün alt
    /// ağacında olduğunu doğrular. Her çözümlemede kontrol TEKRARLANIR
    /// (<see cref="MESNET.Common.Shared.Security.ActiveContextPolicy"/>) — ağaç değişebilir.
    /// </remarks>
    public Guid? ActiveInstitutionId { get; set; }

    /// <summary>
    /// <see cref="ActiveInstitutionId"/>'yi kuran token'ın oturum kimliği (<c>sid</c>).
    /// </summary>
    /// <remarks>
    /// <para><b>Oturum burada TAKİP EDİLMEZ.</b> Oturumu Keycloak yönetir; burada yalnız bir
    /// kopya durur ve tek işi "bu bağlam hangi oturumda kuruldu" sorusuna cevap vermektir.
    /// Yetki kararında kullanılmaz.</para>
    ///
    /// <para><b>Ölçüldü (29.08.2026):</b> <c>sid</c> kullanıcı access token'ında geliyor,
    /// token yenilemede sabit kalıyor, yeni girişte değişiyor. Bağlam bu yüzden oturum içinde
    /// serbestçe değişir ama oturumlar arası taşınmaz.</para>
    ///
    /// <para><b>Sınırı:</b> ömür SSO oturumunun ömrüdür, tarayıcı sekmesinin değil. Sekmeyi
    /// kapatıp dönen kullanıcı, SSO oturumu canlıysa aynı bağlamda devam eder.</para>
    /// </remarks>
    public string? ActiveContextSessionId { get; set; }
```

**Marten notu:** `UserAccount` `DocumentTenancy.Identity` sınıfındadır ve yeni alanlar nullable olduğu için mevcut belgeler sorunsuz okunur. `required` YAPMAYIN — System.Text.Json eksik alan yüzünden her eski kaydı `JsonException` ile keser (aynı tuzak `Institution.ProvinceCode` yorumunda anlatılıyor).

- [ ] **Step 6: Derle ve tüm Security testlerini koştur**

```bash
dotnet build MESNET.slnx
dotnet test tests/MESNET.Security.UnitTests
```

- [ ] **Step 7: Kanıt adımı (zorunlu)**

`ActiveContextPolicy.Resolve` içindeki `sid` karşılaştırma bloğunu **silin**, testi koşun. Beklenen: `Bayat_oturumda_baglam_dusurulur`, `Saklanmis_oturum_kimligi_yoksa_baglam_dusurulur`, `Istekte_oturum_kimligi_yoksa_baglam_dusurulur` ve `Oturum_kimligi_karsilastirmasi_buyuk_kucuk_harfe_duyarlidir` KIRMIZI. Kaç testin kırıldığını ve adlarını raporunuza yazın, sonra geri alın.

İkinci kanıt: `CanAccessByPath` çağrısını `true` ile değiştirin. Beklenen: `Alt_agac_disindaki_hedef_dusurulur` KIRMIZI. Raporunuza yazın, geri alın.

- [ ] **Step 8: Commit**

```bash
git add src/MESNET.Common.Shared/Security/ActiveContextPolicy.cs \
        src/Modules/Security/MESNET.Security.Core/Entities/UserAccount.cs \
        tests/MESNET.Security.UnitTests/ActiveContextPolicyTests.cs
git commit -m "feat(context): aktif bağlam kullanılabilirlik politikası + UserAccount alanları"
```

---

### Task 2: Bağlam değiştirme komutu, handler ve uç

**Files:**
- Create: `src/Modules/Security/MESNET.Security.Application/Commands/SetActiveInstitution.cs`
- Create: `src/Modules/Security/MESNET.Security.Application/Handlers/SetActiveInstitutionHandler.cs`
- Modify: `src/Modules/Security/MESNET.Security.Application/Errors/SecurityErrors.cs`
- Modify: `src/MESNET.Common.Infrastructure/Security/ICurrentUserService.cs` + `CurrentUserService.cs`
- Modify: `src/Modules/Security/MESNET.Security.Api/UserManagementEndpoints.cs`

**Interfaces:**
- Consumes: `ActiveContextPolicy` (Görev 1), `IInstitutionPathLookup.GetPathAsync` (C parçası), `InstitutionScopePolicy.CanAccessByPath`
- Produces:
  - `SetActiveInstitution(Guid? InstitutionId)` komutu
  - `ICurrentUserService.GetSessionId()` → `string?`
  - `SecurityErrors.ActiveContextOutOfScope(Guid institutionId)`

- [ ] **Step 1: `ICurrentUserService`'e oturum kimliği ekle**

`src/MESNET.Common.Infrastructure/Security/ICurrentUserService.cs`, arayüzün sonuna:

```csharp
    /// <summary>
    /// Token'ın oturum kimliği — <c>sid</c> claim'i. Aktif bağlamın hangi oturumda
    /// kurulduğunu işaretlemek için kullanılır; <b>yetki kararında kullanılmaz</b>.
    /// Ölçüldü (29.08.2026): Keycloak bunu kullanıcı access token'ında gönderiyor, token
    /// yenilemede sabit tutuyor, yeni girişte değiştiriyor.
    /// </summary>
    string? GetSessionId();
```

`CurrentUserService.cs`'e uygulaması:

```csharp
    public string? GetSessionId()
        => _httpContextAccessor.HttpContext?.User.FindFirst("sid")?.Value;
```

**Diğer uygulamaları bozacak:** depoda `ICurrentUserService`'in elle yazılmış sahte uygulamaları var. `grep -rln "ICurrentUserService" tests/ src/` ile hepsini bulun ve her birine `public string? GetSessionId() => null;` ekleyin. Kaç uygulama bulduğunuzu raporunuza yazın.

- [ ] **Step 2: Hata kodunu ekle**

`SecurityErrors.cs`'e, dosyadaki mevcut biçimi izleyerek:

```csharp
    /// <summary>
    /// Hedef kurum aktörün alt ağacında değil. <b>"Bulunamadı" DENMEZ</b> — kapsamı olmayan
    /// bir aktöre hangi kimliklerin var olduğunu doğrulatmak, kurum listesini tahminle
    /// taramanın kapısını açar. Aynı gerekçe <c>InstitutionErrors.InstitutionScopeDenied</c>
    /// yorumunda.
    /// </summary>
    public static Error ActiveContextOutOfScope(Guid institutionId) =>
        new("ACTIVE_CONTEXT_OUT_OF_SCOPE",
            $"Bu kurum yetki alanınızda değil: {institutionId}");
```

- [ ] **Step 3: Komutu yaz**

`src/Modules/Security/MESNET.Security.Application/Commands/SetActiveInstitution.cs`:

```csharp
namespace MESNET.Security.Application.Commands;

/// <summary>
/// Aktörün aktif bağlamını değiştirir (B parçası).
/// </summary>
/// <param name="InstitutionId">
/// Adına davranılacak kurum; <c>null</c> bağlamı temizler ve aktörü ev kurumuna döndürür.
/// </param>
/// <remarks>
/// <b>Hedef aktörün KENDİ kaydından okunmaz, istekten gelir</b> — ama yetki değil NİYET
/// olarak. Sunucu hedefin aktörün alt ağacında olduğunu doğrular; değilse <c>DomainException</c>.
/// Aynı ayrım <c>IInstitutionScoped</c> uçlarında da var: kimlik istekten, karar sunucudan.
///
/// <para>Komut <c>Commands/</c> altındadır, dolayısıyla denetim izine kendiliğinden düşer
/// (C parçası). Ayrı bir kayıt yolu yazılmaz.</para>
/// </remarks>
public sealed record SetActiveInstitution(Guid? InstitutionId);
```

- [ ] **Step 4: Handler'ı yaz**

`src/Modules/Security/MESNET.Security.Application/Handlers/SetActiveInstitutionHandler.cs`:

```csharp
using Marten;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Infrastructure.Tenancy;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Security.Application.Commands;
using MESNET.Security.Application.Errors;
using MESNET.Security.Core.Entities;
using Microsoft.Extensions.Caching.Memory;

namespace MESNET.Security.Application.Handlers;

/// <summary>
/// Aktif bağlamı değiştirir.
/// </summary>
/// <remarks>
/// <para><b>Ayrı bir izin GEREKTİRMEZ</b> ve bu bilinçlidir: kapı iznin kendisi değil, alt
/// ağaç kontrolüdür. Okul kullanıcısının alt ağacı yalnız kendisidir; onun için bağlam
/// değiştirmek işlevsizdir, yasak değil. Ayrı bir izin, kapının ikinci bir kopyasını
/// üretmekten başka bir şey yapmazdı.</para>
///
/// <para><b>Önbellek geçersizleme atlanamaz.</b> <c>PermissionClaimsTransformation</c>
/// kullanıcı claim'lerini beş dakika önbellekliyor; çağrılmazsa yeni bağlam o süre boyunca
/// görünmez ve kullanıcı hâlâ eski okulda çalıştığını sanır.</para>
/// </remarks>
public static class SetActiveInstitutionHandler
{
    public static async Task Handle(
        SetActiveInstitution command,
        ICurrentUserService currentUser,
        IDocumentSession session,
        IInstitutionPathLookup pathLookup,
        IMemoryCache cache,
        CancellationToken cancellationToken)
    {
        var actor = currentUser.GetCurrentUser()
            ?? throw new DomainException(SecurityErrors.ActiveContextOutOfScope(
                command.InstitutionId ?? Guid.Empty));

        var account = await session.Query<UserAccount>()
            .SingleOrDefaultAsync(a => a.Id == actor.UserId, cancellationToken)
            ?? throw new DomainException(SecurityErrors.UserNotFound(actor.UserId));

        if (command.InstitutionId is { } target && target != Guid.Empty)
        {
            var targetPath = await pathLookup.GetPathAsync(target, cancellationToken);

            // Kendi kurumuna geçmek her zaman serbesttir: yol henüz kurulmamış olsa da
            // (geçiş ucu koşmamış kurum) kullanıcı kendi okuluna dönebilmelidir.
            var kendiKurumu = actor.InstitutionId is { } own && own == target;

            if (!kendiKurumu
                && !InstitutionScopePolicy.CanAccessByPath(actor.InstitutionPath, targetPath))
            {
                throw new DomainException(SecurityErrors.ActiveContextOutOfScope(target));
            }

            account.ActiveInstitutionId = target;
            account.ActiveContextSessionId = currentUser.GetSessionId();
        }
        else
        {
            account.ActiveInstitutionId = null;
            account.ActiveContextSessionId = null;
        }

        account.UpdatedAt = DateTime.UtcNow;
        session.Store(account);
        await session.SaveChangesAsync(cancellationToken);

        PermissionClaimsTransformation.InvalidateCache(cache, account.KeycloakUserId);
    }
}
```

**Doğrulanacak iki ayrıntı:**
1. `UserAccount` sorgusunun anahtarı: `actor.UserId` Keycloak `sub`'undan Guid'e çevrilir (`CurrentUserService.GetCurrentUser`). `UserAccount.Id` ile aynı mı, yoksa `KeycloakUserId` string'i mi eşleşiyor — depodaki mevcut bir handler'a bakıp (`ChangeUserRolesHandler` gibi) aynı anahtarı kullanın.
2. `PermissionClaimsTransformation.InvalidateCache` imzası ve beklediği anahtar biçimi — dosyadan okuyun (`InvalidateCache(IMemoryCache cache, string keycloakUserId)`).

- [ ] **Step 5: Ucu ekle**

`src/Modules/Security/MESNET.Security.Api/UserManagementEndpoints.cs`, mevcut `MapPost("/{userAccountId:guid}/institution", ...)` satırının yakınına:

```csharp
        // Aktif bağlam — kullanıcının KENDİ bağlamı, başkasınınki değil; bu yüzden yolda
        // kullanıcı kimliği YOK. Ek izin gerektirmez: kapı alt ağaç kontrolüdür ve o
        // handler'dadır. Okul kullanıcısının alt ağacı yalnız kendisidir.
        group.MapPost("/me/context", SetContext);
```

Metot:

```csharp
    private static async Task<IResult> SetContext(SetActiveInstitution command, IMessageBus bus)
    {
        await bus.InvokeAsync(command);
        return Results.Ok(ResponseBuilder.Success().Build());
    }
```

**DİKKAT — rota sırası:** `/me/context` sabit segmentle başlar ve grupta `{userAccountId:guid}` kalıbı var. Sabit rotayı kalıptan **ÖNCE** kaydedin, yoksa `me` bir Guid gibi ele alınmaya çalışılır. Aynı gerekçe `InstitutionEndpoints`'te `/provinces` için yazılı.

- [ ] **Step 6: Derle ve anonim uç sapma testini koştur**

```bash
dotnet build MESNET.slnx
dotnet test tests/MESNET.Security.UnitTests --filter "FullyQualifiedName~AnonymousEndpointDrift"
```

Beklenen: derleme 0 hata, drift testi YEŞİL (grup zaten `RequireAuthorization()` taşıyor).

- [ ] **Step 7: Commit**

```bash
git add src/Modules/Security src/MESNET.Common.Infrastructure/Security
git commit -m "feat(context): aktif bağlam değiştirme komutu, handler ve uç"
```

---

### Task 3: `active_institution_id` claim'i

**Files:**
- Modify: `src/MESNET.Common.Infrastructure/Security/PermissionClaimsTransformation.cs`
- Modify: `tests/MESNET.Security.UnitTests/InstitutionClaimAuthorityTests.cs`

**Interfaces:**
- Consumes: `ActiveContextPolicy.Resolve` (Görev 1), `UserAccount.ActiveInstitutionId` / `ActiveContextSessionId` (Görev 1)
- Produces: `active_institution_id` claim'i (`PermissionClaimsTransformation.ActiveInstitutionClaimType`)

- [ ] **Step 1: Mevcut deseni oku**

`PermissionClaimsTransformation.cs` içinde `institution_path` claim'inin **tam yolunu** okuyun: sabit adı, SQL sorgusu, arama metodu, önbellek anahtarı, boş sonucun önbelleğe alınmaması, ve claim silme metodu. Yeni claim **bire bir aynı iskeleti** izleyecek; yeni bir desen icat etmeyin.

- [ ] **Step 2: Claim üretimini yaz**

Eklenecek parçalar:

```csharp
    public const string ActiveInstitutionClaimType = "active_institution_id";

    private const string ActiveContextLookupSql = """
        SELECT data->>'activeInstitutionId' AS active_institution_id,
               data->>'activeContextSessionId' AS active_context_session_id
        FROM security.mt_doc_useraccount
        WHERE data->>'keycloakUserId' = @keycloakId
        LIMIT 1
        """;
```

**Şema ve alan adlarını DOĞRULAYIN:** tablo adı `security.mt_doc_useraccount` ve JSON alan adları camelCase varsayımıdır. `PermissionClaimsTransformation` içindeki mevcut `UserAccount` sorgularına bakıp (ör. `LookupInstitutionIdAsync`) aynı tablo/alan biçimini kullanın. Marten JSON serileştirmesi camelCase'tir.

Akış (`institution_path` ile aynı sıra):

1. **Token'dan gelen `active_institution_id` claim'lerini KOŞULSUZ sil.** Kayıt boş olsa bile. Bu adım en önemlisidir ve bir bayrağın arkasına konmaz.
2. Kayıttan `ActiveInstitutionId` + `ActiveContextSessionId` oku (önbellekli; **boş sonuç önbelleğe ALINMAZ** — bağlam değişimi anında görünmeli).
3. `ActiveContextPolicy.Resolve(activeId, storedSid, principal'ın "sid" claim'i, principal'ın "institution_path" claim'i, hedefin yolu)` çağır. Hedefin yolu `IInstitutionPathLookup` yerine mevcut `LookupInstitutionPathAsync` ile alınır — bu sınıfın kendi aracı zaten var.
4. Sonuç `null` değilse `active_institution_id` claim'ini ekle. `null` ise **claim eklenmez** — yokluğu "bağlam yok" demektir.

**`institution_path` claim'i AKTİF BAĞLAMLA DEĞİŞMEZ.** O, aktörün *kendi* ağaçtaki yeridir ve alt ağaç kontrolünün girdisidir; bağlamla değiştirilseydi kontrol kendi kendini doğrular hâle gelirdi.

- [ ] **Step 3: Authority drift testini genişlet**

`tests/MESNET.Security.UnitTests/InstitutionClaimAuthorityTests.cs` — mevcut testlerin yanına, aynı kaynak tarama desenini izleyerek:

```csharp
    [Fact]
    public void Token_daki_aktif_baglam_claimi_her_istekte_silinir()
    {
        // Kaynak taraması: PermissionClaimsTransformation "active_institution_id" claim'ini
        // kaldıran bir çağrı içermeli. Kayıt boş olsa bile silinmeli — "kaynak yoksa token'a
        // düş" davranışı, kaydı olmayan kullanıcıya KENDİ bağlamını seçtirirdi.
        var kaynak = OkuDonusumKaynagi();

        kaynak.ShouldContain("ActiveInstitutionClaimType");
        kaynak.ShouldContain("RemoveActiveInstitutionClaims");
    }

    [Fact]
    public void Aktif_baglam_Keycloak_a_YAZILMAZ()
    {
        // Oradaki bir kopya, ileride birinin onu yeniden otorite sanmasına davetiye çıkarır.
        // #195'te realm'e ulaşmayan ayar tam bu sınıf bir sapmaydı.
        foreach (var dosya in KeycloakYazanKaynaklar())
        {
            File.ReadAllText(dosya)
                .ShouldNotContain("active_institution_id",
                    $"{dosya} Keycloak'a aktif bağlam yazıyor olabilir.");
        }
    }
```

`OkuDonusumKaynagi()` ve `KeycloakYazanKaynaklar()` yardımcılarını dosyadaki **mevcut** eşdeğerlerinden alın; yeni bir dosya bulma mekanizması yazmayın. Mevcut testler `institution_id` için aynı iki iddiayı zaten yapıyor — onları örnek alın.

- [ ] **Step 4: Koştur**

```bash
dotnet build MESNET.slnx
dotnet test tests/MESNET.Security.UnitTests
```

- [ ] **Step 5: Kanıt adımı (zorunlu)**

`PermissionClaimsTransformation`'daki `RemoveActiveInstitutionClaims` çağrısını **silin**, `InstitutionClaimAuthorityTests`'i koşun. Beklenen: `Token_daki_aktif_baglam_claimi_her_istekte_silinir` KIRMIZI. Raporunuza mesajı yazın, geri alın.

- [ ] **Step 6: Commit**

```bash
git add src/MESNET.Common.Infrastructure/Security/PermissionClaimsTransformation.cs \
        tests/MESNET.Security.UnitTests/InstitutionClaimAuthorityTests.cs
git commit -m "feat(context): active_institution_id claim'i sunucudan üretilir"
```

---

### Task 4: Kiracı çözümlemesi ve kullanıcı bağlamı

**Files:**
- Modify: `src/MESNET.Common.Shared/Tenancy/TenantResolution.cs`
- Modify: `src/MESNET.Common.Shared/Security/UserContext.cs`
- Modify: `src/MESNET.Common.Infrastructure/Security/CurrentUserService.cs`
- Modify: `src/MESNET.Common.Infrastructure/Tenancy/TenantResolutionMiddleware.cs`
- Test: `tests/MESNET.Security.UnitTests/TenantResolutionActiveContextTests.cs`

**Interfaces:**
- Produces:
  - `TenantResolution.Resolve(Guid? institutionIdClaim, IEnumerable<string> permissions, Guid? activeInstitutionId = null)` → `string?`
  - `UserContext.ActiveInstitutionId` (`Guid?`, son isteğe bağlı parametre)

- [ ] **Step 1: Başarısız testi yaz**

`tests/MESNET.Security.UnitTests/TenantResolutionActiveContextTests.cs`:

```csharp
using MESNET.Common.Shared.Tenancy;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Aktif bağlamın kiracı çözümlemesindeki yeri (B parçası).
///
/// <para><b>Aktif bağlam kiracıyı DEĞİŞTİRİR</b> — okul verisi okulun kiracısındadır ve il
/// yetkilisi oraya ancak o kiracıda çalışarak ulaşır. Ama <c>institution_id</c> claim'i
/// (ev kurumu) DEĞİŞMEZ; denetim izinin "kim olduğun / nerede davrandığın" ayrımı ona
/// bağlıdır.</para>
///
/// <para>Bu fonksiyona ulaşan aktif bağlam <b>zaten doğrulanmıştır</b>
/// (<c>ActiveContextPolicy</c>): geçersiz bağlam claim'e hiç dönüşmez. Buradaki iş yalnız
/// tercih sırasıdır.</para>
/// </summary>
public sealed class TenantResolutionActiveContextTests
{
    private static readonly Guid EvKurumu = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Okul = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly string[] IzinYok = [];
    private static readonly string[] PlatformIzni = ["platform:tenant:manage"];

    [Fact]
    public void Aktif_baglam_varsa_kiraci_odur()
    {
        TenantResolution.Resolve(EvKurumu, IzinYok, Okul)
            .ShouldBe(TenantResolution.ForInstitution(Okul));
    }

    [Fact]
    public void Aktif_baglam_yoksa_ev_kurumu_kiraci_olur()
    {
        TenantResolution.Resolve(EvKurumu, IzinYok, null)
            .ShouldBe(TenantResolution.ForInstitution(EvKurumu));
    }

    [Fact]
    public void Bos_Guid_aktif_baglam_yok_sayilir()
    {
        TenantResolution.Resolve(EvKurumu, IzinYok, Guid.Empty)
            .ShouldBe(TenantResolution.ForInstitution(EvKurumu));
    }

    [Fact]
    public void Kurumu_olmayan_platform_aktoru_baglam_secebilir()
    {
        // Platform aktörünün ev kurumu yoktur; bağlam seçtiğinde o okulda çalışır.
        TenantResolution.Resolve(null, PlatformIzni, Okul)
            .ShouldBe(TenantResolution.ForInstitution(Okul));
    }

    [Fact]
    public void Baglamsiz_platform_aktoru_platform_kiracisinda_kalir()
    {
        TenantResolution.Resolve(null, PlatformIzni, null)
            .ShouldBe(TenantResolution.Platform);
    }

    [Fact]
    public void Kapsamsiz_kullanici_baglamsizken_kiracisiz_kalir()
    {
        // Kapsamsız kullanıcıya kiracı UYDURULMAZ (ADR-0003) — bu davranış değişmedi.
        TenantResolution.Resolve(null, IzinYok, null).ShouldBeNull();
    }

    [Fact]
    public void Eski_iki_parametreli_cagri_davranisi_korunur()
    {
        // Depoda bu fonksiyonun mevcut çağrıları var; üçüncü parametre isteğe bağlıdır.
        TenantResolution.Resolve(EvKurumu, IzinYok)
            .ShouldBe(TenantResolution.ForInstitution(EvKurumu));
    }
}
```

- [ ] **Step 2: Testi koştur, kırmızı gör**

```bash
dotnet test tests/MESNET.Security.UnitTests --filter "FullyQualifiedName~TenantResolutionActiveContext"
```

Beklenen: üç parametreli aşırı yükleme yok — derleme hatası.

- [ ] **Step 3: `TenantResolution.Resolve`'u genişlet**

`src/MESNET.Common.Shared/Tenancy/TenantResolution.cs` — mevcut `Resolve` metodunun imzasına **isteğe bağlı** üçüncü parametre eklenir (mevcut çağrılar bozulmaz) ve gövdenin **başına** şu dal konur:

```csharp
    /// <param name="activeInstitutionId">
    /// Doğrulanmış aktif bağlam (B parçası); <c>null</c> = bağlam yok.
    ///
    /// <para><b>Buraya ulaşan değer zaten doğrulanmıştır</b>
    /// (<see cref="MESNET.Common.Shared.Security.ActiveContextPolicy"/>): oturumu güncel ve
    /// hedef aktörün alt ağacında. Geçersiz bağlam claim'e hiç dönüşmediği için burada
    /// yeniden kontrol edilmez — edilirse aynı karar iki yerde yaşar ve ayrışır.</para>
    /// </param>
    public static string? Resolve(
        Guid? institutionIdClaim,
        IEnumerable<string> permissions,
        Guid? activeInstitutionId = null)
    {
        // Aktif bağlam EN ÖNDE: il yetkilisinin okul verisine ulaşmasının tek yolu, o okulun
        // kiracısında çalışmaktır. Ev kurumu dalı önce gelseydi bağlam hiç etkili olmazdı.
        if (activeInstitutionId is { } active && active != Guid.Empty)
            return ForInstitution(active);

        // ── BURADAN AŞAĞISI DEĞİŞMEZ: mevcut gövdeyi olduğu gibi bırakın ──
        // (kurumu olan aktör → kendi kiracısı; kurumsuz ama platform izinli → platform;
        //  ikisi de değilse → null. Bu üç dal ADR-0003'ün kararıdır, dokunulmaz.)
    }
```

- [ ] **Step 4: `UserContext`'e alan ekle**

`src/MESNET.Common.Shared/Security/UserContext.cs`, **son** isteğe bağlı parametre olarak (mevcut konumsal çağrıları bozmamak için sona eklenmesi zorunludur):

```csharp
    /// <summary>
    /// Aktörün adına davrandığı kurum — <c>active_institution_id</c> claim'i (B parçası).
    /// <c>null</c> = kendi kurumunda çalışıyor.
    /// </summary>
    /// <remarks>
    /// <b><see cref="InstitutionId"/> ile karıştırmayın.</b> O "kim olduğun", bu "nerede
    /// davrandığın". Denetim izi ikisini ayrı alanlara yazar ve
    /// <c>CrossedTenantBoundary</c> tam olarak bu farktan doğar.
    /// </remarks>
    Guid? ActiveInstitutionId = null);
```

`CurrentUserService.GetCurrentUser()` içinde claim'i okuyup kurucuya geçirin:

```csharp
        var activeInstitutionId =
            Guid.TryParse(user.FindFirst("active_institution_id")?.Value, out var actInstId)
                ? actInstId : (Guid?)null;
```

- [ ] **Step 5: Middleware'i güncelle**

`TenantResolutionMiddleware` içindeki `TenantResolution.Resolve(...)` çağrısına aktif bağlam claim'i eklenir. Dosyada `InstitutionIdOf(context.User)` gibi bir yardımcı varsa aynı biçimde `ActiveInstitutionIdOf(context.User)` yazın; yoksa satır içi `Guid.TryParse` kullanın.

**Kimliği doğrulanmamış istek dalına DOKUNMAYIN** — o dal `TenantResolution.Platform`'a düşer ve aktif bağlamla ilgisi yoktur.

- [ ] **Step 6: Koştur**

```bash
dotnet build MESNET.slnx
dotnet test MESNET.slnx --filter "FullyQualifiedName!~MESNET.Api.Tests"
```

Beklenen: hepsi yeşil. `TenantlessSessionDriftTests` özellikle yeşil kalmalı.

- [ ] **Step 7: Kanıt adımı (zorunlu)**

`TenantResolution.Resolve`'un başına eklediğiniz aktif bağlam dalını **silin**, testi koşun. Beklenen: `Aktif_baglam_varsa_kiraci_odur` ve `Kurumu_olmayan_platform_aktoru_baglam_secebilir` KIRMIZI. Raporunuza yazın, geri alın.

- [ ] **Step 8: Commit**

```bash
git add src/MESNET.Common.Shared src/MESNET.Common.Infrastructure tests/MESNET.Security.UnitTests
git commit -m "feat(context): kiracı çözümlemesi aktif bağlamı tercih eder"
```

---

### Task 5: Denetim izi aktif bağlamı tanır (C parçasına dokunuş)

Bu görev C'nin kodunu değiştirir ve **B'nin izli verilmesinin tek sebebini** korur: aktif bağlam altındaki yazma `CrossedTenantBoundary = true` üretmelidir.

**Files:**
- Modify: `src/Modules/Audit/MESNET.Audit.Application/Auditing/AuditContext.cs`
- Modify: `src/Modules/Audit/MESNET.Audit.Application/Auditing/AuditMiddleware.cs`
- Modify: `src/Modules/Audit/MESNET.Audit.Application/Auditing/AuditWriter.cs`
- Modify: `src/Modules/Audit/MESNET.Audit.Core/Services/AuditEntryFactory.cs`
- Test: `tests/MESNET.Audit.UnitTests/AuditEntryFactoryTests.cs` (vaka ekle)
- Test: `tests/MESNET.Audit.UnitTests/AuditMiddlewareContractTests.cs` (vaka ekle)

**Interfaces:**
- Değişen: `AuditEntryFactory.ResolveSubject(object? command, Guid? actorInstitutionId, Guid? activeInstitutionId)` → `(Guid? SubjectInstitutionId, bool CrossedTenantBoundary)`
- Değişen: `AuditInput` kaydına `ActiveInstitutionId` (`Guid?`) eklenir

- [ ] **Step 1: Başarısız testleri yaz**

`AuditEntryFactoryTests.cs`'e ekleyin (mevcut `Girdi()` yardımcısına `activeInstitutionId` parametresi ekleyerek):

```csharp
    [Fact]
    public void Aktif_baglam_varsa_konu_kurum_ODUR_ve_sinir_asilmis_sayilir()
    {
        // B'nin izli verilmesinin tek sebebi bu satır: il yetkilisi hangi okula dokundu.
        var entry = AuditEntryFactory.Succeeded(Girdi(
            command: new { X = 1 },
            actorInstitutionId: AktorKurumu,
            activeInstitutionId: BaskaKurum));

        entry.SubjectInstitutionId.ShouldBe(BaskaKurum);
        entry.CrossedTenantBoundary.ShouldBeTrue();
    }

    [Fact]
    public void Komuttaki_kurum_hedefi_aktif_baglamdan_ONCE_gelir()
    {
        // Komut açıkça bir kurumu hedefliyorsa iz o kurumu göstermelidir; aktif bağlam
        // yalnız hedefsiz komutlarda devreye girer.
        var komut = new OrnekKomut(Guid.NewGuid(), AktorKurumu);

        var entry = AuditEntryFactory.Succeeded(Girdi(
            command: komut,
            actorInstitutionId: AktorKurumu,
            activeInstitutionId: BaskaKurum));

        entry.SubjectInstitutionId.ShouldBe(AktorKurumu);
        entry.CrossedTenantBoundary.ShouldBeFalse();
    }

    [Fact]
    public void Aktif_baglam_kendi_kurumuysa_sinir_asilmaz()
    {
        var entry = AuditEntryFactory.Succeeded(Girdi(
            command: new { X = 1 },
            actorInstitutionId: AktorKurumu,
            activeInstitutionId: AktorKurumu));

        entry.CrossedTenantBoundary.ShouldBeFalse();
    }

    [Fact]
    public void Aktif_baglam_yoksa_eski_davranis_korunur()
    {
        var entry = AuditEntryFactory.Succeeded(Girdi(
            command: new { X = 1 },
            actorInstitutionId: AktorKurumu,
            activeInstitutionId: null));

        entry.SubjectInstitutionId.ShouldBe(AktorKurumu);
        entry.CrossedTenantBoundary.ShouldBeFalse();
    }
```

- [ ] **Step 2: Testi koştur, kırmızı gör**

```bash
dotnet test tests/MESNET.Audit.UnitTests --filter "FullyQualifiedName~AuditEntryFactory"
```

- [ ] **Step 3: `ResolveSubject` düşme sırasını değiştir**

`AuditEntryFactory.cs` — düşme sırası **komuttaki `InstitutionId` → aktif bağlam → ev kurumu** olur:

```csharp
    public static (Guid? SubjectInstitutionId, bool CrossedTenantBoundary) ResolveSubject(
        object? command, Guid? actorInstitutionId, Guid? activeInstitutionId)
    {
        var targets = AuditTargetExtractor.Extract(command);

        // Sıra: komutun açık hedefi → aktif bağlam → ev kurumu.
        //
        // AKTİF BAĞLAM EV KURUMUNDAN ÖNCE GELİR (B parçası). Gelmeseydi, il yetkilisinin
        // okulda yaptığı hedefsiz her yazma ize İL adına düşerdi ve CrossedTenantBoundary
        // her zaman false olurdu — yani "il yetkilisi hangi okula dokundu" sorusu, B'nin
        // izli verilmesinin TEK sebebi, cevapsız kalırdı.
        var subjectInstitutionId =
            targets.TryGetValue(InstitutionTargetName, out var targeted) ? targeted
            : activeInstitutionId is { } active && active != Guid.Empty ? active
            : actorInstitutionId;

        var crossed = actorInstitutionId is { } actorInstitution
                      && subjectInstitutionId is { } subject
                      && actorInstitution != subject;

        return (subjectInstitutionId, crossed);
    }
```

`AuditInput` kaydına `Guid? ActiveInstitutionId` eklenir ve `Build` içindeki `ResolveSubject` çağrısı üç parametreli hâle gelir.

- [ ] **Step 4: `AuditContext` ve middleware'i güncelle**

`AuditContext`'e `public required Guid? ActiveInstitutionId { get; init; }` eklenir. `AuditMiddleware.Before` onu `currentUser.GetCurrentUser()?.ActiveInstitutionId` ile doldurur. `AuditWriter` `AuditInput`'a geçirir ve `ResolveSubject` çağrısını üç parametreli yapar.

- [ ] **Step 5: Sözleşme testine vaka ekle**

`AuditMiddlewareContractTests.cs` içindeki `SahteKullanici` sınıfına aktif bağlam ekleyin (ikinci bir sahte sınıf ya da yapılandırılabilir bir alan — hangisini seçtiğinizi raporda gerekçelendirin) ve şu testi yazın:

```csharp
    [Fact]
    public async Task Aktif_baglam_middleware_araciligiyla_denetim_baglamina_tasinir()
    {
        // C'nin "kim olduğun / nerede davrandığın" ayrımının B'de yaşadığını kilitler.
        // institution_id claim'i aktif bağlamla ezilseydi ikisi eşitlenirdi.
        var (host, yazici) = await AnaBilgisayarKurAsync(aktifBaglam: BaskaKurum);
        using var _ = host;
        var bus = host.Services.GetRequiredService<IMessageBus>();

        await bus.InvokeAsync<string>(new OrnekKomut(Guid.NewGuid(), Reddet: false));

        yazici.Baglamlar.Count.ShouldBe(1);
        yazici.Baglamlar[0].ActiveInstitutionId.ShouldBe(BaskaKurum);
        yazici.Baglamlar[0].ActorInstitutionId.ShouldBe(AktorKurumu);

        await host.StopAsync();
    }
```

Bunun için dosyadaki mevcut altyapıya üç küçük ekleme gerekir:

1. `SahteYazici` yazılan `AuditContext`'i de saklasın:
   ```csharp
   public List<AuditContext> Baglamlar { get; } = [];
   // WriteAsync içinde: Baglamlar.Add(context);
   ```
2. `SahteKullanici` aktif bağlamı yapılandırılabilir alsın:
   ```csharp
   private sealed class SahteKullanici(Guid? aktifBaglam) : ICurrentUserService
   {
       public UserContext? GetCurrentUser() => new(
           UserId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
           FullName: "Ayşe Öğretmen",
           InstitutionId: AktorKurumu,
           ActiveInstitutionId: aktifBaglam);
       // kalan üyeler değişmedi
   }
   ```
   `UserContext` kurucusunda `ActiveInstitutionId` **son** parametredir; adlandırılmış argüman kullanın, konumsal yazmayın.
3. `AnaBilgisayarKurAsync` isteğe bağlı `Guid? aktifBaglam = null` parametresi alsın ve onu `SahteKullanici`'ya geçirsin. **Mevcut dört testin çağrısı değişmez** (varsayılan `null`).

`AktorKurumu` ve `BaskaKurum` sabitlerini sınıfa ekleyin (`AuditEntryFactoryTests`'teki değerlerin aynısı olabilir).

Satırın `CrossedTenantBoundary`'si `AuditEntryFactoryTests`'te zaten kilitli — **aynı iddiayı burada tekrarlamayın**; buradaki iddia bağlamın middleware üzerinden doğru TAŞINDIĞIDIR.

- [ ] **Step 6: Koştur**

```bash
dotnet build MESNET.slnx
dotnet test tests/MESNET.Audit.UnitTests
```

Beklenen: mevcut 44 test + yeni vakalar, hepsi yeşil.

- [ ] **Step 7: Kanıt adımı (zorunlu)**

`ResolveSubject`'teki aktif bağlam dalını **silin** (eski iki dallı hâline döndürün), testi koşun. Beklenen: `Aktif_baglam_varsa_konu_kurum_ODUR_ve_sinir_asilmis_sayilir` KIRMIZI. Raporunuza mesajı yazın, geri alın.

- [ ] **Step 8: Commit**

```bash
git add src/Modules/Audit tests/MESNET.Audit.UnitTests
git commit -m "feat(context): denetim izi aktif bağlamı konu kurum olarak tanır"
```

---

### Task 6: İzinler ve rol eşlemesi

**Files:**
- Modify: `src/MESNET.Common.Shared/Security/Permissions.cs`
- Modify: `src/MESNET.Common.Shared/Security/RolePermissionMap.cs`
- Modify: `src/MESNET.Common.Shared/Security/AssignablePermissionScope.cs`
- Modify: `src/Modules/Internship/MESNET.Internship.Api/InternshipEndpoints.cs`
- Test: `tests/MESNET.Security.UnitTests/DirectoratePermissionMappingTests.cs`
- Modify: `src/Docs/docs/architecture/adr-0002-izin-agaci-ve-onek-secimi.md` (matris testinin yazdırdığı metin)

**Interfaces:**
- Produces: `Permissions.Directorate.InstitutionBootstrap` = `"directorate:institution-bootstrap"`, `Permissions.Internship.ApprovalOverride` = `"internship:approval:override"`

- [ ] **Step 1: Başarısız testi yaz**

`tests/MESNET.Security.UnitTests/DirectoratePermissionMappingTests.cs`:

```csharp
using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// İl/ilçe yetkilisinin izin demeti (B parçası).
///
/// <para><b>Neden `directorate:` diye YENİ bir önek:</b> `institution:` önekli olsaydı
/// <c>InstitutionManager</c>'ın <c>institution:*</c> wildcard'ı üzerinden HER okul müdürüne
/// geçerdi ve okul müdürü kullanıcıları BAŞKA okullara bağlayabilirdi — ADR-0002 önek
/// tuzağının tam kendisi. <c>platform:</c> de kullanılamaz: o önek kurum üstü yetkiyi işaret
/// eder ve il yetkilisine platform yetkisi vermek kapsamı bütün ülkeye açardı.</para>
/// </summary>
public sealed class DirectoratePermissionMappingTests
{
    private static IReadOnlyList<string> PermissionsOf(string role)
        => RolePermissionMap.GetPermissionsForRoles([role]);

    public static TheoryData<string> MudurlukRolleri =>
    [
        MesnetRoles.ProvincialAdmin,
        MesnetRoles.DistrictAdmin,
    ];

    [Theory]
    [MemberData(nameof(MudurlukRolleri))]
    public void Mudurluk_rolleri_kurum_kunyesi_ve_donem_izni_alir(string role)
    {
        PermissionsOf(role).ShouldContain(Permissions.Institution.Manage);
    }

    [Theory]
    [MemberData(nameof(MudurlukRolleri))]
    public void Mudurluk_rolleri_tikanmis_onayi_acabilir(string role)
    {
        PermissionsOf(role).ShouldContain(Permissions.Internship.ApprovalOverride);
    }

    [Theory]
    [MemberData(nameof(MudurlukRolleri))]
    public void Mudurluk_rolleri_okula_ilk_yoneticiyi_baglayabilir(string role)
    {
        PermissionsOf(role).ShouldContain(Permissions.Directorate.InstitutionBootstrap);
    }

    [Theory]
    [MemberData(nameof(MudurlukRolleri))]
    public void Mudurluk_rolleri_onay_zincirinde_NORMAL_ADIM_olamaz(string role)
    {
        // internship:manage müdür onay adımını da açar; istenen yalnız tıkanıklığı açmaktır.
        PermissionsOf(role).ShouldNotContain(Permissions.Internship.Manage);
    }

    [Theory]
    [MemberData(nameof(MudurlukRolleri))]
    public void Mudurluk_rolleri_rol_yonetemez(string role)
    {
        // user:roles:manage alt ağaçtaki her okulda her kullanıcının rollerini değiştirmek
        // demektir — istenen şeyden kat kat geniş.
        PermissionsOf(role).ShouldNotContain(Permissions.UserManagement.RolesManage);
    }

    [Fact]
    public void Bootstrap_izni_baska_HICBIR_role_gitmez()
    {
        foreach (var role in MesnetRoles.All)
        {
            if (role is MesnetRoles.ProvincialAdmin or MesnetRoles.DistrictAdmin) continue;

            PermissionsOf(role).ShouldNotContain(Permissions.Directorate.InstitutionBootstrap,
                $"{role} rolüne bootstrap izni sızmış.");
        }
    }

    [Fact]
    public void Hicbir_rolun_wildcardi_directorate_onekini_yutmaz()
    {
        foreach (var role in MesnetRoles.All)
        {
            RolePermissionMap.GetRawPermissionsForRole(role)
                .ShouldNotContain("directorate:*", $"{role} rolüne directorate: wildcard'ı eklenmiş.");
        }
    }

    [Fact]
    public void Override_iznini_bugun_internship_manage_tasiyan_her_rol_de_alir()
    {
        // GEÇİŞ KAYBI OLMAMALI: ucun izni daraltıldı, kimse yetkisini kaybetmemeli.
        foreach (var role in MesnetRoles.All)
        {
            var izinler = PermissionsOf(role);
            if (!izinler.Contains(Permissions.Internship.Manage)) continue;

            izinler.ShouldContain(Permissions.Internship.ApprovalOverride,
                $"{role} internship:manage taşıyor ama override iznini kaybetti.");
        }
    }

    [Fact]
    public void Yeni_izinler_bireysel_atanamaz()
    {
        AssignablePermissionScope.NeverDirectlyAssignable
            .ShouldContain(Permissions.Directorate.InstitutionBootstrap);
        AssignablePermissionScope.NeverDirectlyAssignable
            .ShouldContain(Permissions.Internship.ApprovalOverride);
    }

    [Fact]
    public void Directorate_oneki_atanabilir_domain_listesinde_YOKTUR()
    {
        AssignablePermissionScope.AllDomains.ShouldNotContain("directorate:");
    }

    [Fact]
    public void Mudurluk_rollerinin_atanabilir_kapsami_BOS_KALIR()
    {
        // A parçasında bilerek boş bırakıldı. Açılırsa il yetkilisi kendi verdiği izinlerle
        // kapsamını genişletir. C yazıldı diye açılmaz — o liste "kime dağıtabilir"
        // sorusudur, "ne yapabilir" değil.
        AssignablePermissionScope.Defaults[MesnetRoles.ProvincialAdmin].ShouldBeEmpty();
        AssignablePermissionScope.Defaults[MesnetRoles.DistrictAdmin].ShouldBeEmpty();
    }
}
```

- [ ] **Step 2: Testi koştur, kırmızı gör**

```bash
dotnet test tests/MESNET.Security.UnitTests --filter "FullyQualifiedName~DirectoratePermissionMapping"
```

- [ ] **Step 3: İzinleri tanımla**

`Permissions.cs` — `Platform` sınıfının altına yeni sınıf:

```csharp
    /// <summary>
    /// İl/ilçe millî eğitim müdürlüğü katmanı (B parçası).
    /// </summary>
    /// <remarks>
    /// <para><b>Neden ayrı bir önek:</b> <c>institution:</c> önekli bir izin
    /// <c>InstitutionManager</c>'ın <c>institution:*</c> wildcard'ı üzerinden her okul
    /// müdürüne geçerdi (ADR-0002 önek tuzağı) ve okul müdürü kullanıcıları başka okullara
    /// bağlayabilirdi. <c>platform:</c> de kullanılamaz — o önek kurum üstü yetkiyi işaret
    /// eder ve kapsamı bütün ülkeye açardı. Müdürlük katmanı ikisinin arasındadır: bir alt
    /// ağacı vardır, ülkesi yoktur.</para>
    /// </remarks>
    public static class Directorate
    {
        /// <summary>
        /// Alt ağaçtaki bir okula <b>ilk yöneticiyi</b> bağlama.
        ///
        /// <para><b>Tek başına yetmez:</b> hedef okulun hiç yöneticisi olmamalıdır
        /// (<c>InstitutionBootstrapPolicy</c>). Müdahale, tıkanıklık fiilen varken açmaktır;
        /// okulun yöneticisi olduğu anda kapı kapanır ve yetki okula döner.</para>
        /// </summary>
        public const string InstitutionBootstrap = "directorate:institution-bootstrap";
    }
```

`Internship` sınıfına:

```csharp
        /// <summary>
        /// Tıkanmış fesih onay zincirini yönetici kararıyla atlama.
        /// </summary>
        /// <remarks>
        /// <b><c>internship:manage</c>'den ayrıldı (B parçası):</b> o izin override ile
        /// birlikte <b>müdür onay adımını</b> da açıyordu. İl yetkilisinin onay zincirinde
        /// normal bir adım olması istenmez — istenen tıkanıklığı açmaktır. Geçişte kayıp
        /// olmaması için bugün <c>internship:manage</c> taşıyan her rol bu izni de açıkça
        /// alır; kilitleyen test: <c>DirectoratePermissionMappingTests</c>.
        /// </remarks>
        public const string ApprovalOverride = "internship:approval:override";
```

- [ ] **Step 4: Rol eşlemesini güncelle**

`RolePermissionMap.cs`:

- `ProvincialAdmin` ve `DistrictAdmin` demetlerine (ikisi de aynı demeti alır, farkı yalnız ağaçtaki yeridir):
  ```csharp
            Permissions.Institution.View,
            // Müdahale yetkisi (B parçası) — dönem açma/kapatma ve kurum künyesi.
            // Yan etki bilinçli: marka paleti ve ders programı yapılandırması da açılır,
            // ikisi de institution:manage altındadır ve ayırmak izin ağacını tek rol için
            // yeniden çizmek olurdu. Üçü de denetlenir.
            Permissions.Institution.Manage,
            // Tıkanmış onayı açma — internship:manage DEĞİL (o müdür onay adımını da açardı).
            Permissions.Internship.ApprovalOverride,
            // Okula ilk yöneticiyi bağlama; koşulludur (okulun hiç yöneticisi olmamalı).
            Permissions.Directorate.InstitutionBootstrap
  ```
- `internship:manage` taşıyan **her role** `Permissions.Internship.ApprovalOverride` açıkça eklenir. `InstitutionManager`'da `internship:*` wildcard'ı zaten yutar ama güvenlik kararı olduğu için açıkça yazılır (aynı gerekçe `Attendance.DirectEntry` yorumunda). Hangi rollere eklediğinizi raporunuza yazın.

`AssignablePermissionScope.cs`:
- `NeverDirectlyAssignable` kümesine iki yeni izin, gerekçe yorumlarıyla.
- `AllDomains`'e `"directorate:"` **EKLENMEZ**.
- `Defaults[ProvincialAdmin]` / `[DistrictAdmin]` **boş kalır**.

- [ ] **Step 5: Override ucunun iznini değiştir**

`src/Modules/Internship/MESNET.Internship.Api/InternshipEndpoints.cs`:

```csharp
        // Override kendi iznine geçti (B parçası): internship:manage müdür onay adımını da
        // açıyordu ve il yetkilisinin zincirde normal bir adım olması istenmiyor.
        group.MapPost("/{internshipId:guid}/approve/override", PostOverride)
            .RequireAuthorization(Permissions.Internship.ApprovalOverride);
```

`/approve/director` ucuna **DOKUNMAYIN** — o `internship:manage` ile kalır.

- [ ] **Step 6: Koştur ve ADR matrisini güncelle**

```bash
dotnet test tests/MESNET.Security.UnitTests
```

`PermissionMatrixDocTests` KIRMIZI olacaktır — beklenen davranıştır, yeni izin eklendi. Test doğru metni üretir; onu testin **okuduğu dosyaya** yazın. Hangi dosya olduğunu testten okuyun (C parçasında `src/Docs/docs/architecture/adr-0002-izin-agaci-ve-onek-secimi.md` çıkmıştı). Testi ya da matris üreticisini değiştirmeyin.

`src/Docs/docs/actors/permissions.md`'ye ayrıca üç cümlelik bir "Müdürlük Katmanı" bölümü ekleyin: hangi rol hangi müdahaleyi yapar, kapsamın aktif bağlam olduğu, ve `directorate:` önekinin neden ayrı olduğu.

- [ ] **Step 7: Kanıt adımları (ikisi de zorunlu)**

1. `Permissions.Internship.ApprovalOverride` değerini geçici olarak `"internship:manage"` yapın (yani ayrımı geri alın), testi koşun. Beklenen: `Mudurluk_rolleri_onay_zincirinde_NORMAL_ADIM_olamaz` KIRMIZI — ayrımın gerçekten taşıyıcı olduğunun ölçümü. Geri alın.
2. `RolePermissionMap`'te `ProvincialAdmin`'e eklediğiniz `Permissions.Directorate.InstitutionBootstrap` satırını **silin**, testi koşun. Beklenen: `Mudurluk_rolleri_okula_ilk_yoneticiyi_baglayabilir(role: "ProvincialAdmin")` KIRMIZI. Geri alın.

- [ ] **Step 8: Commit**

```bash
git add src/MESNET.Common.Shared/Security src/Modules/Internship/MESNET.Internship.Api \
        tests/MESNET.Security.UnitTests src/Docs/docs
git commit -m "feat(context): müdürlük katmanı izinleri ve override ayrımı"
```

---

### Task 7: `InstitutionBootstrapPolicy` ve koşullu bağlama

**Files:**
- Create: `src/MESNET.Common.Shared/Security/InstitutionBootstrapPolicy.cs`
- Modify: `src/Modules/Security/MESNET.Security.Application/Handlers/ChangeUserInstitutionHandler.cs`
- Modify: `src/Modules/Security/MESNET.Security.Application/Errors/SecurityErrors.cs`
- Test: `tests/MESNET.Security.UnitTests/InstitutionBootstrapPolicyTests.cs`

**Interfaces:**
- Consumes: `UserInstitutionScopePolicy.CanAssign(Guid? actorInstitutionId, Guid? currentInstitutionId, Guid? targetInstitutionId, bool hasPlatformScope = false)`
- Produces: `InstitutionBootstrapPolicy.CanBootstrap(bool hasBootstrapPermission, bool targetInActorSubtree, bool targetHasManager)` → `bool`

- [ ] **Step 1: Başarısız testi yaz**

`tests/MESNET.Security.UnitTests/InstitutionBootstrapPolicyTests.cs`:

```csharp
using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Okula ilk yöneticiyi bağlama koşulu (B parçası).
///
/// <para><b>Neden koşullu:</b> "müdahale" tıkanıklığı fiilen varken açmaktır. Okulun
/// yöneticisi olduğu anda kapı kapanır ve yetki okula döner. Koşulsuz bir izin, il
/// yetkilisine alt ağaçtaki her okulun kullanıcı bağlarını süresiz değiştirme yetkisi
/// verirdi — istenen şeyden kat kat geniş.</para>
///
/// <para><b>Üç koşul da BİRLİKTE aranır.</b> Herhangi biri düşerse bootstrap yolu kapalıdır;
/// çağıran normal kapsam kuralına (<c>UserInstitutionScopePolicy.CanAssign</c>) düşer.</para>
/// </summary>
public sealed class InstitutionBootstrapPolicyTests
{
    [Fact]
    public void Uc_kosul_saglaninca_baglanabilir()
    {
        InstitutionBootstrapPolicy.CanBootstrap(
            hasBootstrapPermission: true, targetInActorSubtree: true, targetHasManager: false)
            .ShouldBeTrue();
    }

    [Fact]
    public void Izin_yoksa_baglanamaz()
    {
        InstitutionBootstrapPolicy.CanBootstrap(false, true, false).ShouldBeFalse();
    }

    [Fact]
    public void Hedef_alt_agac_disindaysa_baglanamaz()
    {
        InstitutionBootstrapPolicy.CanBootstrap(true, false, false).ShouldBeFalse();
    }

    [Fact]
    public void Okulun_yoneticisi_VARSA_baglanamaz()
    {
        // Kapının kapandığı an. Tıkanıklık yoksa müdahale de yoktur.
        InstitutionBootstrapPolicy.CanBootstrap(true, true, targetHasManager: true)
            .ShouldBeFalse();
    }
}
```

- [ ] **Step 2: Testi koştur, kırmızı gör**

```bash
dotnet test tests/MESNET.Security.UnitTests --filter "FullyQualifiedName~InstitutionBootstrapPolicy"
```

- [ ] **Step 3: Politikayı yaz**

`src/MESNET.Common.Shared/Security/InstitutionBootstrapPolicy.cs`:

```csharp
namespace MESNET.Common.Shared.Security;

/// <summary>
/// İl/ilçe yetkilisinin alt ağacındaki bir okula <b>ilk yöneticiyi</b> bağlayabilmesinin
/// koşulu (B parçası).
/// </summary>
/// <remarks>
/// <para><b>Neden ayrı bir politika, <c>UserInstitutionScopePolicy.CanAssign</c>'a dal
/// eklemek değil:</b> o politika "aktör kendi kurumuna bağlar" kuralını taşıyor ve tek bir
/// cümlede okunabiliyor. Bootstrap farklı bir sorudur — tıkanıklık var mı — ve girdileri de
/// farklıdır. İkisini birleştirmek beş parametreli, iki ayrı gerçeği kodlayan bir yüklem
/// üretirdi.</para>
///
/// <para><b>Yeni okulun ilk kullanıcısı sorunu A parçasında da vardı</b> ve orada
/// <c>platform:tenant:manage</c> istisnasıyla çözülmüştü. Bu politika aynı boşluğu il
/// yetkilisi için, çok daha dar bir kapıyla açar: yalnız kendi alt ağacında ve yalnız
/// yöneticisi olmayan okulda.</para>
/// </remarks>
public static class InstitutionBootstrapPolicy
{
    /// <param name="hasBootstrapPermission"><c>directorate:institution-bootstrap</c>.</param>
    /// <param name="targetInActorSubtree">
    /// Hedef kurum aktörün yol önekinin altında mı — <c>InstitutionScopePolicy.CanAccessByPath</c>.
    /// </param>
    /// <param name="targetHasManager">
    /// Hedef kurumun <b>etkin</b> bir yöneticisi var mı. Varsa tıkanıklık yoktur ve müdahale
    /// yolu kapalıdır.
    /// </param>
    public static bool CanBootstrap(
        bool hasBootstrapPermission, bool targetInActorSubtree, bool targetHasManager)
        => hasBootstrapPermission && targetInActorSubtree && !targetHasManager;
}
```

- [ ] **Step 4: Testi koştur, yeşil gör**

```bash
dotnet test tests/MESNET.Security.UnitTests --filter "FullyQualifiedName~InstitutionBootstrapPolicy"
```

Beklenen: 4/4 PASS.

- [ ] **Step 5: Hata kodunu ekle**

`SecurityErrors.cs`:

```csharp
    /// <summary>
    /// Hedef okulun zaten bir yöneticisi var; müdahale yolu kapalı. Bağı okulun kendi
    /// yöneticisi kurar.
    /// </summary>
    public static Error InstitutionAlreadyHasManager(Guid institutionId) =>
        new("INSTITUTION_ALREADY_HAS_MANAGER",
            $"Bu kurumun yöneticisi var; kullanıcı bağını kurum kendi yönetir: {institutionId}");
```

- [ ] **Step 6: `ChangeUserInstitutionHandler`'a bootstrap dalını ekle**

Handler'ı **önce okuyun**; mevcut `CanAssign` çağrısını bozmadan, o `false` döndüğünde denenen ikinci bir yol olarak ekleyin:

```csharp
        var normalYol = UserInstitutionScopePolicy.CanAssign(
            actor?.InstitutionId, hedefKullanici.InstitutionId, command.InstitutionId,
            currentUser.HasPermission(Permissions.Platform.TenantManage));

        if (!normalYol)
        {
            // Müdahale yolu: yalnız alt ağaçta ve yalnız yöneticisi OLMAYAN okulda.
            var hedefKurum = command.InstitutionId ?? Guid.Empty;
            var hedefYolu = await pathLookup.GetPathAsync(hedefKurum, cancellationToken);

            var yoneticisiVar = await session.Query<UserAccount>()
                .AnyAsync(a => a.InstitutionId == hedefKurum
                               && a.IsEnabled
                               && a.Roles.Contains(MesnetRoles.InstitutionManager),
                          cancellationToken);

            var mudahale = InstitutionBootstrapPolicy.CanBootstrap(
                currentUser.HasPermission(Permissions.Directorate.InstitutionBootstrap),
                InstitutionScopePolicy.CanAccessByPath(actor?.InstitutionPath, hedefYolu),
                yoneticisiVar);

            if (!mudahale)
            {
                throw new DomainException(yoneticisiVar
                    ? SecurityErrors.InstitutionAlreadyHasManager(hedefKurum)
                    : SecurityErrors.ActiveContextOutOfScope(hedefKurum));
            }
        }
```

**Marten notu:** `a.Roles.Contains(...)` — `Roles` bir `List<string>`'tir ve Marten bunu JSONB `?` operatörüne çevirir. Derlenmez ya da `BadLinqExpressionException` verirse depoda rol içeren mevcut bir sorguyu (`GetUserAccountsHandler` gibi) örnek alın; **kendi ham SQL'inizi yazmayın**.

**`MesnetRoles.InstitutionManager` bir ROL ADIDIR ve burada kapsam kararı için değil, "tıkanıklık var mı" ÖLÇÜMÜ için kullanılıyor** — yetki kararı `CanBootstrap`'in izin parametresindedir. Depo kuralı "rol adına bakan yeni kapsam kontrolü yazılmaz" der; bu bir kapsam kontrolü değildir. Gerekçeyi koda yorum olarak yazın.

- [ ] **Step 7: Koştur**

```bash
dotnet build MESNET.slnx
dotnet test tests/MESNET.Security.UnitTests
```

- [ ] **Step 8: Kanıt adımı (zorunlu)**

`InstitutionBootstrapPolicy.CanBootstrap`'teki `&& !targetHasManager` koşulunu **silin**, testi koşun. Beklenen: `Okulun_yoneticisi_VARSA_baglanamaz` KIRMIZI. Raporunuza mesajı yazın, geri alın.

- [ ] **Step 9: Commit**

```bash
git add src/MESNET.Common.Shared/Security/InstitutionBootstrapPolicy.cs \
        src/Modules/Security tests/MESNET.Security.UnitTests
git commit -m "feat(context): okula ilk yöneticiyi bağlama — koşullu müdahale"
```

---

### Task 8: `/auth/me` ve `authStore` aktif bağlamı taşır

**Files:**
- Modify: `src/MESNET.Presentation/AuthEndpoint.cs`
- Modify: `src/WebUI/src/stores/auth.ts`
- Test: `src/WebUI/src/stores/auth.activeContext.spec.ts`

**Interfaces:**
- Consumes: `UserContext.ActiveInstitutionId` (Görev 4)
- Produces:
  - `/auth/me` yanıtında `activeInstitutionId: string | null`
  - `AuthUser.activeInstitutionId: string | null`
  - `authStore.currentInstitutionId` (computed) → `string | null`

- [ ] **Step 1: Ucu genişlet**

`src/MESNET.Presentation/AuthEndpoint.cs` — `/me` yanıtına `activeInstitutionId` eklenir. Dosyayı okuyup mevcut alanların yazıldığı biçimi (anonim nesne mi DTO mu) birebir izleyin.

**`institutionId` alanı DEĞİŞMEZ** — o ev kurumudur. İkisi yan yana döner.

- [ ] **Step 2: Başarısız ön yüz testini yaz**

`src/WebUI/src/stores/auth.activeContext.spec.ts`:

```typescript
import { describe, it, expect, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useAuthStore } from './auth'

/**
 * Aktif bağlamın ön yüzdeki tek doğruluk kaynağı.
 *
 * <p><b>Neden tek bir computed:</b> kuruma bağlı her store (`institutionStore`,
 * `academicPeriodStore`, `entityOptionsStore`) bugün `user.institutionId`'yi okuyor. Bağlam
 * geldiğinde her biri kendi başına "hangi kurum" sorusunu cevaplasaydı, biri değişip diğeri
 * kalırdı ve kullanıcı bir ekranda A okulunu, diğerinde B okulunu görürdü.</p>
 *
 * <p><b>`institutionId` EV KURUMUDUR ve değişmez.</b> Denetim izinin "kim olduğun / nerede
 * davrandığın" ayrımı ona bağlıdır; ön yüzde de ezilmemelidir.</p>
 */
describe('authStore — aktif bağlam', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('aktif bağlam yokken geçerli kurum EV kurumudur', () => {
    const store = useAuthStore()
    store.user = { institutionId: 'ev-kurumu', activeInstitutionId: null } as never

    expect(store.currentInstitutionId).toBe('ev-kurumu')
  })

  it('aktif bağlam varken geçerli kurum ODUR', () => {
    const store = useAuthStore()
    store.user = { institutionId: 'ev-kurumu', activeInstitutionId: 'okul' } as never

    expect(store.currentInstitutionId).toBe('okul')
  })

  it('aktif bağlam EV kurumunu EZMEZ', () => {
    // Ezilseydi denetim izindeki CrossedTenantBoundary ayrımı ön yüzde de kaybolurdu.
    const store = useAuthStore()
    store.user = { institutionId: 'ev-kurumu', activeInstitutionId: 'okul' } as never

    expect(store.user?.institutionId).toBe('ev-kurumu')
  })

  it('kullanıcı yokken geçerli kurum null olur', () => {
    const store = useAuthStore()
    store.user = null

    expect(store.currentInstitutionId).toBeNull()
  })
})
```

Testin `store.user`'a doğrudan yazması Pinia setup store'da mümkündür (ref dışarı veriliyor). Değilse store'un kendi kurma yolunu kullanın ve testi ona uydurun; **store'un iç yapısını test için değiştirmeyin**.

- [ ] **Step 3: Koştur, kırmızı gör**

```bash
cd src/WebUI && pnpm test:run src/stores/auth.activeContext.spec.ts
```

- [ ] **Step 4: `authStore`'u genişlet**

`AuthUser` arayüzüne `activeInstitutionId: string | null` eklenir. Token'dan okunan yerde (`parsed.institution_id` satırının yanında) `parsed.active_institution_id ?? null` ve `/auth/me` yanıtından okunan yerde `data?.activeInstitutionId` işlenir — **mevcut `institutionId` işleme deseninin birebir aynısı**.

Computed:

```typescript
  /**
   * Ekranların ve store'ların bağlanacağı kurum. Aktif bağlam varsa o, yoksa ev kurumu.
   *
   * <p>Kuruma bağlı her store bunu okur; `user.institutionId`'yi doğrudan okuyan yeni kod
   * YAZILMAZ — o ev kurumudur ve bağlamla değişmez.</p>
   */
  const currentInstitutionId = computed(
    () => user.value?.activeInstitutionId ?? user.value?.institutionId ?? null,
  )
```

`return` bloğuna `currentInstitutionId` eklenir.

- [ ] **Step 5: Koştur**

```bash
cd src/WebUI && pnpm test:run && pnpm type-check
```

- [ ] **Step 6: Kanıt adımı (zorunlu)**

`currentInstitutionId` computed'ını `user.value?.institutionId ?? null` yapın (aktif bağlamı yok sayın), testi koşun. Beklenen: `aktif bağlam varken geçerli kurum ODUR` KIRMIZI. Raporunuza yazın, geri alın.

- [ ] **Step 7: Commit**

```bash
git add src/MESNET.Presentation/AuthEndpoint.cs src/WebUI/src/stores/auth.ts \
        src/WebUI/src/stores/auth.activeContext.spec.ts
git commit -m "feat(context): /auth/me ve authStore aktif bağlamı taşır"
```

---

### Task 9: Bağlam değişimi tek yerden geçer ve store'ları geçersiz kılar

Bu görev **sessiz yanlış-okula-yazma** tuzağını kapatır: bugün `academicPeriodStore` dönem listesini bir kez yükleyip `isLoaded` ile kilitliyor ve kurum kimliğini hatırlamıyor.

**Files:**
- Create: `src/WebUI/src/composables/useInstitutionContext.ts`
- Create: `src/WebUI/src/api/context.ts` (ya da mevcut bir api dosyasına ekleme)
- Modify: `src/WebUI/src/stores/academicPeriod.ts`
- Test: `src/WebUI/src/stores/academicPeriod.contextSwitch.spec.ts`

**Interfaces:**
- Consumes: `authStore.currentInstitutionId` (Görev 8), `POST /api/security/users/me/context` (Görev 2)
- Produces: `useInstitutionContext()` → `{ switching, switchTo(institutionId: string | null): Promise<void> }`

- [ ] **Step 1: `academicPeriodStore` için başarısız testi yaz**

`src/WebUI/src/stores/academicPeriod.contextSwitch.spec.ts`:

```typescript
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import type { AcademicPeriodDto } from 'src/api/institution'

/**
 * SESSİZ YANLIŞ-OKULA-YAZMA TUZAĞININ KİLİDİ.
 *
 * <p>`loadPeriods()` dönem listesini `authStore.user?.institutionId` ile çekiyor ve hangi
 * kurum için çektiğini HATIRLAMIYOR. Bağlam değişip liste yenilenmezse ekranda A okulunun
 * dönemi seçili kalır ve B okuluna <b>A okulunun dönem kimliğiyle</b> yazılır. Sonuç hata
 * değil; sessizce yanlış döneme düşmüş bir kayıt.</p>
 *
 * <p>`institutionStore` bu tuzağı öngörmüş ve `loadedInstitutionId` alanıyla kapatmış;
 * yorumu "kiracı değişirse bayrak hâlâ true'dur, eski okulun adı ve alanları ekranda kalır"
 * diyor. Aynı koruma buraya da gelir.</p>
 */

const listAcademicPeriods = vi.fn()

vi.mock('src/api/institution', () => ({
  institutionApi: {
    listAcademicPeriods: (...args: unknown[]) => listAcademicPeriods(...args),
  },
}))

// authStore'un tamamı taklit edilir: bu testin konusu dönem store'unun DAVRANIŞI, kimlik
// katmanı değil. `currentInstitutionId` testten değiştirilebilir olmalı.
let currentInstitutionId: string | null = 'okul-a'

vi.mock('./auth', () => ({
  useAuthStore: () => ({
    get currentInstitutionId() {
      return currentInstitutionId
    },
  }),
}))

function donem(id: string): AcademicPeriodDto {
  return { id, status: 'Active' } as AcademicPeriodDto
}

describe('academicPeriodStore — bağlam değişimi', () => {
  beforeEach(async () => {
    setActivePinia(createPinia())
    currentInstitutionId = 'okul-a'
    listAcademicPeriods.mockReset()
    listAcademicPeriods.mockImplementation((institutionId: string) =>
      Promise.resolve({ data: { items: [donem(`${institutionId}-donem`)] } }),
    )
  })

  it('AYNI kurum için ikinci yükleme isteği sunucuya GİTMEZ', async () => {
    const { useAcademicPeriodStore } = await import('./academicPeriod')
    const store = useAcademicPeriodStore()

    await store.loadPeriods()
    await store.loadPeriods()

    expect(listAcademicPeriods).toHaveBeenCalledTimes(1)
  })

  it('BAŞKA kurum için yeniden yükler — bayat dönem listesi kalmamalı', async () => {
    // Koruma yoksa bu test kırmızı olur ve tuzak açıktır.
    const { useAcademicPeriodStore } = await import('./academicPeriod')
    const store = useAcademicPeriodStore()

    await store.loadPeriods()
    currentInstitutionId = 'okul-b'
    await store.loadPeriods()

    expect(listAcademicPeriods).toHaveBeenCalledTimes(2)
    expect(store.periods[0]?.id).toBe('okul-b-donem')
  })

  it('kurum değişince seçili dönem sıfırlanır', async () => {
    // Eski okulun dönem kimliği seçili kalırsa yazma o kimlikle gider.
    const { useAcademicPeriodStore } = await import('./academicPeriod')
    const store = useAcademicPeriodStore()

    await store.loadPeriods()
    expect(store.selectedPeriodId).toBe('okul-a-donem')

    currentInstitutionId = 'okul-b'
    await store.loadPeriods()

    expect(store.selectedPeriodId).toBe('okul-b-donem')
  })

  it('kurum yoksa istek atılmaz', async () => {
    const { useAcademicPeriodStore } = await import('./academicPeriod')
    const store = useAcademicPeriodStore()
    currentInstitutionId = null

    await store.loadPeriods()

    expect(listAcademicPeriods).not.toHaveBeenCalled()
  })
})
```

**Mock yollarını doğrulayın:** `vi.mock` yolları store'un kendi `import` satırlarıyla **birebir** eşleşmelidir (`src/api/institution`, `./auth`). Store farklı bir yoldan import ediyorsa mock'u ona uydurun; `AcademicPeriodDto`'nun gerçek alanları farklıysa `donem()` yardımcısını gerçeğe uydurun. **Store'u test için değiştirmeyin.**

- [ ] **Step 2: Koştur, kırmızı gör**

```bash
cd src/WebUI && pnpm test:run src/stores/academicPeriod.contextSwitch.spec.ts
```

- [ ] **Step 3: `academicPeriodStore`'a koruma ekle**

`institutionStore`'daki `loadedInstitutionId` desenini birebir taklit edin:

- `const loadedInstitutionId = ref<string | null>(null)`
- Kurum kimliği `authStore.currentInstitutionId`'den okunur (`user.institutionId` DEĞİL)
- `loadPeriods()` erken dönüşü `isLoaded.value && loadedInstitutionId.value === id` olur
- Yükleme başarılı olunca `loadedInstitutionId.value = id`
- Kurum değiştiğinde `selectedPeriodId.value = null` yapılır ve aktif dönem yeniden seçilir

Yorumla gerekçeyi yazın: `isLoaded` tek başına yetmez, bayrağın hangi kurum için kaldırıldığı bilinmezse bayat liste ekranda kalır.

- [ ] **Step 4: Bağlam composable'ını yaz**

`src/WebUI/src/composables/useInstitutionContext.ts`:

```typescript
import { ref } from 'vue'
import { useAuthStore } from 'stores/auth'
import { useInstitutionStore } from 'stores/institution'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import { useEntityOptionsStore } from 'stores/entityOptions'
import { contextApi } from 'src/api/context'

/**
 * Aktif bağlam değiştirmenin TEK yolu.
 *
 * <p><b>Neden tek bir yer:</b> bağlam değişimi kuruma bağlı bütün önbellekleri geçersiz
 * kılmak zorundadır. Her sayfa kendi başına hatırlasaydı, biri unuttuğunda kullanıcı yeni
 * okulda ama ESKİ okulun dönem listesiyle çalışırdı ve yazma sessizce yanlış döneme
 * giderdi.</p>
 *
 * <p><b>Sıra önemlidir:</b> önce sunucu (kayıt + izin önbelleği geçersizleme), sonra
 * `/auth/me` (yeni claim'ler), sonra yerel store'lar. Ters sırada store'lar eski bağlamla
 * yeniden dolardı.</p>
 */
export function useInstitutionContext() {
  const authStore = useAuthStore()
  const institutionStore = useInstitutionStore()
  const periodStore = useAcademicPeriodStore()
  const entityOptions = useEntityOptionsStore()

  const switching = ref(false)

  async function switchTo(institutionId: string | null): Promise<void> {
    switching.value = true
    try {
      await contextApi.setActiveInstitution(institutionId)

      // Sunucu claim'leri yeniden üretti; kullanıcı bilgisini tazele.
      await authStore.refreshUser()

      // Kuruma bağlı her önbellek düşer. Hangi store'un neye ihtiyacı olduğunu burada
      // bilmek zorunda DEĞİLİZ — hepsi temizlenir, sayfalar kendi yüklemesini yapar.
      institutionStore.clear()
      periodStore.clear()
      entityOptions.clear()
    } finally {
      switching.value = false
    }
  }

  return { switching, switchTo }
}
```

**Doğrulanacaklar:** `authStore`'da kullanıcıyı tazeleyen metodun **gerçek adı** (`refreshUser` varsayımdır — `/auth/me` çağıran mevcut metodu bulun), ve `entityOptionsStore`'un `clear` metodunun varlığı. Yoksa mevcut `invalidate*` metotlarını kullanın; **store'lara yeni metot eklemeyin** — eklemek zorunda kaldıysanız gerekçesini raporunuza yazın.

`src/WebUI/src/api/context.ts`:

```typescript
import api from 'boot/axios'

export const contextApi = {
  /** `null` bağlamı temizler ve kullanıcıyı ev kurumuna döndürür. */
  setActiveInstitution: (institutionId: string | null) =>
    api.post('/security/users/me/context', { institutionId }),
}
```

`api` import biçimini `src/WebUI/src/api/institution.ts`'in ilk satırlarından doğrulayın (C parçasında `default` export çıkmıştı).

- [ ] **Step 5: Koştur**

```bash
cd src/WebUI && pnpm test:run && pnpm type-check
```

- [ ] **Step 6: Kanıt adımı (zorunlu)**

`academicPeriodStore`'daki `loadedInstitutionId.value === id` koşulunu **silin** (yani eski `isLoaded` davranışına dönün), testi koşun. Beklenen: `BAŞKA kurum için yeniden yükler` KIRMIZI. Raporunuza mesajı yazın, geri alın.

- [ ] **Step 7: Commit**

```bash
git add src/WebUI/src/composables/useInstitutionContext.ts src/WebUI/src/api/context.ts \
        src/WebUI/src/stores/academicPeriod.ts src/WebUI/src/stores/academicPeriod.contextSwitch.spec.ts
git commit -m "feat(context): bağlam değişimi tek yerden geçer, bayat dönem listesi kapatıldı"
```

---

### Task 10: Üst bar seçici, bağlamsız sayfa ve sözleşme testi

**Files:**
- Create: `src/WebUI/src/pages/institution/ContextSelectPage.vue`
- Create: `src/WebUI/src/pages/institution/contextSelectQuery.ts`
- Create: `src/WebUI/src/pages/institution/ContextSelectPage.spec.ts`
- Modify: `src/WebUI/src/layouts/MainLayout.vue`
- Modify: `src/WebUI/src/router/index.ts`

**Interfaces:**
- Consumes: `useInstitutionContext()` (Görev 9), `authStore.currentInstitutionId` (Görev 8), `institutionApi.list` (A parçası)

- [ ] **Step 1: Sorgu sözleşmesi dosyasını yaz**

`src/WebUI/src/pages/institution/contextSelectQuery.ts`:

```typescript
/**
 * Bağlam seçim ekranının sunucuya ne sorduğunu belirleyen SAF mantık.
 *
 * <p><b>Neden ayrı dosya:</b> sayfa VE testi aynı kaynağı okusun. Bu depoda ölçülmüş
 * sahte-yeşil kalıbı bunun yokluğundan doğdu — eski `InstitutionListPage.spec.ts` sayfayı
 * hiç import etmiyor, değerleri kendi yeniden yazıyordu; sayfanın varsayılanı değiştirilip
 * koşulduğunda test 5/5 yeşil kaldı.</p>
 */

/** Seçim ekranı OKULLARI listeler — il/ilçe düğümleri seçilebilir bağlam değildir. */
export const DEFAULT_NODE_TYPE_FILTER = 'School'

/** Kurum adına göre sıralı; sırasız liste her yazmadan sonra kayardı. */
export const DEFAULT_SORT_BY = 'fullName'

export interface ContextSelectFilters extends Record<string, unknown> {
  nodeType: string
}

export function buildContextSelectFilters(nodeType: string): ContextSelectFilters {
  return { nodeType }
}
```

**Karar ve gerekçesi (koda yorum olarak yazın):** seçim ekranı yalnız **okulları** listeler. İl/ilçe müdürlüğü düğümünün kiracısında okul verisi yoktur; oraya "geçmek" boş ekranlardan başka bir şey üretmez. Kullanıcı kendi düğümüne dönmek isterse bağlamı **temizler** (`switchTo(null)`), bir düğüm seçmez.

- [ ] **Step 2: Sözleşme testini yaz**

`src/WebUI/src/pages/institution/ContextSelectPage.spec.ts` — `src/WebUI/src/pages/audit/AuditLogPage.spec.ts` desenini birebir izleyin (`vi.useFakeTimers()`, `runAllTimersAsync()`, sabitler **import edilir, yeniden yazılmaz**):

```typescript
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { computed } from 'vue'
import { useServerPagination } from 'src/composables/useServerPagination'
import type { PagedResponse } from 'src/types/pagination'
import type { InstitutionDto } from 'src/api/institution'
import {
  DEFAULT_NODE_TYPE_FILTER,
  DEFAULT_SORT_BY,
  buildContextSelectFilters,
} from './contextSelectQuery'

describe('ContextSelectPage — sunucu sözleşmesi', () => {
  const bosSayfa: PagedResponse<InstitutionDto> = {
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
  ) => Promise<{ data: PagedResponse<InstitutionDto> }>

  let fetchFn: ReturnType<typeof vi.fn<FetchFn>>

  const kur = () =>
    useServerPagination<InstitutionDto>({
      fetchFn,
      filters: computed(() => buildContextSelectFilters(DEFAULT_NODE_TYPE_FILTER)),
      defaultSortBy: DEFAULT_SORT_BY,
    })

  beforeEach(() => {
    vi.useFakeTimers()
    fetchFn = vi.fn(async () => ({ data: bosSayfa }))
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('yalnız OKULLAR listelenir — il/ilçe düğümü seçilebilir bağlam değildir', async () => {
    const { load } = kur()
    await load()
    expect(fetchFn).toHaveBeenCalledWith(expect.objectContaining({ nodeType: 'School' }))
  })

  it('kurum adına göre sıralanır', async () => {
    const { load } = kur()
    await load()
    expect(fetchFn).toHaveBeenCalledWith(expect.objectContaining({ sortBy: 'fullName' }))
  })

  it('arama terimi sunucuya gider — istemci tarafında süzülmez', async () => {
    const { onSearch } = kur()
    onSearch('Atatürk')
    await vi.runAllTimersAsync()
    expect(fetchFn).toHaveBeenCalledWith(expect.objectContaining({ search: 'Atatürk' }))
  })

  it('sayfa isteği sunucuya gider', async () => {
    const { onRequest } = kur()
    onRequest({ pagination: { page: 2, rowsPerPage: 20, sortBy: 'fullName', descending: false } })
    await vi.runAllTimersAsync()
    expect(fetchFn).toHaveBeenCalledWith(expect.objectContaining({ page: 2 }))
  })
})
```

- [ ] **Step 3: Sayfayı yaz**

`src/WebUI/src/pages/institution/ContextSelectPage.vue` — `InstitutionListPage.vue` desenini izleyin (`AppTable` + `useServerPagination` + `onMounted(() => load().catch(() => {}))`).

Farkları:
- Satır aksiyonu "Görüntüle" değil **"Bu kuruma geç"**: `useInstitutionContext().switchTo(row.id)` çağırır, sonra `router.push('/dashboard').catch(() => {})`.
- Aktif bağlam varsa üstte "Şu an **{kurum adı}** adına çalışıyorsunuz" bilgisi ve "Bağlamdan çık" butonu (`switchTo(null)`).
- Başlık: "Kurum Seç".
- Boş durum: "Yetki alanınızda okul bulunamadı." — nötr, uyarı ikonu YOK (depo kuralı: boş liste hata değildir).

**`onMounted(() => load().catch(() => {}))` ZORUNLU** — `useServerPagination`'ın filtre izleyicisi `immediate` değildir; bu satır yoksa sayfa kalıcı olarak boş görünür. A parçasında yaşanmış bir hatadır.

- [ ] **Step 4: Rotayı ekle**

`src/WebUI/src/router/index.ts`, kurum bloğunun altına:

```typescript
        // Bağlam seçimi — il/ilçe yetkilisinin tek çalışma modu okulun bağlamına geçmektir.
        // İzin institution:view'dir: seçim listesi zaten o izinle geliyor ve kapsam
        // sunucudadır (InstitutionScopePolicy).
        {
          path: 'context',
          name: 'ContextSelect',
          component: () => import('pages/institution/ContextSelectPage.vue'),
          meta: { permissions: ['institution:view'] },
        },
```

- [ ] **Step 5: Üst bara seçici ve göstergeyi ekle**

`src/WebUI/src/layouts/MainLayout.vue` — Dönem seçicisinin **üstüne**:

- Aktif bağlam varsa: kurum adını taşıyan belirgin bir gösterge (ör. renkli `q-chip`) ve tıklanınca `/context` sayfasına giden bir eylem.
- Aktif bağlam yoksa ve kullanıcının alt ağacı kendinden büyükse: "Kurum Seç" butonu.
- Okul kullanıcısında **hiçbiri görünmez**.

**Görünürlük ölçütü:** `authStore.user?.activeInstitutionId` dolu mu, ve kullanıcı bir üst düğümde mi. İkincisi için **rol adına BAKMAYIN** — `institutionStore`'daki kurumun `nodeTypeSlug`/`nodeType` alanına ya da `authStore`'daki mevcut bir computed'a bakın. Uygun bir sinyal yoksa `authStore`'a saf bir yardımcı ekleyin ve gerekçesini yazın; rol adı kontrolü depo kuralıyla yasaktır.

**Gösterge ince olamaz.** İl yetkilisinin bütün zamanı bir bağlamın içinde geçer; hangi okul adına davrandığı her an tartışmasız görünmelidir. Yalnız ikon içeren bir buton kullanırsanız hem `aria-label` hem `<q-tooltip>` gerekir; `title` attribute KULLANILMAZ.

`useNavigation.upperNode.spec.ts` menüyü okuyorsa ve kırılırsa **testi yeni öğeyi kabul edecek şekilde güncelleyin**, menüyü değil.

- [ ] **Step 6: Koştur**

```bash
cd src/WebUI && pnpm test:run && pnpm type-check
```

Beklenen: ikisi de yeşil. A parçasında `test:run` yeşilken `vue-tsc` 4× TS2322 vermişti — ikisi de yeşil olmadan görev bitmez.

- [ ] **Step 7: Kanıt adımı (zorunlu)**

`contextSelectQuery.ts`'teki `DEFAULT_NODE_TYPE_FILTER` değerini `'Province'` yapın, testi koşun. Beklenen: `yalnız OKULLAR listelenir` KIRMIZI. Raporunuza mesajı yazın, geri alın.

- [ ] **Step 8: Commit**

```bash
git add src/WebUI/src/pages/institution src/WebUI/src/layouts/MainLayout.vue \
        src/WebUI/src/router/index.ts
git commit -m "feat(context): kurum seçim ekranı ve üst bar bağlam göstergesi"
```

---

## Dağıtım Notları

1. **`rebuild-hierarchy` ÖN KOŞULDUR ve B'de sertleşir.** Aktif bağlam kontrolü yol önekine dayanır (`InstitutionScopePolicy.CanAccessByPath`); yolu olmayan kurumda kontrol **her zaman `false`** döner, yani il yetkilisi hiçbir okula geçemez. A parçasında bu "boş liste" idi, B'de "hiç çalışmıyor" olur. `POST /api/institutions/rebuild-hierarchy` dağıtımdan önce koşturulmalıdır.
2. **Yeni izinler mevcut kullanıcılara kendiliğinden gelmez.** `ProvincialAdmin`/`DistrictAdmin` rolü taşıyan kullanıcıların claim'leri beş dakikalık önbellekte yaşar; dağıtım sonrası ilk beş dakikada eski izin kümesiyle çalışabilirler. Zararsızdır (yalnız gecikme), ama destek çağrısı gelirse sebebi budur.
3. **Elle veri geçişi YOK.** `ActiveInstitutionId` ve `ActiveContextSessionId` nullable'dır; mevcut `UserAccount` belgeleri sorunsuz okunur ve bağlamsız başlar.

---

## Kapsam Dışı (bilinçli)

- **İl/ilçe geneli sayılar** — bağlamsız ekranda okul başına öğrenci/sözleşme/dönem sayıları. Sonraki sürüme bırakıldı; spec'te üç uygulama yolunun bedeli yazılı.
- **Sekme başına bağlam** — sunucuda saklama kararının doğrudan sonucu olarak iki sekme aynı bağlamı paylaşır.
- **403'ün ize girmesi** — C'den devralınan bedel.
- **`internship:manage`'in diğer uçlarının bölünmesi** — yalnız override ayrıldı; müdür onay adımı olduğu gibi kaldı.
- **`ToggleUserStatus` yön kaybı** — C'nin spec'inde kayıtlı, ayrı bir iş.
