---
title: 'ADR-0001: Yetkilendirme permission bazlıdır'
---

# ADR-0001: Yetkilendirme permission/claim bazlıdır, rol bazlı DEĞİLDİR

| | |
|---|---|
| **Durum** | Kabul edildi |
| **Tarih** | 01.08.2026 |
| **Karar sahibi** | Proje sahibi |
| **İlgili** | #126, #129, #130, #147 · ADR-0002 · `src/Docs/docs/actors/permissions.md` |

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
- ✅ **`IsInRole` hiçbir modül kodunda yok** ve frontend'de rol-adı computed'ı kalmadı.
  Borç listesi #172, #184 ve #192 ile kapandı:

| Yer | Neydi | Ne oldu |
|---|---|---|
| `MarkAttendanceHandler` | Devamsızlığı işletme mi girdi (onay bekler) yoksa okul mu | `attendance:direct-entry` izni (#172) |
| `PlacementQueryScope` | Öğretmeni koordine ettiği öğrencilere daraltır | Kapsam merdiveni: `institution:view` → `business_id` claim'i → öğretmen **kaydı** → boş (#184) |
| `PlacementQueryScope` | `CompanyManager`'ı kendi işletmesine daraltır | Rol değil **claim**: `business_id` varsa kapsam odur (#184) |
| `auth.ts` (`isManager`) | Alan seçicisini gösterir | Kaldırıldı — tüketicisi yoktu; kapsam kararı `canManageAllBranches` (#192) |
| `auth.ts` (`isDepartmentHead`) → `TeacherSchedulePage` | Alanı otomatik atar | `writableBranchCodes`; eski koşul `branchCode` `null` olduğu için **hiç tutmuyordu** (#192) |

**Yeni rol-adı kontrolü eklenmez.**

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

## İlgili

- [ADR-0002: İzin ağacı — gruplama ekseni ve önek seçimi](./adr-0002-izin-agaci-ve-onek-secimi.md)
  — bu ADR *neye bakılacağını* söyler; ADR-0002 izinlerin **nasıl adlandırıldığını ve
  gruplandığını** sabitler ve önek seçiminin neden bir güvenlik kararı olduğunu anlatır.
  Koddan üretilen tam izin matrisi oradadır.
