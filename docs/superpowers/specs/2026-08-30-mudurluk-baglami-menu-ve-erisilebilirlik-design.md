# Müdürlük bağlamı: menü kapısı ve müdahale yetkilerinin erişilebilirliği (D1)

**Tarih:** 30.08.2026
**Durum:** onaylandı, plana hazır
**Önceki:** A parçası (#279, kurum hiyerarşisi), C parçası (#280, denetim izi), B parçası (#281, aktif bağlam)

## Problem

Kullanıcı şunu bildirdi: "il ya da ilçe millî eğitim müdürlüğüne geçince navigation drawer
içindeki menüler değişmeli, sonuçta okulla çalışmıyor il ya da ilçe yetkilisi."

Ölçüm bunu doğrulamadı ve **daha ağır bir hata** buldu.

### Ölçüm 1 — bildirilen belirti test hesabından geliyor

`ProvincialAdmin` ve `DistrictAdmin` demetleri (`RolePermissionMap.cs:291` ve `:307`)
**tam olarak dört izin**, wildcard yok:

```
institution:view
institution:manage
internship:approval:override
directorate:institution-bootstrap
```

Bu demetle bugün drawer'da yalnız şunlar görünür: **Ana Sayfa**, **Kurum Yönetimi** →
`Kurumlar`, `Kurum Bilgileri`, `Son İşlemler`. Okul grupları — Kayıt & Öğrenci
(`student:view`), Staj Yönetimi, Koordinasyon, Belgeler & Raporlar — **zaten izinle
gizlidir**.

Belirti `admin` hesabında görüldü: o hesap `SystemAdmin` + `InstitutionManager` taşıyor,
yani `institution:*`, `student:*`, `internship:*`, `attendance:*`, `salary:*`,
`document:*`, `communication:*`, `user:*`, `coordinator:*`, `department:*`, `company:*`
wildcard'larının hepsi ve ayrıca `platform:tenant:manage`.

### Ölçüm 2 — asıl hata ters yönde: B'nin iki müdahale yeteneği de ön yüzden erişilemez

Sunucu ikisini de doğru koruyor; ön yüz üçü ayrı katmanda kapatıyor.

| Yetki | Sunucu | Ön yüz engeli | Konum |
|---|---|---|---|
| `internship:approval:override` | `InternshipEndpoints.cs:35` — `.RequireAuthorization(Permissions.Internship.ApprovalOverride)` | Rota `meta: { permissions: ['internship:view', 'internship:manage'] }` | `router/index.ts:170` |
| `internship:approval:override` | aynı | Sayfa içi buton `hasPermission(Permissions.Internship.Manage)` — **override'a bakmıyor** | `pages/internship/TerminationsPage.vue:387` |
| `internship:approval:override` | aynı | Menüde `Fesihler` girdisi **hiç yok** | `composables/useNavigation.ts` |
| `directorate:institution-bootstrap` | `PermissionPolicies.cs:50` — `AnyOf(UserManagement.RolesManage, Directorate.InstitutionBootstrap)` | Rota `meta: { permissions: ['user:view', 'user:create'] }` | `router/index.ts:370` |
| `directorate:institution-bootstrap` | aynı | Menü girdisi `['user:view', 'user:create']` | `useNavigation.ts:99` |

Müdürlük rolleri bu izinlerin **hiçbirini** taşımıyor, dolayısıyla her üç katman da kapalı.

**Ters yön oluşmuyor — ölçüldü.** `internship:manage` taşıyıp `internship:approval:override`
taşımayan rol yoktur: `InstitutionManager` `internship:*` wildcard'ıyla ikisini de alır,
`DeputyDirector` ikisini de açıkça taşır. Yani "butonu gören ama 403 yiyen kullanıcı"
durumu yoktur; hata tek yönlüdür.

### Neden hiçbir test görmedi

`DirectoratePermissionMappingTests` rol→izin eşlemesini kilitliyor, `PermissionMatrixDocTests`
matrisi kilitliyor, backend 2033 test yeşil, WebUI 300 test yeşil. Hiçbiri **izinden ekrana
giden yolu** kontrol etmiyor: rota `meta.permissions` listeleri sunucudaki politikayla elle
eşleniyor ve aralarında hiçbir kilit yok.

Aynı sınıftan bir hata daha önce kapatılmıştı: `fdf6795 fix(webui): form rotaları okuma
değil yazma izniyle korunuyor`. O sefer rota **fazla gevşekti** ve belirti 403 duvarıydı
(görülür). Bu sefer **fazla dar** ve belirti menüde hiç görünmemek (**görünmez**) — B'nin
uçtan uca canlı doğrulaması da bu yüzden yakalayamadı: doğrulama `admin` ile yapıldı ve
`admin` her iki izne de sahip.

## Kapsam

**Bu spec (D1):** rota/menü/buton izin hizası + kilitleyici test + drawer'ın okul-bağlamı
kapısı. Yeni backend verisi yok, yeni uç yok, dağıtım ön koşulu yok.

**Ayrı spec (D2 — müdürlük panosu):** Ana Sayfa'nın müdürlük düzeyinde farklı içerik
göstermesi; alt kurum ağacı, yöneticisi olmayan okullar, tıkanmış onaylar. Ayrılma nedeni
maliyet değil **kiracılık sınırıdır**: D1'in dokunduğu her şey `Identity` sınıfında
(`Institution`, `UserAccount`) ya da hiç veri değil; D2'nin üçüncü kartı `InternshipSaga`
= `Tenant` olduğu için `ITenantDirectory` ile okul okul dolaşmak, denormalize özet belge ve
yeni bir backfill ön koşulu gerektirir.

## Karar 1 — rota, menü ve buton izinleri sunucudaki politikayla hizalanır

| Yer | Bugün | Olacak |
|---|---|---|
| `router/index.ts:170` `InternshipTerminations` | `['internship:view', 'internship:manage']` | `['internship:view', 'internship:manage', 'internship:approval:override']` |
| `router/index.ts:370` `UserManagement` | `['user:view', 'user:create']` | `['user:view', 'user:create', 'directorate:institution-bootstrap']` |
| `TerminationsPage.vue:387` `canOverride` | `hasPermission(Permissions.Internship.Manage)` | `hasPermission(Permissions.Internship.ApprovalOverride)` |
| `useNavigation.ts:99` `Kullanıcılar` | `['user:view', 'user:create']` | `+ 'directorate:institution-bootstrap'` |
| `useNavigation.ts` Staj Yönetimi | `Fesihler` girdisi yok | Yeni girdi: başlık `Fesihler`, ikon `gavel`, rota `InternshipTerminations`, izinler `['internship:view', 'internship:manage', 'internship:approval:override']` |

`canOverride` **değiştirilir, genişletilmez**: `internship:manage`'i listede tutmak B'nin
ayrımını geri alırdı. Ölçüldüğü gibi kaybeden rol yoktur — `internship:manage` taşıyan iki
rol de override'ı ayrıca taşır.

`UserManagementPage` bootstrap dışındaki aksiyonları sayfa içi `PermissionGuard` ile
korumalıdır; yalnız `directorate:institution-bootstrap` taşıyan kullanıcı sayfayı açıp
diğer aksiyonlarda 403 görmemelidir. Planın ilk adımı bu guard'ların mevcut durumunu
ölçmek ve eksik olanı kapatmaktır.

## Karar 2 — "boş yetki" kilidi

`router/formRoutePermissions.spec.ts` idiyomu: router'ı **metin olarak** tarayan Vitest
(modülü import etmek sayfa bileşenlerini ve Pinia store'larını devreye sokardı).

**Değişmez:** müdürlük rollerine (`ProvincialAdmin`, `DistrictAdmin`) verilen her izin, en
az bir rota `meta.permissions` listesinde **ya da** bir menü girdisinde geçmelidir.

Rol→izin kaynağı ikinci bir kopya DEĞİLDİR: `PermissionMatrixDocTests`'in ürettiği ve
depoya kilitlenmiş matris dosyası okunur
(`src/Docs/docs/architecture/adr-0002-izin-agaci-ve-onek-secimi.md`). Backend o dosyayı
kilitlediği için TS tarafı türetilmiş gerçeği okur, yeniden yazmaz.

Kapsam bilerek **iki rolle sınırlıdır**. Genel bir "her izin bir ekrana bağlanmalı" kuralı
yanlış olurdu: saf API izinleri (`platform:tenant:manage` gibi bakım uçları) ekran
istemez.

## Karar 3 — drawer'ın okul-bağlamı kapısı

`NavVisibilityContext` ikinci bir sinyal kazanır:

```ts
export function resolveIsSchoolContext(nodeType: string | undefined): boolean {
  return nodeType === undefined || nodeType === 'School'
}
```

`institutionStore` **davranılan** kurumu tutar (aktif bağlam varsa o), dolayısıyla
`nodeType` doğrudan "şu an hangi tip kurum adına çalışıyorum" sorusunun cevabıdır.

`undefined` → `true` (göster). Gerekçe: store dolmadan önce okul kullanıcısının **tüm**
drawer'ı kaybolurdu. Mevcut `resolveIsUpperNode` ters yöne varsayılır (`false`), çünkü
orada geç belirmek yalnız tek bir girdiyi (`Kurumlar`) etkiler.

**Kapıya girenler** — müdürlük düzeyinde verisi `Tenant` kiracılıkta olduğu için boş döner:

- Kayıt & Öğrenci
- Staj Yönetimi
- Koordinasyon
- Belgeler & Raporlar

**Kapıya girmeyenler**, gerekçeleriyle:

| Girdi | Neden girmiyor |
|---|---|
| `Kurum Bilgileri` | Müdürlükte çalışıyor — üst düğüm sayfası alt kurum ağacını gösterir (`547c1d4`) |
| `Kurumlar` | Zaten `isUpperNode` kapısında |
| `Son İşlemler` | Müdürlükte **boş değil**: aktörün kendi işlemleri ev kiracısına damgalanır (B'de ölçüldü — bağlam değişimi satırı ev kiracısında görünür). Yalnız sayfa içindeki "kurum kapsamı" süzgeci boş döner |
| `İşletmeler` | **Ölçüldü:** `Business` belgesi `DocumentTenancyMap`'te `Shared` ve `GetBusinessesByStatusHandler`'da `RegisteredByInstitutionId` süzgeci **yok**. Liste müdürlük düzeyinde **dolu döner** — kapının gerekçesi (boş liste) burada geçerli değil |
| `Ana Sayfa` | D2'nin konusu — bu spec'te dokunulmaz |

Kapı bir **görünürlük** kararıdır, yetki kararı değil; yetki sunucudadır. Rota doğrudan
URL ile açılırsa sayfa yine açılır ve boş liste gösterir — bu kabul edilir, çünkü kapının
amacı bilgi taşımayan menü girdisini gizlemektir.

## Bilinen bedeller

1. **Gerçek müdürlük rollerinde Karar 3 bugün davranış değiştirmez.** Dört izinlik demetle
   o gruplar zaten görünmüyor. Kapı, geniş izinli aktörleri (bugün `admin`, ileride iki rolü
   birden taşıyan kullanıcılar) boş listelerden korur. Bilerek kabul edildi.
2. **Kilit yalnız iki rolü kapsar.** Başka bir rolün izni ekrana bağlanmadan kalırsa test
   görmez.
3. **Kilit "en az bir yerde geçiyor" der, "doğru yerde" demez.** İzin yanlış rotaya
   eklenirse test yeşil kalır. Daha güçlü bir değişmez (uç→rota eşlemesi) diller arası
   üretilmiş bir tablo isterdi; bu spec onu kapsam dışı bırakır.
4. **`isSchoolContext` store'a bağlıdır.** `institutionStore` yüklenemezse (ağ hatası)
   `nodeType` `undefined` kalır ve menü tam görünür — güvenli taraf, ama müdürlükte boş
   grupların görünmesi demektir.

## Sonraki sürüme bırakılan

**D2 — müdürlük panosu.** Ana Sayfa müdürlük düzeyinde: alt kurum ağacı (mevcut
`InstitutionChildrenTree` yeniden kullanılır), yöneticisi olmayan okullar (`Institution` +
`UserAccount`, ikisi de `Identity` — tek sorgu, kiracı dolaşımı yok), tıkanmış onaylar
(`InternshipSaga` = `Tenant` — `ITenantDirectory` ile okul okul dolaşan arka plan işi,
denormalize özet belge ve yeni bir backfill ön koşulu). Kullanıcı üç kartı da istedi;
üçüncüsü B'nin "il/ilçe geneli sayılar sonraki sürüm" notunu geri çağırır.

## Test planı

- `resolveIsSchoolContext` saf fonksiyonu kendi spec dosyasında: okul → `true`,
  `Province`/`District` → `false`, `undefined` → `true`
- Boş yetki kilidi: matris dosyasından okunan müdürlük izinlerinin her biri için rota ya da
  menü eşleşmesi
- Her kilit için "korunanı kaldır → kırmızıya döndüğünü kanıtla":
  - `internship:approval:override`'ı `InternshipTerminations` rotasından çıkar → kilit kırmızı
  - `resolveIsSchoolContext`'i sabit `true` yap → menü spec'i kırmızı
- `menuDefinition` gerçek tanım üzerinden test edilir (mevcut kural: yerel kopya kurulmaz)
- `pnpm test:run`, `pnpm type-check`, `pnpm lint` yeşil

## Dağıtım

Elle adım yok. Veri geçişi yok. Yeni uç yok.
