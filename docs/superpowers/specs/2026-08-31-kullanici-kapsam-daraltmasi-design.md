# Kullanıcı ve davet okumalarında kurum kapsamı (güvenlik düzeltmesi)

**Tarih:** 31.08.2026
**Durum:** onaylandı, plana hazır
**Dal:** `fix/kullanici-kapsam-daraltmasi`, `feat/mudurluk-panosu` (D2) üstüne
**Sıra:** D2 → **bu düzeltme** → D1

## Problem

`UserAccount` ve `UserInvitation` `DocumentTenancyMap`'te **`Identity`** sınıfındadır
(`:151`, `:167`). `DocumentTenancyPolicy` yalnız `Tenant` sınıfını `Conjoined` yapar
(`:35-36`), yani bu iki belgeyi **hiçbir arka katman süzmez**. Kapsam kararının tamamı
sorgu handler'ına aittir ve orada yoktur.

`UserQueryHandler`'a `ICurrentUserService` **enjekte bile edilmiyor** (`:36-37`); tek sabit
süzgeç `DeletedAt == null` (`:40`). Kurum süzgeci yalnız istekten gelen isteğe bağlı
`institutionId` parametresiyle çalışır — yani kapsamı **çağıran seçer**.

### Ölçülen açık — dört okuma yolu

| Uç | İzin | Belge | Ne sızıyor |
|---|---|---|---|
| `GET /api/security/users` | `user:view` | `UserAccount` | Tüm okulların kullanıcıları: `Username`, `Email`, `FullName`, `Roles`, `DirectPermissions`, `BranchCodes`, `InstitutionId`/`BusinessId`/`StudentId`, `LinkedStudentIds`, `IsEnabled`, `LastLoginAt` |
| `GET /api/security/users/{id}` | `user:view` | `UserAccount` | Tekil kayıt; `LoadAsync` doğrudan kimlikle, aktörün kurumu **hiç** karşılaştırılmıyor (`:103-111`) |
| `GET /api/security/invitations` | `user:view` | `UserInvitation` | Tüm okulların davetleri + davet edenin ve onaylayanın adı + **`Metadata`** |
| `GET /api/security/role-integrity` | `user:roles:manage` | ikisi de | Kapsam parametresi **hiç yok**: `Query<UserInvitation>()` filtresiz (`:58`), `Query<UserAccount>()` yalnız mezar taşı elemesiyle (`:68`) |

**En ağır kalem:** davet `Metadata`'sı öğrenci davetinde **T.C. kimlik numarası** taşıyor
(`UserInvitation.cs:55-58`) ve `InvitationDto` onu olduğu gibi geçiriyor
(`InvitationHandler.cs:250`). Reşit olmayan öğrencinin kimlik numarası, başka okulun
müdürüne açık.

### Sızıntının yönü — yatay, dikey değil

`user:view` bugün yalnız **`InstitutionManager`** ve **`DeputyDirector`** rollerinde
(`RolePermissionMap.cs:21`, `:56` — `user:*` üzerinden). `ProvincialAdmin`/`DistrictAdmin`
(`:291-313`) ve `SystemAdmin` (`:317-335`) bu izni **taşımıyor**. Yani açık okul müdürleri
arasında yataydır.

**Ama izin bireysel de atanabilir:** `user:` önekli hiçbir izin
`AssignablePermissionScope.NeverDirectlyAssignable` listesinde değil (`:142-171`) ve
`InstitutionManager`'ın atanabilir kapsamı `*` (`:27`) — yani işletme yetkilisine, veliye ya
da öğretmene bugün verilmiş olabilir.

### Kapsam ABARTILMIYOR

Depoda `Guid? InstitutionId` bildiren ~20 kayıt var; çoğu `Tenant` sınıfı belge okur ve
conjoined kiracılık onları **zaten** süzer — güvenlik borcu değildirler.
`GetStudentsWithoutGuardian` da sızdırmaz: çıktısını kiracı damgalı `GuardianLinkView`
sınırlar ve dosya bunu gerekçesiyle yazmıştır (`GuardianLinkGapHandler.cs:21-24`).

Kurum ekseni olan **`Identity`** belge yalnız ikidir: `UserAccount` ve `UserInvitation`.
(`Institution` kiracının kendisidir ve kendi guard'ı vardır; `InstitutionManagerLink`
kurum kimliğiyle süzülmez, yalnız negatif kimlik kümesi üretir.)

---

## Karar 1 — Daraltma alt ağaç kimlik kümesiyle yapılır

`IInstitutionSubtreeDirectory`'ye **ikinci bir metot** eklenir:

