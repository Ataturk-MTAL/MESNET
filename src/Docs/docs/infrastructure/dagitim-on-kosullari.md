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

## Resync / backfill uçları

Hepsi **idempotent**tir (tüketiciler `session.Store` ile upsert yapar), birden çok kez
çağrılabilir.

| Uç | Ne yapar |
| --- | --- |
| `POST /api/attendance/resync-snapshots` | Devamsızlık kayıtlarının **bugünkü hâlini** yeniden yayınlar (#256). Payment'ın `StudentAbsenceView` satırlarını onarır: donmuş `Pending` durumlar, işlenmemiş sağlık raporu onayları, silinmemiş satırlar. `attendance:report` ister (`attendance:manage` **değil** — o izin işletme rollerinde de var). İsteğe bağlı `?academicPeriodId=` ile daraltılır. **Kiracı başına** çağrılır. `AttendanceMarked` yayınlamaz, fesih zinciri tetiklenmez |
| `POST /api/students/resync-projections` | Tüm öğrenciler için `StudentRegistered` yeniden yayınlanır — Attendance/Contract `StudentNameView`, Reporting ve Payment görünümlerini tazeler |
| `POST /api/placements/resync-projections` | Tüm **aktif** yerleştirmeler için `StudentPlaced` yeniden yayınlanır — Payment `PlacementView`, Coordination not giriş görünümleri |
| `POST /api/placements/backfill-branch-authorizations` | İşletmelerin alan yetkilerini mevcut yerleştirmelerden doldurur |
| `POST /api/businesses/resync-projections` | Tüm işletmeler için `BusinessUpdated` yeniden yayınlanır — diğer modüllerin işletme görünümleri |
| `POST /api/coordination/teachers/resync-views` | Koordinasyon görünümlerini kurum bazında yeniden kurar |
| `POST /api/coordination/weekly-visits/resync` | Haftalık ziyaret olaylarını yeniden yayınlar |
| `POST /api/institutions/staff/resync-branch-codes` | Personel kaydından kullanıcı hesabına **kurum (kiracı anahtarı) ve alan kapsamı** backfill'i — **uydurmaz, üzerine yazmaz**; yalnız boş alanı doldurur. **Yalnız çağıranın kendi okulu** için çalışır (#131); kurum üstü aktör `?institutionId=` ile hedef verir |
| `POST /api/security/users/resync-display-names` | Kullanıcı görünen adlarını tazeler |

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

## Sırayı bozmayın

Bir adım başka bir adımın verisini üretiyorsa sıra önemlidir. Örnek: koordinasyon zinciri
dağıtımında önce **yetki backfill'i**, sonra **görünüm resync'i** gerekir — ters sırada
yerleştirme tümden durur. Aynı biçimde kiracı anahtarında önce **backfill**, sonra
**token yolunun kapatılması** gelir.

## Staj saga'sı: kopya birleştirme + sözleşme bağlama (#248, #251)

**Sıra bozulmaz — önce tekilleştirme, sonra bağlama.** Kopyalar dururken sözleşme bağlamak,
24 kardeşten rastgele birine bağlamak demektir.

```bash
# 1) Kopya saga'ları birleştir — kiracı başına, platform:tenant:manage
curl -X POST /api/internships/resync-sagas

# 2) Aktif sözleşmeleri saga'ya yeniden bağla — kiracı başına, platform:tenant:manage
curl -X POST /api/contracts/resync-internship-links
```

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
