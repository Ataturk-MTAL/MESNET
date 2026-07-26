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
| `InstitutionStaff` | Müdür yardımcısı | ✅ |
| `DepartmentHead` | Alan şefi | ✅ |

### Permission erişimi açar, kapsamı belirlemez

"Hangi kurumun/alanın verisi" sorusu ayrı bir kontroldür ve permission ile karıştırılmamalıdır.

- **Kurum kapsamı:** `institution_id` token claim'inden okunur, istekten alınmaz
- **Alan (branş) kapsamı:** bugün **mekanizma yoktur**. `ICurrentUserService` alan bilgisi
  taşımıyor ve koordinasyon uçları `branchCode`'u sorgu parametresinden alıyor; bir alan şefi
  başka alanın saat dağıtımını değiştirebilir. Bilinen açık.

### Bu ilkenin bilinen istisnaları (teknik borç)

Aşağıdaki üç nokta veri kapsamı kararını rol adına bakarak veriyor; permission'a taşınmalıdır:

- `src/Modules/Attendance/MESNET.Attendance.Application/Handlers/MarkAttendanceHandler.cs:55`
- `src/Modules/Enrollment/MESNET.Enrollment.Application/Handlers/PlacementQueryScope.cs:23-34`
- `src/WebUI/src/stores/auth.ts:47`

## Ana Roller ve İzinler

### Temel Roller

```json
{
  "roles": [
    {
      "name": "TenantAdmin",
      "description": "Üst Yönetim (Phase 2)",
      "composite": false
    },
    {
      "name": "InstitutionManager",
      "description": "Kurum Müdürü",
      "composite": false
    },
    {
      "name": "InstitutionStaff",
      "description": "Kurum Personeli",
      "composite": false
    },
    {
      "name": "Teacher",
      "description": "Öğretmen",
      "composite": false
    },
    {
      "name": "Student",
      "description": "Öğrenci",
      "composite": false
    },
    {
      "name": "DepartmentHead",
      "description": "Alan Şefi",
      "composite": false
    },
    {
      "name": "CompanyManager",
      "description": "İşletme Yöneticisi",
      "composite": false
    },
    {
      "name": "BlockchainAdmin",
      "description": "Blockchain Yöneticisi (Phase 2)",
      "composite": false
    }
  ]
}
```

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