```csharp
/// <summary>
/// Yol öneki altındaki <b>bütün</b> kurum kimlikleri — okul, ilçe ve il düğümleri dahil.
/// </summary>
Task<IReadOnlyList<Guid>> GetSubtreeInstitutionIdsAsync(
    string pathPrefix, CancellationToken cancellationToken = default);
```

**Mevcut `GetSchoolTenantsAsync` DEĞİŞTİRİLMEZ.** O bilerek yalnız `School` düğümünü
döndürür (`IInstitutionSubtreeDirectory.cs:11-13`), çünkü kiracı = okul. Kullanıcı
daraltmasında o listeyi kullanmak **müdürlüğün kendi ekibini görememesine** yol açardı:
müdürlük personelinin `UserAccount.InstitutionId`'si müdürlük **düğümüdür** ve okul
listesinde yoktur.

Yeni metot aynı boş-önek korumasını taşır: boş ya da yalnız boşluk önek **boş liste**
döndürür, çünkü Marten `StartsWith("")` ifadesini `LIKE '%'` çevirir ve kapsamı sessizce
tümden açardı.

### Neden bu, denormalize yol alanı değil

`UserAccount`'a `InstitutionPath` denormalize etmek (Audit deseni) tek SQL verirdi ama iki
**sessiz** hata kaynağı ithal ederdi:

1. Audit satırı tarihsel anlık görüntüdür, bayatlaması doğrudur. `UserAccount` **canlı
   otoritedir**: `rebuild-hierarchy` sonrası başka ilçeye taşınan okulun kullanıcıları yeni
   müdürlükte **görünmez**, eski müdürlükte **görünmeye devam eder** — ikisi de sessiz.
2. Yeni alan mevcut satırların hepsinde boş doğar → backfill ucu yazılmadan dağıtılırsa yol
   önekli her aktör **hiçbir kullanıcı görmez**. Hata değil, boş liste.

Bu depoda aşırı daraltmanın belirtisi istisna değil **sessiz boş listedir**; ek sorgunun
maliyeti o riskin yanında ucuzdur.

### Neden elle kimlik eşitliği değil

`u.InstitutionId == aktörün kurumu` biçimi bugünkü iki izin sahibi için doğru sonuç verir
ama `InstitutionScopeDriftTests:334-339`'un adıyla yasakladığı elle kopya kararın ta
kendisidir, ve D1 müdürlük rollerine `user:view` verdiği anda **alt ağaçtaki hiçbir okulun**
kullanıcısını göstermez.

---

## Karar 2 — Çeviri Security'de TEK yerde yaşar

`InstitutionScopePolicy.VisibleScope(actorInstitutionId, actorPath, hasPlatformScope)`
çağrılır; karar **tekrarlanmaz**. Üç hâl:

| Kapsam | Süzgeç |
|---|---|
| `Unrestricted` (`platform:tenant:manage`) | süzgeç yok |
| `PathPrefix` | kimlikler çözülür → `u.InstitutionId == null \|\| ids.Contains(u.InstitutionId.Value)` |
| yalnız `InstitutionId` | `u.InstitutionId == null \|\| u.InstitutionId == id` |

**Platform muafiyeti EN ÖNDE** (`InstitutionScopePolicy.cs:99-100` idiomu). Aksi hâlde kendi
kurumu olmayan platform aktörü `Guid.Empty`'ye düşer ve **her zaman** boş liste görür.

Fonksiyon **tek** olmalı ve hem kullanıcı hem davet sorgusu onu çağırmalıdır. Bu, deponun
üçüncü kapsam-çeviri kopyası olacaktır ve iki mevcut kopya **şimdiden ayrışmıştır**:
Institution'ınki boş/whitespace önek denetimi yapar (`InstitutionQueryExtensions.cs:63`),
Audit'inki **yapmaz** (`GetAuditEntriesHandler.cs:114`). Yeni kopya Institution'ınkini örnek
alır.

---

## Karar 3 — Kurum bağı olmayan hesap SÜZÜLMEZ, görünür kalır (`|| InstitutionId == null`)

**Bu opsiyonel değildir.** Katı süzgeç `InstitutionId == null` olan her hesabı düşürür;
`SyncUsersFromKeycloak` her hesabı böyle üretir (`UserManagementHandler.cs:646`, `:656` —
"yeni hesap her zaman kapsamsız doğar") ve CLAUDE.md bunu bilinçli davranış diye yazar.

Bağı kuran **tek** arayüz bu listedeki satırdır: `UserManagementPage.vue:173-178` "Kurum
bağı yok" rozeti → `:777` `securityApi.changeInstitution` → `POST /users/{id}/institution`.
Kullanıcı listede yoksa uç hiç çağrılamaz ve hesap **kalıcı kapsamsız** kalır — tek yönlü kapı.

