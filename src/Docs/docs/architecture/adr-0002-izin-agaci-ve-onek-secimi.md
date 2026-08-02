---
title: 'ADR-0002: İzin ağacı ve önek seçimi'
---

# ADR-0002: İzin ağacı — gruplama ekseni ve önek seçimi

| | |
|---|---|
| **Durum** | Kabul edildi |
| **Tarih** | 02.08.2026 |
| **Karar sahibi** | Proje sahibi |
| **İlgili** | ADR-0001 · #126, #129, #130, #147, #171, #172, #174, #177 |

## Karar

> **İzinler önek (prefix) ile gruplanır ve önek seçimi bir güvenlik kararıdır.**
> Bir rolün wildcard'ı (`önek:*`) o öneki yutuyorsa, o önekte tanımlanan **her yeni izin o role
> sessizce geçer**. Bu yüzden yeni bir izin adlandırılırken önce "bu öneki kimler wildcard'la
> taşıyor" sorusu cevaplanır.

ADR-0001 *kararın neye bakacağını* söylüyordu (permission/claim, rol adı değil). Bu ADR
*izinlerin nasıl adlandırıldığını ve gruplandığını* sabitliyor.

## Gruplama üç eksende

Önekler tek bir mantıkla üretilmedi; üç ayrı eksen var ve karıştırılmamalıdır.

| Eksen | Önekler | Mantık |
|---|---|---|
| **Kaynağa göre** | `institution:` `student:` `company:` `internship:` `attendance:` `salary:` `document:` `communication:` `user:` | İznin dokunduğu **veri** neyse önek o. Çoğunluk burada. |
| **İşleve göre** | `coordinator:` `department:` | Veri değil **görev** adı. Koordinatörlük ve alan şefliği birer iş tanımıdır; dokundukları veri başka domainlerde durur. |
| **Katmana göre** | `platform:` | **Kurum üstü.** Asgari ücret ve 3308 oranları ulusal mevzuattır — okulun verisi değildir. Hiçbir okul rolünde bulunmaz. |

İşlev ve katman eksenleri bilinçli istisnadır: `coordinator:visit:manage` bir *ziyaret* iznidir
ama ziyaret verisi Coordination modülündedir ve izni taşıyan şey öğretmenin **görevi**dir.
`platform:` ise domain değil **katman** ayrımıdır; kurum verisiyle ulusal parametreyi ayırır.

## Önek seçim kuralı

Yeni bir izin tanımlanırken sıra şudur:

1. **Hedef kümeyi yaz** — bu izin hangi rollerde olmalı, hangilerinde olmamalı.
2. **Wildcard'lara bak** — aşağıdaki üretilmiş tabloda hangi rol hangi öneki yutuyor.
3. **Önek seç** — hedef kümenin *dışında* kalması gereken bir rolün wildcard'ı öneki yutuyorsa
   o önek **kullanılamaz**.
4. **Önek yetmiyorsa kapsama geç** — bkz. "İzin bitince kapsam başlıyor".

### Bu kuralın doğduğu vakalar

| Issue | Reddedilen önek | Neden |
|---|---|---|
| #126 | `department:distribution:all` | `department:*` alan şefinde de var — muafiyet ona da geçer, kapsam kontrolü hiç çalışmazdı |
| #130 | `department:coordination-config` | Aynı sebep — alan şefi doğrudan yazamadığı alanları kurum geneli parametreyle dolaylı etkilerdi |
| #172 | `company:` ve `department:` | Hüküm izni işletme yetkilisine ya da alan şefine geçerdi |
| #147 | Tüm kurum önekleri | Ulusal parametre kurum rollerine geçmemeli → `platform:` |

### Tersi de olur: wildcard bazen istenen sonucu verir

**#171'de** okulda staj dönem notunu alan şefi, müdür yardımcısı ve müdürün girmesi
kararlaştırıldı — `department:*` tam olarak bu üç roldedir. Önek önce `department:` seçildi.

