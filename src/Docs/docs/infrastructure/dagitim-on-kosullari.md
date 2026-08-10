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

## Resync / backfill uçları

Hepsi **idempotent**tir (tüketiciler `session.Store` ile upsert yapar), birden çok kez
çağrılabilir.

| Uç | Ne yapar |
| --- | --- |
| `POST /api/students/resync-projections` | Tüm öğrenciler için `StudentRegistered` yeniden yayınlanır — Attendance/Contract `StudentNameView`, Reporting ve Payment görünümlerini tazeler |
| `POST /api/placements/resync-projections` | Tüm **aktif** yerleştirmeler için `StudentPlaced` yeniden yayınlanır — Payment `PlacementView`, Coordination not giriş görünümleri |
| `POST /api/placements/backfill-branch-authorizations` | İşletmelerin alan yetkilerini mevcut yerleştirmelerden doldurur |
| `POST /api/businesses/resync-projections` | Tüm işletmeler için `BusinessUpdated` yeniden yayınlanır — diğer modüllerin işletme görünümleri |
| `POST /api/coordination/teachers/resync-views` | Koordinasyon görünümlerini kurum bazında yeniden kurar |
| `POST /api/coordination/weekly-visits/resync` | Haftalık ziyaret olaylarını yeniden yayınlar |
| `POST /api/institutions/staff/resync-branch-codes` | Personel kaydından kullanıcı hesabına **kurum (kiracı anahtarı) ve alan kapsamı** backfill'i — **uydurmaz, üzerine yazmaz**; yalnız boş alanı doldurur |
| `POST /api/security/users/resync-display-names` | Kullanıcı görünen adlarını tazeler |

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

## Sırayı bozmayın

Bir adım başka bir adımın verisini üretiyorsa sıra önemlidir. Örnek: koordinasyon zinciri
dağıtımında önce **yetki backfill'i**, sonra **görünüm resync'i** gerekir — ters sırada
yerleştirme tümden durur. Aynı biçimde kiracı anahtarında önce **backfill**, sonra
**token yolunun kapatılması** gelir.
