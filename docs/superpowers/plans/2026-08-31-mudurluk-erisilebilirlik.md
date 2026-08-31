# Müdahale yetkilerinin erişilebilirliği (D1) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** B parçasının sunucuda koruduğu iki müdahale yetkisini (`internship:approval:override`, `directorate:institution-bootstrap`) ön yüzden fiilen kullanılabilir yapmak, ve rota izinleri ile sunucu politikası arasına kilit koymak.

**Architecture:** İki okuma izni rol demetine eklenir; rota `meta.permissions`, menü girdileri ve sayfa içi buton koşulu sunucudaki politikayla hizalanır; hizanın bozulmasını yakalayan kaynak-tarama testi eklenir; drawer müdürlük bağlamında okul girdilerini gizler.

**Tech Stack:** .NET 10, Vue 3 + Quasar + TypeScript (pnpm, Vitest, vue-tsc), xUnit + Shouldly.

**Spec:** `docs/superpowers/specs/2026-08-31-mudurluk-erisilebilirlik-design.md`

**Dal:** `feat/mudurluk-erisilebilirlik`, `dev` üstünde (D2 #285 ve kapsam düzeltmesi #286 merge edilmiş durumda).

## Global Constraints

- Yetkilendirme **permission bazlıdır**, rol adına ASLA bakılmaz (ADR-0001).
- Rota `meta.permissions` ve menü girdisi izinleri **birebir aynı** olmalıdır; menüde görünüp rotada 403 yiyen ya da tersi bir girdi bırakılmaz.
- Yeni izin sabiti eklerken önek tuzağı (ADR-0002) gözetilir; bu planda yeni izin **tanımlanmıyor**, var olanlar dağıtılıyor.
- `<script setup>` içinde mutable state `ref()` ile; düz `let` yasak.
- Commit'lere `Co-Authored-By` trailer'ı EKLENMEZ.
- Türkçe yorum, XML doc, test adı ve arayüz metni; Türkçe karakterler doğru (ç, ş, ğ, ü, ö, ı, İ).

## Bilinmesi zorunlu iki ölçüm

1. **`InternshipSummary` `Tenant` sınıfındadır.** Müdürlük kendi bağlamındayken fesih listesi **boştur** ve bu **beklenen davranıştır** — yol aktif bağlamdır (#281). Bunu "bug" sanıp kiracılar arası ikinci bir liste ucu AÇMAYIN.
2. **`canOverride` düzeltmesi kimseden yetki almaz.** `internship:manage` taşıyıp `internship:approval:override` taşımayan rol yoktur (`InstitutionManager` `internship:*`, `DeputyDirector` açıkça).

## Komutlar

```bash
dotnet build MESNET.slnx && dotnet test MESNET.slnx
cd src/WebUI && pnpm test:run && pnpm exec vue-tsc --noEmit
```

**Temel çizgi:** backend 2073 test — **2072 geçer, 1 BİLEREK kırmızıdır**
(`IdentityDocumentScopeDriftTests`, tek ihlali `RoleIntegrityHandler.cs`, karar #283'te açık).
Bu kırmızıyı düzeltmeye çalışmayın. Frontend 308 geçer.

## Dosya Yapısı

| Dosya | Değişiklik |
|---|---|
| `src/WebUI/src/utils/permissions.ts` | `Internship.ApprovalOverride` + `Directorate.InstitutionBootstrap` sabitleri |
| `src/MESNET.Common.Shared/Security/RolePermissionMap.cs` | Müdürlük demetlerine iki okuma izni |
| `tests/MESNET.Security.UnitTests/DirectoratePermissionMappingTests.cs` | İki yeni grant + gerekçeleri |
| `src/WebUI/src/router/index.ts` | İki rotanın `meta.permissions`'ı |
| `src/WebUI/src/pages/internship/TerminationsPage.vue` | `canOverride` doğru izne bakar |
| `src/WebUI/src/composables/useNavigation.ts` | `Fesihler` girdisi, `Kullanıcılar` izni, bağlam kapısı |
| `src/WebUI/src/router/routePermissionAlignment.spec.ts` | **Yeni** — rota/politika hizası kilidi |
| `src/WebUI/src/composables/useNavigation.spec.ts` | Bağlam kapısı testleri (dosya varsa genişlet) |

---

### Task 1: Ön yüz izin sabitleri

**Files:** Modify `src/WebUI/src/utils/permissions.ts`

**Interfaces:**
- Produces: `Permissions.Internship.ApprovalOverride = 'internship:approval:override'` ve `Permissions.Directorate.InstitutionBootstrap = 'directorate:institution-bootstrap'`. Task 3 ve 4 bunları tüketir.

- [ ] **Step 1: Sunucudaki sabitlerin tam değerlerini ölç**

Run: `grep -n "ApprovalOverride\|InstitutionBootstrap" src/MESNET.Common.Shared/Security/Permissions.cs`

Dize değerlerini birebir kullan; tahmin etme.

- [ ] **Step 2: Sabitleri ekle**

`Internship` bloğuna, `Manage` satırının altına:

```typescript
    /** Onay zincirine müdahale — müdürlük yetkisi, okul rollerinde de bulunur. */
    ApprovalOverride: 'internship:approval:override',
```

Dosyada `Directorate` bloğu **yok**; `Internship` bloğunun ardına ekle:

```typescript
  /**
   * İl/ilçe müdürlüğü yetkileri. Önek bilerek `directorate:` — `institution:` olsaydı
   * InstitutionManager'ın `institution:*` wildcard'ı üzerinden her okul müdürüne geçerdi
   * (ADR-0002 önek tuzağı).
   */
  Directorate: {
    InstitutionBootstrap: 'directorate:institution-bootstrap',
  },
```

- [ ] **Step 3: Doğrula ve commit**

Run: `cd src/WebUI && pnpm exec vue-tsc --noEmit && pnpm test:run`

```bash
git add src/WebUI/src/utils/permissions.ts
git commit -m "feat(webui): müdahale yetkilerinin izin sabitleri eklendi"
```

---

### Task 2: Müdürlük demetine iki okuma izni

**Files:**
- Modify `src/MESNET.Common.Shared/Security/RolePermissionMap.cs`
- Modify `tests/MESNET.Security.UnitTests/DirectoratePermissionMappingTests.cs`

**Interfaces:** Produces — `ProvincialAdmin` ve `DistrictAdmin` artık `internship:view` ve `user:view` taşır.

- [ ] **Step 1: Başarısız testleri yaz**

`DirectoratePermissionMappingTests.cs` içine, mevcut testlerin sesiyle iki yeni `[Theory]` ekle
(`[InlineData(MesnetRoles.ProvincialAdmin)]` + `[InlineData(MesnetRoles.DistrictAdmin)]`
deseni dosyadaki mevcut testlerden kopyalanır):

```csharp
    /// <summary>
    /// Fesih listesini OKUYABİLMELİ. Müdahale izni (<c>internship:approval:override</c>) yazma
    /// yetkisidir; onsuz sayfaya ulaşılsa bile liste ucu 403 döner
    /// (<c>PermissionPolicies.InternshipViewOrOwn</c>).
    ///
    /// <para><b>Kendi bağlamında liste BOŞTUR ve bu doğrudur:</b> <c>InternshipSummary</c>
    /// kiracıya aittir, müdürlük düğümü kiracı değildir. Yol aktif bağlamdır (#281).</para>
    /// </summary>
    [Theory]
    [InlineData(MesnetRoles.ProvincialAdmin)]
    [InlineData(MesnetRoles.DistrictAdmin)]
    public void Mudurluk_rolleri_fesih_listesini_okuyabilir(string role)
    {
        RolePermissionMap.GetPermissionsForRoles([role])
            .ShouldContain(Permissions.Internship.View);
    }

    /// <summary>
    /// Kullanıcı listesini OKUYABİLMELİ — okula ilk yöneticiyi bağlamanın (bootstrap) tek
    /// arayüzü o listedir.
    ///
    /// <para><b>Bu izin daha önce verilemezdi:</b> <c>UserAccount</c> kimlik katmanındadır ve
    /// liste aktörden türeyen hiçbir daraltma yapmıyordu — <c>user:view</c> ülke geneli okuma
    /// demekti. Kapsam düzeltmesiyle liste artık aktörün ALT AĞACINA daralıyor.</para>
    /// </summary>
    [Theory]
    [InlineData(MesnetRoles.ProvincialAdmin)]
    [InlineData(MesnetRoles.DistrictAdmin)]
    public void Mudurluk_rolleri_kullanici_listesini_okuyabilir(string role)
    {
        RolePermissionMap.GetPermissionsForRoles([role])
            .ShouldContain(Permissions.UserManagement.View);
    }
```

- [ ] **Step 2: Testlerin başarısız olduğunu doğrula**

Run: `dotnet test tests/MESNET.Security.UnitTests/MESNET.Security.UnitTests.csproj --filter "FullyQualifiedName~DirectoratePermissionMappingTests"`
Expected: iki yeni test FAIL (izinler henüz yok). Mevcut testler geçmeye devam etmeli.

- [ ] **Step 3: İzinleri ekle**

`RolePermissionMap.cs` içinde `ProvincialAdmin` ve `DistrictAdmin` demetlerinin **ikisine de**,
mevcut dört iznin yanına:

```csharp
            // Fesih listesini okumak için. Müdahale (approval:override) yazma yetkisidir;
            // liste ucu ayrıca internship:view ister. Kendi bağlamında liste boştur — yol
            // aktif bağlamdır (#281).
            Permissions.Internship.View,
            // Okula ilk yöneticiyi bağlamanın (bootstrap) tek arayüzü kullanıcı listesidir.
            // Liste aktörün alt ağacına daralır; bu daraltma olmadan bu izin verilemezdi.
            Permissions.UserManagement.View,
```

**Wildcard EKLEME.** Demet açık liste olarak kalır; `internship:*` ya da `user:*` yazmak
müdürlüğe fesih onaylama ve rol yönetme yetkisi de verirdi.

- [ ] **Step 4: Testlerin geçtiğini ve MATRİSİN güncellendiğini doğrula**

Run: `dotnet test MESNET.slnx`

`PermissionMatrixDocTests` izin matrisini kilitler ve yeni grant'lerle **kırmızı olması
beklenir**; testin hata mesajı dosyaya yazılacak doğru metni verir. O metni
`src/Docs/docs/architecture/adr-0002-izin-agaci-ve-onek-secimi.md` içine uygula ve tekrar koş.

Expected son durum: yalnız bilinen `IdentityDocumentScopeDriftTests` kırmızı.

- [ ] **Step 5: Commit**

```bash
git add src/MESNET.Common.Shared/Security/RolePermissionMap.cs \
        tests/MESNET.Security.UnitTests/DirectoratePermissionMappingTests.cs src/Docs
git commit -m "feat(security): müdürlük rolleri fesih ve kullanıcı listesini okuyabilir"
```

---

### Task 3: Rota, buton ve menü izinlerini hizala

**Files:**
- Modify `src/WebUI/src/router/index.ts`
- Modify `src/WebUI/src/pages/internship/TerminationsPage.vue`
- Modify `src/WebUI/src/composables/useNavigation.ts`

**Interfaces:** Consumes Task 1'in sabitleri.

- [ ] **Step 1: Rota metalarını genişlet**

`router/index.ts` — `InternshipTerminations` (satır ~170):

```typescript
              meta: { permissions: ['internship:view', 'internship:manage', 'internship:approval:override'] },
```

`UserManagement` (satır ~370):

```typescript
              meta: { permissions: ['user:view', 'user:create', 'directorate:institution-bootstrap'] },
```

Gerekçe her ikisinin üstüne kısa bir Türkçe yorum olarak yazılır: sunucu politikası bu izinleri
`AnyOf` ile kabul ediyor, rota daha dar olursa sunucunun kabul ettiği aktör ön yüzden hiç
ulaşamaz.

- [ ] **Step 2: Buton koşulunu düzelt**

`TerminationsPage.vue:387`:

```typescript
// Sunucu bu eylemi internship:approval:override ile korur (InternshipEndpoints.cs:34).
// Önceden internship:manage'e bakılıyordu; müdürlük rolleri o izni TAŞIMAZ ve butonu hiç
// göremiyordu. Ters yönde kimse kapanmaz: manage taşıyıp override taşımayan rol yoktur.
const canOverride = computed(() => authStore.hasPermission(Permissions.Internship.ApprovalOverride))
```

- [ ] **Step 3: Menüyü güncelle**

`useNavigation.ts` — `institution` grubundaki `Kullanıcılar` girdisinin izin listesini rota ile
aynı üçlüye çıkar. `internship` grubuna `Fesihler` girdisi ekle:

```typescript
      { title: 'Fesihler', icon: 'link_off', to: { name: 'InternshipTerminations' }, permissions: ['internship:view', 'internship:manage', 'internship:approval:override'] },
```

Gerçek grup anahtarını ve mevcut girdilerin biçimini dosyadan oku; ikon adını Quasar'ın
Material Icons kümesinden seç.

- [ ] **Step 4: Doğrula ve commit**

Run: `cd src/WebUI && pnpm test:run && pnpm exec vue-tsc --noEmit`

```bash
git add src/WebUI/src/router/index.ts src/WebUI/src/pages/internship/TerminationsPage.vue \
        src/WebUI/src/composables/useNavigation.ts
git commit -m "fix(webui): müdahale yetkileri rota, menü ve butondan erişilebilir"
```

---

### Task 4: Rota/politika hizası kilidi

**Files:** Create `src/WebUI/src/router/routePermissionAlignment.spec.ts`

- [ ] **Step 1: Mevcut idiomu oku**

`src/WebUI/src/router/formRoutePermissions.spec.ts` — bu test `index.ts`'i **metin olarak**
okur, çünkü modülü import etmek sayfa bileşenlerini ve Pinia store'larını çeker. Aynı yaklaşımı
kullan: dosyayı `fs.readFileSync` ile oku, regex ile rota bloklarını çıkar.

- [ ] **Step 2: Kilidi yaz**

Test, açıkça yazılmış bir tablo üzerinden çalışır: rota adı → o rotanın `meta.permissions`
listesinde **bulunması zorunlu** izinler.

```typescript
/**
 * Rota izinleri, sunucudaki yetkilendirme politikasıyla ELLE eşleniyor ve aralarında hiçbir
 * bağ yok. Bu testin varlık nedeni o eşlemenin sessizce ayrışmasıdır.
 *
 * Ölçüldü (31.08.2026): `internship:approval:override` ve `directorate:institution-bootstrap`
 * sunucuda doğru korunuyordu ama rota metaları onları içermediği için müdürlük rolleri
 * sayfalara HİÇ ulaşamıyordu. Belirti 403 duvarı değil, menüde hiç görünmemekti — yani
 * GÖRÜNMEZ.
 *
 * Kural: bir yeteneğin sunucu politikası birden çok izni kabul ediyorsa (AnyOf), o yeteneğe
 * ulaşan rotanın meta.permissions listesi o izinlerin HEPSİNİ içermelidir.
 */
```

Tablo:

| Rota adı | Zorunlu izinler |
|---|---|
| `InternshipTerminations` | `internship:view`, `internship:manage`, `internship:approval:override` |
| `UserManagement` | `user:view`, `user:create`, `directorate:institution-bootstrap` |
| `PermissionScope` | `user:roles:manage` |

Başarısızlık mesajı hangi rotanın hangi izni kaçırdığını ve **sunucu politikasının o izni
kabul ettiğini** söylemeli; yalnız "eşleşmedi" demek yetmez.

- [ ] **Step 3: Kilidin kilitlediğini kanıtla**

`router/index.ts`'te `InternshipTerminations` metasından `'internship:approval:override'`
değerini **geçici olarak** çıkar.

Run: `cd src/WebUI && pnpm test:run src/router/routePermissionAlignment.spec.ts`
Expected: FAIL, mesajda o rota ve o izin adı geçiyor.

Değeri geri koy, tekrar koş. Expected: PASS. Kırmızı çıktıyı raporla ve `git diff` ile geçici
düzenlemenin commit'e girmediğini doğrula.

- [ ] **Step 4: Commit**

```bash
git add src/WebUI/src/router/routePermissionAlignment.spec.ts
git commit -m "test(webui): rota izinleri sunucu politikasıyla hizalı kalmak zorunda"
```

---

### Task 5: Drawer'ın müdürlük bağlamı kapısı

**Files:**
- Modify `src/WebUI/src/composables/useNavigation.ts`
- Modify (ya da create) `src/WebUI/src/composables/useNavigation.spec.ts`

**Interfaces:** Consumes `isActingAsDirectorate(nodeType)` (`src/WebUI/src/utils/directorateContext.ts`, D2'de yazıldı ve `directorateContext.spec.ts` ile kilitli).

- [ ] **Step 1: Görünürlük bağlamını genişlet**

`NavVisibilityContext` arayüzüne alan ekle:

```typescript
  /**
   * Aktör ŞU AN müdürlük olarak mı davranıyor? `isUpperNode` ile karıştırmayın: o "aktör üst
   * düğüm mü" der ve aktif bağlam açıkken de true kalır (Kurumlar ağacı okula geçince de
   * görünmeli). Bu alan aktif bağlam açıkken FALSE olur — kiracı o okuldur, okul menüleri
   * doğrudur.
   */
  isActingAsDirectorate: boolean
```

Bağlamı kuran `computed` içinde `isActingAsDirectorate(institutionStore.institution?.nodeType)`
ile doldur. `resolveIsUpperNode` çağrısına **dokunma**.

- [ ] **Step 2: Okul girdilerini kapat**

`Staj Yönetimi` grubunun `Fesihler` **dışındaki** çocuklarına ekle:

```typescript
        visibleWhen: (ctx) => !ctx.isActingAsDirectorate,
```

`Fesihler` girdisine bu koşul **KONMAZ** — müdürlük oraya bağlam değiştirdikten sonra gidecek
ve menü onun giriş kapısıdır.

`Kurum Yönetimi` grubuna dokunulmaz: `Kurumlar`, `Kurum Bilgileri`, `Kullanıcılar`,
`Son İşlemler` müdürlüğün kendi işidir.

- [ ] **Step 3: Testi yaz**

`menuDefinition` **gerçek tanımdır** ve testler onu import eder (yerel kopya üzerinde koşan
test gerilemeyi yakalayamaz). `isNavItemVisible` saf fonksiyondur ve tek başına koşar.

Ölçülecekler:
- Müdürlük bağlamında (`isActingAsDirectorate: true`) `Staj Yönetimi`'nin `Fesihler` dışındaki
  girdileri **görünmez**
- Aynı bağlamda `Fesihler` **görünür** (izin varsa)
- Okul bağlamında (`false`) hepsi görünür
- `Kurum Yönetimi` girdileri her iki bağlamda da görünür

- [ ] **Step 4: Kilidin kilitlediğini kanıtla**

Bir `visibleWhen` koşulunu geçici olarak sil, testin kırmızıya döndüğünü gör, geri koy.
Kırmızı çıktıyı raporla.

- [ ] **Step 5: Doğrula ve commit**

Run: `cd src/WebUI && pnpm test:run && pnpm exec vue-tsc --noEmit`

```bash
git add src/WebUI/src/composables/useNavigation.ts src/WebUI/src/composables/useNavigation.spec.ts
git commit -m "feat(webui): müdürlük bağlamında okul menüleri gizlenir"
```

---

## Uygulama sonrası kontrol listesi

- [ ] `dotnet test MESNET.slnx` — yalnız bilinen `IdentityDocumentScopeDriftTests` kırmızı (#283)
- [ ] `cd src/WebUI && pnpm test:run && pnpm exec vue-tsc --noEmit` — temiz
- [ ] Task 4 kilidi kırılabildiği kanıtlandı
- [ ] Task 5 kilidi kırılabildiği kanıtlandı
- [ ] Rota `meta.permissions` ile menü girdisi izinleri **birebir aynı**

## Elle doğrulama (canlı yığında, müdürlük hesabıyla)

1. `Fesihler` menüde görünüyor; kendi bağlamında liste **boş** — beklenen.
2. Bir okulun bağlamına geç: liste doluyor, müdahale butonu **görünüyor**.
3. `Kullanıcılar` açılıyor ve **alt ağacın** kullanıcıları listeleniyor.
4. Kendi bağlamında `Staj Yönetimi`'nin diğer girdileri **görünmüyor**.