Alternatif ("kapsamsız hesapları yalnız platform görsün") **ölçümle elendi**: `SystemAdmin`
`UserManagement.Create` ve `UserManagement.RolesManage` taşır ama `UserManagement.View`
**taşımaz** (`RolePermissionMap.cs:317` bloğu). Platform rolü listeyi zaten okuyamadığı için
o yol onarımı kimseye bırakmazdı.

Aynı kural davetler için de geçerlidir: `CreateInvitation` `InstitutionId`'yi isteğe bağlı
alır (`InvitationCommands.cs:15`); kurumsuz davetler düşerse onaylanamaz/reddedilemez hâle
gelirler.

Bu kararın kapattığı iki ölçülmüş kilitlenme daha:

- **Tavuk-yumurta.** Kuruma personel eklemek için Keycloak kullanıcısı bu listeden seçilir
  (`AddStaffForm.vue:130`) ama kurum bağı personel eklendikten **sonra** `StaffAuthorized`
  olayıyla kurulur (`StaffBranchSyncConsumer.cs:52-56`). Kapsamsız kullanıcı gizlenirse yeni
  öğretmen/personel **hiç** eklenemez — hata da vermez, açılır liste boş gelir.
- **Veri kaybı.** `StudentFormPage.vue:369-371` düzenleme modunda kayıtlı `keycloakUserId`'yi
  önbelleklenmiş tam listede arar. Daraltma o kullanıcıyı düşürürse `find` `undefined` döner,
  seçici boş görünür ve kaydetme (`:436`) öğrencinin kullanıcı bağını **sessizce** kopartır.

---

## Karar 4 — Süzgeç `MissingBranchOnly` dalından ÖNCE uygulanır

`UserQueryHandler.cs:60-63` bu dalda önce **tüm** eşleşen satırları belleğe çeker
(`ToListAsync`), sonra bellekte süzer ve sayfalar. Kapsam süzgeci `queryable`'a bu satırdan
**önce** eklenmezse sızıntı tam da bu yolda sürer. Sonra eklenirse `TotalCount` daralmış
kümeye göre yeniden hesaplanmalıdır, aksi hâlde sayfalayıcı var olmayan sayfalar gösterir.

---

## Karar 5 — Davet listesinden `Metadata` kaldırılır

Kapsam daraltılsa bile kendi okulunun her davetini gören herkes reşit olmayanların kimlik
numarasını okumaya devam ederdi. Bu, kapsamdan **bağımsız** bir veri minimizasyonu kararıdır.

Uygulama sırası: önce `Metadata`'yı kimin tükettiği ölçülür. Tüketen yoksa alan liste
DTO'sundan düz kaldırılır; tüketen varsa tekil davet ucuna taşınır.

---

## Karar 6 — `role-integrity` bilerek AÇIK BIRAKILIR ve kilit kırmızı kalır (#283)

`GET /api/security/role-integrity` kapsam parametresi hiç taşımaz
(`RoleIntegrityCommands.cs:14`) ve Keycloak bacağı doğası gereği realm geneldir
(`RoleIntegrityHandler.cs:82`). Kurum düzeyinde mi platform düzeyinde mi olduğu bir **ürün
kararıdır** ve bu spec onu vermez.

Karar verilene kadar: yeni drift kilidi bu dosyayı işaret eder ve **muafiyet listesine
KONMAZ**. Test kırmızı kalır. Gerekçe: borç görünür olur, sessiz kalmaz.

**Bu, dalın testlerinin kırmızı biteceği anlamına gelir ve bilinçlidir.** Ayrı bir iş
açılmadan merge edilmemelidir.

**Kırmızı testin mesajı kendini savunmalıdır.** Kilit `RoleIntegrityHandler.cs` için
başarısız olduğunda mesaj, bunun bir gözden kaçma değil **açık bir ürün kararı** olduğunu,
kararın ne olduğunu (kurum düzeyi mi platform düzeyi mi), **#283**'ü ve muafiyet listesine
eklemenin **kararı vermek anlamına geldiğini** yazmalıdır. Aksi hâlde bir sonraki okuyan testi
"düzeltmek" için dosyayı listeye ekler ve karar hiç verilmemiş olarak kapanır — bu oturumda
tekrar eden içi boş kilit kalıbının aynısı.

---

## Karar 7 — Kilit: mevcut testi genişletme, YENİ ve DAR bir kilit ekle

### Kör nokta doğrulandı

