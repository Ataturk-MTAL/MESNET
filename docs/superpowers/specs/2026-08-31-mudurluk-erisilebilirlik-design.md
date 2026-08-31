# Müdahale yetkilerinin erişilebilirliği ve drawer'ın bağlam kapısı (D1)

**Tarih:** 31.08.2026
**Durum:** onaylandı, plana hazır
**Yerini aldığı:** `2026-08-30-mudurluk-baglami-menu-ve-erisilebilirlik-design.md` — o spec'in
Karar 1'i **geçersizdi** (aşağıda), bu spec onun yerine geçer.
**Önceki:** D2 (#285), kapsam güvenlik düzeltmesi (#286)

## Problem

`ProvincialAdmin` ve `DistrictAdmin` demetleri **tam olarak dört izin** taşır, wildcard yok
(`RolePermissionMap.cs`):

```
institution:view   institution:manage
internship:approval:override   directorate:institution-bootstrap
```

Son ikisi B parçasında (#281) tanımlandı ve sunucuda **doğru korunuyor**. Ama ön yüzden
**hiçbiri kullanılamıyor** — üç ayrı katman kapalı:

| Yetki | Sunucu | Ön yüz engeli |
|---|---|---|
| `internship:approval:override` | `InternshipEndpoints.cs:34` | Rota `meta: ['internship:view','internship:manage']` (`router/index.ts:170`) |
| aynı | aynı | Buton `hasPermission(Permissions.Internship.Manage)` — **override'a bakmıyor** (`TerminationsPage.vue:387`) |
| aynı | aynı | Menüde `Fesihler` girdisi **hiç yok** (`useNavigation.ts`) |
| `directorate:institution-bootstrap` | `PermissionPolicies.cs:50` — `AnyOf(UserManagement.RolesManage, Directorate.InstitutionBootstrap)` | Rota `meta: ['user:view','user:create']` (`router/index.ts:370`) |
| aynı | aynı | Menü girdisi `['user:view','user:create']` (`useNavigation.ts`) |

Yani B parçası **yazma yetkisini** verdi ama **nesneye ulaşma yolunu** hiç sormadı. Sunucuda
korunan iki yetenek ön yüzde erişilemez durumda.

### Neden hiçbir test görmedi

`DirectoratePermissionMappingTests` rol→izin eşlemesini kilitliyor, `PermissionMatrixDocTests`
matrisi kilitliyor. Hiçbiri **izinden ekrana giden yolu** kontrol etmiyor: rota
`meta.permissions` listeleri sunucudaki politikayla **elle** eşleniyor ve aralarında hiçbir
kilit yok.

Aynı sınıftan bir hata daha önce kapatılmıştı (`fdf6795`, form rotaları). O sefer rota **fazla
gevşekti** ve belirti 403 duvarıydı — **görülür**. Bu sefer **fazla dar** ve belirti menüde hiç
görünmemek — **görünmez**.

---

## Karar 1 — Okuma izinleri verilir, ama fesih yolunun anlamı AKTİF BAĞLAMDADIR

Önceki spec yalnız rota `meta.permissions` listelerini genişletmeyi öneriyordu. **Bu yetmez:**
sayfaların OKUMA uçları müdürlük rollerinin taşımadığı izinleri ister —
`GET /api/internships` `AnyOf(internship:view, internship:view-own)` (`PermissionPolicies.cs:30`),
`GET /api/security/users` `user:view`. Rota açılsa sayfa 403 ile boş gelirdi.

Bu yüzden iki okuma izni verilir:

```csharp
Permissions.Internship.View,      // fesih listesini okuyabilmek için
Permissions.UserManagement.View,  // bootstrap ekranını okuyabilmek için
```

### `user:view` artık güvenli — ön koşul karşılandı

Bu izin **önce verilemezdi**: `UserAccount` `Identity` sınıfındadır, conjoined kiracılık onu
süzmez ve liste aktörden türeyen hiçbir daraltma yapmıyordu — `user:view` **ülke geneli okuma**
demekti. #286 o açığı kapattı: liste artık `InstitutionScopePolicy.VisibleScope` ile aktörün
**alt ağacına** daralıyor. Müdürlük kendi bağlamında alt ağacındaki kullanıcıları görür; bu
tam olarak bootstrap'ın ihtiyacı.

### `internship:view`'in değeri KENDİ bağlamında doğmaz — ölçüldü

`InternshipSummary` `DocumentTenancyMap`'te **`Tenant`**'tır (`:90`). Müdürlük düğümü kiracı
değildir, dolayısıyla müdürlük **kendi bağlamındayken** fesih listesi **boştur** — hata değil,
sessiz boş liste.

**Bu bir kusur değil, tasarımın kendisidir.** Müdürlüğün fesihe müdahale yolu B parçasının
aktif bağlamıdır (#281):

1. Müdürlük panosunda tıkanmış onayı görür (D2, `stuck-approvals` — o sorgu
   `TenantIsOneOf` ile kiracılar arası çalışır, bu yüzden **kendi bağlamında** doludur)
2. İlgili okulun bağlamına geçer → `TenantResolution` aktif bağlamı **en önde** alır, kiracı
   o okul olur
3. `Fesihler` sayfası artık dolu; `internship:approval:override` butonu görünür

`internship:view` bu zincirin **ikinci adımını** açar. Onsuz, bağlama geçse bile liste ucu 403
döner.

**Spec'e yazılıyor ki ileride biri "müdürlük fesih listesini boş görüyor, bug" diye
bildirmesin.** Beklenen davranış budur.

---

## Karar 2 — Rota ve buton izinleri sunucudaki politikayla hizalanır

| Yer | Şimdi | Olacak |
|---|---|---|
| `router/index.ts:170` (`InternshipTerminations`) | `['internship:view','internship:manage']` | `['internship:view','internship:manage','internship:approval:override']` |
| `TerminationsPage.vue:387` (`canOverride`) | `hasPermission(Internship.Manage)` | `hasPermission(Internship.ApprovalOverride)` |
| `router/index.ts:370` (`UserManagement`) | `['user:view','user:create']` | `['user:view','user:create','directorate:institution-bootstrap']` |
| `useNavigation.ts` `Kullanıcılar` girdisi | `['user:view','user:create']` | aynı üçlü |

`Permissions.Internship.ApprovalOverride` ve `Permissions.Directorate.InstitutionBootstrap`
sabitleri **ön yüzde yok**; `src/WebUI/src/utils/permissions.ts` dosyasına eklenir.

**`canOverride` düzeltmesi ters yönde kimseyi kapatmaz — ölçüldü.** `internship:manage` taşıyıp
`internship:approval:override` taşımayan rol yoktur: `InstitutionManager` `internship:*` ile
ikisini de alır, `DeputyDirector` ikisini de açıkça taşır. Yani değişiklik yalnız müdürlüğe
butonu açar, kimseden almaz.

---

## Karar 3 — Menüye `Fesihler` girdisi eklenir

`Staj Yönetimi` grubunun altına, izin listesi rota `meta` ile **birebir aynı** üçlü.

Menüde görünüp rotada 403 yiyen ya da rotaya girip menüde görünmeyen girdi bırakılmaz — bu
zaten Karar 4'ün kilitlediği şey.

---

## Karar 4 — Rota izinleri ile sunucu politikası arasına KİLİT konur

Bugün eşleme elle yapılıyor ve aralarında hiçbir bağ yok. Bu spec'in düzelttiği hata tam olarak
o eşlemenin sessizce ayrışmasıydı.

Yeni test `src/WebUI/src/router/routePermissionAlignment.spec.ts` — depoda kanıtlanmış kaynak
tarama idiomu (`formRoutePermissions.spec.ts`) ile aynı: `index.ts`'i **metin olarak** okur
(modülü import etmek sayfa bileşenlerini ve Pinia store'larını çeker).

Kilit şunu dayatır: **bir yeteneğin sunucu politikası birden çok izni kabul ediyorsa
(`AnyOf`), o yeteneğe ulaşan rotanın `meta.permissions` listesi o izinlerin hepsini
içermelidir.** Aksi hâlde sunucunun kabul ettiği bir aktör ön yüzden hiç ulaşamaz.

Kilit tablosu testin içinde açık yazılır ve şu üç satırla başlar:

| Rota | Zorunlu izinler |
|---|---|
| `InternshipTerminations` | `internship:view`, `internship:manage`, `internship:approval:override` |
| `UserManagement` | `user:view`, `user:create`, `directorate:institution-bootstrap` |
| `PermissionScope` | `user:roles:manage` |

**Neden tam otomatik türetme değil:** rota adı ile sunucu politikası arasında makine tarafından
izlenebilir bir bağ yok (biri TypeScript rotası, öteki C# politikası). Elle yazılmış ama
**kilitli** bir tablo, hiç olmayan bir kilitten iyidir; tablo değişmeden rota değişirse test
kırmızı olur.

---

## Karar 5 — Drawer'ın okul-bağlamı kapısı

Müdürlük kendi bağlamındayken okul işine ait menü grupları gösterilmez. Ölçüm: bugün o gruplar
**zaten izinle gizli** (`student:view`, `company:view` vb. müdürlük demetinde yok), yani
kullanıcının bildirdiği belirti `admin` hesabından geliyordu — o hesap `SystemAdmin` +
`InstitutionManager` taşıyor.

**Yine de kapı konur**, çünkü Karar 1 iki okuma izni ekliyor ve `internship:view` `Staj
Yönetimi` grubunun diğer girdilerini de açar (`Stajlar`, `Yerleştirme` vb.) — müdürlük kendi
bağlamında onları boş görürdü.

Mekanizma **zaten var**: `NavItem.visibleWhen(ctx)` ve `resolveIsUpperNode`. Ama bu kararın
sorusu farklıdır — "aktör üst düğüm mü" değil, **"şu an müdürlük olarak mı davranıyorum"**.
D2'de tam bu ayrım için `isActingAsDirectorate(nodeType)` yazıldı ve
`directorateContext.spec.ts` ile kilitlendi. `NavVisibilityContext` o alanla genişletilir.

Kapatılacak girdiler: `Staj Yönetimi` grubunun `Fesihler` DIŞINDAKİ çocukları. `Fesihler`
görünür kalır — müdürlük oraya bağlam değiştirdikten sonra gidecek ve menü onun giriş kapısıdır.

**Kurum Yönetimi grubu kapatılmaz:** `Kurumlar`, `Kurum Bilgileri`, `Kullanıcılar`,
`Son İşlemler` müdürlüğün kendi işidir.

---

## Kapsam DIŞI

- D2'nin Task 10-12'si (pano bileşeni, ulusal parametre ekranı, dağıtım belgesi). Bu spec
  onların ön koşulunu karşılar; kendileri ayrı iştir.
- `role-integrity` kapsam kararı (#283) ve davet yazma uçları (#284).
- Müdürlüğün fesih listesini **kendi bağlamında** dolu görmesi. Karar 1'de gerekçelendirildi:
  yol aktif bağlamdır, kiracılar arası ikinci bir liste ucu açılmaz.

## Dağıtım ön koşulu

**Yok.** Yeni belge, alan ya da backfill yok. Rol→izin haritası değişikliği çalışma zamanında
etkilidir; Keycloak realm'ine dokunulmaz (izinler realm'de değil `RolePermissionMap`'te yaşar).
