---
title: İzin Matrisi
---

# MESNET İzin Yapılandırması

> **Not:** Blockchain, NFT ve Tenant ile ilgili tüm izinler, roller ve policy'ler Phase 2 kapsamındadır.
> Phase 1'de bu izinler implementasyona alınmayacaktır.

## Yetkilendirme İlkesi (mimari karar)

**Tüm yetkilendirme permission bazlıdır, rol bazlı değildir.** Rol, bir permission demetine
verilen isimden ibarettir; erişim kararı her zaman permission'a bakar.

**Gerekçe:** MEB okullarında aynı işi birden çok unvan yapabiliyor — işletme koordinatörlük
saatini okul müdürü de, yetkili müdür yardımcısı da, alan şefi de takdir edebilir. Rol adına
göre yazılmış bir kontrol her yeni unvanda koda dokunmayı gerektirir; permission'a göre
yazılmış kontrol yalnız rol→permission haritasına satır eklemeyi gerektirir.

**Sonuçları:**

- Uç noktalar `RequireAuthorization(Permissions.X.Y)` ile korunur; `RequireRole` kullanılmaz
- Handler içi kararlarda `ICurrentUserService.HasPermission(...)` kullanılır
- Frontend'de buton/menü görünürlüğü permission'a bakar, rol adına değil
- Rol → permission eşleşmesi tek yerde tutulur: `src/MESNET.Common.Shared/Security/RolePermissionMap.cs`
  (wildcard destekli, ör. `department:*` → `department:` ile başlayan tüm izinler)
- Yeni yetki gerektiğinde yeni permission tanımlanır ve ilgili rollerin listesine eklenir;
  koda rol adı gömülmez

**Aynı permission'a sahip roller aynı işi yapar.** İşletme koordinatörlük saati takdiri
`department:distribution:manage` ister; bu izin üç rolde de `department:*` ile bulunur:

| Rol | Karşılığı | `department:*` |
|---|---|---|
| `InstitutionManager` | Okul müdürü | ✅ |
| `DeputyDirector` | Müdür yardımcısı | ✅ |
| `DepartmentHead` | Alan şefi | ✅ |