`InstitutionScopeDriftTests.cs:357`:

```csharp
match.Groups["body"].Value.Contains("Guid InstitutionId", StringComparison.Ordinal)
```

Düz literal. `Guid? InstitutionId` bu alt diziyi **içermez** — `?` eşleşmeyi bozar.

**İkinci kör nokta:** `RecordDeclaration` regex'i (`:49-50`) yalnız kurucu parametre
listesini görür (`body` = `[^)]*`); gövde bloğundaki `{ get; init; }` özellikleri hiç
taranmaz.

### Mevcut testi TEK BAŞINA genişletmek YANLIŞ

Literali `Guid\?? InstitutionId` regex'ine çevirmek `KnownDebt`'i 49'dan ~68'e çıkarır,
docstring'in üç ayrı sayımını ölçüsüz bırakır ve listenin "yalnız KÜÇÜLÜR" sözleşmesini
(`:70-73`) sulandırır — çünkü eklenecek ~19 satırın çoğu yalnız `Tenant` sınıfı belge okur ve
güvenlik borcu **değildir**. Gerçek iki riskli satır 68'in içinde kaybolur.

Ayrıca gevşek desen **yanlış pozitif** üretir: Coordination'ın `{ get; init; }` bildirimleri
claim'den doldurulur (`StudentTermGradeCommands.cs:42-43`), `CreateUser`/`ChangeUserInstitution`'ın
`ActorInstitutionId`'si uçta claim'den doldurulur (`UserManagementEndpoints.cs:100`) — üçü de
**doğru** desendir.

### Yeni kilit: `IdentityDocumentScopeDriftTests`

`tests/MESNET.Security.UnitTests/IdentityDocumentScopeDriftTests.cs` — mesaj **kaydını**
değil, `Query<UserAccount>()` / `Query<UserInvitation>()` çağıran **dosyayı** tarar. Her
dosya ya paylaşılan kapsam yardımcısını çağırmalı ya da gerekçesi **yazılı** bir izin
listesinde olmalıdır.

Bu şekil depoda kanıtlanmıştır: `CrossTenantQueryDriftTests`'in tam-yol izin listesi
(`:36-40`) ve `InstitutionScopeDriftTests.MayEnumerateAll` (`:219-239`) aynı idiom.

Başlangıç izin listesi ve gerekçeleri:

| Dosya | Gerekçe |
|---|---|
| `GuardianLinkGapHandler.cs` | Çıktı kiracı damgalı `GuardianLinkView` ile sınırlı; yalnız üyelik sorulmuş (dosyada `:21-24` yazılı) |
| `ReplayUserAccountsHandler.cs` | Dağıtım ön koşulu, `platform:tenant:manage` ile korunuyor |
| `SyncUsersFromKeycloakHandler.cs` | Kimlik senkronizasyonu; kurum bağı KURMAZ |

`RoleIntegrityHandler.cs` **konmaz** (Karar 6).

`DocumentTenancyMap.cs:164-166`'daki "KALAN BORÇ" notu, davet listesi kapatıldığında silinir.

---

## Karar 8 — Kararı SAF bir fonksiyona çıkar ve onu tüketici tarafından test et

Bugün hiçbir test aşırı daraltmayı göremez. `SecurityApiTests.cs:207-211` yalnız `200`
bekler; `TenantStampIntegrityTests.cs:106-107` yalnız `IsSuccessStatusCode` bekler. Fixture
kimliği `admin` hem `user:*` hem `platform:tenant:manage` taşır, yani `VisibleScope`'ta
`Unrestricted`'a düşer ve **her iki yönde de** yeşil kalır: sızdıran bugünkü hâl de, boş liste
döndüren bozuk daraltma da.

### Ölçüldü: canlı API davranış testi bu dalda YAZILAMAZ

`MESNET.Api.Tests` **çalışan yığına** karşı koşan kara kutu paketidir; Keycloak'tan tek bir
kimlik için token alır (`ApiTestFixture.cs`). İkinci okula bağlı bir aktörle test yazmak
`mesnet-realm.json`'da ikinci kurum + ikinci kullanıcı istiyor; realm'de bugün yalnız tek
okulun kullanıcıları var (`admin`, `teacher1..3`, `student1`, `viceprincipal`). Bu test kodu
değil **ortam işidir** ve bu düzeltmenin kapsamına sığmaz.

**Sahte kapsama üretilmez.** Sadece durum kodu ölçen bir test eklemek, iki hata yönünü de
yeşil bırakacağı için kilit değil süstür.

### Bunun yerine: karar saf fonksiyona çıkar

