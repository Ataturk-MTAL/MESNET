---
title: 'ADR-0001: Yetkilendirme permission bazlıdır'
---

# ADR-0001: Yetkilendirme permission/claim bazlıdır, rol bazlı DEĞİLDİR

| | |
|---|---|
| **Durum** | Kabul edildi |
| **Tarih** | 01.08.2026 |
| **Karar sahibi** | Proje sahibi |
| **İlgili** | #126, #129, #130, #147 · `src/Docs/docs/actors/permissions.md` |

## Karar

> **Sistemdeki hiçbir erişim kararı rol adına bakmaz.**
> Karar her zaman **permission** (izin) veya **claim** (token/kayıt özniteliği) üzerinden verilir.
> Roller yalnızca bir permission demetine verilen isimdir.

Sahibin ifadesi: *"Roller gelir geçer ama permission/claim baki kalır."*

## Bağlam

Rol adı, organizasyonun bugünkü şemasının bir fotoğrafıdır ve o şema değişir:

- **#129** — "müdür yardımcısı" demeti aslında `InstitutionStaff` rolünün içinde duruyordu.
  Ayrı role (`DeputyDirector`) çıkarıldı. Rol adına bakan her kontrol o gün ya kırıldı ya
  elle genişletildi: `PlacementQueryScope` üç ayrı `IsInRole` çağrısıyla yamalandı, frontend
  `isManager` üç rol adının birleşimine döndü.
- **#147** — `SystemAdmin` rolü "ulusal" varsayımıyla adlandırıldı; karar sonradan "il yönetimi
  girer" oldu. Ad karara uymuyor, ama izin (`platform:parameter:manage`) doğru kaldı.

Her iki olayda da **izinler ayakta kaldı, adlar kaydı**. Kural bundan çıktı.

## Kesin kurallar

1. **Uç noktalar** `RequireAuthorization(Permissions.X.Y)` ile korunur. `RequireRole` kullanılmaz.
2. **Handler içi karar** gerekiyorsa `ICurrentUserService.HasPermission(...)` kullanılır.
   `IsInRole(...)` yeni kodda kullanılmaz.
3. **Frontend** buton/menü görünürlüğü `hasPermission()` / `hasAnyPermission()` ile belirlenir,
   `user.roles.includes(...)` ile değil.
4. **Yeni bir yetki gerektiğinde yeni permission tanımlanır** ve ilgili rollerin listesine
   eklenir. Koda rol adı gömülmez.
5. Rol → permission eşleşmesi **tek yerdedir**: `src/MESNET.Common.Shared/Security/RolePermissionMap.cs`.
   Kaynak doküman: `docs/actors/permissions.md`.

### Aynı izin = aynı yetki

