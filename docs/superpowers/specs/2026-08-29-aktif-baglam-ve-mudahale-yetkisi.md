# Aktif bağlam değiştirme ve müdahale yetkisi (B parçası)

**Tarih:** 29.08.2026
**Durum:** Tasarım onaylandı, uygulama planı yazılmadı
**Kapsam:** Yalnız B. A (kurum hiyerarşisi, PR #279) ve C (denetim izi, PR #280) bitti.

---

## Problem

A parçası il/ilçe düğümlerini ve yol tabanlı kapsamı getirdi, ama **yalnız `Institution` belgesi için**. O belge `DocumentTenancy.Identity` sınıfındadır ve kiracı damgası taşımaz — bu yüzden yol öneki onu süzebiliyor.

Okul verisi öyle değil. `StudentProfile`, `AttendanceRecord`, `TeacherProfile`, `PlacementView`, `AcademicPeriod` — hepsi `Tenant`. Conjoined kiracılık bu satırları kiracıya göre süzüyor ve il yetkilisinin kiracısı kendi düğümüdür. İl müdürlüğü düğümünün kiracısında öğrenci, sözleşme, devamsızlık **yoktur** — olamaz da.

Ölçüldü (C'nin dal geneli incelemesi): il düğümü kullanıcısı `GET /api/audit/institution` çağırdığında **hata değil boş liste** alır. Aynısı her modül için geçerlidir.

Yani il/ilçe yetkilisi bugün alt ağacındaki hiçbir okul verisini göremiyor. **Bağlam değiştirme bir kolaylık değil, o rolün tek çalışma modudur** — okuma bile ona bağlıdır.

---

## Kararlar

| Soru | Karar |
|---|---|
| Yazma kapsamı | Okuma + dört adlı müdahale (okulun tüm yazma yüzeyi AÇILMAZ) |
| Bağlam anahtarının kaynağı | Sunucuda kayıtlı aktif bağlam (`UserAccount`), istekten alınmaz |
| Bağlamsız hâl | İl geneli ağaç listesi + seçim (sayı yok) |
| Kalıcılık | Oturum boyunca; yeni girişte düşer |
| Oturum içi değişim | Serbest, sınırsız |

---

## 1. Aktif bağlam nerede yaşıyor

`UserAccount`'a iki alan:

```
ActiveInstitutionId      Guid?    aktif bağlam; null = kendi kurumu
ActiveContextSessionId   string?  bağlamı kuran token'ın oturum kimliği (sid)
```

Değiştirme tek uçtan: `POST /api/security/context` (`SetActiveInstitution(Guid? institutionId)`; `null` temizler). Komut `Commands/` altındadır, dolayısıyla **denetim izine kendiliğinden düşer** — C bunu kapsar, yeni bir kayıt yolu yazılmaz.

### Bağlam claim'e dönüşür, istekte taşınmaz

`PermissionClaimsTransformation` zaten kullanıcı başına 5 dakikalık önbellekle `institution_id`, `institution_path`, `branch_codes`, `business_id`, `student_id` üretiyor. `active_institution_id` oraya eklenir.

Bağlam değişimi `PermissionClaimsTransformation.InvalidateCache(cache, keycloakUserId)` çağırır — rol değişiminde kullanılan desenin aynısı. **Çağrılmazsa yeni bağlam beş dakika görünmez** ve kullanıcı hâlâ eski okulda çalıştığını sanır.

### Bu claim ADR-0003'ü ÇİĞNEMEZ — iki kural sayesinde

ADR-0003 token'dan **gelen** kapsam anahtarını yasaklar, çünkü Keycloak'ta *unmanaged* özniteliktir: realm politikası yanlış kurulursa kullanıcı `manage-account` ile kendi kapsamını kendi yazar. Token'ın imzalı olması içeriğin kullanıcı tarafından belirlenmediği anlamına gelmez.

Ürettiğimiz claim başka bir şeydir. `IClaimsTransformation` her istekte, kimlik doğrulamadan sonra, süreç içinde çalışır; gelen değeri siler, kayıttan yeniden üretir. Claim istemciye dönmez, hiçbir token'a imzalanmaz, Keycloak'a yazılmaz. Ömrü tek istektir. `institution_id` ve `institution_path` ile **aynı şekildir**.

İki kural bağlayıcıdır ve testle kilitlenir:

1. **Token'daki `active_institution_id` her istekte koşulsuz silinir** — kayıt boş olsa bile. "Kaynak yoksa token'a düş" davranışı, kaydı olmayan kullanıcıya kendi bağlamını seçtirirdi.
2. **Keycloak'a yazılmaz** — ne bağlam değiştirmede, ne `SyncUsersFromKeycloak`'ta. Oradaki bir kopya, ileride birinin onu yeniden otorite sanmasına davetiye çıkarır.

Kilitleyen test: `InstitutionClaimAuthorityTests` yeni claim'i kapsayacak şekilde genişletilir. Bu sınıf bir sapma #195'te fiilen ölçüldü (realm'e ulaşmayan ayar); varsaymak yetmez.

### Doğrulama İKİ yerde

- **Değiştirme anında:** hedef aktörün alt ağacında mı — `InstitutionScopePolicy.CanAccessByPath(actorPath, targetPath)`, hedefin yolu C'nin önbellekli `IInstitutionPathLookup`'ından. Değilse `DomainException` (422) ve iz satırı.
- **Her çözümlemede:** aynı kontrol tekrarlanır. Şart, çünkü ağaç değişebilir — okul başka ilçeye taşınabilir, kullanıcının kendi kurumu değişebilir. Yalnız yazma anında doğrulanan bir bağlam sessizce yetki taşımaya devam ederdi.

### Oturum bağı

Bağlam, onu kuran token'ın `sid` claim'iyle birlikte saklanır. Çözümlemede token'ın `sid`'i kayıttakinden farklıysa bağlam **bayat** sayılır ve kendi kurumuna düşülür.

- Oturum **içinde** bağlam serbestçe, sınırsız değişir; `sid` sabit kalır.
- Oturum**lar arası** taşınmaz: dünkü seçim bugünkü girişte geçerli değildir.
- Keycloak'ta `sid` SSO oturum kimliğidir ve token yenilenmesinde sabit kalır — uzun çalışma gününde seçim düşmez.
- Tarayıcı kapatılsa da çalışır; çıkış ucu çağrılmasına bağlı değildir.

`sid` yetki kararında **kullanılmaz**, yalnız bağlamı düşürmek için. En kötü hâlde yanlış karşılaştırır ve kullanıcı okulu yeniden seçer.

**Ölçüldü (29.08.2026):** `sid` kullanıcı access token'ında geliyor, yenilemede sabit kalıyor, yeni girişte değişiyor. Ayrıntı ve kararın gerçek sınırı için bkz. "Ölçüm sonuçları".

---

## 2. `institution_id` claim'ine DOKUNULMAZ

En ucuz yol `institution_id`'yi aktif bağlamla ezmek olurdu: kiracı çözümlemesi, `InstitutionScopeGuardMiddleware`, `UserContext`, bütün ön yüz kendiliğinden takip ederdi.

**Yapılmaz, çünkü C'yi öldürür.** Denetim satırı iki ayrı şeyi ayırt etmek zorundadır: *kim olduğun* (`ActorInstitutionId`) ve *nerede davrandığın* (`SubjectInstitutionId`). `institution_id` ezilirse ikisi eşitlenir, `CrossedTenantBoundary` **her zaman `false`** olur ve "il yetkilisi hangi okula dokundu" sorusu — B'nin izli verilmesinin tek sebebi — cevapsız kalır.

Bu yüzden:

- `institution_id` = **ev kurumu** (kullanıcının kaydındaki kurum)
- `active_institution_id` = **davranılan kurum**

### B'nin C'ye dokunduğu tek yer

`AuditEntryFactory.ResolveSubject` bugün hedefsiz komutta *aktörün kurumuna* düşüyor. Aktif bağlam varsa **ona** düşmelidir; yoksa okula yapılan yazma izde ile ait görünür ve `CrossedTenantBoundary` yanlış hesaplanır.

Değişiklik küçük ama taşıyıcıdır: `AuditContext` aktif bağlamı da taşır, `ResolveSubject` düşme sırası `komuttaki InstitutionId → aktif bağlam → ev kurumu` olur. C'nin testlerine bir vaka eklenir: **aktif bağlam altındaki yazma `CrossedTenantBoundary = true` üretir.**

---

## 3. Kiracı çözümlemesi

`TenantResolution.Resolve` aktif bağlamı tercih eder:

```
geçerli aktif bağlam varsa   → ForInstitution(activeInstitutionId)
yoksa                        → bugünkü davranış (ev kurumu, yoksa platform, yoksa null)
```

"Geçerli" = `sid` eşleşiyor **ve** hedef aktörün alt ağacında. İkisinden biri tutmazsa aktif bağlam **yok sayılır**; hata verilmez, ev kurumuna düşülür. Gerekçe: bayat bağlam bir yetki ihlali değil, bir zamanaşımıdır; kullanıcı okulu yeniden seçer.

---

## 4. Yazma yetkisi

**İzin "yapabilir misin" sorusunu cevaplar, aktif bağlam "nerede" sorusunu.** Dört müdahalenin üçü okul düzeyinde **zaten var olan** yeteneklerdir; il yetkilisinin farkı yeteneğinde değil, bunu dışarıdan yapmasındadır. Yeni izin icat etmek var olanı kopyalayıp ikinci bir gerçeklik üretmek olurdu.

### Mevcut izinlerle karşılanan üçü

| Müdahale | Bugünkü izin | Uç |
|---|---|---|
| Akademik dönem açma/kapatma | `institution:manage` | `POST /api/institutions/{id}/academic-periods` ve aynı grubun `{periodId}/close` ucu |
| Kurum künyesi düzeltme | `institution:manage` | `PATCH /api/institutions/{id}` |
| Tıkanmış onay zincirini açma | `internship:manage` | `POST /api/internships/{id}/approve/override` |

`ProvincialAdmin` ve `DistrictAdmin` rollerine `institution:manage` verilir. Bunun yan etkisi bilinçlidir: marka paleti ve ders programı yapılandırması da açılır (ikisi de `institution:manage` altındadır). İkisi de okul ayarıdır, denetlenir, ve ayrı bir izne bölmek var olan izin ağacını il yetkilisi için yeniden çizmek olurdu.

**Override ayrı bir izne bölünür.** `internship:manage` bugün override ile birlikte **müdür onay adımını** da açıyor (`POST /approve/director`). İl yetkilisinin onay zincirinde normal bir adım olması istenmez — istenen tıkanıklığı açmaktır. Bu yüzden override ucu kendi iznine geçer: `internship:approval:override`.

Geçişte kayıp olmaması için bugün `internship:manage` taşıyan **her rol** yeni izni de açıkça alır. Kilitleyen test: yeni izin öncesi override yapabilen rol kümesi ile sonrası aynı olmalı, artı iki yeni rol.

`internship:` öneki `InstitutionManager`'ın `internship:*` wildcard'ıyla yutulur — zararsızdır, o rol zaten override yapabiliyor.

### Gerçekten yeni olan tek yetenek: okula ilk yöneticiyi bağlama

Hiçbir okul rolü bir kullanıcıyı **başka** bir kuruma bağlayamaz; bugün yalnız `platform:tenant:manage` yapar (`UserInstitutionScopePolicy.CanAssign`: "aktör yalnız kendi kurumuna bağlar").

İl yetkilisine `user:roles:manage` (`Permissions.UserManagement.RolesManage`) vermek çözüm **değildir**: o, alt ağaçtaki her okulda her kullanıcının rollerini değiştirmek demektir — istenen şeyden kat kat geniş.

**Yeni izin:** `directorate:institution-bootstrap`

**Neden yeni bir önek (`directorate:`):** `institution:` önekli olsaydı `InstitutionManager`'ın `institution:*` wildcard'ı üzerinden **her okul müdürüne** geçerdi ve okul müdürü kullanıcıları başka okullara bağlayabilirdi — ADR-0002 önek tuzağının tam kendisi. `platform:` de kullanılamaz: o önek kurum üstü yetkiyi işaret eder ve il yetkilisine platform yetkisi vermek kapsamı bütün ülkeye açardı.

**Koşullu müdahale.** Bu izin tek başına yetmez; il yetkilisi bir hesabı okula ancak **o okulun hiç yöneticisi yokken** bağlayabilir. Müdahale kelimesinin karşılığı budur — tıkanıklık fiilen varken açmak. Okulun yöneticisi olduğu anda kapı kapanır ve yetki okula döner.

Koşul saf bir fonksiyonda durur (`InstitutionBootstrapPolicy`) ve makine tarafından kontrol edilebilir olduğu için testle kilitlenir: yöneticisi olan okulda `DomainException`, olmayan okulda geçer.

### `AssignablePermissionScope` AÇILMAZ

A parçası `ProvincialAdmin`/`DistrictAdmin` için atanabilir domain listesini bilerek boş bıraktı; yorumu "yazma denetim izi (C parçası) yazılmadan bu domainler tekrar açılmamalı" diyor. C yazıldı — **B yine de açmaz.**

O liste "bu rol başkasına hangi izinleri dağıtabilir" sorusudur. İl yetkilisinin izin dağıtması B'nin kapsamında değildir, ve açılırsa bir il yetkilisi kendi verdiği izinlerle kendi kapsamını genişletebilir.

`directorate:institution-bootstrap` ve `internship:approval:override` ayrıca `NeverDirectlyAssignable` kümesine girer.

---

## 5. Ön yüz

**Seçici** üst barda, Dönem/Yarıyıl seçicilerinin üstünde. Yalnız alt ağacı kendinden büyük olan kullanıcıya görünür; okul müdüründe hiç çıkmaz. Görünürlük kolaylıktır — karar sunucudadır.

**Bağlam değişimi TEK bir yerden geçer** ve kuruma bağlı bütün store'ları geçersiz kılar: `institutionStore`, `academicPeriodStore`, `entityOptionsStore`. Her sayfanın kendi kendine hatırlamasına bırakılmaz.

`authStore`'a tek bir `currentInstitutionId` computed'ı gelir (`activeInstitutionId ?? institutionId`); kuruma bağlı store'lar ona bağlanır.

### Sessiz yanlış-okula-yazma tuzağı

`academicPeriodStore` dönem listesini bir kez yükleyip `isLoaded` ile kilitliyor ve kurum kimliğini hatırlamıyor. Bağlam değişip dönem listesi yenilenmezse ekranda A okulunun dönemi seçili kalır ve B okuluna **A okulunun dönem kimliğiyle** yazılır. Sonuç hata değil, sessizce yanlış döneme düşmüş bir kayıttır.

`institutionStore` bu tuzağı öngörmüş ve `loadedInstitutionId` alanıyla kapatmış; yorumu "kiracı değişirse bayrak hâlâ true'dur, eski okulun adı ve alanları ekranda kalır" diyor. Aynı koruma `academicPeriodStore`'a eklenir.

**Bağlamsız hâl:** il geneli ağaç listesi (il → ilçe → okul; kurum kodu, ilçe) + seçim. Liste zaten ücretsizdir — `Institution` kiracı damgası taşımaz ve A parçası yol kapsamını ona getirdi.

**Bağlam açıkken** hangi okul adına davranıldığı üst barda tartışmasız görünür. İl yetkilisinin bütün zamanı bir bağlamın içinde geçtiği için bu ince bir gösterge olamaz.

---

## 6. Testler

**Saf birim testleri**
- Bağlam çözümlemesi: geçerli bağlam / bayat `sid` / alt ağaç dışı hedef / bağlam yok
- `InstitutionBootstrapPolicy`: yöneticisi olan okul reddedilir, olmayan geçer
- `TenantResolution`: aktif bağlam tercih edilir, geçersizse ev kurumuna düşülür

**Kilit testler — bu tasarımın varlık nedenleri**
- **Aktif bağlam altındaki yazma `CrossedTenantBoundary = true` üretir.** C'nin ayrımının B'de yaşadığını kilitler; `institution_id` ezilirse kırmızıya döner.
- **Bağlam değişimi dönem store'unu geçersiz kılar.** Sessiz yanlış-okula-yazma tuzağının kilidi.
- **Alt ağaç dışı bir bağlam reddedilir ve iz bırakır.**

**Sapma testleri**
- `InstitutionClaimAuthorityTests` `active_institution_id`'yi kapsar: token'dan silinir, Keycloak'a yazılmaz
- Yeni iki izin `NeverDirectlyAssignable`'da ve `AllDomains`'te değil
- `internship:approval:override` geçişinde rol kümesi daralmadı

**Ön yüz**
- Kapsam→uç ve bağlam→store geçersizleme sözleşmesi, sayfa ve testin **aynı** saf kaynağı okuduğu desenle (`auditListQuery.ts` / `institutionScope.ts` deseni)

---

## 7. Bilinen bedeller

Bunlar tasarım seçimlerinin **kalıcı** sonuçlarıdır — ertelenen kapsam değil.

0. **Bağlamın ömrü SSO oturumunun ömrüdür, sekmeninki değil.** Sekmeyi kapatıp uygulamayı yeniden açan kullanıcı, SSO oturumu canlıysa aynı bağlamda devam eder (ölçüldü: `sid` aynı kalır). "Yeni girişte düşer" yalnız gerçek çıkışta ya da oturum zaman aşımında geçerlidir.
1. **İki sekme aynı bağlamı paylaşır.** Bağlamı sunucuda saklamanın doğrudan sonucu: bir sekmede okul değiştirmek diğerini de değiştirir. Sekme başına bağlam istemci tarafı saklama isterdi; o da ADR-0003'ün kaçındığı şeydir. Kullanıcının iki okulu yan yana açması mümkün değildir.
2. **Bağlam değişimi izin önbelleğini geçersiz kılar.** Kullanıcının bütün claim'leri yeniden hesaplanır (izinler, alan kodları, yol). Sık bağlam değiştiren kullanıcıda bu ek bir veritabanı gidişidir; kabul edilir, çünkü alternatifi bayat kapsamla çalışmaktır.
3. **`institution:manage` müdahale sınırını aşar.** İl yetkilisi dönem ve künye için aldığı izinle marka paletini ve ders programı yapılandırmasını da değiştirebilir. Bilinçlidir: var olan izin ağacını tek rol için yeniden çizmemek adına kabul edildi. Üçü de denetlenir.
4. **Yetki reddi (403) hâlâ ize girmez** (C'den devralınır). Bağlam dışı bir okula erişim denemesi `DomainException` ürettiği için ize **girer**; izin katmanının kestiği istekler girmez.

---

## 8. Sonraki sürüme bırakılanlar

**İl/ilçe geneli sayılar.** Bağlamsız ekranda okul başına öğrenci, aktif sözleşme ve açık dönem sayıları gösterilecek. B'de yapılmıyor çünkü hangi sayının karar değiştirdiği henüz ölçülmedi ve üç uygulama yolunun üçü de bir kuralı çiğniyor:

- **Kiracı kiracı dolaşma:** hiçbir kuralı çiğnemez, her zaman taze; gecikme okul sayısıyla doğrusal büyür (bugün 3, gerçek bir ilde 50–200).
- **Denormalize özet okuma modeli:** okuma hızlı; okul verisi okulun kiracısı dışında yaşar, yeni tüketiciler ve resync ucu gerekir, sayaç beslemesi kaçarsa sessizce yanlış sayı gösterir.
- **Kiracılar arası tek ham SQL:** ucuz ve taze; şema izolasyonunu çiğner.

Doğal ilk adım kiracı kiracı dolaşmadır: kural çiğnemez ve gerçek ölçüm verisi toplandıktan sonra diğer ikisine geçmek için elde sayı olur.

Okul *listesi* B'de zaten vardır ve seçim ekranı onunla çalışır; eksik olan yalnız sayılardır.

---

## 9. Ölçüm sonuçları (29.08.2026, canlı Keycloak)

Planlamadan önce ölçüldü; **yedek yola ihtiyaç yok**.

| Ölçüm | Sonuç |
|---|---|
| Kullanıcı access token'ında `sid` | **Var** — mapper gerekmiyor, realm değişikliği gerekmiyor |
| `session_state` | Yok — yeni Keycloak'ta `sid` onun yerini aldı |
| Token yenilemede `sid` | **Sabit** — uzun çalışma gününde bağlam düşmez |
| Yeni girişte `sid` | **Değişiyor** — bağlam oturumlar arası taşınmaz |
| Servis hesabı token'ında `sid` | Yok (beklenen: kullanıcı oturumu yaratmaz) |

Realm dosyasına oturum claim eşleyicisi **eklenmeyecek**; #195'teki "realm'e ulaşmayan ayar" riski bu kararla hiç doğmuyor.

### Ölçümden çıkan nüans — kararın gerçek sınırı

Bağlamın ömrü **tarayıcı sekmesinin değil, Keycloak SSO oturumunun** ömrüdür.

Ölçümdeki "yeni giriş" bir parola akışıydı ve her seferinde yeni oturum açtı. Tarayıcıdaki PKCE akışı farklıdır: kullanıcı sekmeyi kapatıp uygulamayı yeniden açtığında SSO oturumu hâlâ canlıysa aynı `sid` döner ve **bağlam taşınır**.

Yani "çıkışta sıfırlanır" kararı şu koşulla geçerlidir: gerçek çıkış yapıldığında ya da SSO oturumu zaman aşımına uğradığında. Sekmeyi kapatmak bağlamı düşürmez. Bu, sunucuda saklama kararının doğal sonucudur ve arayüzde hangi okulda olunduğunun tartışmasız görünmesi gereğini güçlendirir.

## Sıra

B, C'nin (PR #280) üstüne yığılır; C de A'nın (PR #279) üstünde. Birleşme sırası: #278 → #279 → #280 → B.
