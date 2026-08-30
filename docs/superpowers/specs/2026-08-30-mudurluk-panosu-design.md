# Müdürlük panosu (D2)

**Tarih:** 30.08.2026
**Durum:** onaylandı, plana hazır
**Önceki:** A parçası (#279, kurum hiyerarşisi), C parçası (#280, denetim izi), B parçası (#281, aktif bağlam)
**Sipariş kısıtı:** **D1 önce biter.** Gerekçe aşağıda, "Sipariş bağımlılığı".

## Problem

İl/ilçe millî eğitim yetkilisi `Ana Sayfa`'ya girdiğinde okul panosunu görüyor: öğrenci durum
dağılımı, sözleşme durumları, devamsızlık grafikleri. Bunların hiçbiri onun işi değil ve
hiçbiri veri döndürmüyor.

**Ölçüldü — neden boş döndüğü kritik.** `InstitutionTenantDirectory` kiracı listesini
`OfNodeType(School)` ile süzer: **il ve ilçe düğümleri kiracı değildir** ve kiracı damgalı
hiçbir veri taşımazlar. Müdürlük yetkilisi kendi bağlamındayken kiracısı kendi kurumudur;
`Student`, `Contract`, `AttendanceRecord` gibi `Tenant` sınıfı belgelere attığı her sorgu
**hata değil boş sonuç** döner. Yani bugünkü pano ona bozuk değil, **yanlış** görünüyor:
"okulumda hiç öğrenci yok" der.

Müdürlüğün işi başka: alt ağacındaki okulları görmek, yönetilemez durumdaki okulu bulmak,
takılmış fesih onay zincirine müdahale etmek (`internship:approval:override`).

## Kapsam

**Bu spec (D2):** `Ana Sayfa`'nın müdürlük bağlamında üç kartlık bir panoya dallanması, üç
kartı besleyen okuma yolları, kiracılar arası okumanın tek kapıya hapsedilmesi ve eşik
parametresinin yönetici ekranı.

**Bu spec DEĞİL:** rota/menü/buton izin hizası ve drawer'ın okul-bağlamı kapısı — D1.
Kullanıcı listesi kapsam açığı (`GetUserAccounts` / `GetInvitations` aktörden türeyen
daraltma yapmıyor) — ayrı güvenlik düzeltmesi, D1'in ön koşulu.

---

## Karar 1 — Kart 1: alt kurum ağacı, yeni backend YOK

`GetInstitutionsHandler` zaten aktörün alt ağacına süzüyor: `InstitutionScopePolicy.VisibleScope`
yol öneki (`Path.StartsWith(prefix)`) verir, kapsam istekten alınmaz.

Kart iki sayı gösterir ve ikisi de mevcut uçtan gelir:

- il bağlamında: `GET /api/institutions?nodeType=District&pageSize=1` → `TotalCount` = ilçe sayısı
- her bağlamda: `GET /api/institutions?nodeType=School&pageSize=1` → `TotalCount` = okul sayısı

Eylem çağrısı `Kurumlar` sayfasına gider (`/institutions`).

**Neden yeni uç açılmıyor:** sayılar zaten sayfalı sorgunun `TotalCount`'unda. Özet uç açmak
aynı kapsam kararının ikinci bir kopyasını doğururdu ve o kopya ayrışabilirdi.

---

## Karar 2 — Kart 2: yöneticisi olmayan okullar

**Ölçüt:** okula bağlı, etkin ve `institution:manage` iznini taşıyan **hiçbir** kullanıcı yok.

İzin, kullanıcının rollerinden `RolePermissionMap` ile türetilir — **rol adına bakılmaz**
(ADR-0001). "Hiç kullanıcısı yok" ölçütü bilerek seçilmedi: beş öğretmeni olan ama müdürü
olmayan okul da fiilen yönetilemez durumdadır ve bootstrap iş listesi tam olarak budur.

### Read-model KULLANICI başınadır, kurum başına sayaç DEĞİL

İlk tasarım kurum başına bir sayaç (`ManagerCount`) tutup olay olay artırıp azaltıyordu.
**Ölçüm bunu çürüttü:** sayacı azaltması gereken üç olayın hiçbiri kurum kimliği taşımıyor.

| Olay | Alanlar | Kurum kimliği |
|---|---|---|
| `UserCreated` | `UserAccountId, KeycloakUserId, Username, FullName, Email, Roles, InstitutionId, BusinessId, Metadata` | **var** |
| `UserInstitutionChanged` | `UserAccountId, KeycloakUserId, PreviousInstitutionId, InstitutionId` | **var** |
| `UserRolesChanged` | `UserAccountId, KeycloakUserId, PreviousRoles, NewRoles` | **yok** |
| `UserDeactivated` | `UserAccountId, KeycloakUserId, Reason` | **yok** |
| `UserDeleted` | `UserAccountId, KeycloakUserId` | **yok** |

Institution modülü, rolü değişen kullanıcının hangi okula bağlı olduğunu bilemez; sayacı
hangi satırdan düşeceğini de bilemez. Olaylara kurum kimliği eklemek Security modülünün
sözleşmesini bu kartın ihtiyacına göre değiştirmek olurdu.

Doğru model **kullanıcı başına bir satır**dır. Her olay tek bir kullanıcının durumunu **mutlak**
olarak yazar; artırma/azaltma hiç yoktur, dolayısıyla kayan sayaç sorunu da yoktur.

```csharp
public sealed class InstitutionManagerLink
{
    public Guid Id { get; set; }                  // = UserAccountId
    public Guid? InstitutionId { get; set; }      // null = kurum kapsamsız
    public bool IsEnabled { get; set; } = true;
    public bool HasManagePermission { get; set; } // rollerden RolePermissionMap ile türer
    public DateTime UpdatedAt { get; set; }
}
```

`DocumentTenancyMap` sınıflandırması: **`Identity`**. Kaynağı `UserAccount` (`Identity`),
hedefi `Institution` (`Identity`); ikisi de kiracı damgası taşımaz.

### Besleyen olaylar

Hepsi düz (aggregate olmayan) handler dönüşleridir, yani Wolverine tarafından cascading olarak
yayınlanır.

| Olay | Etki |
|---|---|
| `UserCreated` | Satırı yazar; `HasManagePermission` `Roles`'tan türer |
| `UserInstitutionChanged` | Satırın `InstitutionId`'sini yazar |
| `UserRolesChanged` | `HasManagePermission`'ı `NewRoles`'tan yeniden türetir |
| `UserActivated` / `UserDeactivated` | `IsEnabled` |
| `UserDeleted` | Satırı siler |

İzin rollerden `RolePermissionMap.GetPermissionsForRoles(roles)` ile türetilir ve
`Permissions.Institution.Manage` aranır — **rol adına bakılmaz** (ADR-0001). Wildcard
(`institution:*`) bu fonksiyonda zaten genişletilir.

**Tüketici sıralı olmak zorundadır.** Sınıf `IConfigureLocalQueue` uygular ve
`public static void Configure(LocalQueueConfiguration c) => c.Sequential();` yazar (#262:
sticky yerel kuyruk varsayılan olarak paralel ve sırasızdır). Load-modify-store yapan her
tüketici için geçerli. Statik sınıf arayüz uygulayamaz → `sealed class`, metotlar statik kalır.

### Sorgu iki adımlıdır — ve NEGATİF yönde

Marten join yapmaz. Sıra **önemlidir** ve tersi sayfalamayı bozar:

1. `InstitutionManagerLink` içinde `IsEnabled && HasManagePermission && InstitutionId != null`
   → **yönetilen** kurum kimlikleri (tekilleştirilmiş)
2. `Institution` içinde kapsam süzgeci **ve** `!managedIds.Contains(i.Id)` → sayfalanır

İkinci adım bilerek **negatiftir**: aranan şey satırı OLMAYAN kurumdur. Pozitif yönde
(yöneticisiz kurumların kimliklerini toplayıp `Contains` demek) her kurum için bir satırın var
olmasını gerektirirdi ve hiç kullanıcı olayı görmemiş kurum o listede hiç doğmazdı — aranan
kurum tam olarak o.

Ters sıra (önce kurumları sayfala, sonra bellekte süz) sayfa boyutlarını yanlışlardı: 20
satırlık bir sayfadan 3'ü kalırsa istemci "3 sonuç var" sanır.

### Backfill — ZORUNLU dağıtım ön koşulu

Read-model boş doğar; hiçbir olay geçmişe dönük yeniden oynatılmaz. Boş read-model'de
**yönetilen kurum kümesi boştur**, yani negatif sorgu **her okulu** "yöneticisi yok" olarak
döndürür — hata değil, yanlış liste.

`POST /api/security/users/replay` (`Permissions.Platform.TenantManage`) — **Security
modülünde**, Institution'da değil.

**Neden orada:** Institution modülü `UserAccount` belgesini okuyamaz (şema izolasyonu).
Backfill, Security'nin kendi kayıtlarını `UserCreated` olarak **yeniden yayınlaması** ve
Institution tüketicisinin onları normal yoldan işlemesidir. Depoda birebir emsali var:
`POST /api/institutions/staff/resync-branch-codes` olayı yeniden yayınlar, Security tüketir.

Kurum başına satır YOKTUR — model kullanıcı başınadır, dolayısıyla hiç kullanıcısı olmayan
okul için yazılacak bir şey de yoktur; o okul negatif sorguda kendiliğinden "yöneticisiz"
çıkar. İdempotenttir: tüketici satırı mutlak olarak yazar.

`src/Docs/docs/infrastructure/dagitim-on-kosullari.md` dosyasına eklenir.

### Uç

`GET /api/institutions/unmanaged` → `PagedResult<UnmanagedInstitutionDto>`
(`Permissions.Institution.View`). Kapsam Karar 1 ile aynı kaynaktan gelir
(`InstitutionScopePolicy.VisibleScope`), tekrarlanmaz.

---

## Karar 3 — Kart 3: tıkanmış onaylar, kiracılar arası TEK sorgu

### Ölçüm: `ITenantDirectory` taraması gerekmiyor

D1 spec'i bu kartı "tüm kiracıları dolaşmak" olarak fiyatlamıştı. Ölçüm bunu çürüttü:

```csharp
public static string ForInstitution(Guid institutionId) => institutionId.ToString();
```

Kiracı kimliği okul kimliğinin **kendisidir** (`TenantResolution`, 1:1 eşleşmenin yaşadığı tek
yer, #148). Alt ağaç zaten okul kimliklerini veriyor. Marten 9.11.0 `TenantIsOneOf(params
string[])` operatörünü sağlar ve SQL'de `tenant_id IN (...)` üretir. Yani **tek sorgu**;
kiracı kiracı dağılma da, denormalize özet belge de, ikinci bir backfill de gerekmez.

### Modül sınırı: `IInstitutionSubtreeDirectory`

Internship modülü `Institution` belgesini okuyamaz (şema izolasyonu). Depoda bunun için
yerleşik desen var — `ITenantDirectory`: **sözleşme `Common.Infrastructure`'da, uygulaması
modülde**.

```csharp
// src/MESNET.Common.Infrastructure/Tenancy/IInstitutionSubtreeDirectory.cs
public interface IInstitutionSubtreeDirectory
{
    /// <summary>Yol öneki altındaki OKUL düğümlerinin kiracı kimlikleri.</summary>
    Task<IReadOnlyList<string>> GetSchoolTenantsAsync(
        string pathPrefix, CancellationToken cancellationToken = default);

    /// <summary>Bütün okul kiracıları — yalnız kapsamsız (platform) aktör için.</summary>
    Task<IReadOnlyList<string>> GetAllSchoolTenantsAsync(
        CancellationToken cancellationToken = default);
}
```

Uygulaması `Institution.Application/Services/InstitutionSubtreeDirectory.cs`,
`InstitutionTenantDirectory` ile birebir aynı gerekçelerle: `TenantResolution.Platform`
session'ı, `OfNodeType(School)` süzgeci, çevrim `TenantResolution.ForInstitution` ile.

### Kiracı yalıtımını delen operatörün TEK kapısı

`TenantIsOneOf` kiracı yalıtımını **bilerek** deler. Depoya serbest girmez; tek sarıcıda hapsedilir:

```csharp
// src/MESNET.Common.Infrastructure/Tenancy/SubtreeTenantScope.cs
public sealed class SubtreeTenantScope(IInstitutionSubtreeDirectory directory)
{
    /// <summary>
    /// Aktörün görebileceği okul kiracıları. Kimlikler <b>istekten HİÇ gelmez</b>;
    /// InstitutionVisibility'den (yol öneki / kurum kimliği / kapsamsız) türer.
    /// </summary>
    public async Task<IReadOnlyList<string>> ResolveAsync(
        InstitutionVisibility scope, CancellationToken cancellationToken = default);
}
```

Üç hâl, tek kod yolu:

| Kapsam | Sonuç |
|---|---|
| `Unrestricted` (`platform:tenant:manage`) | `GetAllSchoolTenantsAsync()` — **`AnyTenant()` DEĞİL** |
| `PathPrefix` var (müdürlük) | `GetSchoolTenantsAsync(prefix)` |
| Yalnız `InstitutionId` (okul) | tek elemanlı liste |
| Kapsamsız | **boş liste** → sorgu hiçbir satır bulmaz |

**Boş liste `TenantIsOneOf()`'a verilmez.** Parametresiz çağrının SQL'de ne ürettiğine
güvenilmez; sarıcı boş listede sorguyu hiç kurmaz ve çağırana boş sonuç döndürür. Kapsamsız
aktörün her şeyi görmesi ile hiçbir şey görmesi arasındaki fark budur.

**`AnyTenant()` depoda tamamen yasaktır.** Kapsamsız aktör için bile kullanılmaz: tek kod
yolu, tek gözden geçirme noktası.

### Kilitleyen test — `CrossTenantQueryDriftTests`

Depo idiomu (`TenantlessSessionDriftTests`, `AnonymousEndpointDriftTests`,
`InstitutionScopeDriftTests`) ile aynı: kaynak taraması.

1. `AnyTenant(` hiçbir `.cs` dosyasında geçmez (test dosyaları dahil).
2. `TenantIsOneOf(` yalnız `SubtreeTenantScope.cs` ve onun kendi testinde geçer.

**Neden kaynak taraması:** derlenmesi ve testin yeşil olması bir operatörün *nerede*
kullanıldığını kanıtlamaz. Yeni bir handler `AnyTenant()` yazarsa hiçbir davranış testi kırmaz
— kiracılar arası okuma sessizce açılır.

**Tarama nullable körü olmayacak.** `InstitutionScopeDriftTests` literal `"Guid InstitutionId"`
arıyor ve `Guid? InstitutionId` bildirimlerini hiç görmüyor (ayrı güvenlik düzeltmesinin
konusu). Buradaki tarama tek bir tanımlayıcı adı arar, tip imzası aramaz; aynı tuzağa
düşmez.

### Ölçüt ve sorgu

```csharp
Phase == InternshipPhase.TerminationInProgress
  && !ApprovalChain.IsCompleteOrOverridden()
  && (TerminationRequestedAt is null || TerminationRequestedAt < now - threshold)
```

**Faz alanı sorguya HİÇ girmez — ölçülmüş gerekçeyle.**

`InternshipSaga.Phase` bir `InternshipPhase` SmartEnum'udur ve düz bir `PhaseName` ikizi
yoktur. Marten LINQ'te SmartEnum nested path'i (`data->'phase'->>'Name'`) **her zaman NULL**
döner (CLAUDE.md). İlk akla gelen çözüm — `PhaseName` alanı eklemek — burada **yanlış yöne**
başarısız olur: alan yeni olduğu için mevcut saga satırlarında yoktur, deserialize `null` ya
da boş string verir, o satırlar süzgece takılmaz ve **kart onları sessizce hiç göstermez.**
Aranan kayıtlar tam olarak eskiler olduğu için bu, kartı işe yaramaz yapardı.

Faz zaten türetilebilir: zincir varsa ve kapanmamışsa saga tanımı gereği
`TerminationInProgress`'tedir. Sorgu bu yüzden yalnız `ApprovalChain` üzerinden kurulur:

```csharp
x.ApprovalChain != null
  && !x.ApprovalChain.IsOverridden
  && !(x.ApprovalChain.TeacherApproved
       && x.ApprovalChain.DeputyApproved
       && x.ApprovalChain.DirectorApproved)
```

Bu alanların hepsi düz `bool`'dur ve `TerminationApprovalChain` JSON'da **nesne** olarak
serialize edilir (SmartEnum gibi çıplak string değil), dolayısıyla nested path çalışır. Alan
#218'den beri var; mevcut satırlarda dolu. **Yeni alan yok, backfill yok, sessiz yön yok.**

**Bedeli ve karşılığı:** `IsCompleteOrOverridden()` bir metottur, LINQ'e çevrilemez; kararı
açarak yazmak onu ikinci bir yerde yaşatır. Karşılığı bir doğruluk tablosu testidir:
`TerminationApprovalChain`'in 16 bayrak birleşiminin hepsinde LINQ ifadesinin sonucu
`!IsCompleteOrOverridden()` ile aynı olmalıdır. Zincir kuralı bir gün değişirse (dördüncü
onaycı) test kırmızı olur — ayrışma sessiz kalmaz.

### Uç

`GET /api/internships/stuck-approvals` → `StuckApprovalSummaryDto`
(`Permissions.Internship.ApprovalOverride`).

**Neden `internship:view` değil `approval:override`:** kart yalnız müdahale edebilecek aktöre
bilgi taşır. Görüp müdahale edemeyen kullanıcı için kart bilgi değil gürültüdür.

```csharp
public sealed record StuckApprovalSummaryDto(
    int TotalCount,
    int ThresholdDays,
    IReadOnlyList<StuckApprovalByInstitutionDto> ByInstitution);

public sealed record StuckApprovalByInstitutionDto(
    Guid InstitutionId, string? InstitutionName, int Count, int? OldestDays);
```

`InstitutionName` Internship modülünde yoktur ve oradan okunamaz (şema izolasyonu); backend
alanı **her zaman `null`** döndürür, ön yüz `useInstitutionOptions` benzeri lookup map ile
doldurur (depo deseni: ContractListPage zenginleştirmesi). Alan DTO'da yine de durur ki ön yüz
kendi tipini uydurmasın.

`OldestDays` **nullable'dır**: `TerminationRequestedAt` `null` olan saga tıkanmış sayılır ama
yaşı bilinmez. Kırılımda o okul için `null` döner ve ön yüz "bilinmiyor" gösterir — sıfır ya da
büyük bir sentinel yazmak sayıyı sessizce yanlışlardı. Bir okulda hem yaşı bilinen hem
bilinmeyen kayıt varsa `OldestDays` bilinenlerin en eskisidir.

---

## Karar 4 — `TerminationRequestedAt` ve eksik verinin yönü

### Ölçüm: talep zamanı hiç tutulmuyor

`InternshipSaga.Handle(InternshipTerminationRequested)` zinciri `ApprovalChain = new
TerminationApprovalChain()` ile kurar. `TerminationApprovalChain` yalnız `OverriddenAt` ve
`CompletedAt` taşır — **ikisi de zincir KAPANINCA dolar.** Yani bugün "kaç gündür bekliyor"
hesaplanamaz.

### Yeni alan

`InternshipSaga`'ya `public DateTime? TerminationRequestedAt { get; set; }` eklenir ve
`Handle(InternshipTerminationRequested)` içinde `DateTime.UtcNow` ile doldurulur — zincirin
kurulduğu satırın yanında, ayrı bir yola konmaz.

### `null` TIKANMIŞ SAYILIR

Mevcut sagalarda alan `null` doğar. Karar: **`null` eşiği aşmış sayılır.**

Gerekçe CLAUDE.md'nin kendi ilkesi (#252): *eksik veri sınırı gevşetemez.* Ters karar
(`null` → sayma) fesih zincirinde aylardır takılı duran her eski kaydı panodan **sessizce**
silerdi — tam olarak kartın var olma sebebi olan durum.

**Bunun değerli sonucu:** geriye dönük doldurma **gerekmez.** Doldurulmazsa kart fazla sayar
(görünür ve düzeltilebilir), az saymaz (sessiz ve fark edilmez) — aşağıda ölçülen mimari engel
bu yüzden bir sorun değil.

### Backfill YAPILMAZ — ölçülmüş mimari engel

Tasarım sırasında `AuditEntry.OccurredAt` (C parçası) doğal kaynak gibi görünüyordu. Ölçüm
bunu kapattı: `MESNET.Internship.Application.csproj` `MESNET.Audit.Core`'a **referans
vermiyor ve veremez** — modüller arası referans kuralı yalnız `.Shared` katmanına izin verir
(CLAUDE.md, "csproj Proje Referansı Kuralları"). Denetim satırını Internship modülünden okumak
şema izolasyonunu kırardı.

Ters yön de kapalı: Audit modülü `AuditEntry`'yi bilir ama `InternshipSaga`'yı bilmez.

Doğru çözüm (Audit'in zaman damgalarını olayla ya da sözleşmeyle dışa vermesi) **isteğe bağlı
bir iyileştirme için fazla iştir**, çünkü eksik veri zaten güvenli yönde ele alınıyor: zamanı
bilinmeyen açık zincir tıkanmış sayılır, yani kart onu **gösterir**. Karar: backfill yok.

Görünen sonuç: bu alan eklenmeden önce açılmış zincirlerde kart yaşı "bilinmiyor" der ve kayıt
eşikten bağımsız listelenir. Zamanla kendiliğinden düzelir — yeni her talep zamanını yazar.

---

## Karar 5 — Eşik ulusal tekil parametredir

Emsal birebir mevcut: `AttendanceLimitConfig` — `Shared` sınıfı tekil belge (sabit
`SingletonId`), yazma izni `platform:parameter:manage`, hiçbir okul rolünde yok.

```csharp
public sealed class InternshipApprovalConfig
{
    public static readonly Guid SingletonId = Guid.Parse("8c62ac6c-a944-4eb6-b3b0-342fe7ffc3a6");
    public Guid Id { get; set; } = SingletonId;
    public int StuckApprovalDays { get; set; } = 14;
    public Guid UpdatedById { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

`DocumentTenancyMap["InternshipApprovalConfig"] = Shared`.

**Belge yoksa varsayılan 14 kullanılır** ve belge yazılmaz. Okuma yolunun yan etkisi olmaz;
ayrıca ilk okuma bir yazma tetikleseydi kiracı kararı okuma ucuna sızardı.

Uçlar:
- `GET /api/internships/approval-config` — `Permissions.Internship.ApprovalOverride`
- `PUT /api/internships/approval-config` — `Permissions.Platform.ParameterManage`

Doğrulama: `1 <= StuckApprovalDays <= 365`. Sıfır ve negatif her açık zinciri tıkanmış yapar;
üst sınır yazım hatasını (`1400`) kartı sessizce boşaltmadan durdurur.

### Yönetici ekranı

Ulusal parametre katmanının bugün **hiç ön yüzü yok** — `AttendanceLimitConfig` uçları var,
sayfası yok. Bu eşik o katmanın ilk ekranı olur.

`src/WebUI/src/pages/admin/PlatformParametersPage.vue`, rota `/admin/parameters`,
`meta: { permissions: ['platform:parameter:manage'] }`, menüde `Yönetim` grubunun altında.

**YAGNI:** sayfa yalnız eşiği taşır. Devamsızlık sınırları oraya taşınmaz — ayrı iş, ayrı
doğrulama, ayrı mevzuat gerekçesi. Sayfa ileride onları da barındırabilir; bu spec açmaz.

---

## Karar 6 — Ön yüz dallanması

### `isActingAsDirectorate` — `resolveIsUpperNode` DEĞİL

Kod tabanında birbirine çok benzeyen ama **aktif bağlam açıkken ayrışan** iki soru var:

| Soru | Fonksiyon | Aktif bağlam açıkken |
|---|---|---|
| "Aktör üst düğüm mü?" | `resolveIsUpperNode(nodeType, activeInstitutionId)` | **true** — `Kurumlar` ağacı okula geçince de görünmeli |
| "Şu an müdürlük olarak mı davranıyorum?" | `isActingAsDirectorate(nodeType)` | **false** — kiracı o okuldur, okul panosu doğrudur |

```typescript
// src/WebUI/src/utils/directorateContext.ts
export function isActingAsDirectorate(nodeType: string | null | undefined): boolean {
  return nodeType === 'Province' || nodeType === 'District'
}
```

`institutionStore.institution?.nodeType` aktif bağlama bağlıdır, dolayısıyla bu tek girdi
doğru cevabı verir.

**Buradaki bariz hata `resolveIsUpperNode`'u kopyalamak olurdu**: il yetkilisi bir okula
geçtiğinde müdürlük panosunu görür, o pano da okul kiracısında alt ağaç sorar ve okul kendi
altında hiçbir şey bulamaz — **hata değil, boş pano**. Test bu farkı açıkça kilitler:
aktif bağlam dolu + `nodeType === 'School'` → `false`.

### Bileşen ayrımı

`DashboardPage.vue` bugün 418 satır. Müdürlük dalını içine gömmek CLAUDE.md'nin composable
çıkarma eşiğini (300 satır / 3'ten fazla ilgi alanı) aşar.

- `src/WebUI/src/pages/dashboard/DirectorateDashboard.vue` — üç kart
- `src/WebUI/src/composables/useDirectorateDashboard.ts` — üç çağrı, yükleme/hata durumları
- `DashboardPage.vue` yalnız dallanır: `isActingAsDirectorate` → biri ya da öteki

Rota değişmez (`Ana Sayfa`). Ayrı rota açılsaydı drawer'da ikinci bir `Ana Sayfa` girdisi ya
da bir yönlendirme gerekirdi; ikisi de bedava değil.

### Kart içerikleri ve boş durumlar

| Kart | Dolu | Boş |
|---|---|---|
| Alt kurum ağacı | "12 ilçe, 148 okul" → `/institutions` | "Alt ağacınızda kayıtlı kurum yok." + nötr ikon |
| Yöneticisi olmayan okullar | sayı + ilk 5 okul adı → kullanıcı yönetimi | "Tüm okulların yöneticisi var." + nötr ikon |
| Tıkanmış onaylar | sayı + okula göre kırılım → `Fesihler` | "14 günden uzun bekleyen onay yok." + nötr ikon |

Boş durum **hata değildir**: uyarı (⚠) ikonu kullanılmaz, nötr ikon + varsa eylem çağrısı
(CLAUDE.md, boş-durum nötr olmalı).

Kart 3 `internship:approval:override` taşımayan kullanıcıya **hiç gösterilmez** — ucun izniyle
aynı karar, ön yüzde ikinci kez sorulmaz, `authStore.hasPermission` ile bakılır.

---

## Sipariş bağımlılığı

**D1 önce biter.** Kart 2 ve 3'ün eylem çağrıları D1'in düzelttiği katmanlardan geçer:

- Kart 2 → kullanıcı yönetimi rotası; bugünkü `meta: { permissions: ['user:view','user:create'] }`
  müdürlük rollerini dışarıda bırakıyor
- Kart 3 → `Fesihler` rotası (`['internship:view','internship:manage']`) ve sayfa içi buton
  (`hasPermission(Internship.Manage)`); ikisi de `approval:override` taşıyan müdürlüğü
  dışarıda bırakıyor, ayrıca menüde `Fesihler` girdisi hiç yok

D1 de kendi sırasında kullanıcı listesi kapsam düzeltmesini bekliyor
(`GetUserAccounts` / `GetInvitations` aktörden türeyen daraltma yapmıyor; `UserAccount` ve
`UserInvitation` `Identity` sınıfı olduğu için conjoined kiracılık onları süzmez). Tam sıra:

```
kapsam güvenlik düzeltmesi  →  D1  →  D2
```

---

## Test kapsamı

**Backend — saf birim testleri (montaj yok):**
- `SubtreeTenantScope`: dört kapsam hâli; kapsamsızda boş liste ve sorgunun hiç kurulmaması
- Tıkanma ölçütü: eşiğin altı/üstü, `null` → tıkanmış, kapanmış zincir → sayılmaz,
  override edilmiş zincir → sayılmaz
- `InternshipApprovalConfig` doğrulaması: 0, −1, 366 reddedilir; belge yokken varsayılan 14
- `InstitutionAdminView` tüketicisi: her olay için sayaç yönü; sıfırın altına düşmez

**Backend — kilitleyen kaynak taramaları:**
- `CrossTenantQueryDriftTests`: `AnyTenant(` hiç yok; `TenantIsOneOf(` yalnız sarıcıda
- `DocumentTenancyMap` sınıflandırma testi (mevcut) yeni iki belgeyi zorunlu kılar

**Ön yüz:**
- `isActingAsDirectorate` spec'i: Province/District → true; School → false; **aktif bağlam
  dolu + School → false** (kopyalama hatasını kilitler); `null`/`undefined` → false
- `useDirectorateDashboard` spec'i: üç çağrıdan biri patlarsa diğer iki kart yine dolar

**Kilidin kilit olduğu kanıtlanır:** `CrossTenantQueryDriftTests` yazıldıktan sonra rastgele
bir handler'a `AnyTenant()` eklenip testin kırmızıya döndüğü görülür, sonra geri alınır. Bu
oturumda tekrar eden başarısızlık kalıbı **içi boş kilit**: yeşil ama hiçbir şeyi korumuyor.

## Dağıtım ön koşulları

| Uç | Zorunlu mu | Atlanırsa |
|---|---|---|
| `POST /api/security/users/replay` | **evet** | Her okul "yöneticisi yok" görünür |

`src/Docs/docs/infrastructure/dagitim-on-kosullari.md` dosyasına eklenir.
