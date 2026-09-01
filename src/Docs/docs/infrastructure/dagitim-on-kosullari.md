---
title: Dağıtım Ön Koşulları
sidebar_label: Dağıtım Ön Koşulları
---

# Dağıtım Ön Koşulları

Bazı değişiklikler dağıtımdan sonra **elle bir adım** gerektirir. Bu adımlar atlanınca sistem
**hata vermez** — özellik sessizce çalışmaz. Belirti hep aynıdır: liste boş gelir, sayı sıfır
çıkar, buton hiçbir şey yapmaz.

Bu sayfa o adımların tamamını tek yerde tutar.

## Ne zaman gerekir?

| Değişiklik | Gereken adım |
| --- | --- |
| Yeni **read-model** (denormalize görünüm) eklendi | İlgili resync ucu çağrılır |
| Var olan read-model'e **yeni alan** eklendi | İlgili resync ucu çağrılır |
| Realm'e yeni **rol / politika / client** eklendi | Realm doğrulaması (aşağıya bakınız) |
| Yeni **unique index** eklendi | Mevcut kopyalar **önce** temizlenir (aşağıya bakınız) |
| Projeksiyonun **kimliği** değişti | Görünüm yeniden inşa edilir (aşağıya bakınız) |
| Devamsızlık **sayma / eşik** kuralı değişti | Geçmiş tetiklemeler **elden** denetlenir — otomatik düzeltme yoktur (aşağıya bakınız) |

Ortak kök neden: olay-beslemeli read-model'ler yalnız **bundan sonra** gelen olayları yazar.
Seeder idempotent olduğu için kaynağı yeniden yaratmaz, yani olay bir daha yayınlanmaz ve mevcut
kayıtlar için görünüm hiç dolmaz.

## Atlanan adım artık açılışta bildiriliyor

Bu sayfadaki adımların bir kısmı atlandığında **belirtisi ölçülebilir**. Açılışta
`DeploymentPrerequisiteVerificationHostedService` o belirtileri okur:

- Bulgu **`LogCritical`** olarak yazılır: ölçüm + sonuç + birebir çağrılabilir adım, tek blokta
- **Hiçbir ortamda açılış durmaz** — gerekçesi aşağıda
- **Ölçüm yapılamaması bulgu sayılmaz** — ilk açılışta tablo henüz yoksa kontrol atlanır ve
  uyarı yazılır

:::note Neden Realm doğrulaması gibi Development'ta durdurmuyor
`RealmVerificationHostedService` ve `DocumentTenancyVerificationHostedService` Development'ta
**açılışı durdurur**, çünkü onların çaresi süreç dışındadır (Keycloak ayarı, kaynak kodda
sınıflandırma). Buradaki çare ise **bu API'nin kendi ucudur**. Açılış dursaydı uç ulaşılamaz
olur ve sistem **kendi çaresine erişemeyen** bir kilitlenmeye girerdi — üstelik her yeni
kurulumda, çünkü boş bir veritabanında bu bulgular tanımı gereği vardır. Koruma değil tuzak
olurdu. Kilitleyen test: `DeploymentPrerequisiteVerificationTests.Bulgu_varken_bile_acilis_DURMAZ`.
:::