Aynı permission'a sahip iki rol aynı işi yapabilir; ikisini ayırmaya çalışan ek bir rol kontrolü
yazılmaz. Örnek: işletme koordinatörlük saati takdiri `department:distribution:manage` ister;
bu izin `department:*` yoluyla `InstitutionManager`, `DeputyDirector` ve `DepartmentHead`
rollerinin üçünde de vardır — üçü de tam yetkilidir (#129).

### Wildcard önek tuzağı (kritik)

`RolePermissionMap` wildcard destekler (`department:*` → `department:` ile başlayan her izin).
Bu, **izin adının önekini bir güvenlik kararı yapar**:

- Bir izni belirli bir rolden **uzak tutmak** istiyorsan, o rolün wildcard'ının altına
  düşmeyen bir önek seçmelisin.
- Okul rollerinin tuttuğu wildcard önekleri: `institution:`, `student:`, `internship:`,
  `attendance:`, `salary:`, `document:`, `communication:`, `user:`, `coordinator:`,
  `department:`, `company:`.
- Bu yüzden ulusal parametre izni `platform:parameter:manage` adını taşır — `salary:` ya da
  `institution:` olsaydı `salary:*` / `institution:*` yoluyla her okul müdürüne **sessizce**
  geçerdi (#147).
- Aynı sebeple alan kapsamı muafiyeti `department:` önekiyle adlandırılamaz (#126).
  Kilitleyen test: `tests/MESNET.Coordination.UnitTests/BranchScopeExemptionMappingTests.cs`.

**Sessizce geçen izin, hiç konmamış kontrolden daha tehlikelidir** — kod doğru görünür,
davranış yanlıştır.

## Permission erişimi açar, KAPSAMI belirlemez

İki ayrı sorudur ve ikisi de rol adına bakmaz:

| Soru | Nereden okunur |
|---|---|
| *Bu işlemi yapabilir mi?* | permission |
| *Hangi kurumun verisi?* | `institution_id` claim'i |
| *Hangi alanın (branş) verisi?* | `UserAccount.BranchCodes` kaydı (otoriter), `branch_codes` claim'i değil |

Kapsam **istekten alınmaz**. Satır bazlı uçlarda kapsam istekten değil **çözümlenmiş satırdan**
okunur. Ayrıntı: `docs/actors/permissions.md` → "Alan (Branş) Kapsamı Kontrolü".

> ⚠ `UserAccount.BranchCodes` otoriterdir, token claim'i değil. `branch_codes` Keycloak'ta
> *unmanaged* özniteliktir; politika `ENABLED` olsaydı kullanıcı `manage-account` ile kendine
> alan ekleyip kapsamı aşabilirdi. **Token'ın imzalı olması, içeriğin kullanıcı tarafından
> belirlenmediği anlamına gelmez.**

## Bugünkü uyum durumu (01.08.2026)

`RequireRole` ve `IsInRole` taraması:

- ✅ **`RequireRole` hiçbir uçta yok.** Kural 1 tam uygulanıyor.
- ⚠ **`IsInRole` üç yerde** + frontend'de iki rol-adı computed'ı. Hepsi bilinen teknik borçtur:

| Yer | Ne yapıyor | Doğru çözüm |
|---|---|---|
| `src/Modules/Attendance/MESNET.Attendance.Application/Handlers/MarkAttendanceHandler.cs:61-62` | Devamsızlığı **işletme mi girdi** (onay bekler) yoksa okul mu (doğrudan kayıt) | Yeni izin, okul wildcard'larının **dışında** bir önekle (ör. `workplace:attendance:self-report`); yalnız `CompanyManager` + `MasterTrainer` alır. `company:` **kullanılamaz** — `company:*` okul müdüründe var |
| `src/Modules/Enrollment/MESNET.Enrollment.Application/Handlers/PlacementQueryScope.cs:26-30` | Öğretmeni yalnız koordine ettiği öğrencilere daraltır | Kurum geneli görüş için muafiyet izni (ör. `institution:placement:all-students`) — `Institution.AllBranches` deseninin aynısı: `InstitutionManager`'a `institution:*` ile, `DeputyDirector` + `InstitutionStaff`'a açıkça verilir |
| `src/Modules/Enrollment/MESNET.Enrollment.Application/Handlers/PlacementQueryScope.cs:39` | `CompanyManager`'ı kendi işletmesine daraltır | Rol değil **claim**: `business_id` claim'inin varlığı zaten kapsamdır |
| `src/WebUI/src/stores/auth.ts:82-86` (`isManager`) | Alan seçicisini gösterir | `canManageAllBranches` (permission bazlı, mevcut) |
| `src/WebUI/src/stores/auth.ts:89-91` (`isDepartmentHead`) → `pages/coordination/TeacherSchedulePage.vue:516` | Alanı otomatik atar | `!canManageAllBranches && writableBranchCodes.length === 1` |

Bu satırlar düzeltilene kadar **yeni rol-adı kontrolü eklenmez**; borç büyütülmez.

## Sonuçlar

**Kazanç:** organizasyon şeması değiştiğinde (rol bölünmesi, yeniden adlandırma, yeni aktör)
kod değişmez — `RolePermissionMap` değişir. #129'da ayrı role çıkarma kod değişikliği
gerektirdiyse, sebebi tam olarak kalan rol-adı kontrolleriydi.

**Bedel:** izin sayısı rol sayısından hızlı büyür ve `permissions.md` ile `RolePermissionMap.cs`
eşzamanlı tutulmalıdır. İzin adı seçimi (önek) artık bir **güvenlik kararıdır**, isimlendirme
zevki değil.

**Reddedilen seçenek — "kritik yerlerde rol kontrolü de olsun" (defense in depth):** iki
otorite iki farklı cevap verir; hangisinin kazandığı okuyana göre değişir. Rol yeniden
adlandırıldığında rol kontrolü **sessizce false döner** ve kimse fark etmez. Tek otorite:
permission.

## İhlal tespiti

```bash
grep -rn "RequireRole"  src --include='*.cs'                        # boş dönmeli
grep -rn "IsInRole"     src --include='*.cs'                        # yalnız yukarıdaki borç satırları
grep -rn "roles.includes\|roles.some" src/WebUI/src --include='*.ts' --include='*.vue'
```

Yeni bir eşleşme çıkarsa, ilgili PR'da ya permission'a taşınır ya da bu ADR'ye gerekçesiyle
yazılır. Üçüncü seçenek yok.