> `InstitutionStaff` (kurum yetkilendirdiği personel) bu üçlüye **dahil değildir** (#129):
> koordinasyon dağıtımı onun görevi değildir, `department:*` almaz.

Üçü de **yetkilidir**; farkı yaratan **kapsamdır** — alan şefi yalnız kendi alan(lar)ına
yazabilir (#126).

> **Tam izin ağacı ve önek seçim kuralı:** [ADR-0002](../architecture/adr-0002-izin-agaci-ve-onek-secimi.md)
> — koddan üretilen matris (hangi izin hangi rolde, açık satırla mı wildcard'la mı) ve yeni izin
> tanımlarken izlenecek dört adım. Bu doküman gerekçeleri, ADR-0002 haritayı taşır.

### Permission erişimi açar, kapsamı belirlemez

"Hangi kurumun/alanın verisi" sorusu ayrı bir kontroldür ve permission ile karıştırılmamalıdır.

- **Kurum kapsamı:** `institution_id` claim'i, **kullanıcı kaydından üretilir** — token'dan
  gelen değer hiç kabul edilmez (ADR-0003 adım 2).
  Ayrıntı: [Kurum (Kiracı) Kapsamı](#kurum-kiracı-kapsamı)
- **Alan (branş) kapsamı:** `branch_codes` token claim'inden okunur (#126).
  `ICurrentUserService.GetBranchCodes()` taşır; koordinasyon **yazma** handler'ları
  `BranchScopeGuard` ile kontrol eder. Ayrıntı: [Alan (Branş) Kapsamı Kontrolü](#alan-branş-kapsamı-kontrolü)

### Bilinen istisna KALMADI (#172, #184, #192)

Borç listesi kapandı: **rol adına bakan kapsam kararı yoktur.** `IsInRole` çağrısı modül
kodunun tamamında bulunmaz (arayüz `ICurrentUserService`'te durur, çağıranı yoktur) ve
frontend'de rol adına bakan computed kalmadı.

| Eski borç | Yerine geçen | Kapatan |
|---|---|---|
| `MarkAttendanceHandler` işletme girişini `IsInRole(CompanyManager \|\| MasterTrainer)` ile ayırıyordu | `attendance:direct-entry` izni — bkz. [Sağlık Raporu Onay Zinciri](#sağlık-raporu-onay-zinciri--giriş-geniş-hüküm-dar-172) | #172 |
| `PlacementQueryScope` üç ayrı `IsInRole` çağrısıyla yamalanmıştı | Saf `PlacementScopePolicy` — kapsam merdiveni: `institution:view` → `business_id` claim'i → öğretmen kaydı → **boş** | #184 |
| `auth.ts` → `isDepartmentHead` / `isManager` computed'ları | `canManageAllBranches` / `writableBranchCodes` (permission bazlı) | #126, #192 |

`isDepartmentHead` ve `isManager` **kaldırıldı** — ikisinin de tüketicisi kalmamıştı;
`TeacherSchedulePage` alan ön-seçimindeki `isDepartmentHead && user.branchCode` koşulu
`branchCode` #126 ile `null` atandığından zaten **hiç tutmuyordu**.

Kilitleyen testler: `tests/MESNET.Enrollment.UnitTests/PlacementScopePolicyTests.cs`,
`tests/MESNET.Security.UnitTests/RoleNameScopeDriftTests.cs`,
`src/WebUI/src/stores/roleNameScope.spec.ts` (yasak desen taraması).

## Ana Roller ve İzinler

### Temel Roller

Phase 1'de **11 realm rolü** vardır. Tek doğruluk kaynağı
`src/MESNET.Common.Shared/Security/MesnetRoles.cs`'tir; Keycloak realm tanımı
(`src/MESNET.AppHost/keycloak/mesnet-realm.json` → `roles.realm`) ve rol-izin haritası
(`RolePermissionMap.cs`) bu listeyle **birebir** aynı olmak zorundadır — sapmayı kilitleyen
testler: `tests/MESNET.Security.UnitTests/RoleModelDriftTests.cs`.

| Rol | Türkçe etiket | Karşılık gelen aktör |
|---|---|---|
| `InstitutionManager` | Kurum Müdürü | Kurum Müdürü |
| `DeputyDirector` | Müdür Yardımcısı | Müdür Yardımcısı |
| `InstitutionStaff` | Kurum Personeli | Kurum Yetkilendirdiği Personel |
| `DepartmentHead` | Alan Şefi | Alan Şefi (Bölüm Başkanı) |
| `Teacher` | Koordinatör Öğretmen | Öğretmen (Koordinatör) |
| `CompanyManager` | İşletme Yetkilisi | İşletme Yöneticisi |
| `MasterTrainer` | Usta Öğretici | Usta Öğretici |
| `CompanyHR` | İşletme İnsan Kaynakları | İşletme İnsan Kaynakları |
| `Student` | Öğrenci | Öğrenci |
| `Parent` | Veli | Veli (#174) |
| `SystemAdmin` | Sistem Yöneticisi | — (ulusal parametre girişi, geçici taşıyıcı — #147) |

`SystemAdmin` bir okul aktörü **değildir**: kurum verisine hiçbir yetkisi yoktur, yalnız
`platform:parameter:manage` ve `salary:parameter:view` taşır. Bkz.
[Ulusal (Platform) Parametreler](#ulusal-platform-parametreler--bakanlık-katmanı-147).

Velinin niteliği: **girdiği kayıt doğrudan hüküm doğurmaz.** Sağlık raporu dahil her girişi
koordinatör öğretmen onayından geçer (#172). Okul tarafı — koordinatör öğretmen, müdür
yardımcısı, müdür — aynı kaydı **onaysız** girebilir; onay zaten kendilerinde biter.

#### `CompanyHR` neden ayrı rol (#172)

Sahibin kuralı sağlık raporunu girebilecek tarafları sayarken işletme insan kaynaklarını
işletme yöneticisinden ayrı andı. Aynı rolde bırakılsaydı İK, yöneticinin demetini
(öğrenci talebi, dekont yükleme, işletme belge yönetimi, dönem notu girişi) de alırdı ve
"kim ne yapabilir" denetlenemez hâle gelirdi. Demeti dar tutuldu: işletme görüntüleme,
devam çizelgesi, devamsızlık girişi, sağlık raporu yükleme, iletişim.

Hüküm izinleri (`attendance:direct-entry`, `attendance:health-report:direct`) bu rolde
**yoktur** — girdiği her kayıt onaya düşer.

> **`CompanyHR` zorunlu değildir.** Her işletmede ayrı bir İK bulunmaz. İşletme sahibi aynı
> zamanda usta öğretici olabilir, ya da sahip/yönetici ile usta öğretici farklı kişiler
> olabilir. Bu yüzden sağlık raporunu **usta öğretici tek başına da girebilir**
> (`attendance:upload` demetindedir); İK rolü yalnız ayrı bir İK personeli varsa atanır.
>
> Bir kullanıcının izinleri rollerinin **birleşimidir**. İşletme rollerinin hiçbirinde hüküm
> izni olmadığı için birleşimleri de üretmez: "hem sahip hem usta öğretici" olan kullanıcı da
> raporu onaya düşürür. Kilitleyen test:
> `AttendanceDirectEntryMappingTests.Isletme_rolleri_birlestiginde_de_hukum_izni_dogmaz`.

> **Türkçe etiketler koda gömülü değildir.** `MesnetRoles.Catalog` her rol için ad + etiket +
> açıklama taşır ve `GET /api/security/roles` bunları döndürür; arayüz kendi rol listesini ya da
> etiket haritasını tutmaz. SmartEnum `Name`/`Slug` deseninin aynısıdır: `Name` İngilizce ve
> serialize edilir, etiket yalnız gösterimdir.

#### `DeputyDirector` ve `MasterTrainer` neden eklendi (#129)

Arayüzdeki rol listesi gerçek rollerle eşleşmiyordu: `deputy_director`, `coordinator_teacher`,
`master_trainer` değerlerinin sistemde karşılığı yoktu. Karşılığı olmayan bir rol adı Keycloak'ta
çözülemediği için kullanıcı **sıfır realm rolüyle** açılıyor, hiçbir izin alamıyor ve hata da
görmüyordu. Kalıcı çözüm iki yönlüdür:

1. **Model gerçeğe uyduruldu:** müdür yardımcısı ve usta öğretici birer aktördü ama rolleri yoktu.
   `InstitutionManager` realm açıklaması müdür yardımcısını kendine sayıyor, `RolePermissionMap`
   yorumu `InstitutionStaff`'a sayıyordu — çelişki ayrı rollerle çözüldü.
2. **Sunucu artık tanımadığı rolü reddediyor:** `SecurityErrors.InvalidRole` ile 422; Keycloak'ta
   çözülemeyen rol adı **sessizce başarı dönmüyor**.

**`InstitutionStaff` daraltıldı.** Eski demeti (`user:*`, `department:*`, kapsam muafiyeti, tüm
onaylar) gerçekte müdür yardımcısının demetiydi ve `DeputyDirector`'e taşındı. `InstitutionStaff`
artık actors.md'deki "Kurum Yetkilendirdiği Personel" sorumluluklarıdır: öğrenci kayıt işlemleri,
belge doğrulama, devamsızlık takibi, maaş hesaplamaları — **yürütür, onaylamaz**.

> ⚠️ **Mevcut kurulumlarda etki:** bugüne kadar `InstitutionStaff` atanmış bir müdür yardımcısı,
> rolü `DeputyDirector` olarak güncellenene kadar onay/kullanıcı yönetimi yetkilerini kaybeder.
> Kimin müdür yardımcısı kimin personel olduğu okulun bilgisidir; **kod tahmin etmez ve otomatik
> düzeltmez**. Tespit yolu: `GET /api/security/role-integrity` (aşağıda).

```json
{
  "roles": [
    { "name": "InstitutionManager", "description": "Okul müdürü" },
    { "name": "DeputyDirector", "description": "Müdür yardımcısı" },
    { "name": "InstitutionStaff", "description": "Kurum personeli" },
    { "name": "DepartmentHead", "description": "Alan şefi" },
    { "name": "Teacher", "description": "Koordinatör öğretmen" },
    { "name": "CompanyManager", "description": "İşletme yetkilisi" },
    { "name": "MasterTrainer", "description": "Usta öğretici" },
    { "name": "CompanyHR", "description": "İşletme insan kaynakları" },
    { "name": "Student", "description": "Öğrenci" }
  ],
  "_comment_phase2": "TenantAdmin ve BlockchainAdmin Phase 2'dedir; realm'de tanımlı değildir."
}
```

#### `DeputyDirector` izin demetinin gerekçesi

Kaynak: actors.md → "Müdür Yardımcısı" (staj işlemleri koordinasyonu, evrak takibi ve onayı,
öğretmen görevlendirmeleri, dekont ve maaş süreçleri yönetimi).

| İzin | Gerekçe |
|---|---|
| `user:*` | Davet onay zinciri müdür yardımcısındadır; kullanıcı ve rol yönetimi yapar |
| `department:*` | Öğretmen görevlendirmeleri + işletme dağıtımı |
| `institution:distribution:all-branches` | Kurum geneli koordinasyon — tek alanla sınırlı değildir (#126) |
| `institution:coordination-config:manage` | Kurum geneli koordinasyon yapılandırması — mevzuat türevi, kurum düzeyi ayar (#130) |
| `internship:*` (view/manage/approve/contract) | Staj işlemleri koordinasyonu ve fesih onay zinciri |
| `document:approve`, `document:verify`, `document:track` | Evrak takibi **ve onayı** |
| `salary:approve`, `salary:parameter:view` | Dekont onay zinciri; asgari ücreti **görür, değiştiremez** (#147) |
| `attendance:approve` | Devamsızlık onayı |

`InstitutionStaff` ile farkı: personelde `user:*`, `department:*`, kapsam muafiyeti, kurum
geneli koordinasyon yapılandırması ve `*:approve` izinlerinin **hiçbiri yoktur**; personel
kaydı girer ve hesaplar, kararı müdür yardımcısı verir.

#### `MasterTrainer` izin demetinin gerekçesi

Usta öğretici işletmede öğrencinin eğitiminden sorumludur; işletmenin **yönetiminden** değil.
Demet bilinçli olarak dardır:

| İzin | Gerekçe |
|---|---|
| `company:attendance:manage` | Devam takibi |
| `company:grade:enter` | Dönem notu girişi |
| `student:view` | Kendi öğrencilerini görüntüleme |
| `attendance:manage` | Devamsızlık girişi — kaydı `Pending` başlar (#172) |
| `attendance:upload` | Sağlık raporu tarayıp girme — onaya düşer (#172) |
| `communication:*` | Kurum ve koordinatörle iletişim |

**ALMAZ:** `company:student:request` (öğrenci talebi), `company:receipt:upload` (dekont),
`company:document:manage` (işletme belgeleri), `company:manage`. Bunlar `CompanyManager`'da kalır.
Hüküm izinleri (`attendance:direct-entry`, `attendance:health-report:direct`) de **almaz**.

> `attendance:manage` #172'de eklendi. Devamsızlık girişi ucu o izni ister; satır yokken usta
> öğretici `POST /api/attendance`'a hiç ulaşamıyordu — oysa `MarkAttendanceHandler` onu giriş
> yapan işletme aktörü sayıp kaydını `Pending`'e düşürüyordu.

### İzin Sabitleri (.NET)

```csharp
public static class Permissions 
{
    // Tenant İzinleri (Phase 2)
    // public static class Tenant
    // {
    //     public const string View = "tenant:view";
    //     public const string Manage = "tenant:manage";
    //     public const string Configure = "tenant:configure";
    //     public const string ReportView = "tenant:report:view";
    //     public const string SetParameter = "tenant:parameter:set";
    // }

    // Kurum İzinleri
    public static class Institution
    {
        public const string View = "institution:view";
        public const string Manage = "institution:manage";
        public const string Delete = "institution:delete";
        public const string Staff = "institution:staff:manage";
        public const string Report = "institution:report:view";
        // Kurum genelinde tüm alanların koordinasyon verisine yazma muafiyeti (#126).
        // "department:" öneki KULLANILMAZ — DepartmentHead'in department:* wildcard'ı
        // muafiyeti ona da verirdi ve kapsam kontrolü hiç çalışmazdı.
        public const string AllBranches = "institution:distribution:all-branches";
        // Kurum geneli koordinasyon yapılandırmasını DEĞİŞTİRME yetkisi (#130):
        // mesafe-saat kuralları, büyükşehir sınırı, azami haftalık ek ders saati.
        // Aynı wildcard kuralı geçerli — "department:" öneki KULLANILMAZ.
        public const string CoordinationConfigManage = "institution:coordination-config:manage";
        // Okulda staj dönem notu girişi (#171) — okulda staj kurumun işidir. Müdür
        // "institution:*" ile, müdür yardımcısı ve alan şefi AÇIK SATIRLA alır.
        public const string SchoolGradeEnter = "institution:school-grade:enter";
    }

    // Öğrenci İzinleri
    public static class Student
    {
        public const string View = "student:view";
        public const string Manage = "student:manage";
        public const string ViewOwn = "student:view-own";
        public const string UpdateOwn = "student:update-own";
        public const string Attendance = "student:attendance:manage";
        public const string Salary = "student:salary:manage";
    }

    // Protokol İzinleri
    public static class Protocol 
    {
        public const string View = "protocol:view";
        public const string Create = "protocol:create";
        public const string Approve = "protocol:approve";
        public const string Manage = "protocol:manage";
        public const string Program = "protocol:program:manage";
    }

    // Blockchain İzinleri (Phase 2)
    // public static class Blockchain
    // {
    //     public const string View = "blockchain:view";
    //     public const string Mint = "blockchain:mint";
    //     public const string Deploy = "blockchain:deploy";
    //     public const string Manage = "blockchain:manage";
    //     public const string Monitor = "blockchain:monitor";
    // }

    // Sertifika İzinleri (Phase 2 - NFT kısmı)
    // public static class Certificate
    // {
    //     public const string View = "certificate:view";
    //     public const string Prepare = "certificate:prepare";
    //     public const string Approve = "certificate:approve";
    //     public const string Mint = "certificate:mint";
    //     public const string Validate = "certificate:validate";
    // }

    // İşletme İzinleri
    public static class Company
    {
        public const string View = "company:view";
        public const string Manage = "company:manage";
        public const string Document = "company:document:manage";
        public const string Student = "company:student:manage";
        public const string Visit = "company:visit:manage";
        public const string RequestStudent = "company:student:request";
        public const string Attendance = "company:attendance:manage";
        public const string UploadReceipt = "company:receipt:upload";
        public const string MasterTrainer = "company:trainer:manage";
    }

    // Staj İzinleri
    public static class Internship
    {
        public const string Apply = "internship:apply";
        public const string Review = "internship:review";
        public const string Approve = "internship:approve";
        public const string ViewOwn = "internship:view-own";
        public const string Manage = "internship:manage";
        public const string Contract = "internship:contract:manage";
        public const string Report = "internship:report:manage";
    }

    // Devamsızlık İzinleri
    public static class Attendance
    {
        public const string View = "attendance:view";
        public const string ViewOwn = "attendance:view-own";
        public const string Manage = "attendance:manage";
        public const string Report = "attendance:report";
        public const string Upload = "attendance:upload";
        public const string Approve = "attendance:approve";
        // Hüküm izinleri (#172) — girilen kaydın onay beklememesi.
        public const string DirectEntry = "attendance:direct-entry";
        public const string HealthReportDirect = "attendance:health-report:direct";
        // Ücretli izin onay zinciri (#177) — işletme adımını izin değil business_id kapsamı bağlar.
        public const string LeaveRequest = "attendance:leave:request";
        public const string LeaveBusinessApprove = "attendance:leave:business-approve";
        public const string LeaveApprove = "attendance:leave:approve";
    }

    // Maaş İzinleri
    public static class Salary
    {
        public const string View = "salary:view";
        public const string ViewOwn = "salary:view-own";
        public const string Calculate = "salary:calculate";
        public const string Approve = "salary:approve";
        public const string Receipt = "salary:receipt:manage";
        // Asgari ücret/oranları GÖRÜNTÜLEME. Yazma ulusal izindir (#147) → Platform.ParameterManage
        public const string ParameterView = "salary:parameter:view";
    }

    // Ulusal (kurum üstü) izinler (#147). Hiçbir okul rolünde yoktur.
    public static class Platform
    {
        public const string ParameterManage = "platform:parameter:manage";
    }

    // Koordinatör İzinleri
    public static class Coordinator
    {
        public const string Assign = "coordinator:assign";
        public const string Schedule = "coordinator:schedule:manage";
        public const string Visit = "coordinator:visit:manage";
        public const string Report = "coordinator:report:manage";
        public const string Communication = "coordinator:communication";
    }

    // Alan Şefi İzinleri
    public static class DepartmentHead
    {
        public const string Distribution = "department:distribution:manage";
        public const string Workload = "department:workload:view";
        public const string TeacherAssign = "department:teacher:assign";
        public const string ScheduleView = "department:schedule:view";
    }

    // Evrak İzinleri
    public static class Document
    {
        public const string View = "document:view";
        public const string Upload = "document:upload";
        public const string Approve = "document:approve";
        public const string Scan = "document:scan";
        public const string Verify = "document:verify";
        public const string Track = "document:track";
    }

    // İletişim İzinleri
    public static class Communication
    {
        public const string SendMessage = "communication:send";
        public const string ViewMessages = "communication:view";
        public const string ReportIssue = "communication:issue:report";
        public const string ManageIssues = "communication:issue:manage";
    }
}

// Keycloak permission claim dönüşümü için extension
public static class KeycloakPermissionExtensions
{
    public static IServiceCollection AddKeycloakPermissions(this IServiceCollection services)
    {
        services.AddOptions<AuthorizationOptions>()
            .Configure(options =>
            {
                // Her bir permission için policy oluştur
                foreach (var permission in GetAllPermissions())
                {
                    options.AddPolicy(permission, policy =>
                        policy.RequireClaim("permissions", permission));
                }
            });

        return services;
    }

    private static IEnumerable<string> GetAllPermissions()
    {
        // Permission sabitleri otomatik olarak alınıyor
        return typeof(Permissions)
            .GetNestedTypes()
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static))
            .Where(f => f.IsLiteral && !f.IsInitOnly)
            .Select(f => (string)f.GetValue(null));
    }
}
```

### Keycloak Rol-İzin Eşleştirmeleri

```json
{
  "rolePermissionMappings": {
    "_comment_TenantAdmin": "Phase 2 — tenant:*, institution:*",
    "InstitutionManager": [
      "institution:manage",
      "student:*"
    ],
    "DepartmentHead": [
      "department:*",
      "student:view",
      "coordinator:schedule:manage"
    ],
    "Teacher": [
      "student:view",
      "student:manage"
    ],
    "Student": [
      "student:view-own",
      "student:update-own"
    ]
  }
}
```

### .NET Authorization Policies

```csharp
public static class AuthorizationPolicies
{
    public static void AddMesnetPolicies(this AuthorizationOptions options)
    {
        // Temel Politikalar
        // Phase 2 — TenantAdmin
        // options.AddPolicy("RequireTenantAdmin", policy =>
        //     policy.RequireClaim("role", "TenantAdmin"));

        options.AddPolicy("RequireInstitutionManager", policy =>
            policy.RequireClaim("role", "InstitutionManager"));

        // İşlem Bazlı Politikalar
        options.AddPolicy("CanManageStudents", policy =>
            policy.RequireAssertion(context =>
                context.User.HasClaim(c => 
                    c.Type == "permission" && 
                    (c.Value == "student:manage" || 
                     c.Value.StartsWith("student:*")))));

        options.AddPolicy("CanViewOwnData", policy =>
            policy.RequireAssertion(context =>
                context.User.HasClaim(c => 
                    c.Type == "permission" && 
                    c.Value == "student:view-own")));

        // Blockchain Politikaları (Phase 2)
        // options.AddPolicy("CanMintNFT", policy =>
        //     policy.RequireAssertion(context =>
        //         context.User.HasClaim(c =>
        //             c.Type == "permission" &&
        //             (c.Value == "blockchain:mint" ||
        //              c.Value == "blockchain:*"))));

        // Sertifika Politikaları (Phase 2)
        // options.AddPolicy("CanApproveCertificates", policy =>
        //     policy.RequireAssertion(context =>
        //         context.User.HasClaim(c =>
        //             c.Type == "permission" &&
        //             c.Value == "certificate:approve")));
    }
}
```

### Keycloak Client Konfigürasyonu

```json
{
  "realm": "mesnet",
  "auth-server-url": "https://auth.mesnet.com",
  "ssl-required": "external",
  "resource": "mesnet-api",
  "verify-token-audience": true,
  "credentials": {
    "secret": "your-client-secret"
  },
  "confidential-port": 0,
  "policy-enforcer": {
    "paths": [
      {
        "path": "/api/students/*",
        "methods": [
          {
            "method": "GET",
            "scopes": ["student:view", "student:view-own"]
          },
          {
            "method": "POST",
            "scopes": ["student:manage"]
          }
        ]
      },
      {
        "_comment": "Phase 2 - Blockchain",
        "path": "/api/blockchain/mint",
        "methods": [
          {
            "method": "POST",
            "scopes": ["blockchain:mint"]
          }
        ],
        "condition": {
          "claim": "business_hours",
          "pattern": "true"
        }
      }
    ]
  }
}
```

### Middleware Implementation

```csharp
public class MesnetAuthorizationMiddleware
{
    private readonly RequestDelegate _next;

    public MesnetAuthorizationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var user = context.User;
        var path = context.Request.Path;
        var method = context.Request.Method;

        // Kurum bazlı erişim kontrolü
        if (path.StartsWithSegments("/api/institution"))
        {
            var institutionId = GetInstitutionId(context);
            if (!await CanAccessInstitution(user, institutionId))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
        }

        // Öğrenci bazlı erişim kontrolü
        if (path.StartsWithSegments("/api/students"))
        {
            var studentId = GetStudentId(context);
            if (!await CanAccessStudent(user, studentId))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
        }

        // Phase 2 - Blockchain işlemleri için mesai saati kontrolü
        // if (path.StartsWithSegments("/api/blockchain/mint"))
        // {
        //     if (!IsBusinessHours())
        //     {
        //         context.Response.StatusCode = StatusCodes.Status403Forbidden;
        //         return;
        //     }
        // }

        await _next(context);
    }
}
```

### Minimal API Yetki Kontrolü Örneği

```csharp
// Endpoint tanımlamaları
public static class StudentEndpoints
{
    public static IEndpointRouteBuilder MapStudentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/students")
            .WithTags("Öğrenciler")
            .WithOpenApi()
            .RequireAuthorization();

        // Öğrenci listeleme
        group.MapGet("/", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetStudentsQuery());
            return Results.Ok(result);
        })
        .RequireAuthorization("student:view")
        .WithName("GetStudents");

        // Öğrenci ekleme
        group.MapPost("/", async (CreateStudentCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return Results.Created($"/api/students/{result.Id}", result);
        })
        .RequireAuthorization("student:manage")
        .WithName("CreateStudent");

        // Phase 2 - NFT Sertifika talebi
        // group.MapPost("/{id}/certificates", async (Guid id, CreateCertificateCommand command, IMediator mediator) =>
        // {
        //     if (!IsBusinessHours())
        //         return Results.Problem(
        //             "Sertifika talebi sadece mesai saatlerinde yapılabilir.",
        //             statusCode: StatusCodes.Status403Forbidden);
        //     var result = await mediator.Send(command);
        //     return Results.Ok(result);
        // })
        // .RequireAuthorization("certificate:request")
        // .WithName("RequestCertificate");

        return app;
    }
}

// Yetki kontrolleri için extension
public static class AuthorizationExtensions
{
    public static bool HasInstitutionAccess(this ClaimsPrincipal user, Guid institutionId)
    {
        // Tenant Admin tüm kurumlara erişebilir
        if (user.HasClaim("role", "TenantAdmin"))
            return true;

        // Kullanıcının kurum id'si ile eşleşme kontrolü
        var userInstitutionId = user.FindFirst("institution_id")?.Value;
        return userInstitutionId == institutionId.ToString();
    }

    public static bool IsBusinessHours()
    {
        var now = DateTime.Now;
        return now.Hour >= 9 && now.Hour < 17 && 
               now.DayOfWeek != DayOfWeek.Saturday && 
               now.DayOfWeek != DayOfWeek.Sunday;
    }
}
```

### Policy Sabitleri ve Extension Metotları

```csharp
public static class PolicyConstants
{
    public const string ViewStudents = "student:view";
    public const string ManageStudents = "student:manage";
    public const string ViewOwnStudent = "student:view-own";
    public const string UpdateOwnStudent = "student:update-own";
    
    public const string ViewInstitution = "institution:view";
    public const string ManageInstitution = "institution:manage";
    
    // Phase 2
    // public const string ManageBlockchain = "blockchain:manage";
    // public const string MintNFT = "blockchain:mint";
    // public const string ManageCertificates = "certificate:manage";
    // public const string ApproveCertificates = "certificate:approve";
}

public static class AuthorizationExtensions
{
    public static RouteHandlerBuilder RequireStudentView(this RouteHandlerBuilder builder)
        => builder.RequireAuthorization(PolicyConstants.ViewStudents);
        
    public static RouteHandlerBuilder RequireStudentManage(this RouteHandlerBuilder builder)
        => builder.RequireAuthorization(PolicyConstants.ManageStudents);
        
    // Phase 2
    // public static RouteHandlerBuilder RequireMintNFT(this RouteHandlerBuilder builder)
    //     => builder.RequireAuthorization(PolicyConstants.MintNFT);
}
```

### Minimal API Kullanım Örneği

```csharp
public static class StudentEndpoints
{
    public static IEndpointRouteBuilder MapStudentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/students")
            .WithTags("Öğrenciler")
            .WithOpenApi()
            .RequireAuthorization();

        // Öğrenci listeleme
        group.MapGet("/", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetStudentsQuery());
            return Results.Ok(result);
        })
        .RequireAuthorization(PolicyConstants.ViewStudents) // Sabit kullanımı
        .WithName("GetStudents");

        // Öğrenci ekleme
        group.MapPost("/", async (CreateStudentCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return Results.Created($"/api/students/{result.Id}", result);
        })
        .RequireAuthorization(PolicyConstants.ManageStudents) // Sabit kullanımı
        .WithName("CreateStudent");

        // Phase 2 - NFT Sertifika talebi
        // group.MapPost("/{id}/certificates", async (Guid id, CreateCertificateCommand command) =>
        // {
        //     if (!IsBusinessHours())
        //         return Results.Problem(
        //             "Sertifika talebi sadece mesai saatlerinde yapılabilir.",
        //             statusCode: StatusCodes.Status403Forbidden);
        //     // ...işlem detayları
        // })
        // .RequireAuthorization(PolicyConstants.MintNFT)
        // .WithName("RequestCertificate");

        return app;
    }
}
```

### Policy Yapılandırması

```csharp
public static class PolicyConfiguration
{
    public static void AddMesnetPolicies(this AuthorizationOptions options)
    {
        // Keycloak'tan gelen her permission için ilgili policy'i oluştur
        foreach (var permission in GetAllPermissions())
        {
            options.AddPolicy(permission, policy =>
                policy.RequireAssertion(context =>
                    // Kullanıcının claim'lerinde ilgili permission var mı?
                    context.User.HasClaim(c => 
                        c.Type == "permissions" && 
                        (c.Value == permission || 
                         // Wildcard kontrolü (örn: student:* student:view'ı kapsar)
                         (c.Value.EndsWith(":*") && 
                          permission.StartsWith(c.Value.TrimEnd('*')))))));
        }

        // Phase 2 - Özel policy'ler (birden fazla permission gerektiren durumlar)
        // options.AddPolicy("CanManageCertificates", policy =>
        //     policy.RequireAssertion(context =>
        //         context.User.HasClaim(c => c.Type == "permissions" &&
        //             c.Value == Permissions.CertificateManage) &&
        //         context.User.HasClaim(c => c.Type == "permissions" &&
        //             c.Value == Permissions.CertificateApprove)));
    }

    private static IEnumerable<string> GetAllPermissions()
    {
        // Permission sabitleri otomatik olarak alınıyor
        return typeof(Permissions)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string))
            .Select(x => (string)x.GetValue(null));
    }
}

// Token Dönüşümü ve Permission Claim'leri Oluşturma
public static class KeycloakPermissionTransformer
{
    public static ClaimsPrincipal Transform(ClaimsPrincipal principal)
    {
        var claimsIdentity = new ClaimsIdentity();
        var identity = principal.Identity as ClaimsIdentity;

        if (identity == null) return principal;

        // Mevcut claims'leri kopyala
        foreach (var claim in identity.Claims)
        {
            claimsIdentity.AddClaim(new Claim(claim.Type, claim.Value));
        }

        // Scope'ları permission'lara dönüştür
        var scopeClaim = principal.FindFirst("scope")?.Value;
        if (!string.IsNullOrEmpty(scopeClaim))
        {
            var scopes = scopeClaim.Split(' ');
            foreach (var scope in scopes)
            {
                claimsIdentity.AddClaim(new Claim("permissions", scope));
            }
        }

        // Resource access'ten gelen rolleri ekle
        var resourceAccess = principal.FindFirst("resource_access")?.Value;
        if (!string.IsNullOrEmpty(resourceAccess))
        {
            var resourceClaims = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(resourceAccess);
            if (resourceClaims.ContainsKey("mesnet-api"))
            {
                var roles = resourceClaims["mesnet-api"].GetProperty("roles").EnumerateArray();
                foreach (var role in roles)
                {
                    claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role.GetString()));
                }
            }
        }

        return new ClaimsPrincipal(claimsIdentity);
    }
}

// Minimal API Kullanımı
public static class StudentEndpoints
{
    public static IEndpointRouteBuilder MapStudentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/students")
            .WithTags("Öğrenciler")
            .WithOpenApi()
            .RequireAuthorization();

        // Artık policy'ler permission'lardan otomatik oluşuyor
        group.MapGet("/", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetStudentsQuery());
            return Results.Ok(result);
        })
        .RequireAuthorization(Permissions.StudentView)  // Policy otomatik oluşturuldu
        .WithName("GetStudents");

        // ...diğer endpointler
    }
}
```

### Keycloak Client Yapılandırmaları

```json
{
  "clients": {
    "mesnet-api": {
      "clientId": "mesnet-api",
      "enabled": true,
      "clientAuthenticatorType": "client-secret",
      "secret": "api-secret",
      "bearerOnly": true,
      "protocol": "openid-connect",
      "attributes": {
        "access.token.lifespan": 3600
      },
      "protocolMappers": [
        {
          "name": "permissions",
          "protocol": "openid-connect",
          "protocolMapper": "oidc-usermodel-attribute-mapper",
          "config": {
            "claim.name": "permissions",
            "jsonType.label": "String",
            "multivalued": "true"
          }
        }
      ]
    },
    "mesnet-web": {
      "clientId": "mesnet-web",
      "enabled": true,
      "publicClient": true,
      "protocol": "openid-connect",
      "redirectUris": [
        "http://localhost:3000/*",
        "https://mesnet.com/*"
      ],
      "webOrigins": [
        "http://localhost:3000",
        "https://mesnet.com"
      ],
      "attributes": {
        "pkce.code.challenge.method": "S256"
      }
    },
    "mesnet-mobile": {
      "clientId": "mesnet-mobile",
      "enabled": true,
      "publicClient": true,
      "protocol": "openid-connect",
      "redirectUris": [
        "com.mesnet.mobile://*"
      ],
      "attributes": {
        "pkce.code.challenge.method": "S256",
        "access.token.lifespan": 7200
      }
    }
  },
  "clientScopes": {
    "mesnet-scope": {
      "name": "mesnet-scope",
      "protocol": "openid-connect",
      "attributes": {
        "include.in.token.scope": "true"
      },
      "protocolMappers": [
        {
          "name": "roles",
          "protocol": "openid-connect",
          "protocolMapper": "oidc-usermodel-realm-role-mapper",
          "config": {
            "claim.name": "roles",
            "multivalued": "true"
          }
        }
      ]
    }
  }
}
```

Özellikleri:

1. mesnet-api (Backend API)

- Bearer token authentication
- Client credentials flow
- Sadece token doğrulama

2. mesnet-web (Web Arayüzü)

- Public client (frontend)
- PKCE desteği
- Authorization code flow
- CORS yapılandırması

3. mesnet-mobile (Mobil Uygulama)

- Public client
- PKCE desteği
- Custom URL scheme
- Daha uzun token ömrü

4. Ortak Özellikler

- OpenID Connect protokolü
- Role ve permission mapper'lar
- Token scope yapılandırması

## Kurum (Kiracı) Kapsamı

`institution_id` claim'i **kiracı anahtarıdır** (ADR-0003). Bu yüzden diğer kapsam claim'lerinden
katı bir kuralı vardır:

> **Token'dan gelen `institution_id` hiçbir koşulda kabul edilmez** — kullanıcı kaydı boş olsa
> bile. Kapsamsız kalmak, kullanıcının kendi seçtiği kiracıya düşmekten iyidir.

**İki kaynak, ikisi de sunucu tarafında:**

1. **Kullanıcı kaydı** — `UserAccount.InstitutionId`. Tek otorite.
2. **Personel kaydı yedeği** — kurum belgesindeki `staff[]` eşleşmesi. Mevcut kullanıcılar için
   geçiş adımıdır, birincil yol değildir.

İkisi de boşsa claim eklenmez; kurum kapsamı isteyen uçlar o kullanıcıya kapanır ve durum
`LogInformation` ile kaydedilir.

**Neden `branch_codes`'tan katı:** alan kapsamı kiracı *içinde* bir yetki sınırıdır ve orada
kayıt boşken token yedeği hâlâ kabul edilir. `institution_id` kiracının kendisini seçer; orada
"yedek kaynak" diye bir şey olamaz. `institution_id` Keycloak'ta **unmanaged** bir özniteliktir —
realm politikası yanlış kurulursa kullanıcı kendi `manage-account` rolüyle onu yazabilir.

### Bağı kim kurar

Tek yazma yolu: `POST /api/security/users/{id}/institution` — izin `user:roles:manage`
(alan kapsamı ve rol atamasıyla aynı seviye; üçü de yetki kapsamı kararıdır).

Kapsam kararı `UserInstitutionScopePolicy` içindedir ve aktörün kurumu **token claim'inden**
okunur, istekten alınmaz:

| Durum | Sonuç |
| --- | --- |
| Aktörün kendi kurumu yok | Yazamaz — kapsamsızlık sınırsızlık değildir |
| Hedef kullanıcı bağsız → aktörün kurumu | ✅ |
| Hedef kullanıcı **başka kuruma** bağlı | ❌ Devralma tek taraflı değildir |
| Hedef kurum **başka bir kurum** | ❌ Hiçbir aktörün yetkisinde değil |
| Kendi kullanıcısının bağını çözme (`null`) | ✅ Kullanıcı kapsamsız kalır |

**Keycloak'a öznitelik yazılmaz.** Ne `CreateUser` ne davet kabulü ne de bu uç `institution_id`
özniteliğini yazar. Keycloak'ta bir kopya bırakmak, ileride birinin o kopyayı yeniden otorite
sanmasına davetiye çıkarır. Kilitleyen test:
`tests/MESNET.Security.UnitTests/InstitutionClaimAuthorityTests.cs`.

**`SyncUsersFromKeycloak` kurum bağı kurmaz.** Dışarıdan gelen kullanıcı kapsamsız doğar; sync
sonucundaki `WithoutInstitution` sayısı ve uç mesajı bu boşluğu görünür kılar.

## Alan (Branş) Kapsamı Kontrolü

**Permission erişimi açar, kapsamı belirlemez.** "Hangi kurumun/alanın verisi" sorusu ayrı bir
kontroldür:

- **Kurum kapsamı:** yukarıdaki [Kurum (Kiracı) Kapsamı](#kurum-kiracı-kapsamı) bölümü
- **Alan (branş) kapsamı:** `branch_codes` token claim'inden okunur (#126)

### Claim: `branch_codes`

Kullanıcının sorumlu olduğu alan kodlarının **listesi**. Liste olmasının nedeni: küçük okullarda
bir alan şefi birden çok alandan sorumlu olabiliyor.

> **Boş `branch_codes` bir hata değildir.** Herkesin branş kodu olmak zorunda değildir; okul
> müdürü ve müdür yardımcısı hiçbir alana bağlı değildir ve bu **doğru durumdur**, veri
> eksikliği değil. Claim yoksa/boşsa doğrulama hatası üretilmez, uyarı gösterilmez, "eksik
> veri" olarak işaretlenmez. Keycloak tarafında da `branch_codes` **zorunlu alan değildir**
> (unmanaged, opsiyonel öznitelik).
>
> Boş liste yalnız **muafiyeti olmayan** kullanıcı için kısıtlayıcıdır: personel kaydında
> branş kodu bulunmayan bir alan şefi hiçbir alana yazamaz. Bu, yöneticinin boş listesinden
> farklı bir durumdur ve düzeltmesi Kurum → Personel ekranından branş kodunun girilmesidir.

#### Kaynak: kayıt sırasında girilir

**Alan bilgisi kullanıcı oluşturulurken girilir; sistem tahmin etmez veya sonradan türetmez.**
`CreateUser.BranchCodes`, `InstitutionId` / `BusinessId` ile aynı desende **birinci sınıf
alandır** — `Metadata` sözlüğünden okunmaz. Davet akışındaki `Metadata["BranchCode"]` yalnız
taşıma biçimidir; davet tamamlanınca birinci sınıf alana yazılır, iki ayrı doğruluk kaynağı
bırakılmaz.

Değişiklik yolu: `POST /api/security/users/{id}/branches` (`ChangeUserBranches`).
`user:roles:manage` izniyle korunur — alan kapsamı bir **yetki kapsamı** kararıdır, kimlik
bilgisi değil; bu yüzden `UpdateUser` (ad/soyad/e-posta, `user:update`) ile birleştirilmez.
Kapsam değişimi permission cache'ini geçersiz kılar.

#### Claim çözümleme sırası — kullanıcı kaydı OTORİTERDİR

1. **Kullanıcı kaydı** (`UserAccount.BranchCodes`) — **otoriterdir**. Doluysa token'dan gelen
   `branch_codes` claim'leri **atılır** ve yerine kayıttaki değerler konur.
2. **Token claim'i** — yalnız kullanıcı kaydında alan **yokken** kabul edilir (#126 öncesi
   oluşturulmuş, kaydı henüz doldurulmamış kullanıcılar).
3. **Personel kaydı yedeği (geçiş adımı)** — `institution.mt_doc_institution` →
   `staff[].branchCode`. 5 dakikalık `IMemoryCache` ile.

Üçü de boşsa claim eklenmez — bu bir hata değildir.

:::danger Bu sırayı ters çevirmeyin

"Token zaten Keycloak'tan geliyor, imzalı, güvenilir" düşüncesi burada **yanlıştır**.
`branch_codes` Keycloak'ta *unmanaged* bir kullanıcı özniteliğidir. Realm politikası
`ENABLED` olsaydı kullanıcı, varsayılan `manage-account` rolüyle kendi Account
konsolundan/REST API'sinden **kendi özniteliğini yazabilirdi**: EET alan şefi kendine `MTT`
ekler, token'ında `branch_codes: [EET, MTT]` görünür ve #126'nın engellemek için var olduğu
şeyi — başka alanın saat dağıtımını ezmeyi — yapardı.

**Token'ın imzalı olması, içeriğin kullanıcı tarafından belirlenmediği anlamına gelmez.**
:::

Koruma **iki katmanlıdır** ve ikisi de gereklidir:

| Katman | Nerede | Ne yapar |
|---|---|---|
| Realm politikası | `mesnet-realm.json` → `unmanagedAttributePolicy: ADMIN_EDIT` | Unmanaged öznitelikler yalnız **admin** bağlamında görünür/yazılır; kullanıcı ne görür ne yazar |
| Otoriter kayıt | `PermissionClaimsTransformation` | Kullanıcı kaydı doluysa token claim'i **silinir**, yerine kayıt konur |

`ADMIN_EDIT`, uygulamanın yazma yolunu **etkilemez**: `KeycloakAdminService`
`client_credentials` servis hesabı token'ıyla `/admin/realms/{realm}/users/{id}` üzerinden
yazar — admin bağlamıdır. Aynı politika `institution_id` / `business_id` / `student_id`
için de aynı korumayı getirir.

> **Neden DB yedeği yine de var:** Keycloak özniteliği hiç yazılamamışsa (kurulum eksik)
> kapsam yine de çalışsın diye. Yedek yol sistemin kilitlenmemesini sağlar; güvenlik kararı
> ise otoriter kayda dayanır, realm yapılandırmasına **bağımlı değildir**.

#### Alan zorunluluğu ve dolgu

Zorunluluk **permission'dan türetilir, rol adından değil** (`BranchRequirement`): kullanıcı
`department:distribution:manage` iznine sahip ama `institution:distribution:all-branches`
muafiyetine sahip değilse en az bir alan zorunludur (`CreateUserValidator`). Muafiyeti
olanda alan istenmez.

Mevcut kullanıcılar için **ikincil geçiş adımı**:
`POST /api/institutions/staff/resync-branch-codes` — personel kayıtlarındaki alan bilgisini
`StaffAuthorized` olayı olarak yeniden yayınlar; Security modülündeki `StaffBranchSyncConsumer`
kullanıcı kaydının **boş** alan listesini doldurur. İdempotenttir.

- Alanı olmayan personel (müdür, müdür yrd.) **atlanır** — eksik değil, normal
- Kullanıcı kaydında zaten alan varsa **üzerine yazılmaz** — elle girilen kapsam korunur
- Belirsiz kalanlar uydurulmaz; kullanıcı yönetimi ekranında **"Branş atanmamış"** rozetiyle
  ve `?missingBranchOnly=true` filtresiyle listelenir, idare elle girer

### Muafiyet izni: `institution:distribution:all-branches`

Kurum geneli yetkili roller (okul müdürü, müdür yardımcısı) tüm alanları yönetebilmelidir.
Bu muafiyet **rol adına değil permission'a** bağlıdır.

| Rol | `department:*` | `institution:distribution:all-branches` | Sonuç |
|---|---|---|---|
| `InstitutionManager` | ✅ | ✅ (`institution:*` ile) | Tüm alanlara yazar |
| `DeputyDirector` | ✅ | ✅ (açık kayıt) | Tüm alanlara yazar |
| `DepartmentHead` | ✅ | ❌ | Yalnız kendi alan(lar)ına yazar |
| `InstitutionStaff` | ❌ | ❌ | Koordinasyon dağıtımına hiç yazmaz (#129) |

> **#129 ile değişti:** muafiyet önce `InstitutionStaff`'taydı, gerekçesi "müdür yardımcısı tüm
> alanları yönetebilmeli" idi. Müdür yardımcısı ayrı role (`DeputyDirector`) çıkınca muafiyet de
> oraya taşındı; `InstitutionStaff`'ın actors.md'deki sorumluluklarında koordinasyon dağıtımı yok.

> **İsimlendirme tuzağı (dikkat):** Muafiyet izni `department:distribution:all` olarak
> adlandırılamaz. Üç rolün de `department:*` wildcard'ı vardır; o önekteki her yeni izin
> **alan şefine de** geçer ve kapsam kontrolü sessizce hiç çalışmaz. Bu yüzden izin
> `institution:` öneki altındadır. Kilitleyen test:
> `tests/MESNET.Coordination.UnitTests/BranchScopeExemptionMappingTests.cs`

#### Muafiyet izni bireysel olarak ASLA atanamaz

`AssignablePermissionScope.NeverDirectlyAssignable` sabit listesindedir; `CanAssign(...)`
bu izinleri **yapılandırmadan bağımsız** olarak reddeder ve `ChangeUserPermissionsHandler`
kontrolü yapılandırılabilir kapsam kontrolünden **önce** uygular.

Gerekçe: rol → atanabilir domain haritası `PUT /api/security/permission-scopes` ile çalışma
zamanında değiştirilebilir ve bu uç yalnız `user:roles:manage` ister — o izin müdür
yardımcısında da vardır. Sabit liste olmasaydı müdür yardımcısı önce `DepartmentHead`'in
listesine `institution:` ekler, sonra bir alan şefine muafiyet iznini vererek kapsam
kontrolünü tümden kaldırabilirdi. **Kural mutlaktır; yapılandırma onu gevşetemez.**

Bu izinler yalnız `RolePermissionMap` üzerinden, role bağlı olarak gelir. Benzer izinler
ileride eklenirse **tek yer** bu listedir.

### Karar tablosu

Saf mantık `MESNET.Common.Shared/Security/BranchScopePolicy.cs` içindedir.
**Karar sırası: önce muafiyet, sonra liste.** Muafiyet varsa alan listesine hiç bakılmaz —
liste önce kontrol edilseydi, branşı olmayan yöneticiler kilitlenirdi.

| Muafiyet izni | İstenen alan | Kullanıcının alanları | Yazabilir mi |
|---|---|---|---|
| var | herhangi | **boş** (müdür/müdür yrd. — normal) | ✅ |
| var | herhangi | herhangi | ✅ |
| yok | `EET` | `[EET]` | ✅ |
| yok | `EET` | `[EET, MTT]` | ✅ |
| yok | `MTT` | `[EET]` | ❌ |
| yok | `EET` | `[]` (branşı girilmemiş alan şefi) | ❌ |
| yok | boş / null | herhangi | ❌ |

İhlalde `DomainException(Coordination.BranchScopeDenied)` → HTTP 422.

### Okuma açık, yazma kapalı

Kontrol **yalnız yazma uçlarındadır**. Alan şefi başka alanın saat dağıtımını görebilir
(koordinasyon bütününü görmek işe yarar), değiştiremez.

**Kısıtlanan (yazma) uçları:**

| Uç | Handler |
|---|---|
| `PATCH /api/coordination/teachers/assignments/branch-hours` | `UpdateBranchAssignedHoursHandler` |
| `PATCH /api/coordination/teachers/assignments/{businessId}/hours` | `UpdateBusinessAssignedHoursHandler` |
| `POST /api/coordination/teachers/assignments` | `AssignBusinessToTeacherHandler` |
| `DELETE /api/coordination/teachers/assignments/{businessId}` | `UnassignBusinessFromTeacherHandler` |
| `DELETE /api/coordination/teachers/assignments/{businessId}/slot` | `UnassignBusinessSlotHandler` |
| `PUT /api/coordination/teachers/branch-workload/{branchCode}` | `UpsertBranchWorkloadConfigHandler` |

**Kısıtlanmayan (okuma) uçları:** `GET /assignments`, `GET /summary`, `GET /overview-all`,
`GET /business-clusters`, `GET /assignments/suggest-hours`, `GET /branch-workload/{branchCode}`,
`GET /assignments/{businessId}/history`.

> **Kapsam istekten değil, çözümlenmiş satırdan okunur.** Satır bazlı uçlarda kontrol,
> istekteki `branchCode` parametresine değil yüklenen `BusinessCoordinationView.BranchCode`
> değerine bakar — parametre boş bırakılıp eski tek-satır yedeğine düşülerek kontrol
> atlatılamasın diye.

### Frontend

`BranchSelector` bileşeni `write-context` prop'u ile çalışır: yazma bağlamında kullanıcının
yazamayacağı alanlar **listelenmez**; kapsam tek alansa seçici yerine salt okunur alan gösterilir.
Salt görüntüleme/filtre bağlamında (öğrenci listesi, devamsızlık, ödeme) tüm alanlar görünür.
Karar `authStore.canManageAllBranches` / `authStore.writableBranchCodes` üzerinden verilir —
rol adına bakılmaz.

Kullanıcı yönetimi ekranında (`UserManagementPage`):

- **Alan** sütunu kullanıcının alan kodlarını gösterir; boşsa nötr `—` (uyarı değil)
- Alan beklenip girilmemişse **"Branş atanmamış"** uyarı rozeti çıkar
- **Yalnız branş atanmamış kullanıcılar** filtresi (`missingBranchOnly`) idarenin bu
  kullanıcıları bulmasını sağlar; rol değişimiyle branşsız kalanlar burada görünür
- Satır aksiyonlarındaki 🎓 düğmesi alan kapsamını düzenler (çoklu seçim, kurum branş
  kataloğundan)
- Davet formunda alan çoklu seçimi vardır; zorunlu değildir

## Kurum Geneli Koordinasyon Yapılandırması (#130)

`CoordinationConfig` alan bazlı **değildir**: kurum düzeyi ve mevzuat türevi üç ayar tutar.

| Ayar | Anlamı |
|---|---|
| `DistanceHourRules` | Mesafe-saat eşleme tablosu (MEB mevzuatı): `≤1km→2s`, `≤3km→4s`, `≤5km→6s`, `>5km→8s` |
| `IsMetropolitan` | Okul büyükşehir belediyesi sınırları içinde mi |
| `MaxWeeklyExtraHours` | Öğretmen başına azami haftalık ek ders saati (varsayılan 20) |

### Neden ayrı bir izin gerekti

#126 koordinasyon **yazma** uçlarına alan kapsamı kontrolü getirdi, ama bu uç kapsanamadı —
yapılandırmanın alanı yoktur. Yazma da `department:distribution:manage` istediği sürece alan
şefi, doğrudan yazamadığı diğer alanları **kurum geneli parametreyi değiştirerek dolaylı**
etkileyebiliyordu:

- `MaxWeeklyExtraHours` düşürülürse diğer alanların mevcut atamaları limit üstüne çıkar ve o
  alanların koordinatörleri yeni atama yapamaz
- `DistanceHourRules` değişirse tüm alanların `MaxCoordinationHours` tavanları ve dolayısıyla
  #116 otomatik dağıtım önerileri kayar

### Neden muafiyet izni kullanılmadı

`institution:distribution:all-branches` "tüm **alanlara** yazabilir" demektir. Kurum geneli
yapılandırma ise alan kavramıyla hiç ilgili değildir; muafiyeti buraya uydurmak anlamını
bulanıklaştırırdı. Bu yüzden ayrı ve kesin bir izin tanımlandı:
`institution:coordination-config:manage`.

### Rol dağılımı

| Rol | `department:*` | `institution:coordination-config:manage` | Sonuç |
|---|---|---|---|
| `InstitutionManager` | ✅ | ✅ (`institution:*` ile) | Yapılandırmayı değiştirir |
| `DeputyDirector` | ✅ | ✅ (açık kayıt) | Yapılandırmayı değiştirir |
| `DepartmentHead` | ✅ | ❌ | Yalnız **görür** |
| `InstitutionStaff` | ❌ | ❌ | Yalnız **görür** (dağıtım izni de yok → uca hiç erişemez) |

> **Aynı wildcard tuzağı (#126 ile bire bir):** izin `department:` önekiyle adlandırılamaz.
> `DepartmentHead` `department:*` taşır; o önekteki her yeni izin alan şefine de geçer ve
> kısıt sessizce hiç çalışmaz. Kilitleyen test:
> `tests/MESNET.Coordination.UnitTests/CoordinationConfigPermissionTests.cs`

### Okuma açık, yazma kapalı

| Uç | İzin | Gerekçe |
|---|---|---|
| `GET /api/coordination/teachers/config` | `department:distribution:manage` | Alan şefi hangi kurallara göre çalıştığını **görmelidir** |
| `POST /api/coordination/teachers/config` | `institution:coordination-config:manage` | Kurum düzeyi karar |

Bu ayrım #126'nın "okuma açık, yazma kapalı" kararıyla aynıdır. Uç kayıtlarını
`tests/MESNET.Coordination.UnitTests/CoordinationEndpointAuthorizationTests.cs` endpoint
metadata'sından okuyarak kilitler — okumayı da kısıtlayan biri kırmızı test görür.

### Bireysel (direct) atama

Bu izin bir **erişim** iznidir, kapsam muafiyeti değil — bu yüzden
`AssignablePermissionScope.NeverDirectlyAssignable` listesinde **yer almaz** ve kurum düzeyi
yetkili bir kullanıcıya bireysel olarak atanabilir. `DepartmentHead`'e atanamaz: o rolün
varsayılan atanabilir domain listesinde `institution:` yoktur (`department:`, `coordinator:`,
`internship:`, `attendance:`, `document:`, `communication:`, `student:`).

### Arayüz

Yapılandırma formu WebUI'de **henüz yoktur** — `coordinationApi.getConfig` /
`coordinationApi.upsertConfig` istemci fonksiyonları tanımlı ama hiçbir sayfa çağırmıyor.
Form eklendiğinde görünürlük/salt-okunurluk kararı `Permissions.Institution.CoordinationConfigManage`
iznine bakmalıdır (`src/WebUI/src/utils/permissions.ts`), **rol adına değil**; yetkisi olmayan
kullanıcıya form salt okunur gösterilir ve kaydet düğmesi devre dışı bırakılır.


## Ulusal (Platform) Parametreler — Bakanlık Katmanı (#147)

Asgari ücret ve 3308 oranları **ulusal mevzuattır**. Bunlar kurum kapsamında tutulduğu sürece
her okul aynı sayıyı ayrı ayrı giriyor ve değerler sapabiliyordu; ayrıca yazma ucu kurum
kimliğini istek gövdesinden aldığı için yetkili bir kullanıcı **başka kurumun** ücretini
değiştirebiliyordu. İkisi de aynı kökün sonucuydu: parametre yanlış katmandaydı.

### Katman ayrımı

| Katman | Ne | İzin |
|---|---|---|
| **Ulusal** | Asgari ücret, 16 yaş altı ücret, `ApprenticeRate` (Madde 25), `PersonnelThreshold`, `%15`/`%30` taban, devlet katkısı kesirleri (Geçici Madde 12) | `platform:parameter:manage` |
| **Kurum** | Ders programı (`DailyPeriodCount`), mesafe-saat kuralları, `MaxWeeklyExtraHours` (#130) | `institution:coordination-config:manage` |
| **İl / İlçe** | Kurum kaydındaki `ProvinceCode` (MEB il kodu) + `DistrictName` (kapalı listeden ilçe adı). Planlamanın ekseni budur; Bakanlık düzeyi bir aktör tasarlanmaz | — |

### Önek neden `platform:`

`RolePermissionMap`'te `InstitutionManager` hem `institution:*` hem `salary:*` wildcard'ını
tutuyor. Ulusal izin `salary:national:manage` ya da `institution:...` diye adlandırılsaydı
wildcard onu **her okul müdürüne sessizce** verir ve ayrım hiç çalışmazdı — #126'daki
muafiyet-öneki tuzağının birebir tekrarı.

`platform:` öneki hiçbir okul rolünde yoktur. Kilitleyen test:
`tests/MESNET.Security.UnitTests/PlatformScopeMappingTests.cs` — hem bilinen izin adını hem
önekin tamamını kontrol eder, böylece ileride eklenen bir platform izni sessizce okul rolüne
düşerse test kırılır.

### Okuma açık, yazma kapalı

Okul rolleri yürürlükteki asgari ücreti **görür** (`salary:parameter:view`, `salary:*` ile
gelir), yazamaz. Uç noktalar aynı yolda iki farklı izne bağlıdır:

- `GET /api/payments/config/minimum-wage` → `salary:parameter:view`
- `PUT /api/payments/config/minimum-wage` → `platform:parameter:manage`

### Bireysel (direct) atama

`platform:parameter:manage`, `AssignablePermissionScope.NeverDirectlyAssignable` listesindedir.
Gerekçe #126 ile aynı, bir basamak yukarısı: `InstitutionManager`'ın atanabilir kapsamı `"*"`
olduğu için bu liste olmasaydı bir okul müdürü ulusal izni istediği kullanıcıya bireysel atayıp
ayrımı tümden kaldırabilirdi. **Yapılandırma bu kuralı gevşetemez.**

### `SystemAdmin` rolü — geçici taşıyıcı

Ulusal parametreyi bugün `SystemAdmin` rolü girer. Bu rolün **kurum verisine yetkisi yoktur**:
`salary:view`, `student:view`, `attendance:view`, `institution:view`, `company:view` almaz —
yalnız `platform:parameter:manage` ve yazdığı değerin geçmişini görmek için
`salary:parameter:view`.

Gerçek işletimde bu girişi **Bakanlık düzeyi bir aktör** yapar (asgari ücreti Asgari Ücret
Tespit Komisyonu belirler, Resmî Gazete'de yayımlanır; 3308 Geçici Madde 12'nin son paragrafı
usulü Bakanlık ve İŞKUR'a bırakır). O aktör tanımlandığında aynı `platform:` izinlerini alır
ve `SystemAdmin` yerini ona bırakır.

### Kaldırılan ikinci yazma yolu

Institution modülünde ikinci bir `UpdateMinimumWage` komutu vardı: `SystemParameter["MinimumWage"]`
yazıyor, `MinimumWageUpdated` yayınlıyor, Business modülü onu `MinimumWageReference`'a
kaydediyordu. **Bu zincirin hiçbir halkası okunmuyordu** ve hiçbir uç noktadan tetiklenmiyordu;
hesap yalnız Payment'ın `SalaryCalculationConfig`'ini okuyor. Bir uca bağlanması ulusal katmanı
sessizce atlayacağı için zincir tümüyle kaldırıldı.

## Sağlık Raporu Onay Zinciri — Giriş Geniş, Hüküm Dar (#172)

Bir veriyi **girebilmek** ile o girişin **hüküm doğurması** ayrı kararlardır ve ayrı
permission'larla verilir. Bu ayrım #172'nin çekirdeğidir.

### Bulunan açık

`POST /api/attendance/{id}/health-report` ucu `attendance:manage` istiyordu ve o izin
`CompanyManager`'da vardı. Rapor eklendiği anda agregada devamsızlık türü `HealthReport`
oluyordu; o tür ücret kesintisine tabi değildir (`business-rules.md` §6.2). Yani **ödemeyi
yapan taraf, öğrencinin ücretinden yapılacak kesintiyi kendi kararıyla kaldırabiliyordu** ve
arada hiçbir onay adımı yoktu.

İkinci açık aynı yerdeydi ve ters yöne çalışıyordu: `HealthReportAttached` olayı Payment
modülünde **hiç dinlenmiyordu**. Attendance'ta tür değişse bile Payment'ın yerel kaydı eski
türde kalıyor, geçerli raporu olan öğrencinin ücreti kesilmeye devam ediyordu.

Üçüncüsü: `POST /api/attendance/{id}/correct` de türü değiştiriyor ve yine `attendance:manage`
istiyordu — yani onay zinciri kurulsa bile işletme o kapıdan aynı sonucu alabilirdi.

### Kural

| Kim girer | Sonuç |
|---|---|
| İşletme yetkilisi, işletme İK, usta öğretici, öğrenci, (veli — planlanan) | **Onay bekler** — koordinatör öğretmen onaylayana kadar tür değişmez, kesinti sürer |
| Koordinatör öğretmen, müdür yardımcısı, müdür | **Doğrudan geçerli** — onay zaten kendilerinde biter |

Kurum personeli (`InstitutionStaff`) **devamsızlığı** doğrudan girer ama **sağlık raporunu**
onaysız geçerli kılamaz: sahibin saydığı taraf üç roldür ve #129'un "yürütür, onaylamaz"
ayrımı burada da geçerlidir.

2. adım ayrı bir devamsızlık uç noktası değildir: müdür yardımcısı / müdür kesinti kararını
**mevcut dekont onay zincirinde** (`salary:approve`) uygular.

### İzinler

| İzin | Ne açar | Kimde |
|---|---|---|
| `attendance:upload` | Sağlık raporu **yükleme** (giriş) | Okul rolleri + `CompanyManager`, `MasterTrainer`, `CompanyHR`, `Student` |
| `attendance:direct-entry` | Girilen **devamsızlığın** onay beklememesi | `InstitutionManager`, `DeputyDirector`, `InstitutionStaff`, `Teacher` |
| `attendance:health-report:direct` | Girilen **raporun** onay beklememesi | `InstitutionManager`, `DeputyDirector`, `Teacher` |
| `attendance:approve` | Onay/ret adımı (1. onay) | `InstitutionManager`, `DeputyDirector`, `Teacher` |

### Önek neden `attendance:`

`company:` **kullanılamaz** — `CompanyManager` `company:` önekli izinleri taşır, hüküm izni
işletmeye geçerdi. `department:` de **kullanılamaz** — `DepartmentHead` ve `DeputyDirector`
`department:*` taşır. `attendance:*` wildcard'ı yalnız `InstitutionManager`'dadır ve müdürün
bu izne sahip olması istenen sonuçtur; işletme rolleri attendance izinlerini tek tek satırla
alır, bu yüzden hüküm izni onlara wildcard'la sızmaz.

Kilitleyen test: `tests/MESNET.Security.UnitTests/AttendanceDirectEntryMappingTests.cs`.

### Bireysel (direct) atama

`attendance:direct-entry` ve `attendance:health-report:direct`,
`AssignablePermissionScope.NeverDirectlyAssignable` listesindedir. Gerekçe #126 ve #147'nin
aynısı, bir basamak daha somut: işletme rollerinin atanabilir domain listesinde `attendance:`
**vardır** (giriş ve yükleme için gerekli). Sabit liste olmasaydı `user:roles:manage` yetkisi
olan biri bir işletme yetkilisine `attendance:health-report:direct`'i bireysel atayıp onay
zincirini tümden kaldırabilirdi.

### Rol adı kontrolü kaldırıldı

`MarkAttendanceHandler` işletme girişini rol adına bakarak (`CompanyManager` ||
`MasterTrainer`) ayırıyordu ve bu CLAUDE.md'de teknik borç olarak yazılıydı. Karar artık
`attendance:direct-entry` iznine bakar. Borcun gerçek maliyeti #172'de görüldü: yeni eklenen
`CompanyHR` rolü listede olmadığı için o rolün girdiği kayıt okul girmiş gibi doğrudan
`Recorded` olurdu.

## Ücretli İzin Onay Zinciri — İki Taraflı Onay Kapsamla Kurulur (#177)

MESEM'de ücretli izin bir tür seçimi değil, **başvuru**dur: öğrenci açar, işletme onaylar, okul
onaylar. Ancak son adımda resmîleşir ve o güne ait devamsızlık kaydı doğar.

```
Öğrenci başvurur → İşletme onaylar → Okul (müdür yrd./müdür) onaylar → RESMİLEŞİR
```

### İzinler

| İzin | Ne açar | Kimde |
|---|---|---|
| `attendance:leave:request` | Başvuru açma | `Student` (veli #174 ile eklenecek) |
| `attendance:leave:business-approve` | 1. adım — işletme onayı/reddi | `CompanyManager`, `MasterTrainer`, `CompanyHR` (+ wildcard'la `InstitutionManager`) |
| `attendance:leave:approve` | 2. adım — okul onayı/reddi | `InstitutionManager`, `DeputyDirector` |

Koordinatör öğretmen zincirde **adım tutmaz**; yalnız bildirim alır (SSE
`attendance.paid-leave-approved`). Sahibin kararı: *"Müdür yardımcısı ve müdür yeterli ama
öğretmene de izin bilgisini verelim, notifikasyon gibi düşün."*

### İki taraflı onay permission ile GARANTİ EDİLEMEZ

`InstitutionManager` **her domain wildcard'ını** taşır (`institution:*`, `attendance:*`,
`company:*`, `department:*`, `student:*` …). Yani işletme adımı için tanımlanacak izin — hangi
önekte olursa olsun — okul müdürüne de gider; `platform:` dışında serbest önek yoktur. Önek
seçerek çözülemeyen ilk vaka budur.

**Çözüm kapsamdır, izin değil** (ADR-0001: permission erişimi açar, kapsamı belirlemez):

1. **İşletme adımı `business_id` claim'i ister.** Token'daki claim başvurunun `BusinessId`'siyle
   eşleşmek zorundadır ve okul rollerinde o claim yoktur — müdür wildcard izne rağmen adımı
   yapamaz. Aynı desen `StudentTermGradeEndpoints`'te kullanılıyor.
2. **İşletme onayını veren okul onayını veremez.** Bir kullanıcı iki rolü birden taşıyabilir
   (izinler rollerin birleşimidir); bu kural olmasaydı tek kişi zincirin iki adımını da
   yürütürdü. Kimliksiz onay (`Guid.Empty`) da reddedilir — iki tarafı da boş bir onay eşitlik
   kontrolünü sessizce geçerdi.

Kapsam kararı saf sınıftadır: `MESNET.Attendance.Core/Services/PaidLeaveApprovalPolicy.cs`.
Kilitleyen testler: `tests/MESNET.Attendance.UnitTests/PaidLeaveApprovalPolicyTests.cs` ve
`tests/MESNET.Security.UnitTests/PaidLeaveApprovalMappingTests.cs`.

### Bireysel (direct) atama

`attendance:leave:approve`, `AssignablePermissionScope.NeverDirectlyAssignable` listesindedir.
İşletme rollerinin atanabilir domain listesinde `attendance:` **vardır** (devamsızlık girişi ve
rapor yükleme için gerekli); sabit liste olmasaydı `user:roles:manage` yetkisi olan biri bir
işletme kullanıcısına okul adımını atayabilir ve zincir tek tarafa çökerdi. "Aynı kullanıcı iki
adımı yapamaz" kuralı bunu **kapatmaz** — ikinci bir işletme kullanıcısı okul adımını yapardı.

İşletme adımı (`attendance:leave:business-approve`) listede **değildir**: onu izin değil
`business_id` kapsamı sınırlar, okul kullanıcısına atansa bile işe yaramaz.

### Ücretli izin artık doğrudan girilemez

`POST /api/attendance` ve `POST /api/attendance/{id}/correct` uçları `PaidLeave` türünü
**reddeder** — okul tarafı için de. Türü seçmek doğrudan para kararıdır (ücretli izin kesinti
doğurmaz); doğrudan giriş açık kalsaydı iki taraflı onay tek komutla atlanabilirdi, tıpkı #172
öncesinde `/correct` ucunun sağlık raporu zincirini atlaması gibi. Kısıt **komut yolundadır**;
onaydan doğan kayıtlar olay tüketicisiyle (`PaidLeaveAttendanceConsumer`) açıldığı için bu
kapıdan geçmez.

## Okulda Staj Dönem Notu — Kurum İşverenin Yerine Geçer (#171)

Dönem notu akışının her adımı işletmeye bağlıydı; okulda staj yapan öğrenci (#159, işverensiz
yerleştirme) not giriş listesinde hiç görünmüyor ve **notu hiç girilemiyordu**.

### İki akış, iki izin

| | İşletmede staj | Okulda staj |
|---|---|---|
| Notu giren | İşletme yetkilisi / usta öğretici | **Alan şefi, müdür yardımcısı, müdür** |
| İzin | `company:grade:enter` | `institution:school-grade:enter` |
| Kapsam | `business_id` claim'i | `institution_id` claim'i + okulda staj yerleştirmesi |
| Fiş (MEB Form 8) | Üretilir | **Üretilmez** |

Okuldaki şefin `business_id` claim'i **yoktur** — işletme izni ona verilse bile hiçbir işe
yaramazdı. Kapsam bu yüzden kurum claim'i ve `SchoolPlacedStudentView` üzerinden kurulur.

### Önek neden `institution:`

Sahibin kararı: *"Resmî kuruma bağlı izinler kurumsal olmalı."* Öğrenci okulda staj yaptığında
**kurum, işverenin yerine geçer** — bu bir alan/bölüm işi değildir. İzin bu yüzden
`department:` değil `institution:` öneklidir.

> **Önek kapsamı BELİRLEMEZ.** "Herkes kendi kurumuna göre yetkilenir" kuralı önekten değil
> `institution_id` claim'inden gelir ve ADR-0001 gereği izinden bağımsız çalışır. (Phase 2'de
> tenant katmanı gelirse claim adı değişebilir; kural aynı kalır.)

**Bu önekte wildcard hedefi tek başına karşılamaz:** `institution:*` yalnız
`InstitutionManager`'dadır. `DeputyDirector` ve `DepartmentHead` izni **açık satırla** alır —
satır silinirse o iki rol izni sessizce kaybeder ve belirti ancak dönem sonunda çıkar. Alan
şefinin `institution:` önekli başka hiçbir izni yoktur; kurum yönetimi izinleri (silme,
personel yetkilendirme) ona geçmez.

`Teacher` ve işletme rollerinde bu izin hiçbir yoldan bulunmaz. Kilitleyen testler:
`tests/MESNET.Security.UnitTests/SchoolTermGradeMappingTests.cs` (rol dağılımı + önek kararı) ve
`tests/MESNET.Coordination.UnitTests/SchoolTermGradeEndpointAuthorizationTests.cs` (uç → izin
eşlemesi + işletme akışı regresyonu).

### Fiş üretim yolu üç katmanda kapalı

1. Okul gönderimi `StudentTermGradeSubmitted` olayını **yayınlamaz** — Reporting'in
   `StudentTermGradeView` kaydı hiç oluşmaz
2. Fiş listesi (`GET /term-grades/submitted`) `BusinessId != null` filtresi taşır — koordinatör
   ekranda görmez, üretmeyi denemez
3. Fiş üretim handler'ı işverensiz yerleştirmeyi açık hatayla reddeder
   (`Reporting.TermGradeSlipNotAvailableForSchoolPlacement`)

Tek katman yeterli olurdu; üçü birden var çünkü ilk ikisi *sessiz* korumadır — biri kaldırılsa
belirti dönem sonuna kadar görünmezdi.

## Veli Aktörü — Kapsam İzinle Değil Bağ Kaydıyla Verilir (#174)

Veli `actors.md`'de tanımlıydı ama **realm rolü olarak yoktu**: sistemde hiç kullanıcısı
olmadığı için #172'nin sağlık raporu zincirine, #177'nin ücretli izin başvurusuna ve fesih
zincirindeki kendi adımına giremiyordu.

### Kapsam neden permission ile verilemez

**Tüm velilerin izinleri aynıdır**; onları birbirinden ayıran tek şey hangi öğrenciye bağlı
oldukları. İzin bazlı bir çözüm ya her veliyi her öğrenciye açardı ya da öğrenci başına izin
üretmeyi gerektirirdi. ADR-0001: *izin erişimi açar, kapsamı belirlemez.*

Kapsam bir **kayıt**tır: `UserAccount.LinkedStudentIds`. Desen `BranchCodes` (#126) ile
birebir aynıdır:

| | Alan kapsamı (#126) | Veli kapsamı (#174) |
|---|---|---|
| Kayıt | `UserAccount.BranchCodes` | `UserAccount.LinkedStudentIds` |
| Claim | `branch_codes` | `linked_student_ids` |
| Otorite | **Kayıt** — token claim'i ezilir | **Kayıt** — token claim'i ezilir |
| Yönetim ucu | `POST /api/security/users/{id}/branches` | `POST /api/security/users/{id}/students` |
| İzin | `user:roles:manage` | `user:roles:manage` |
| Boş liste anlamı | "Alana bağlı değil" (müdür için normal) | **"Bağ kurulmamış"** — hiçbir erişim |

> **Token yedeği YOKTUR.** `branch_codes`'ta mevcut kullanıcılar için bir token/DB yedeği
> bırakılmıştı; burada yoktur. Öznitelik Keycloak'ta *unmanaged*'dır: yedek bırakılsaydı
> kullanıcı kendi Account konsolundan kendine öğrenci ekleyip başka bir öğrencinin verisine
> erişebilirdi. **Token'ın imzalı olması içeriğin kullanıcı tarafından belirlenmediği anlamına
> gelmez.**

### İzin demeti

| İzin | Ne açar |
|---|---|
| `student:view-own`, `internship:view-own`, `attendance:view-own`, `salary:view-own` | Öğrencisinin verisi (kapsam bağ kaydından) |
| `attendance:upload` | Sağlık raporu — **onaya düşer** (#172) |
| `attendance:leave:request` | MESEM ücretli izin başvurusu (#177) |
| `communication:*` (view/send/issue) | Okulla iletişim |

**Hüküm izinleri verilmez:** `attendance:direct-entry` ve `attendance:health-report:direct`
velide **yoktur** — veli, öğrencisinin ücret kesintisini tek taraflı kaldıramaz. `Parent`,
`AttendanceDirectEntryMappingTests.NonSchoolRoles` listesindedir.

### Veli fesih zincirinde onaycı değildir (#218)

Fesih onay zinciri **koordinatör öğretmen → müdür yardımcısı → müdür**'den ibarettir. Veli ve
işletme yetkilisi fesih **talep eder**, onaylamaz.

Önceden zincirde ayrı bir "veli adımı" vardı ve `internship:approve:parent` izni onu açıyordu.
Model gerçek kuralla uyuşmuyordu; adım kaldırılınca izin de kaldırıldı — hiçbir uca bağlı
olmayan bir izin, olmayan bir yetkiyi varmış gibi gösterir.

Talebi kimin açtığı `RequestedBy`/`ReasonType` ile kaydedilir.

### Guard tek yerdedir

`ParentScopeGuard` (Common.Infrastructure) — `BranchScopeGuard` deseninin aynısı. Kural:
**bağ kaydı olan kullanıcı yalnız bağlı olduğu öğrenciye dokunabilir; bağı olmayan kullanıcı
bu kontrolden etkilenmez.** Okul ve işletme tarafının kapsamı kurum/işletme claim'lerinden
gelir; guard yalnız bağ kapsamını uygular, erişim kararını değil.

**Öğrenci kimliği istekten ALINMAZ**, sunucuda çözülmüş kayıttan okunur (devamsızlık kaydı,
staj saga'sı, yerleştirme). Tek istisna ücretli izin başvurusudur: orada öğrenci istekte gelir
ama **bağ kaydına karşı doğrulanır** — karar yine sunucudaki otoriter kayda dayanır.

Uygulandığı yerler: sağlık raporu yükleme, ücretli izin başvurusu ve listesi, fesih zinciri
veli adımı. Kilitleyen test: `tests/MESNET.Security.UnitTests/ParentScopeTests.cs`.

> **Kapsam dışı bırakıldı:** devamsızlık/staj/maaş **listeleme** uçları `attendance:view` gibi
> okul izinleri istiyor ve `*:view-own` izinleri bugün hiçbir ucu korumuyor — bu öğrenci rolü
> için de geçerli olan, #174 öncesinden gelen bir borçtur. Veliye o izinler verildi ama okuma
> yüzeyi ayrı bir işte ele alınmalı.

## "Kendi verisi" Kapsam Merdiveni (#182)

`attendance:view-own`, `internship:view-own` ve `salary:view-own` izinleri tanımlıydı ve
öğrenciye dağıtılmıştı ama **hiçbir uçta kullanılmıyordu**. Listeleme uçları okul tarafı iznini
(`attendance:view` vb.) istiyordu; öğrenci **kendi devamsızlığını**, veli (#174) de
öğrencisininkini hiç göremiyordu. Kırık bir şey yoktu — akış sessizce hiç başlamıyordu.

### Tek uç, iki izin

Uçlar artık **birleşik policy** ister: `attendance:view | attendance:view-own`. Erişim ile
kapsam ayrıdır (ADR-0001) — policy yalnız ucun açılıp açılmayacağına karar verir, hangi veriyi
göreceğine handler'daki merdiven karar verir.

| Uç | Policy |
|---|---|
| `GET /api/attendance`, `GET /api/attendance/{id}` | `PermissionPolicies.AttendanceViewOrOwn` |
| `GET /api/internships`, `GET /api/internships/{id}` | `PermissionPolicies.InternshipViewOrOwn` |
| `GET /api/payments`, `GET /api/payments/{id}` | `PermissionPolicies.SalaryViewOrOwn` |

**Neden ayrı uç değil:** ayrı uç aynı sorguyu iki kez yazdırır ve kapsam kuralını iki yerde
tutar; biri güncellenip diğeri unutulduğunda sapma sessiz olur.

### Merdiven

`OwnDataScope.Resolve(currentUser, broadViewPermission)` — sıra kritiktir:

1. **Geniş görüntüleme izni** varsa → kapsam daraltılmaz (okul/işletme tarafının bugünkü
   davranışı korunur)
2. **Veli** — `linked_student_ids` doluysa yalnız bağlı öğrenciler (#174)
3. **Öğrenci** — `student_id` claim'i varsa yalnız kendisi
4. Hiçbiri yoksa → **boş sonuç**

> Son basamak "hepsini göster" olsaydı, `view-own` taşıyan ama kapsamı çözülemeyen bir kullanıcı
> **tüm kurumun verisini** görürdü — iznin açtığı ucun tam tersi sonuç.

Veli bağı `student_id` claim'inden **önce** gelir: ikisi birden olan bir kullanıcıda açıkça
kaydedilmiş olan kazanır.

Kilitleyen test: `tests/MESNET.Security.UnitTests/OwnDataScopeTests.cs`.

### Kapsam dışı bırakıldı

Sözleşme ve dönem notu listeleri bu merdivene alınmadı — öğrenci/velinin o ekranlara ihtiyacı
ayrıca kararlaştırılmalı. Yazma uçlarına dokunulmadı.