Sonra sahibin kararıyla `institution:`'a taşındı: *"Resmî kuruma bağlı izinler kurumsal
olmalı."* Öğrenci okulda staj yaptığında **kurum, işverenin yerine geçer**; bu bir alan/bölüm
işi değildir. Taşıma kapsamı değiştirmedi (kapsam zaten `institution_id` claim'inden geliyordu),
**dağıtım yolunu** değiştirdi: `institution:*` yalnız müdürdedir, diğer iki rol izni artık
**açık satırla** alır.

> Bu ayrım tabloda `●` (açık satır) ve `○` (wildcard'dan) olarak görünür. **Açık satır silinirse
> izin kaybolur; wildcard'dan gelen kaybolmaz.**

## `platform:` dışında serbest önek yoktur

Kurum müdürü **on bir wildcard'ın hepsini** taşır. Sonuç: yeni tanımlanan her izin — hangi
önekte olursa olsun — okul müdürüne de gider.

Bu, "şu rol bu izni ALMASIN" gereksiniminin önekle karşılanamadığı durumları doğurur. #177'de
ücretli iznin **işletme onayı** adımı tam olarak buna takıldı: hangi önek seçilirse seçilsin
izin müdüre gidiyordu ve iki taraflı onay tek tarafa çöküyordu.

**Çözüm kapsamdır, izin değil** (ADR-0001): işletme adımı `business_id` claim'inin başvurunun
işletmesiyle eşleşmesini ister; müdürde o claim yoktur.

## İzin bitince kapsam başlıyor

İzin **erişimi** açar, "hangi verinin" sorusunu cevaplamaz. Beş kapsam kaynağı vardır ve
**hiçbiri istekten okunmaz**.

| Claim | Kapsam | Otorite |
|---|---|---|
| `institution_id` | Kurum | Kullanıcı kaydı |
| `business_id` | İşletme | Token claim'i (#60, #177) |
| `student_id` | Öğrencinin kendisi | Token claim'i |
| `branch_codes` | Alan (branş) — #126 | **`UserAccount.BranchCodes` kaydı**; token claim'i ezilir |
| `linked_student_ids` | Veli–öğrenci bağı — #174 | **`UserAccount.LinkedStudentIds` kaydı**; token yedeği YOK |

> **Token'ın imzalı olması, içeriğin kullanıcı tarafından belirlenmediği anlamına gelmez.**
> `branch_codes` ve `linked_student_ids` Keycloak'ta *unmanaged* özniteliktir — kullanıcı
> `manage-account` ile kendi Account konsolundan kendine değer ekleyebilirdi. Bu yüzden otorite
> kayıttadır, claim'de değil. İkinci katman: realm'de `unmanagedAttributePolicy: ADMIN_EDIT`.

## Ağaç

<!-- BEGIN generated: permission-matrix -->

> Bu bölüm **koddan üretilir** — elle düzenlenmez. Değişiklik için
> `Permissions.cs` / `RolePermissionMap.cs` düzenlenir ve
> `PermissionMatrixDocTests` çalıştırılır (sapma kırmızı testtir).

### Rol başına toplam izin

Wildcard'lar genişletilmiş hâliyle.

| Rol | İzin | Wildcard önekleri |
| --- | ---: | --- |
| Kurum Müdürü (`InstitutionManager`) | 78 | `attendance:*` `communication:*` `company:*` `coordinator:*` `department:*` `document:*` `institution:*` `internship:*` `salary:*` `student:*` `user:*` |
| Müdür Yardımcısı (`DeputyDirector`) | 44 | `department:*` `user:*` |
| Kurum Personeli (`InstitutionStaff`) | 18 | — |
| Alan Şefi (`DepartmentHead`) | 14 | `department:*` |
| Koordinatör Öğretmen (`Teacher`) | 23 | — |
| İşletme Yetkilisi (`CompanyManager`) | 14 | — |
| Usta Öğretici (`MasterTrainer`) | 10 | `communication:*` |
| İşletme İnsan Kaynakları (`CompanyHR`) | 8 | — |
| Öğrenci (`Student`) | 11 | — |
| Veli (`Parent`) | 10 | — |
| Sistem Yöneticisi (`SystemAdmin`) | 2 | — |

### Domainler

| Önek | İzin | `önek:*` wildcard'ını taşıyan roller |
| --- | ---: | --- |
| `institution:` | 9 | Kurum Müdürü |
| `student:` | 6 | Kurum Müdürü |
| `company:` | 10 | Kurum Müdürü |
| `internship:` | 9 | Kurum Müdürü |
| `attendance:` | 12 | Kurum Müdürü |
| `salary:` | 6 | Kurum Müdürü |
| `platform:` | 1 | — (her rol tek tek alır) |
| `coordinator:` | 5 | Kurum Müdürü |
| `department:` | 5 | Kurum Müdürü, Müdür Yardımcısı, Alan Şefi |
| `document:` | 6 | Kurum Müdürü |
| `communication:` | 4 | Kurum Müdürü, Usta Öğretici |
| `user:` | 6 | Kurum Müdürü, Müdür Yardımcısı |

### Tam matris

`●` rol haritasında **açık satır** · `○` **wildcard'dan** geliyor · `·` yok

Ayrım önemlidir: açık satır silinirse izin kaybolur, wildcard'dan gelen kaybolmaz.

| İzin | MÜD | MYRD | PERS | AŞEF | ÖĞRT | İŞYT | USTA | İİK | ÖĞRC | VELİ | SİSY |
| --- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **`institution:`** |  |  |  |  |  |  |  |  |  |  |  |
| `institution:view` | ○ | ● | ● | · | · | · | · | · | · | · | · |
| `institution:manage` | ○ | · | · | · | · | · | · | · | · | · | · |
| `institution:delete` | ○ | · | · | · | · | · | · | · | · | · | · |
| `institution:staff:manage` | ○ | · | · | · | · | · | · | · | · | · | · |
| `institution:report:view` | ○ | · | · | · | · | · | · | · | · | · | · |
| `institution:grade-window:manage` | ○ | · | · | · | · | · | · | · | · | · | · |
| `institution:distribution:all-branches` | ● | ● | · | · | · | · | · | · | · | · | · |
| `institution:coordination-config:manage` | ● | ● | · | · | · | · | · | · | · | · | · |
| `institution:school-grade:enter` | ● | ● | · | ● | · | · | · | · | · | · | · |
| **`student:`** |  |  |  |  |  |  |  |  |  |  |  |
| `student:view` | ○ | ● | ● | ● | ● | · | ● | ● | · | · | · |
| `student:manage` | ○ | ● | ● | · | ● | · | · | · | · | · | · |
| `student:view-own` | ○ | · | · | · | · | · | · | · | ● | ● | · |
| `student:update-own` | ○ | · | · | · | · | · | · | · | ● | · | · |
| `student:attendance:manage` | ○ | · | · | · | · | · | · | · | · | · | · |
| `student:salary:manage` | ○ | · | · | · | · | · | · | · | · | · | · |
| **`company:`** |  |  |  |  |  |  |  |  |  |  |  |
| `company:view` | ○ | ● | ● | · | · | ● | · | ● | · | · | · |
| `company:manage` | ○ | ● | · | · | · | ● | · | · | · | · | · |
| `company:document:manage` | ○ | ● | · | · | · | ● | · | · | · | · | · |
| `company:student:manage` | ○ | · | · | · | · | ● | · | · | · | · | · |
| `company:visit:manage` | ○ | · | · | · | · | · | · | · | · | · | · |
| `company:student:request` | ○ | · | · | · | · | ● | · | · | · | · | · |
| `company:attendance:manage` | ○ | · | · | · | · | ● | ● | ● | · | · | · |
| `company:receipt:upload` | ○ | · | · | · | · | ● | · | · | · | · | · |
| `company:trainer:manage` | ○ | · | · | · | · | ● | · | · | · | · | · |
| `company:grade:enter` | ○ | · | · | · | · | ● | ● | · | · | · | · |
| **`internship:`** |  |  |  |  |  |  |  |  |  |  |  |
| `internship:apply` | ○ | · | · | · | · | · | · | · | ● | · | · |
| `internship:view` | ○ | ● | ● | · | ● | · | · | · | · | · | · |
| `internship:review` | ○ | · | · | · | ● | · | · | · | · | · | · |
| `internship:approve` | ○ | ● | · | · | ● | · | · | · | · | · | · |
| `internship:approve:parent` | ○ | ● | · | · | ● | · | · | · | · | ● | · |
| `internship:view-own` | ○ | · | · | · | · | · | · | · | ● | ● | · |
| `internship:manage` | ○ | ● | · | · | · | · | · | · | · | · | · |
| `internship:contract:manage` | ○ | ● | · | · | · | · | · | · | · | · | · |
| `internship:report:manage` | ○ | · | · | · | · | · | · | · | · | · | · |
| **`attendance:`** |  |  |  |  |  |  |  |  |  |  |  |
| `attendance:view` | ○ | ● | ● | ● | ● | · | · | · | · | · | · |
| `attendance:view-own` | ○ | · | · | · | · | · | · | · | ● | ● | · |
| `attendance:manage` | ○ | ● | ● | · | ● | ● | ● | ● | · | · | · |
| `attendance:report` | ○ | ● | ● | ● | ● | · | · | · | · | · | · |
| `attendance:upload` | ● | ● | ● | · | ● | ● | ● | ● | ● | ● | · |
| `attendance:approve` | ○ | ● | · | · | ● | · | · | · | · | · | · |
| `attendance:direct-entry` | ● | ● | ● | · | ● | · | · | · | · | · | · |
| `attendance:health-report:direct` | ● | ● | · | · | ● | · | · | · | · | · | · |
| `attendance:leave:request` | ○ | · | · | · | · | · | · | · | ● | ● | · |
| `attendance:leave:business-approve` | ○ | · | · | · | · | ● | ● | ● | · | · | · |
| `attendance:leave:approve` | ● | ● | · | · | · | · | · | · | · | · | · |
| `attendance:delete` | ○ | · | · | ● | · | · | · | · | · | · | · |
| **`salary:`** |  |  |  |  |  |  |  |  |  |  |  |
| `salary:view` | ○ | ● | ● | · | ● | · | · | · | · | · | · |
| `salary:view-own` | ○ | · | · | · | · | · | · | · | ● | ● | · |
| `salary:calculate` | ○ | ● | ● | · | · | · | · | · | · | · | · |
| `salary:approve` | ○ | ● | · | · | ● | · | · | · | · | · | · |
| `salary:receipt:manage` | ○ | · | · | · | · | · | · | · | · | · | · |
| `salary:parameter:view` | ○ | ● | · | · | · | · | · | · | · | · | ● |
| **`platform:`** |  |  |  |  |  |  |  |  |  |  |  |
| `platform:parameter:manage` | · | · | · | · | · | · | · | · | · | · | ● |
| **`coordinator:`** |  |  |  |  |  |  |  |  |  |  |  |
| `coordinator:assign` | ○ | · | · | · | · | · | · | · | · | · | · |
| `coordinator:schedule:manage` | ○ | · | · | ● | ● | · | · | · | · | · | · |
| `coordinator:visit:manage` | ○ | · | · | · | ● | · | · | · | · | · | · |
| `coordinator:report:manage` | ○ | · | · | · | ● | · | · | · | · | · | · |
| `coordinator:communication` | ○ | · | · | · | ● | · | · | · | · | · | · |
| **`department:`** |  |  |  |  |  |  |  |  |  |  |  |
| `department:distribution:manage` | ○ | ○ | · | ○ | · | · | · | · | · | · | · |
| `department:workload:view` | ○ | ○ | · | ○ | · | · | · | · | · | · | · |
| `department:teacher:assign` | ○ | ○ | · | ○ | · | · | · | · | · | · | · |
| `department:schedule:view` | ○ | ○ | · | ○ | · | · | · | · | · | · | · |
| `department:weekly-visit:manage` | ○ | ○ | · | ○ | · | · | · | · | · | · | · |
| **`document:`** |  |  |  |  |  |  |  |  |  |  |  |
| `document:view` | ○ | ● | ● | ● | ● | · | · | · | · | · | · |
| `document:upload` | ○ | ● | ● | · | ● | · | · | · | · | · | · |
| `document:approve` | ○ | ● | · | · | · | · | · | · | · | · | · |
| `document:scan` | ○ | · | · | · | · | · | · | · | · | · | · |
| `document:verify` | ○ | ● | ● | · | · | · | · | · | · | · | · |
| `document:track` | ○ | ● | ● | · | · | · | · | · | · | · | · |
| **`communication:`** |  |  |  |  |  |  |  |  |  |  |  |
| `communication:send` | ○ | ● | ● | ● | ● | ● | ○ | ● | ● | ● | · |
| `communication:view` | ○ | ● | ● | ● | ● | ● | ○ | ● | ● | ● | · |
| `communication:issue:report` | ○ | · | · | · | · | · | ○ | · | ● | ● | · |
| `communication:issue:manage` | ○ | · | · | · | · | · | ○ | · | · | · | · |
| **`user:`** |  |  |  |  |  |  |  |  |  |  |  |
| `user:view` | ○ | ○ | · | · | · | · | · | · | · | · | · |
| `user:create` | ○ | ○ | · | · | · | · | · | · | · | · | · |
| `user:update` | ○ | ○ | · | · | · | · | · | · | · | · | · |
| `user:delete` | ○ | ○ | · | · | · | · | · | · | · | · | · |
| `user:roles:manage` | ○ | ○ | · | · | · | · | · | · | · | · | · |
| `user:approve` | ○ | ○ | · | · | · | · | · | · | · | · | · |

### Bireysel (direct) atanamayan izinler

Hiçbir yapılandırmayla tek bir kullanıcıya verilemez —
`AssignablePermissionScope.NeverDirectlyAssignable` sabit listesi yapılandırmayı ezer.

- `attendance:direct-entry`
- `attendance:health-report:direct`
- `attendance:leave:approve`
- `institution:distribution:all-branches`
- `platform:parameter:manage`

<!-- END generated: permission-matrix -->

## Sonuçlar

**İyi:**

- Önek seçimi artık bir kontrol listesi; "hangi wildcard yutuyor" sorusu tabloya bakılarak
  cevaplanıyor
- Tablo koddan üretiliyor ve testle kilitli — referans kaynağın çürümesi kırmızı test
- `●`/`○` ayrımı, açık satırların silinmesinin sonucunu görünür kılıyor

**Bedeli:**

- Yeni izin eklerken ADR'nin üretilmiş bloğu da güncellenmeli (test hatırlatır ve doğru metni
  dosyaya yazar)
- Üç eksenli gruplama tek bir kuralla açıklanamıyor; `coordinator:` ve `department:` öneklerinin
  neden "veri" olmadığı her yeni gelene anlatılmalı

**Kabul edilen:**

- Kurum müdürünün her wildcard'ı taşıması **kaldırılmadı** — okul müdürünün kurumdaki her şeye
  yetkili olması istenen sonuçtur. Bedeli, "müdür hariç" gereksiniminin izinle ifade
  edilememesidir; o gereksinim kapsamla çözülür.

## İhlal tespiti

| Kontrol | Test |
|---|---|
| Rol modeli sapması (rol listesi ↔ izin haritası ↔ atanabilir kapsam ↔ realm JSON) | `RoleModelDriftTests` |
| Bu ADR'deki matris ↔ kod | `PermissionMatrixDocTests` |
| Hüküm izinleri önek tuzağı (#172) | `AttendanceDirectEntryMappingTests` |
| İki taraflı onay kapsamı (#177) | `PaidLeaveApprovalMappingTests`, `PaidLeaveApprovalPolicyTests` |
| Okulda staj notu önek kararı (#171) | `SchoolTermGradeMappingTests`, `SchoolTermGradeEndpointAuthorizationTests` |
| Veli kapsamı (#174) | `ParentScopeTests` |
| "Kendi verisi" kapsam merdiveni (#182) | `OwnDataScopeTests` |
| Alan kapsamı muafiyeti (#126) | `BranchScopeExemptionMappingTests`, `BranchScopeGuardTests` |

## İlgili

- [ADR-0001: Yetkilendirme permission bazlıdır](./adr-0001-yetkilendirme-permission-bazli.md)
- `src/Docs/docs/actors/permissions.md` — izin matrisinin anlatımlı hâli, karar gerekçeleri
- `src/MESNET.Common.Shared/Security/` — `Permissions.cs`, `RolePermissionMap.cs`,
  `AssignablePermissionScope.cs`