:::warning Ölçer, koşturmaz
Doğrulayıcı hiçbir resync ucunu **çağırmaz** ve hiçbir şey **yazmaz**. Açılıştan koşturmak bu
depoda mümkün değildir: `UseWolverine` host'tan **sonra** başlar, açılıştan yapılan her yayın
`WolverineHasNotStartedException` fırlatır. Ayrıca iki uç idempotent değildir (#290, #291) ve her
yeniden başlatmada sayacı biraz daha bozardı. Koşturma sırası ve kimliği **operatördedir**.
:::

### Neyin ölçüldüğü — ve neyin ölçülmediği

| Ön koşul | Ölçülen belirti | Adım |
| --- | --- | --- |
| Kurum ağacı | Okul var, ama `Path` alanı boş olan okul var | `POST /api/institutions/rebuild-hierarchy` |
| Yönetici bağı görünümü | Okul var, `InstitutionManagerLink` görünümünde **0 satır** | `POST /api/security/users/replay` |
| Staj saga'sı kopyaları | Saga sayısı, işaret ettiği tekil yerleştirme sayısından fazla | `POST /api/internships/resync-sagas` → `POST /api/contracts/resync-internship-links` |

Kalan adımların belirtisi **tek modül içinden ölçülemez** (ör. "öğrenci var ama `StudentNameView`
boş" iki modülü birden görmeyi gerektirir; şema izolasyonu buna izin vermez). Onlar bu listede
**yoktur** ve doğrulayıcının sessizliği onlar için bir şey söylemez. Kapsam her açılışta loglanır:

```
Dağıtım ön koşulları: 3/3 ölçüldü, 0 eksik bulundu.
```

Yeni bir sonda eklemek tek dosyadır: `IDeploymentPrerequisiteProbe` uygulayın ve modülün
`ServiceRegistration`'ında kaydedin. Sonda **yalnız okur**.

## Şema göçü betikle YAPILMAZ — elden uygulanır

`scripts/migrate.sh` ve `scripts/migrate.ps1` **silindi** (#293). İkisi de
`dotnet run -- marten-apply` çağırıyordu; depoda böyle bir komut ana bilgisayarı **yok**
(`RunJasperFxCommands` / `AddJasperFx` araması: 0 sonuç, Oakton/JasperFx paket referansı: 0).
Argüman yutuluyor, API normal açılıyor ve betik **0 ile çıkıyordu** — yani "göç uygulandı"
diyen, hiçbir şey yapmamış bir betik.

Yerine konmadı, çünkü doğru yol zaten elden uygulamaktır:

```bash
psql "$CONNECTION" -f src/Docs/docs/infrastructure/sql/149-conjoined-kiracilik.sql
psql "$CONNECTION" -f src/Docs/docs/infrastructure/sql/149-kiraci-damgalama.sql
```

`ApplyAllDatabaseChangesOnStartup()` bu depoda kullanılmaz: Marten'ın kendisiyle çelişen deltası
(`42710: constraint "fkey_mt_events_stream_id_tenant_id" ... already exists`) API'yi öldürür.
Aynı delta `marten-apply` üzerinden de gelirdi — bir komut ana bilgisayarı eklemek, bilinen bozuk
yolu diriltmek olurdu. **Gözden geçirilebilir SQL** bilinçli bir karardır; otomatik uygulayan bir
sarmalayıcı o gözden geçirmeyi ortadan kaldırır.

## Koşturma — `scripts/deploy-prereqs.sh`

Adımları **sırasıyla** koşturan betik. Sıra betiğin içinde gerekçesiyle yazılıdır.

```bash
export MESNET_API_URL=https://mesnet.example.gov.tr
export MESNET_KEYCLOAK_TOKEN_URL=https://kc.example.gov.tr/realms/mesnet/protocol/openid-connect/token
export MESNET_OPERATOR_USER=<platform:tenant:manage taşıyan gerçek kullanıcı>

./scripts/deploy-prereqs.sh --dry-run     # önce planı görün
./scripts/deploy-prereqs.sh               # sonra koşturun (parola terminalden sorulur)
```

**Kimlik: adlandırılmış operatör hesabı** — kalıcı bir `DeploymentOperator` servis hesabı
**değil**. Servis hesabı, yılda beş kez kullanılmak için 365 gün boyunca bütün okulların verisine
yazma yetkisi taşıyan kalıcı bir anahtar olurdu. Parola betikte saklanmaz; çalışma anında ortam
değişkeninden ya da terminalden alınır ve süreçle birlikte ölür. Denetim kaydı gerçek bir kişinin
`sub`'unu taşır.

**Varsayılan hedef yoktur.** URL de kimlik de tahmin edilmez; yanlış hedefe sessizce koşan bir
dağıtım betiği, koşmayan betikten kötüdür. Geliştirme için `--dev`.

### Betik üç sınıf tanır

| Sınıf | Anlamı | Varsayılan |
| --- | --- | --- |
| `safe` | İdempotent, serbestçe yeniden koşturulur | Koşar |
| `once` | Gerekli ama idempotent **değil** — tam bir kez | **Atlanır**; `--allow-once` + damga dosyası |
| `broken` | Bilinen hatalı, veri bozar | **Atlanır**; `--include-broken` |

Bugün `once`: `students/resync-projections` (#290 — şube öğrenci sayacını her koşuda şişirir).
Bugün `broken`: **yok**.

`placements/resync-projections` **onarıldı (#291)** ve artık `safe`: uç yaşam döngüsü olayını
(`StudentPlaced`) değil, onarım olayını (`PlacementSnapshotResynced`) yayınlıyor. Ayrıntı aşağıda.

### Onarım olayı yayınlanır, yaşam döngüsü olayı değil (#291)

`POST /api/placements/resync-projections` eskiden `StudentPlaced`'i yeniden yayınlıyordu. O olay
`InternshipSaga`'nın **başlatıcı** olayıdır; deterministik saga kimliği (#251) yüzünden ikinci
yayın **tekil kısıt ihlali** üretiyor, o kuyruk ölü mektuba düşüyor ve
`MultipleHandlerBehavior.Separated` yüzünden kardeş kuyruklar commit etmeye devam ediyordu.
Sonuç: **uç 200 döner**, saga yazılmaz, kapasite bozulur, hiçbir yerde hata görünmez.

Ayrıca `Business.StudentPlacedConsumer` kapasiteyi `CountAsync() + 1` ile yazıyordu; onarım
yolunda satır zaten sayıldığı için **her koşuda kapasite bir artıyordu**. Artık küme kullanılıyor
(`Coordination.StudentPlacedConsumer` ile aynı desen) — canlı yolda sonuç değişmez.

Kilitleyen testler: `ResyncEventDriftTests` (onarım handler'ı başlatıcı olay yayınlayamaz;
onarım olayını saga tüketemez).

### 200 dönmek "yapıldı" demek değildir

Betik, yanıtı **doğrulayabildiği** fazlarda doğrular ve doğrulayamadığını `DOĞRULANMADI` diye
yazar — "TAMAM" demez. Gerekçe #292'de ölçüldü: uç 200 döndü ve **sıfır** satır işledi. Doğrulama
tutmayan faz `ŞÜPHELİ` sayılır ve betik sıfırdan farklı kodla çıkar.

## Realm ayarları — artık otomatik yakalanıyor

Keycloak realm import **tek seferliktir**: `mesnet-realm.json`'a sonradan eklenen rol, politika
ya da client ayarı **mevcut bir kaba hiç ulaşmaz**.

Bu artık elle takip edilmez. Açılışta `RealmVerificationHostedService` çalışan realm'i
`RealmInvariants` ile karşılaştırır:

- **Development:** sapma varsa **açılış durur**, düzeltme yolu hata mesajındadır
- **Diğer ortamlar:** `LogCritical`
- Keycloak'a ulaşılamaması sapma sayılmaz — kontrol atlanır, açılış durmaz

Gerçek bir örnekte depoda 11 rol tanımlıyken çalışan realm'de yalnız 6'sı vardı; eksik beşi
farklı sürümlerde eklenip her seferinde unutulmuştu.

### Rolün var olması yetmez — atandığını da denetleyin

İlk sürüm yalnız rolün realm'de **var olduğuna** bakıyordu. Bu, sorunun bir katman üstüydü:
rol listesi tamamlandıktan sonra bile **kullanıcı→rol ataması** eksik kalabiliyor.

Gerçek örnek (#205): 11 rolün tamamı yerindeydi, rol denetimi temiz geçiyordu; ama `admin`
kullanıcısında yalnız `InstitutionManager` atanmıştı. `SystemAdmin` eksik olduğu için
`platform:parameter:manage` hiç gelmedi, `PUT /api/payments/config/minimum-wage` 403 döndü ve
asgari ücret o ortamda hiç girilemedi.

Doğrulama artık ikisini de kapsıyor. Beklenen atamalar
`RealmInvariants.ExpectedSeedUserRoles`'ta durur ve bir testle `mesnet-realm.json`'a bağlıdır —
realm dosyasına eklenen bir atama sabite yansımazsa denetim onu **hiç aramaz**.

**Bulunmayan kullanıcı sapma değildir.** `admin`, `teacher1` gibi kullanıcılar yalnız geliştirme
realm'inin tohum verisidir; gerçek kurulumda hiçbiri bulunmaz. Denetlenen tek şey, var olan
kullanıcının eksik rolüdür.

### Rolü Keycloak'ta düzeltmek YETMEZ — üç katman var

Bu ölçülerek bulundu: `admin` kullanıcısına Keycloak'ta `SystemAdmin` atandıktan sonra bile
`PUT /api/payments/config/minimum-wage` **403 dönmeye devam etti**. Token doğruydu
(`realm_access.roles` içinde `SystemAdmin` vardı) ama API onu hiç kullanmıyordu.

Yetki üç katmandan geçer ve **her biri ayrı ayrı dönmelidir**:

| # | Katman | Nasıl güncellenir | Gecikme |
| --- | --- | --- | --- |
| 1 | Keycloak rol ataması | `POST /admin/realms/{realm}/users/{id}/role-mappings/realm` | anında |
| 2 | `UserAccount.Roles` kaydı | `POST /api/security/users/sync` | anında |
| 3 | İzin önbelleği (`user-permissions:{sub}`) | kendiliğinden düşer | **5 dakika** |

İkinci katman kritiktir: `PermissionClaimsTransformation` **kayıt varsa token'daki rollere hiç
bakmaz** — kayıt otoriterdir (`BranchCodes` ile aynı ilke). Yani Keycloak'ta yapılan rol
değişikliği senkronizasyon çağrılmadan sisteme hiç ulaşmaz.

Üçüncü katman ölçüldü: kayıt düzeltildikten sonra uç **3 dakika daha 403 döndü**, dördüncü
denemede 200'e geçti. Düzeltmenin işe yaramadığı sanılıp geri alınmasın.

### İkinci katman artık kendini bildiriyor (#208)

Kayıt Keycloak'tan sapmışsa sistem **uyarı basar** — sessiz 403 dönemi kapandı:

```
UserAccount kaydı Keycloak'tan sapmış — kullanıcı 'admin', kayıtta eksik rol: SystemAdmin.
Kayıt otoriter olduğu için bu roller izin ÜRETMEZ ve ilgili uçlar 403 döner.
Düzeltme: POST /api/security/users/sync (izin önbelleği nedeniyle etkisi 5 dakikaya kadar gecikebilir).
```

Kontrol, kullanıcı sisteme geldiğinde çalışır (açılışta değil — gerçek bir kurulumda binlerce
kullanıcı olabilir) ve kullanıcı başına en fazla **5 dakikada bir** konuşur; izin önbelleği
tazelenirken kontrol edilir.

**Uyarı erişimi değiştirmez.** Token'daki rolü kullanmak, kaydın otoriterliğini sessizce iptal
ederdi; düzeltme kararı yöneticinindir.

Yanlış alarm üretmemesi için iki koşul aranır:

- Token, kayıttan **sonra** üretilmiş olmalı — yoksa uygulamadan yapılan her rol kaldırma
  işlemi token ömrü boyunca (dev realm'inde 1800 sn) alarm çalardı
- Rol, projenin tanıdığı bir MESNET rolü olmalı — Keycloak'ın teknik rolleri
  (`offline_access`, `uma_authorization`, `default-roles-*`) `UserAccount`'a hiç yazılmaz

> **Üçüncü katman hâlâ elle.** Senkronizasyon izin önbelleğini temizlemiyor (#209); düzeltmenin
> etkisi 5 dakikaya kadar gecikebilir. "İşe yaramadı" sanıp geri almayın.

## Yeni unique index — kopyalar önce temizlenir (#237)

`StudentProfile`'a doğal anahtar kısıtı eklendi:
**`(AcademicPeriodId, StudentNumber)`**, kısmi (numarası olanlar) ve **kiracı başına**.

:::danger Kopya varsa index OLUŞMAZ

PostgreSQL, mevcut satırlar kısıtı ihlal ediyorsa unique index'i **yaratmaz** — hata verir.
Development'ta `AutoCreate.All` bunu açılışta dener; üretimde göç elden yapılır (bkz.
`ApplyAllDatabaseChangesOnStartup` neden kullanılmıyor).

Bu tam da beklenen durum: kısıt **zaten kopya olduğu için** eklendi. #204'te ölçülmüştü —
**122 öğrenci → 774 kayıt**, bazıları 24 kopya.
:::

**Önce sayın:**

```sql
SELECT tenant_id,
       data ->> 'academicPeriodId' AS donem,
       data ->> 'studentNumber'    AS numara,
       count(*)                    AS kopya
FROM   enrollment.mt_doc_studentprofile
WHERE  data ->> 'studentNumber' IS NOT NULL
GROUP  BY 1, 2, 3
HAVING count(*) > 1
ORDER  BY kopya DESC;
```

Sıfır dönerse index sorunsuz oluşur. Dönmezse temizlik **elle** yapılır ve karar veri sahibinindir:
hangi kopyanın kalacağı (en eskisi mi, en çok ilişkisi olan mı) otomatik verilemez — kopyaların
sözleşmesi, devamsızlığı ve maaş dönemi de doğmuş olabilir.

> **Silmeden önce bağlı kayıtları sayın.** Kopya öğrenciye bağlı sözleşme/devamsızlık/ödeme
> varsa silme, o kayıtları öksüz bırakır. Bağı olan kopya tutulur, olmayan silinir.

**Sonra index'i uygulayın** (üretimde; development'ta Marten kendisi yaratır):

```sql
CREATE UNIQUE INDEX CONCURRENTLY idx_studentprofile_period_number
ON enrollment.mt_doc_studentprofile (
       tenant_id,
       (data ->> 'academicPeriodId'),
       (data ->> 'studentNumber'))
WHERE data ->> 'studentNumber' IS NOT NULL;
```

`tenant_id` index'te **bulunmak zorunda**: öğrenci numarası okul içinde benzersizdir, iki okulun
aynı numarayı kullanması normaldir. Marten tarafında bunun karşılığı
`x.TenancyScope = TenancyScope.PerTenant`'tır ve **varsayılan `Global`'dir** — yazılmazsa kısıt
okullar arası uygulanır ve ikinci okul kendi `1101`'ini kaydedemez.

---

## Devamsızlık sayacı yeniden inşası (#242)

`AttendanceView` projeksiyonunun **kimliği değişti**: eskiden `StudentId` (öğrenci başına tek
satır), şimdi `{studentId}:{academicPeriodId}`. Görünüme `AcademicPeriodId` alanı eklendi.

:::danger Mevcut satırlar yanlış kimlikte — yeniden inşa ZORUNLU

Eski satırlar `Guid` kimlikli ve dönem bilgisiz. Yeniden inşa edilmezse sayaç eski satırları
**hiç bulamaz**, her öğrenci sıfırdan başlar ve devamsızlık limiti (fesih tetikleyicisi) bir
dönem boyunca **hiç dolmaz**. Belirti yok: hata çıkmaz, log temiz kalır.
:::

Projeksiyon `Async` yaşam döngüsünde (`ProjectionLifecycle.Async`) ve `MultiStreamProjection`
olduğu için Marten async daemon'ı ile yeniden kurulabilir:

```bash
# Uygulama dururken ya da daemon devredeyken tek seferlik
dotnet run --project src/MESNET.Presentation -- projections rebuild --projection AttendanceViewProjection
```

> Marten'ın komut satırı entegrasyonu kuruluysa yukarıdaki komut yeterlidir. Değilse eski tablo
> boşaltılıp daemon'ın akışı baştan işlemesi sağlanır:
>
> ```sql
> TRUNCATE attendance.mt_doc_attendanceview;
> DELETE FROM shared.mt_event_progression WHERE name = 'AttendanceViewProjection';
> ```
>
> Olay akışı kaynaktır; görünüm ondan yeniden üretilir, veri kaybı olmaz.

**Doğrulama** — yeniden inşa sonrası her satırın kimliği `:` içermeli ve dönem dolu olmalı:

```sql
SELECT count(*) FILTER (WHERE id NOT LIKE '%:%')      AS eski_kimlik,
       count(*) FILTER (WHERE data ->> 'academicPeriodId' IS NULL) AS donemsiz
FROM   attendance.mt_doc_attendanceview;
```

İkisi de **0** olmalı.

---

## Devamsızlık sayacı: onay bekleyen kayıtlar sayılmaz (#252)

Fesih sayacı artık **onay bekleyen** (`Pending`) devamsızlık kayıtlarını saymıyor
(`AttendanceCounterScope.CountsTowardLimit`). Dışlama listesi bilerek dardır: yalnız `Pending`
dışlanır; `Recorded`, `Verified`, `Corrected` ve **tanınmayan** durum sayılır — eksik veri
sınırı gevşetmemelidir.

Buna karşılık sınır artık **üç** olayda yeniden ölçülür: `AttendanceMarked`,
`AttendanceApproved`, `AttendanceCorrected`. Yani `Pending` kayıt onaylandığında sayaca o anda
girer.

:::danger Atlanırsa ne olur
Düzeltmeden önce işletmenin **tek taraflı** girdiği `Pending` kayıtlar sayaca giriyor ve fesih
**onay zincirini** başlatabiliyordu. (Feshin kendisi otomatik değildir: `TerminateContract`
yalnız `POST /api/contracts/{id}/terminate` ucundan gelir. Otomatik olan, yalnız onay zincirinin
başlatılmasıdır.)

Zincir başlamış olabilir ve **kod bunu geri alamaz**: `AttendanceLimitExceeded` hiçbir olay
akışına append edilmiyor, yalnız cascading mesaj olarak geçiyor — geriye dönük sorgulanacak bir
iz yok. Geçmiş tetiklemeler bu yüzden **elden** denetlenir.
:::

`Pending` bir kayıt kendiliğinden onaylanmaz: `AutoApproveExpiredAttendance` **kodda yoktur**,
yalnız `business-rules.md` içinde yazılıdır. Onay gelene kadar kayıt `Pending` kalır.

### Denetim — resync değil, okuma

Devamsızlık gerekçesiyle işaretlenmiş sözleşmeleri listeleyin:

```sql
select data->>'studentId', data->>'businessId',
       data->>'terminationReason', data->>'statusName'
from   contract.mt_doc_internshipcontract
where  data->>'terminationReasonType' = 'AttendanceLimitExceeded';
```

Gerekçe metni `"Devamsızlık limiti aşıldı: {gün}/{limit} gün"` biçimindedir. Çıkan her satır
için sayının **onaylanmış** kayıtlardan mı, yoksa işletmenin tek taraflı girdiği `Pending`
kayıtlardan mı doğduğu elden kontrol edilir.

### Yürüyen fesih zincirleri de teyit edilmeli

`InternshipSaga` artık yürüyen bir zinciri **yeniden başlatmıyor**
(`TerminationChainPolicy.CanStart`); önce her `InternshipTerminationRequested` olayında
koşulsuz `ApprovalChain = new(...)` yazıyor ve toplanmış öğretmen / müdür yardımcısı / müdür
onaylarını **siliyordu**.

İkinci tetikleme artık zinciri sıfırlamıyor; ama **geçmişte sıfırlanmış** zincirlerde onaylar
kaybolmuş olabilir ve zincirde **kimin onayladığı saklanmadığı** için bu kayıt üzerinden tespit
edilemez. Yürüyen fesihler elden teyit edilmelidir.

:::warning Yeni resync ucu YOK — eklenmemeli
Fesih sayacı için backfill **gerekmez**: `CheckAttendanceLimitHandler` sayacı görünümden değil
`AttendanceRecord` agregasından okur, yani düzeltme yürürlüğe girdiği an doğru sayar.
`AttendanceMarked` olaylarını yeniden yayınlamak ise **limiti tekrar tetikler**, yani fesih onay
zincirini yeniden başlatır. Bu iş için aşağıdaki "Resync / backfill uçları" tablosuna satır
**eklenmez**.
:::

### Payment'ın devamsızlık görünümü geçmişe dönük EKSİK — düzeltme onu iyileştirmez

Aynı düzeltmede altı `[AggregateHandler]` olayı **ilk kez** mesaj olarak yayınlanır hâle geldi
(`AttendanceApproved`, `AttendanceCorrected`, `AttendanceVerified`, `AttendanceDeleted`,
`HealthReportApproved`, `HealthReportAttached`). Bunlar bugüne kadar **hiç teslim edilmiyordu**:
`[AggregateHandler]` dönüşü yalnız olay akışına yazılır, hiçbir tüketiciye yönlendirilmez.

Sonucu Payment'ın yerel kaydında görülür: `payment.mt_doc_studentabsenceview` satırları
`AttendanceMarked` anındaki durumda **donmuş**tur. Agregada `Recorded` olan kayıt görünümde
`Pending` kalmış, onaylanan sağlık raporu görünümde `Unexcused` kalmış, silinen kayıt
görünümden hiç silinmemiş olabilir.

- **İleriye dönük** davranış düzeltmeden sonra doğrudur — yeni onay/düzeltme/silme işlenir
- **Geriye dönük** satırlar kendiliğinden düzelmez — onarım ucu: `POST /api/attendance/resync-snapshots` (#256)
- Etkisi **iki yönlüdür**: eksik kesinti (görünüm `Pending` donmuş) ya da **fazla kesinti**
  (onaylanmış raporu görünüme işlenmemiş). İkincisi öğrenci aleyhinedir

Denetim sorgusu — agrega ile görünümün ayrıştığı satırlar:

```sql
select a.id,
       a.data->>'statusName'      as agrega_durum,
       p.data->>'statusName'      as gorunum_durum,
       a.data->'type'             as agrega_tur,
       p.data->>'absenceTypeName' as gorunum_tur
from   attendance.mt_doc_attendancerecord a
join   payment.mt_doc_studentabsenceview  p on p.id = a.id
where  a.data->>'statusName'  is distinct from p.data->>'statusName'
   or  (a.data->>'isDeleted')::boolean is true;
```

Çıkan satırlar `POST /api/attendance/resync-snapshots` ile onarılır (#256). Uç kaydın **bugünkü
hâlini** ayrı bir olayla (`AttendanceSnapshotResynced`) yeniden yayınlar; `AttendanceMarked`
yayınlamaz, yani devamsızlık sınırını ölçtürmez ve fesih onay zincirini **başlatmaz**.

Onarım, sayılabilirlik değişen kayıtlar için maaşı da yeniden hesaplatır — ama yalnız
`AwaitingReceipt` fazındaki dönemleri; dekont yüklenmiş ödemelerin tutarı bilerek **donuktur**.

**Ölçüt: üretimde ölçün.** Dev ortamında `Pending` kayıt sayısı **0**, `AttendanceApproved` olay
sayısı **0** çıktı. Bu, düzeltmenin etkisiz olduğunu göstermez — yalnız dev tohum verisinin
işletme giriş yolunu hiç çalıştırmadığını gösterir. Ölçüm üretim verisinde tekrarlanmalıdır.

---

## Ücretli izin: resmîleşmiş başvurular devamsızlık kaydı üretmemiş (#254)

`ApprovePaidLeaveHandler` olayı yalnız olay akışına yazıyordu, mesaj olarak yayınlamıyordu
(#253 ile aynı kök neden). Sonuç: başvuru **Resmileşti** durumuna geçiyor ama
`PaidLeaveAttendanceConsumer` hiç çağrılmadığı için o günler için **hiçbir devamsızlık kaydı
doğmuyordu**. Ücretli izin komut yolundan da girilemediği için `PaidLeave` türü sisteme hiçbir
yoldan girmemiş durumda.

Düzeltme **ileriye dönüktür**. Daha önce onaylanmış başvurular için kayıtlar kendiliğinden
doğmaz.

:::danger Atlanırsa ne olur
Resmîleşmiş izin günleri sistemde **devamsızlık olarak hiç görünmez**: ücret kesintisi hesabına
da girmez, MESEM'in toplam gün sınırına (`MesemTotalDayLimit`, 3308 md. 26 izin hakkı) da
sayılmaz. Öğrenci izin hakkını kullanmış ama sistem kullanmamış gibi davranır.
:::

Tespit sorgusu — resmîleşmiş ama karşılığında `PaidLeave` kaydı olmayan başvurular:

```sql
select r.id,
       r.data->>'studentId'  as ogrenci,
       r.data->>'startDate'  as baslangic,
       r.data->>'endDate'    as bitis
from   attendance.mt_doc_paidleaverequest r
where  r.data->>'statusName' = 'Approved'
   and not exists (
         select 1
         from   attendance.mt_doc_attendancerecord a
         where  a.data->>'studentId' = r.data->>'studentId'
           and  a.data->'type'->>'Name' is null          -- tür düz string serialize edilir
           and  a.data->>'type' = 'PaidLeave'
           and  (a.data->>'date')::date
                between (r.data->>'startDate')::date and (r.data->>'endDate')::date
       );
```

**Hazır bir backfill ucu YOK.** `PaidLeaveApproved`'ı toplu yeniden yayınlamak kayıt üretimi
açısından güvenlidir — `PaidLeaveAttendanceConsumer` yeniden çalıştırmaya dayanıklıdır, aynı gün
için ikinci kayıt açmaz — ama `PaidLeaveNotificationConsumer` de uyanır ve **eski onaylar için
şimdi bildirim gider**. Toplu düzeltme yapılacaksa bildirimsiz bir yol yazılmalıdır.

---

## Resmî devamsızlık formu: onay bekleyen kayıtlar (#257)

Resmî MEB aylık devamsızlık formu, işletmenin girdiği ve **okul onayı bekleyen** kayıtları
devamsızlık olarak gösteriyordu (`D` sembolü + `UnexcusedAbsences` sütunu). Ayrıca
`AttendanceCorrected` ve `AttendanceDeleted` tüketicileri **boş no-op**'tu: yanlış girilip
düzeltilen ya da silinen devamsızlık formda **kalıcı** hâle geliyordu.

:::danger Atlanırsa ne olur
Form, velinin ve idarenin gördüğü **resmî belgedir**. Onaylanmamış bir bildirim orada devamsızlık
olarak görünüyor, düzeltilen kayıt düzelmiyor, silinen kayıt silinmiyordu.
:::

Düzeltme `AbsentDayEntry`'ye iki alan ekliyor: `AttendanceId` (artımlı olaylar tarih taşımadığı
için kayıt ancak kimlikten bulunabiliyor) ve `StatusName`. **İkisi de sona ve varsayılanlı
eklendi** — eski belgeler bozulmadan deserialize olur.

**Ama eski satırlar bu alanları taşımaz.** Durumu bilinmeyen satır formda **gösterilmeye devam
eder** (gizlemek var olan formdan veri silmek olurdu) ve kimliği olmayan satır artımlı olaylarla
güncellenemez.

**Onarım zorunludur:** `POST /api/attendance/resync-snapshots` (#256). Aynı uç hem Payment'ın hem
Reporting'in görünümünü onarır.

---

## Kademeli bildirim: doğum tarihi ve ilk açılış (#247)

18 yaş kuralı için `StudentNameView.BirthDate` gerekiyor. Alan #247 ile eklendi; **olay şekli
değişmedi** — `StudentRegistered` bu alanı zaten taşıyordu (#85), Attendance'ın tüketicisi okuyup
atıyordu.

- **Mevcut satırlar boştur.** `POST /api/students/resync-projections` ile dolar; alanı yayınlayan
  kod o uçta zaten var, **yeni uç yazılmadı**.
- Atlanırsa: doğum tarihi `null` kalır ve **öğrencilere bildirim yine gider**
  (`AbsenceNotificationPolicy.ShouldNotifyStudent` bilinmeyen tarihte gönderme yönünde karar
  verir). Yani atlamak sessiz bir kayıp üretmez, yalnız 18 altı öğrencilere fazladan bildirim
  gider.

:::warning İlk açılışta sıçrama
Özellik açıldığında eşiği **zaten geçmiş** öğrenciler için defter boştur. Politika bu durumda
yalnız **en yüksek** kademeyi bildirir (5/15/25'in üçünü değil), atlananları `SkippedSteps` ile
kayda geçirir. Yine de ilk devamsızlık girişi/onayı dalgasında toplu bildirim beklenir — özelliği
dönem ortasında açarken bunu hesaba katın.
:::

---

## Kademeli bildirim teslimatı: veli bağı ZORUNLU ön koşul (#247)

Tebligat üç alıcıya gider: **veli** (koşulsuz), **işletme** (koşulsuz) ve — 18 yaşını
doldurmuşsa — **öğrencinin kendisi**. Veli ve işletme ayakları md. 36 (4)'ün doğrudan
emrettiği alıcılardır.

:::danger Veli bağı kurulmamışsa tebligat boş kümeye gider
**Yeni veliler** davet anında bağlanır (#271): `CreateInvitation.StudentIds` verilir, davet kabul
edilince `UserAccount.LinkedStudentIds` ve Keycloak özniteliği kurulur. Bağ **yalnız veli
rolünde** kurulabilir.

**Mevcut veliler için otomatik yol YOKTUR** ve olamaz: ortak anahtar yok — `UserAccount`'ta TC
alanı bulunmuyor, `StudentRegistered` veli bilgisi taşımıyor, ad eşleştirmesi güvenilmez. Onlar
elle bağlanır: `POST /api/security/users/{id}/students`.

**Eksiği ölçün:** `GET /api/security/users/guardian-links/missing` velisi bağlı olmayan
öğrencileri ve sayısını döner. O öğrencilerin velisine devamsızlık tebligatı **ulaşmaz**.

:::warning `resync-projections` ZORUNLU — yoksa bağ kurulamaz
Veli bağı artık kiracı kontrolünden geçiyor (#271): istenen öğrenci kimliği, kiracının
`GuardianLinkView` görünümünde **bulunmak zorunda**. Görünüm `StudentRegistered`'dan beslenir ve
mevcut öğrenciler için **boştur**.

`POST /api/students/resync-projections` çalıştırılmadan **hiçbir veli bağı kurulamaz** — ne
davetten ne elle. Kontrol bilerek kapalı tarafa düşüyor: kapsamsız kalmak, yanlış kapsama
düşmekten iyidir (ADR-0003 adım 2 ile aynı yön). Hata mesajı operatörü bu uca yönlendirir.
:::

Kod bunu ayrıca **sessiz bırakmıyor** — alıcı bulunamadığında `LogWarning` yazılır — ama log
okunmazsa yükümlülük yerine getirilmemiş olur.
:::

**Öğrenci ayağı da bir ön koşula bağlı:** `UserAccount.StudentId` #230 öncesinde hiç
yazılmıyordu. `POST /api/students/resync-projections` çalıştırılmazsa öğrenci de hata değil
**sessiz boş** alır. (Aynı uç `StudentNameView.BirthDate`'i de doldurur — yukarı bakınız.)

### Kanallar ve hukuki ağırlıkları

| Kanal | Kalıcı mı | Tebligat kanıtı olur mu |
| --- | --- | --- |
| Uygulama içi (SSE) | **Hayır** — bağlı olmayan kullanıcının bildirimi düşer, sunucuda hiçbir yere yazılmaz, yeniden bağlanma yok | **Hayır** — kolaylıktır |
| E-posta | Evet — SMTP gönderimi ve log kaydı | **Evet** — "yazılı bildirim" gereğini bu karşılar |
| Yazdırılabilir tebligat | Talep üzerine, koordinatör isterse | İmzalatılırsa en güçlü iz |

SMTP ayarları: `SmtpSettings:*` (dev'de Mailpit, `localhost:1025`, TLS yok). Ayar eksikse
gönderim **başarısız olur ve loglanır** — sessiz düşmez.

---

## Resync / backfill uçları

Hepsi **idempotent**tir (tüketiciler `session.Store` ile upsert yapar), birden çok kez
çağrılabilir.

| Uç | Ne yapar |
| --- | --- |
| `POST /api/attendance/resync-snapshots` | Devamsızlık kayıtlarının **bugünkü hâlini** yeniden yayınlar (#256). Payment'ın `StudentAbsenceView` **ve** Reporting'in `StudentAttendanceReportView` satırlarını onarır: donmuş `Pending` durumlar, işlenmemiş sağlık raporu onayları, silinmemiş satırlar (#256, #257). `attendance:report` ister (`attendance:manage` **değil** — o izin işletme rollerinde de var). İsteğe bağlı `?academicPeriodId=` ile daraltılır. **Kiracı başına** çağrılır. `AttendanceMarked` yayınlamaz, fesih zinciri tetiklenmez |
| `POST /api/students/resync-projections` | Tüm öğrenciler için `StudentRegistered` yeniden yayınlanır — Attendance/Contract `StudentNameView`, Reporting ve Payment görünümlerini tazeler |
| `POST /api/placements/resync-projections` | Tüm **aktif** yerleştirmeler için `StudentPlaced` yeniden yayınlanır — Payment `PlacementView`, Coordination not giriş görünümleri |
| `POST /api/placements/backfill-branch-authorizations` | İşletmelerin alan yetkilerini mevcut yerleştirmelerden doldurur |
| `POST /api/businesses/resync-projections` | Tüm işletmeler için `BusinessUpdated` yeniden yayınlanır — diğer modüllerin işletme görünümleri |
| `POST /api/coordination/teachers/resync-views` | Koordinasyon görünümlerini kurum bazında yeniden kurar |
| `POST /api/coordination/weekly-visits/resync` | Haftalık ziyaret olaylarını yeniden yayınlar |
| `POST /api/institutions/staff/resync-branch-codes` | Personel kaydından kullanıcı hesabına **kurum (kiracı anahtarı) ve alan kapsamı** backfill'i — **uydurmaz, üzerine yazmaz**; yalnız boş alanı doldurur. **Yalnız çağıranın kendi okulu** için çalışır (#131); kurum üstü aktör `?institutionId=` ile hedef verir |
| `POST /api/security/users/resync-display-names` | Kullanıcı görünen adlarını tazeler |
| `POST /api/institutions/rebuild-hierarchy` | Kurum **ağacını** mevcut okul künyelerinden (`ProvinceCode` / `DistrictName`) kurar: il ve ilçe müdürlüğü düğümlerini açar, `ParentId` ve `Path` yazar. `platform:tenant:manage` ister. **İdempotent** — ikinci koşu düğüm çoğaltmaz, bozulmuş yolu onarır |
| `POST /api/security/users/replay` | Mevcut kullanıcı hesaplarını `UserAccountReplayed` olarak yeniden yayınlar — Institution modülünün `InstitutionManagerLink` görünümünü doldurur (bkz. aşağıdaki bölüm). `platform:tenant:manage` ister. **İdempotent** |

### Personel backfill'i tek okulludur (#131)

`resync-branch-codes` eskiden **bütün kurumların** personelini tarıyordu; kodda "Faz 1 tek
kurumlu olduğu için pratik etkisi yok" diyen bir TODO vardı. O varsayım ikinci okulla birlikte
çöktü ve ölçüldü: kendi okulunda **1** personeli olan bir müdür ucu çağırdığında **9** personel
işlendi — üç okulun tamamı. Yayınlanan olaylar okuma değil, Security tarafında kullanıcı
**kapsamı** yazıyor.

Artık hedef aktörün claim'inden gelir. Çok okullu bir kurulumda **her okul için ayrı
çalıştırın**; kurum üstü aktör hepsini sırayla hedefleyebilir:

```
POST /api/institutions/staff/resync-branch-codes                      → kendi okulu
POST /api/institutions/staff/resync-branch-codes?institutionId=<id>   → platform:tenant:manage
```

Yabancı hedef veren okul aktörü **422** alır.

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

**Denetim izi bu ön koşula farklı bir şekilde bağlıdır — sıraya, listeye değil.**
`rebuild-hierarchy` hiç koşmamış bir kurulumda kurum kapsamlı denetim listesi (`GET
/api/audit/institution`) **normal çalışır**: yol yoksa `InstitutionScopePolicy.VisibleScope`
aktörün kimliğine düşer (`SubjectInstitutionId == institutionId`) ve bu alan her satırda
doludur (`AuditEntryFactory.ResolveSubject` konu kurumu bulamazsa aktörün kurumuna düşer) —
boş liste görülmez.

Asıl tehlike **kısmi sıradır**. Aktörün kurum yolu boşken (`rebuild-hierarchy` henüz
koşturulmamışken) yazılan satırlara `SubjectInstitutionPath = null` işlenir — sıcak yolda ek
okuma yapılmaz, yol doğrudan aktörün claim'inden kopyalanır. `rebuild-hierarchy` **sonradan**
koşup aktör yol kazanınca `VisibleScope` yol-önekiyle süzen dala geçer
(`SubjectInstitutionPath != null && StartsWith(...)`) ve **arada yazılmış o satırlar kurum
kapsamlı görünümde kalıcı olarak görünmez olur** — hata dönmez, sayaç yok, uç 200 ve liste
eksik gelir. Bunu geri dolduracak bir uç yoktur: `rebuild-hierarchy` yalnız `Institution`
belgelerini yazar, `AuditEntry.SubjectInstitutionPath` yazma anında donmuş bir kopyadır ve
geri işlenmez.

**Bu yüzden kurum hiyerarşisi geçişi denetim izi dağıtılmadan önce koşturulmalıdır.**
Sonrasına kalırsa aradaki satırlar yalnız kurum kapsamlı görünümden kaybolur; aktörün kendi
"İşlemlerim" görünümü (`ActorId` ile süzülür, yol kontrolü yapmaz) onları göstermeye devam
eder.

### Yerleştirme resync'i: atlanan kayıtlar

`POST /api/placements/resync-projections` yanıtı `{ placementCount, skipped }` döndürür.
**`skipped` sıfırdan büyükse bakın** — kaynak kaydı (öğrenci profili ya da işletme görünümü)
bulunamayan yerleştirmeler yayınlanmamıştır, çünkü eksik adla yayın tüketicilerin denormalize
alanlarını boş dizeyle ezerdi.

Tamamlanmış ve feshedilmiş yerleştirmeler bilerek atlanır: yeniden yayınlanırlarsa tüketici
modüllerde yeniden "aktif" işaretlenirlerdi.

**Okulda staj (işverensiz) yerleştirmeler atlanmaz** — işletmenin yokluğu orada eksik veri
değildir. Kural `PlacementResyncPolicy` içinde adlandırılmış ve testle kilitlenmiştir; koşulun
"işletme kaydı yoksa atla" diye sadeleştirilmesi okulda staj yapan her öğrenciyi sessizce
düşürür ve dönem notu girilemez hâle getirir.

## Kiracı anahtarı eklenen görünümler (#147 adım 1)

Dört görünüme `InstitutionId` eklendi. Alan yeni olduğu için **mevcut satırlarda boştur** ve
doldurulması gerekir:

| Görünüm | Backfill yolu |
| --- | --- |
| `StudentNameView` (Attendance + Contract) | `POST /api/students/resync-projections` |
| `StudentPaymentProfile` | `POST /api/students/resync-projections` |
| `AttendanceView` | Marten projeksiyon yeniden kurulumu (async daemon) — uç gerekmez |
| `StudentAbsenceView` | ⚠ **Hazır yol YOK** — aşağıya bakınız |

### `StudentAbsenceView` boşluğu

Bu görünümü `AbsenceTallyConsumer` yazıyor ve kaynağı `AttendanceMarked` olayı. Attendance
modülünde **resync ucu yok**, yani olayı yeniden yayınlayacak bir yol bulunmuyor.

İki seçenek: (a) Attendance modülüne diğerleriyle aynı desende bir resync ucu eklemek,
(b) tek seferlik SQL ile doldurmak — `StudentAbsenceView.Id` devamsızlık kaydının kimliğidir ve
kiracı bilgisi o kayıttan okunabilir.

**Ölçüt:** backfill sonrası kiracıya ait hiçbir belgede `institutionId` boş kalmamalı. Kontrol:

```sql
select count(*) from payment.mt_doc_studentabsenceview
where coalesce(data->>'institutionId','') = '';
```

## Kiracı anahtarı: token yolu kapandı (ADR-0003 adım 2)

`institution_id` claim'i artık **yalnız sunucu tarafından** üretilir: önce kullanıcı kaydı
(`UserAccount.InstitutionId`), sonra personel kaydı yedeği. **Token'daki değer hiç okunmuyor.**

:::danger Bu dağıtımın ön koşulu
Önce `POST /api/institutions/staff/resync-branch-codes` çalışmış olmalı. Backfill yapılmadan
token yolu kapatılırsa, kaydı boş olan **mevcut kullanıcılar kapsamsız kalır ve kilitlenir**.
Bu yüzden backfill ayrı bir dağıtım olarak (#223) önce gitti.
:::

**Dağıtım sonrası kontrol** — kapsamsız hesap kalmamalı:

```sql
select data->>'username'
from security.mt_doc_useraccount
where data->>'deletedAt' is null
  and coalesce(data->>'institutionId','') = '';
```

Çıkan her satır için kurum bağını elle kurun:

```
POST /api/security/users/{userAccountId}/institution
{ "institutionId": "<kurum-guid>" }
```

**Personel olmayan kullanıcılar** (işletme yetkilisi, öğrenci, veli) bu listede çıkabilir:
personel kaydı yedeği onları kapsamaz ve `SyncUsersFromKeycloak` artık kurum bağı kurmuyor.
Sync yanıtındaki `withoutInstitution` sayısı aynı boşluğu gösterir.

## İşletme provenance göçü (ADR-0003 adım 4)

`Business.InstitutionId` → `RegisteredByInstitutionId` olarak yeniden adlandırıldı. Alan
**provenance**tır (kaydı hangi okul girdi), kapsam değil — işletme kataloğu paylaşımlıdır.

Marten belgeyi JSON olarak saklar, yani **ad değişikliği anahtarı değiştirir**. Mevcut
belgeler göç etmeden `registeredByInstitutionId` alanı boş (`Guid.Empty`) okunur.

:::danger Atlanırsa ne olur
Sorgular etkilenmez (hiçbir sorgu bu alanla filtrelemez), ama işletme **onaylandığında veya
yeniden aktifleştirildiğinde** koordinasyon görünümü `Guid.Empty` kapsamıyla açılır ve işletme
koordinasyon ekranlarından **kaybolur**. Boş provenance `LogWarning` üretir
(`BusinessScopeOrigin`), yoksa bu sessiz olurdu.
:::

```sql
update business.mt_doc_business
set data = (data - 'institutionId')
        || jsonb_build_object('registeredByInstitutionId', data->'institutionId')
where data ? 'institutionId';
```

Doğrulama — ikisi de `0` dönmeli:

```sql
select count(*) from business.mt_doc_business where data ? 'institutionId';
select count(*) from business.mt_doc_business
where coalesce(data->>'registeredByInstitutionId','') in
      ('', '00000000-0000-0000-0000-000000000000');
```

Göç idempotenttir: `where data ? 'institutionId'` koşulu ikinci koşuda hiçbir satır seçmez.

## Conjoined kiracılık göçü (ADR-0003 adım 5) — TEK YÖNLÜ KAPI

Var olan bir veritabanını kiracılığa geçirir. **Yeni (boş) veritabanı bu adıma ihtiyaç
duymaz** — Marten tabloları zaten kiracılı yaratır; kıran şey yalnız var olan tablonun
deltasıdır.

Betikler: `src/Docs/docs/infrastructure/sql/`

```bash
# 0) Yedek. Göç yıkıcı değil (ölçüldü: DROP/DELETE/TRUNCATE yok) ama geri dönüşü zordur.
pg_dump -Fc mesnet > mesnet-conjoined-oncesi.dump

# 1) Şema — TEK transaction. Yarıda kalırsa tamamı geri alınır.
psql -d mesnet --single-transaction -v ON_ERROR_STOP=1 -f 149-conjoined-kiracilik.sql

# 2) Damgalama — hedef kiracı AÇIKÇA verilir.
psql -d mesnet -v ON_ERROR_STOP=1 \
     -v tenant="$(psql -tAqc 'select id from institution.mt_doc_institution' mesnet)" \
     -f 149-kiraci-damgalama.sql
```

Adım 2 kendi doğrulamasını yapar: `DAMGASIZ TOPLAM: 0` yazmalıdır.

:::danger İki betiğin arası kesinti penceresidir
Birinci betik sütunları ekler ama satırları `*DEFAULT*` kovasında bırakır. O hâlde **hiçbir
okul kendi verisini göremez** — API 200 döner, listeler boş gelir. İki adım ayrı günlere
bölünmez; ikisi de bittikten sonra uygulama açılır.
:::

:::warning Kuyruk boşken yapın
Geçiş anında bekleyen kiracısız zarflar en sinsi durumdur: tüketici kiracı olmadan çalışır ve
`DefaultTenantUsageDisabledException` alır. Wolverine yeniden dener, ama dener durur.
:::

**Neden açılışta değil, elden:** `ApplyAllDatabaseChangesOnStartup()` denendi ve API'yi
açılışta öldürdü. Marten'ın ürettiği conjoined deltası aynı yabancı anahtarı iki kez ekliyor
(`42710: constraint "fkey_mt_events_stream_id_tenant_id" ... already exists`) ve bütün göçü geri
alıyor. Depodaki betikte yinelenen blok silinmiş, kalan ekleme `DROP ... IF EXISTS` ile
idempotent yapılmıştır — Marten sürümü yükseltilirken bu düzeltmenin hâlâ gerekli olup olmadığı
yeniden üretilerek kontrol edilmelidir.

**Kalıcı doğrulama:** `TenantStampIntegrityTests` CI'da her koşuda `*DEFAULT*` satır sayısının
sıfır olduğunu ve `mt_streams` birincil anahtarının `(tenant_id, id)` olduğunu ölçer.

### Geri alma diye bir şey yok — eski sürüm damgayı SESSİZCE siler

Bu ölçüldü, tahmin değil. Göç edilmiş bir veritabanına **kiracılık öncesi kod** bağlanırsa
Marten `AutoCreate` ile şemayı kendi beklentisine uydurur ve kiracılığı **geri alır**. Üç GET
isteği yetti:

| | Önce | Üç istekten sonra |
| --- | ---: | ---: |
| `tenant_id` taşıyan tablo | 49 | **46** |
| `mt_doc_studentprofile` PK | `(tenant_id, id)` | **`(id)`** |
| Öğrenci satırı | 121 | 121 |

Satırlar durur, **damga gider**. Hata yoktur, log temizdir, uçlar 200 döner. Tabloya
dokunuldukça kayıp yayılır.

:::danger Sürüm geri alınırsa göç de geri alınmış olmaz
Kiracı bilgisi kolondaydı ve kolon düşürüldü; ileri sürüme dönmek onu geri getirmez, yalnız
sütunu boş (`*DEFAULT*`) olarak yeniden yaratır. Tek çözüm yedekten dönmektir.

Pratik sonuç: bu dağıtımdan sonra **eski imaja dönülmez**. Sorun çıkarsa ileri düzeltme yapılır
ya da veritabanı yedekten geri yüklenir. Aynı kural geliştirme makineleri için de geçerlidir —
göç edilmiş bir yerel veritabanına eski daldan API bağlamayın.
:::

## Keycloak'ta artık kalan kapsam anahtarı öznitelikleri (ADR-0003 adım 3, #229)

Kapsam anahtarlarının **ikisi de** artık yalnız `UserAccount` kaydından üretiliyor:
`institution_id` (ADR-0003 adım 2) ve `business_id` (#229). Token'daki değerler her istekte
siliniyor ve hiçbir kod onları Keycloak'a yazmıyor (`InstitutionClaimAuthorityTests`,
`BusinessClaimAuthorityTests`).

Ama **eski kayıtlar duruyor.** Dev realm'inde ölçüldü: 8 kullanıcının 6'sında öznitelik hâlâ var.

:::note Zararsız ama temizlenmeli
Öznitelik **atıldır** — okunmuyor, yazılmıyor. Tehlike teknik değil insani: duran bir kopya,
ileride birinin onu yeniden otorite sanmasına davetiye çıkarır. Aynı sebeple ADR "Keycloak'a
`institution_id` YAZILMAZ" diyor.
:::

```
POST /api/security/users/purge-institution-attribute      (user:roles:manage)
```

Uç tüm Keycloak kullanıcılarını tarar ve **her iki özniteliği** de **öznitelik yazan normal yoldan** siler:
gövde taze bir GET'ten kurulduğu için ad, e-posta ve diğer öznitelikler kaybolmaz.
**Idempotenttir** — ikinci koşuda `purged = 0` döner.

Yanıt dört sayı verir; **`failed` sıfırdan farklıysa bakın**, o kullanıcılarda artık duruyor
demektir. Dev ortamında ölçüldü:

```
1. koşu: 7 kullanıcı tarandı: 6 özniteliği silindi, 1 zaten temizdi, 0 başarısız
2. koşu: 7 kullanıcı tarandı: 0 özniteliği silindi, 7 zaten temizdi, 0 başarısız
```

Silme sonrası profiller ve diğer öznitelikler (`branch_codes`, `business_id`) yerinde kaldı.

:::warning Keycloak konsolundan elle silmeyin
Admin API'ye yalnız `{"attributes": {...}}` göndermek `firstName`/`email` alanlarını siler ve
**204 döner** (ölçüldü, Keycloak 26.7.0). Gövde tam temsil olmalıdır — bkz.
`KeycloakUserWritePolicy`.
:::

## İşletme vergi kimliği — mevcut kayıtlar boş kalır (#150)

Vergi kimliği artık **zorunlu** ve paylaşımlı işletme kataloğunun doğal anahtarı: aynı firmayı
iki okulun ayrı ayrı kaydetmesini engelleyen tek alan odur.

**Mevcut kayıtlar etkilenmez.** Benzersizlik kısıtı bilerek **kısmidir**:

```sql
CREATE UNIQUE INDEX idx_business_taxno_uniq ON business.mt_doc_business
  ((data ->> 'taxNumber')) WHERE (data ->> 'taxNumber') IS NOT NULL;
```

Ölçüldü: dağıtım öncesi 100 işletmenin **100'ünde** alan `NULL`'dur. Tam kısıt kullanılsaydı
göç ilk açılışta düşerdi.

:::warning Eski kayıtlar kopya üretmeye devam eder
`NULL` alanlar birbirini engellemez. Yani #150 **bundan sonra** doğacak kopyaları keser; hâlihazırdaki
kayıtlar için koruma **yoktur**. Vergi kimlikleri doldurulana kadar aynı firma iki okulda iki kayıt
olarak durabilir.
:::

Doldurma yolu: işletme düzenleme formu (`PATCH /api/businesses/{id}`, alan artık formda).
İlerleme sorgusu:

```sql
SELECT count(*) FILTER (WHERE data->>'taxNumber' IS NULL) AS eksik,
       count(*)                                          AS toplam
FROM business.mt_doc_business;
```

Alan dolduruldukça kısıt kendiliğinden devreye girer; ayrı bir göç adımı gerekmez.

## Öğrenci kapsam otoritesi — resync ZORUNLU (#230)

`student_id` claim'i artık `UserAccount.StudentId`'den üretiliyor. O alan #230 öncesinde
**hiçbir yerde yazılmıyordu** — ölçüldü: 11 hesabın **0**'ında doluydu.

:::danger Sıra bozulursa öğrenciler kapsamsız kalır
Yeni kod token'daki `student_id`'yi **siler**. Otorite doldurulmadan dağıtılırsa her öğrenci
kendi devamsızlığını, stajını ve ücretini göremez; ücretli izin başvurusu yapamaz; bildirimleri
ulaşmaz. Hata da almazlar — **boş** sonuç görürler.
:::

Dağıtımdan **hemen sonra**:

```
POST /api/students/resync-projections      (student:manage)
```

`StudentRegistered` yeniden yayınlanır; Security tüketicisi öğrencinin Keycloak kimliğiyle
eşleşen hesabı bulup `UserAccount.StudentId`'yi doldurur. Doğrulama:

```sql
SELECT count(*) FILTER (WHERE data->>'studentId' IS NOT NULL) AS dolu,
       count(*)                                               AS toplam
FROM security.mt_doc_useraccount WHERE data->'roles' ? 'Student';
```

**Eşleşmeyen öğrenci normaldir:** öğrenci profili gerçek bir Keycloak kullanıcısına bağlı
değilse (dev tohum verisinde çoğu böyledir) tüketici sessizce atlar — uydurmaz. Bu öğrencilerin
sistemde hesabı yoktur, dolayısıyla kapsam da gerekmez.

## Müdürlük panosu: yöneticisiz okul listesi — replay ZORUNLU

Müdürlük panosunun "yöneticisiz okullar" kartı `InstitutionManagerLink` görünümünden okur. Bu
görünüm Security modülünün `UserCreated` / `UserRolesChanged` / `UserActivated` /
`UserDeactivated` / `UserDeleted` olaylarıyla **bundan sonra** beslenir — dağıtım öncesi var
olan kullanıcı hesapları için satır **hiç yoktur**.

```
POST /api/security/users/replay      (platform:tenant:manage)
```

**Ne zaman çalıştırılır:** bu işi dağıttıktan sonra **bir kez**. **İdempotenttir** — birden
çok kez çağrılabilir, ikinci koşu satırları aynı değerle yeniden yazar.

**Ne yapar:** `DeletedAt == null` olan tüm kullanıcı hesaplarını `UserAccountReplayed` olarak
yeniden yayınlar (`UserCreated` **değil** — bkz. `UserAccountReplayed` XML doc, I-2); Institution
modülünün `InstitutionManagerLinkConsumer`'ı bu olayı dinleyip kurum bağı, etkinlik durumu ve
`institution:manage` yetkisini satıra mutlak olarak yazar.

:::danger Atlanırsa ne olur
Görünüm **boş kalır**, dolayısıyla "yöneticisiz okul" sorgusu **HER okulu** yöneticisiz sayar —
gerçekte yöneticisi olan okullar da dahil. Hata dönmez, log basılmaz; panoda yalnız yanlış bir
liste görünür.
:::

:::note Olaylar asenkron işlenir
Uç 200 döndüğünde olaylar yalnız **yayınlanmıştır**, işlenmiş olması gerekmez —
`InstitutionManagerLinkConsumer`'ın kuyruğu ayrıca ilerler. Yanıttan hemen sonra panoyu
kontrol etmek eksik görünebilir; kuyruk boşalana kadar bekleyin.
:::

## Sırayı bozmayın

Bir adım başka bir adımın verisini üretiyorsa sıra önemlidir. Örnek: koordinasyon zinciri
dağıtımında önce **yetki backfill'i**, sonra **görünüm resync'i** gerekir — ters sırada
yerleştirme tümden durur. Aynı biçimde kiracı anahtarında önce **backfill**, sonra
**token yolunun kapatılması** gelir.

## Staj saga'sı: kopya birleştirme + sözleşme bağlama (#248, #251)

**Sıra bozulmaz — önce tekilleştirme, sonra bağlama.** Kopyalar dururken sözleşme bağlamak,
24 kardeşten rastgele birine bağlamak demektir.

```bash
# 1) Kopya saga'ları birleştir — TÜM kiracılar, tek çağrı, platform:tenant:manage
curl -X POST /api/internships/resync-sagas

# 2) Aktif sözleşmeleri saga'ya yeniden bağla — TÜM kiracılar, tek çağrı, platform:tenant:manage
curl -X POST /api/contracts/resync-internship-links
```

:::warning Bu iki uç eskiden 200 dönüp SIFIR kayıt işliyordu (#292)
`platform:tenant:manage` taşıyan aktörün kurumu yoktur, dolayısıyla **platform kiracısına**
düşer. `InternshipSaga` ve `InternshipContract` ise kiracı damgalıdır ve orada **hiçbir satırı
yoktur**. Enjekte edilen istek session'ıyla çalışan eski sürüm bu yüzden boş sonuç buluyor,
uç 200 veriyordu — operatör onarımın yapıldığını sanıyordu.

Dev'de görünmemesinin nedeni: `admin` hesabı `InstitutionManager` **ve** `SystemAdmin`
rollerini birlikte taşıyor, yani kendi okulunun kiracısında koşuyordu.

Her iki uç artık `ITenantDirectory` ile **bütün kiracıları dolaşıyor** ve yanıtta
`tenantsProcessed` dönüyor — sıfır kiracı, sıfır bulgudan farklı bir şeydir ve ayırt
edilebilmelidir. Yayınlanan `ContractActivated` da `DeliveryOptions.TenantId` ile
damgalanıyor; damgalanmasaydı olay yayınlayanın (platform) kiracısını devralır, tüketici
saga'yı yanlış kiracıda arar ve hiçbir hata vermezdi.

Kilitleyen test: `PlatformScopedTenantDocumentDriftTests` — `platform:tenant:manage` ile
korunan bir ucun kiracı damgalı belgeye enjekte session'la dokunmasını kırmızıya çevirir.
:::

**Neden gerekli:** saga'nın modüller arası yarısı çalışmıyordu (#248) — `ContractActivated`,
`ContractTerminated`, `ContractCompleted` ve `AttendanceLimitExceeded` saga kimliği
çözülemediği için ölü mektup kuyruğuna düşüyordu. Ölçüm: **2248 saga'nın hiçbirinde**
`contractId` yazılı değildi. Ayrıca kimlik `Guid.NewGuid()` ile üretildiği için tekrar
yayınlanan `StudentPlaced` her seferinde yeni saga doğuruyordu (#251): 2248 saga, yalnız
95 yerleştirme.

Kod düzeltildi ama **geçmiş kendiliğinden düzelmez**; olaylar bir daha yayınlanmaz.

- Adım 1 kopyaları siler, **en ileri fazdaki** kazanır — yürüyen fesih zincirini iptal etmemek için
- Adım 2 yalnız **aktif** sözleşmeler için `ContractActivated` yeniden yayınlar.
  `Terminated`/`Completed` **bilerek atlanır**: yeniden yayınlansaydı yeniden yerleştirme
  talebi ve staj kapanışı **ikinci kez** tetiklenirdi

> **Atlanırsa:** stajlar sözleşmeleriyle bağlanmaz, `AwaitingContract`'ta çakılı kalır ve
> hangi saga'nın gerçek olduğu belirsizdir. Hata görünmez — yalnız fesih ve kapanış hiç çalışmaz.