Kapsamın hangi dala düştüğü — süzgeç yok / kimlik kümesi — saf bir fonksiyonda yaşar ve
`MESNET.Security.UnitTests` içinde **DB'siz, Keycloak'sız** tüketici tarafından ölçülür:

```csharp
/// <summary>Kapsamın kullanıcı/davet sorgusuna nasıl çevrileceği. Saf karar.</summary>
public static class UserScopePolicy
{
    /// <returns>
    /// <c>null</c> = süzgeç UYGULANMAZ (platform kapsamı).
    /// Boş liste = yalnız kurum bağı olmayan kayıtlar görünür.
    /// Dolu liste = bu kimlikler VEYA kurum bağı olmayanlar görünür.
    /// </returns>
    public static IReadOnlyList<Guid>? VisibleInstitutionIds(
        InstitutionVisibility scope, IReadOnlyList<Guid> subtreeIds);
}
```

Ölçülecek hâller (hepsi zorunlu):

| Girdi | Beklenen |
|---|---|
| `Unrestricted` | `null` — süzgeç yok |
| `PathPrefix` + alt ağaç kimlikleri | o kimlikler |
| `PathPrefix` + boş alt ağaç | boş liste (yalnız bağsızlar) |
| Yalnız `InstitutionId` | tek elemanlı liste |
| `InstitutionId == Guid.Empty` (kapsamsız) | boş liste |

**Sıra dayatılır:** platform muafiyeti EN ÖNDE ölçülür — kendi kurumu olmayan platform
aktörünün `Guid.Empty`'ye düşüp her zaman boş liste görmesi, bu düzeltmenin en olası sessiz
hatasıdır.

### Üç dallı yükleme TEK bir yükleme şekline indirgenir

`Unrestricted` dışındaki iki hâl aynı yüklemi kullanır, çünkü kimlik hâli tek elemanlı bir
kümedir:

```csharp
u.InstitutionId == null || ids.Contains(u.InstitutionId.Value)
```

Böylece Karar 3 (`|| null`) tek yerde yaşar ve iki çağrı yerine kopyalanan şey bir satırlık
yüklemdir; Karar 7'nin kilidi zaten her çağrı yerinin paylaşılan çözücüyü kullanmasını
dayatır.

### Devredilen iş — sessizce düşürülmez

İkinci kuruma bağlı aktörle uçtan uca API testi **yapılacaklar listesine yazılır** ve gerekçesi
kaydedilir: `mesnet-realm.json`'a ikinci kurum ve o kuruma bağlı `user:view` taşıyan bir
kullanıcı eklenmesi gerekir. Realm import **tek seferliktir** (#195), yani bu değişiklik
çalışan kaba ulaşmaz — dev ortamının yeniden kurulmasını ya da elle kullanıcı açılmasını
gerektirir. Bu yüzden ayrı iştir.

## Karar 9 — Pano davet sayacındaki ölü süzgeç aynı değişiklikte düzeltilir

`useDashboardStats.ts:208-210` `securityApi.listInvitations({ status: 'Pending', pageSize: 1 })`
gönderir. **`'Pending'` geçerli bir `InvitationStatus` adı değildir** — doğrusu
`'PendingApproval'` (`InvitationStatus.cs:7`). `InvitationHandler.cs:236`'daki `TryFromName`
başarısız olur ve durum süzgeci **sessizce düşer**: kart bugün tüm durumların *ve* tüm
okulların davetini sayıyor.

Kapsam daraltması sayıyı bir anda düşürecek ve "kapsam düzeldi" sanılacaktır; oysa durum
süzgeci hâlâ hiç çalışmıyor olacaktır. İkisi aynı değişiklikte düzeltilir.

---

## Kapsam DIŞI

- `role-integrity`'nin kapsam kararı (Karar 6 — ayrı iş)
- Aktif bağlamın kullanıcı listesini daraltıp daraltmayacağı. Mevcut idiom **daraltmaz**:
  `institution_path` claim'i aktif bağlamla değişmez ve bu bilinçlidir
  (`PermissionClaimsTransformation.cs:162-165`). Bu spec o idioma uyar.
- Müdürlük rollerine `user:view` verilmesi (D1'in işi). Bu düzeltme, verildiğinde alt ağacın
  doğru çalışmasını **sağlar**; izni kendisi vermez.
- `InstitutionScopeDriftTests`'in nullable körlüğünün geniş kapsamlı onarımı — Karar 7'de
  gerekçesiyle reddedildi; yerine dar kilit konur.

## Dağıtım ön koşulu

**Yok.** Yeni belge alanı yok, backfill yok. Bu bilerek seçilmiştir (Karar 1).
