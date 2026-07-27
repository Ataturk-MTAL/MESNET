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

### Permission erişimi açar, kapsamı belirlemez

"Hangi kurumun/alanın verisi" sorusu ayrı bir kontroldür ve permission ile karıştırılmamalıdır.

- **Kurum kapsamı:** `institution_id` token claim'inden okunur, istekten alınmaz
- **Alan (branş) kapsamı:** `branch_codes` token claim'inden okunur (#126).
  `ICurrentUserService.GetBranchCodes()` taşır; koordinasyon **yazma** handler'ları
  `BranchScopeGuard` ile kontrol eder. Ayrıntı: [Alan (Branş) Kapsamı Kontrolü](#alan-branş-kapsamı-kontrolü)

### Bu ilkenin bilinen istisnaları (teknik borç)

Aşağıdaki iki nokta veri kapsamı kararını rol adına bakarak veriyor; permission'a taşınmalıdır:

- `src/Modules/Attendance/MESNET.Attendance.Application/Handlers/MarkAttendanceHandler.cs:55`
- `src/Modules/Enrollment/MESNET.Enrollment.Application/Handlers/PlacementQueryScope.cs:23-34`

`src/WebUI/src/stores/auth.ts` kapsam kararı #126 ile permission bazlına geçti
(`canManageAllBranches` / `writableBranchCodes`); `isDepartmentHead` yalnız kapsam dışı
görünürlük için kalmıştır.

## Ana Roller ve İzinler

### Temel Roller

Phase 1'de **8 realm rolü** vardır. Tek doğruluk kaynağı
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
| `Student` | Öğrenci | Öğrenci |

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
| `salary:approve`, `salary:parameter:manage` | Dekont onay zinciri, asgari ücret parametresi |
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
| `communication:*` | Kurum ve koordinatörle iletişim |

**ALMAZ:** `company:student:request` (öğrenci talebi), `company:receipt:upload` (dekont),
`company:document:manage` (işletme belgeleri), `company:manage`. Bunlar `CompanyManager`'da kalır.

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
    }

    // Maaş İzinleri
    public static class Salary
    {
        public const string View = "salary:view";
        public const string ViewOwn = "salary:view-own";
        public const string Calculate = "salary:calculate";
        public const string Approve = "salary:approve";
        public const string Receipt = "salary:receipt:manage";
        public const string Parameter = "salary:parameter:manage";
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

## Alan (Branş) Kapsamı Kontrolü

**Permission erişimi açar, kapsamı belirlemez.** "Hangi kurumun/alanın verisi" sorusu ayrı bir
kontroldür:

- **Kurum kapsamı:** `institution_id` token claim'inden okunur, istekten alınmaz
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
