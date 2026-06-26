using JasperFx;
using JasperFx.Events.Daemon;
using Marten;
using Weasel.Core;
using MESNET.Attendance.Api;
using MESNET.Business.Api;
using MESNET.Contract.Api;
using MESNET.Coordination.Api;
using MESNET.Enrollment.Api;
using MESNET.Institution.Api;
using MESNET.Common.Infrastructure.Notifications;
using MESNET.Common.Shared;
using MESNET.Institution.Persistence.SeedData;
using MESNET.Presentation;
using MESNET.Internship.Api;
using MESNET.Payment.Api;
using MESNET.Reporting.Api;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Exceptions;
using Wolverine;
using Wolverine.Http;
using Wolverine.Marten;
using Keycloak.AuthServices.Sdk;
using MESNET.Common.Infrastructure.Email;
using MESNET.Common.Infrastructure.Security;
using MESNET.Security.Api;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Wolverine.FluentValidation;
using Wolverine.RabbitMQ;

// Npgsql 6+: UTC DateTime → timestamp without time zone uyumsuzluğunu önle
// DateTime.Kind=UTC değerlerini legacy davranışla kabul et
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Bootstrap logger — uygulama ayağa kalkmadan önceki loglar için
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog — replaces default logging
    builder.Host.UseSerilog((context, services, config) => config
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithExceptionDetails()
        .Enrich.WithEnvironmentName()
        .Enrich.WithThreadId()
        .Enrich.WithProperty("Application", "MESNET")
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{NewLine}  {Message:lj}{NewLine}{Exception}")
        .WriteTo.OpenTelemetry(otel =>
        {
            otel.Endpoint = context.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317";
            otel.Protocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc;
        }));

    // Aspire Service Defaults (telemetry, health checks, resilience)
    builder.AddServiceDefaults();

    // MinIO File Storage
    builder.AddMinioFileStorage();

    // SSE Notification Altyapısı
    builder.AddSseNotifications();

    // Marten — PostgreSQL Document DB + Event Store
    builder.Services.AddMarten(opts =>
    {
        opts.Connection(builder.Configuration.GetConnectionString("mesnet")!);
        opts.DatabaseSchemaName = "shared";
        opts.AutoCreateSchemaObjects = AutoCreate.All;

        // SmartEnum → Name-based JSON serialization (tüm modüllerdeki SmartEnum tipleri otomatik tanınır)
        // Marten varsayılan STJ serializer'ını kullanmaya devam et, sadece SmartEnum converter ekle
        opts.UseSystemTextJsonForSerialization(
            EnumStorage.AsString,
            Casing.CamelCase,
            configure: o => o.Converters.Add(new SmartEnumJsonConverterFactory()));
    })
    .InitializeWith(new FieldOfStudySeedData())
    .IntegrateWithWolverine()
    .AddAsyncDaemon(DaemonMode.HotCold);

    // ────────────────────────────────────────────────────────────────────────────────
    // Modül Registrations (Her modül kendi katmanlarını kaydeder)
    // ────────────────────────────────────────────────────────────────────────────────
    builder.Services.AddInstitutionModule();
    builder.Services.AddBusinessModule();
    builder.Services.AddEnrollmentModule();
    builder.Services.AddContractModule();
    builder.Services.AddAttendanceModule();
    builder.Services.AddPaymentModule();
    builder.Services.AddCoordinationModule(builder.Configuration);
    builder.Services.AddInternshipModule();
    builder.Services.AddReportingModule();
    builder.Services.AddSecurityModule();

    // Email (MJML template + MailKit SMTP)
    builder.Services.AddEmailServices();

    // ────────────────────────────────────────────────────────────────────────────────
    // Authentication + Authorization
    // ────────────────────────────────────────────────────────────────────────────────
    // 1. Keycloak JWT Bearer Authentication
    // Keycloak.AuthServices kütüphanesi yerine direkt AddJwtBearer kullanıyoruz.
    // Kütüphanenin Configure<IServiceProvider> sıralama sorunlarından kaçınmak için.
    var keycloakAuthority = builder.Configuration["Keycloak:auth-server-url"]?.TrimEnd('/')
        + "/realms/" + builder.Configuration["Keycloak:realm"];
    var keycloakAudience = builder.Configuration["Keycloak:resource"];

    builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.Authority = keycloakAuthority;
            options.Audience = keycloakAudience;
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
            // JWT claim'lerin orijinal isimleriyle gelmesi için mapping'i kapat
            // Yoksa "sub" → ClaimTypes.NameIdentifier, "email" → ClaimTypes.Email olarak map edilir
            options.MapInboundClaims = false;
            // Keycloak hazır olana kadar OIDC discovery'nin zaman aşımına uğramaması için
            options.BackchannelTimeout = TimeSpan.FromSeconds(30);
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuer = !builder.Environment.IsDevelopment(),
                ValidateAudience = !builder.Environment.IsDevelopment(),
                ValidateLifetime = true,
                NameClaimType = "preferred_username",
                RoleClaimType = "role"
            };
            // Development'ta 401 nedenini logla
            if (builder.Environment.IsDevelopment())
            {
                options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtBearer");
                        logger.LogWarning(context.Exception,
                            "JWT doğrulama hatası: {Message}", context.Exception.Message);
                        return Task.CompletedTask;
                    }
                };
            }
        });

    // 2. Authorization Policies + Custom Permission Handler + Claims Transformation
    builder.Services.AddMesnetSecurity(builder.Configuration);

    // 3. Keycloak Admin SDK (client credentials flow — Admin API erişimi)
    builder.Services.AddDistributedMemoryCache();
    builder.Services
        .AddKeycloakAdminHttpClient(builder.Configuration);

    // ────────────────────────────────────────────────────────────────────────────────
    // CORS — Sadece frontend origin'e izin ver
    // ────────────────────────────────────────────────────────────────────────────────
    var frontendUrl = builder.Configuration["FrontendUrl"] ?? "http://localhost:5173";
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("MesnetCors", policy =>
        {
            policy
                .WithOrigins(frontendUrl)
                .AllowAnyHeader()
                .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH")
                .AllowCredentials(); // SSE için gerekli
        });
    });

    // ────────────────────────────────────────────────────────────────────────────────
    // Rate Limiting
    // ────────────────────────────────────────────────────────────────────────────────
    builder.Services.AddRateLimiter(options =>
    {
        // Global sabit pencere — genel API koruması
        options.AddFixedWindowLimiter("GlobalApi", limiterOptions =>
        {
            limiterOptions.PermitLimit = 300;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit = 10;
        });

        // SSE endpoint'i — bağlantı başına uzun süreli, düşük limit
        options.AddFixedWindowLimiter("SseConnections", limiterOptions =>
        {
            limiterOptions.PermitLimit = 5;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
        });

        options.RejectionStatusCode = 429;
    });

    // SmartEnum → Name-based JSON serialization (ASP.NET Core request/response)
    builder.Services.ConfigureHttpJsonOptions(o =>
        o.SerializerOptions.Converters.Add(new SmartEnumJsonConverterFactory()));

    // OpenAPI
    builder.Services.AddOpenApi();

    // Wolverine HTTP — modül Api projelerindeki WolverineFx.Http bağımlılığı için gerekli
    builder.Services.AddWolverineHttp();

    // Wolverine — CQRS + Messaging
    builder.Host.UseWolverine(opts =>
    {
        opts.UseFluentValidation();
        // Wolverine 6'da service-location varsayılan NotAllowed → opaque/named-registration servisleri
        // (örn. IKeycloakAdminService: named HttpClient + IConfiguration ile lambda kayıtlı) kullanan
        // handler'ların codegen'i patlıyordu (InvalidServiceLocationException → 500). İzin ver (uyarıyla).
        opts.ServiceLocationPolicy = JasperFx.CodeGeneration.Model.ServiceLocationPolicy.AllowedButWarn;
        opts.MultipleHandlerBehavior = MultipleHandlerBehavior.Separated;
        opts.Durability.MessageStorageSchemaName = "wolverine";
        opts.Policies.AutoApplyTransactions();
        opts.Policies.UseDurableLocalQueues();

        // Kapalı akademik dönem koruması (#8) — Payment maaş/dekont yazma command'ları
        opts.Policies.ForMessagesOfType<MESNET.Payment.Application.ISalaryPeriodScoped>()
            .AddMiddleware(typeof(MESNET.Payment.Application.SalaryPeriodGuardMiddleware));

        // Modül Application assembly'lerini handler keşfi için tanıt
        // Wolverine varsayılan olarak sadece host assembly'yi tarar
        opts.Discovery.IncludeAssembly(typeof(MESNET.Institution.Application.Commands.CreateInstitution).Assembly);
        opts.Discovery.IncludeAssembly(typeof(MESNET.Business.Application.Commands.RegisterBusiness).Assembly);
        opts.Discovery.IncludeAssembly(typeof(MESNET.Enrollment.Application.Commands.RegisterStudent).Assembly);
        opts.Discovery.IncludeAssembly(typeof(MESNET.Contract.Application.Commands.CreateContract).Assembly);
        opts.Discovery.IncludeAssembly(typeof(MESNET.Attendance.Application.Commands.MarkAttendance).Assembly);
        opts.Discovery.IncludeAssembly(typeof(MESNET.Payment.Application.Commands.UploadReceiptByStudent).Assembly);
        opts.Discovery.IncludeAssembly(typeof(MESNET.Coordination.Application.Commands.CreateBusinessEvaluation).Assembly);
        opts.Discovery.IncludeAssembly(typeof(MESNET.Internship.Application.Commands.RequestTermination).Assembly);
        opts.Discovery.IncludeAssembly(typeof(MESNET.Reporting.Application.Commands.GenerateInternshipContractDocument).Assembly);
        opts.Discovery.IncludeAssembly(typeof(MESNET.Security.Application.Commands.CreateUser).Assembly);

        // RabbitMQ Transport — Aspire connection string'inden okur
        var rabbitUri = builder.Configuration.GetConnectionString("rabbitmq");
        if (!string.IsNullOrEmpty(rabbitUri))
        {
            opts.UseRabbitMq(new Uri(rabbitUri)).AutoProvision();
        }
        else
        {
            opts.UseRabbitMq(rabbit =>
            {
                rabbit.HostName = builder.Configuration["RabbitMQ:HostName"] ?? "localhost";
                rabbit.UserName = builder.Configuration["RabbitMQ:UserName"] ?? "guest";
                rabbit.Password = builder.Configuration["RabbitMQ:Password"] ?? "guest";
            }).AutoProvision();
        }
    });

    var app = builder.Build();


    // MinIO bucket initialization (startup)
    using (var scope = app.Services.CreateScope())
    {
        var fileStorage = scope.ServiceProvider.GetRequiredService<MESNET.Common.Infrastructure.Storage.IFileStorageService>();
        var minioOptions = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MESNET.Common.Infrastructure.Storage.MinioStorageOptions>>().Value;

        var bucketResult = await fileStorage.EnsureBucketExistsAsync(minioOptions.DefaultBucket);
        if (bucketResult.IsFailure)
        {
            app.Logger.LogWarning("MinIO bucket oluşturulamadı: {Error}", bucketResult.Error.Description);
        }

        var mebFormsBucketResult = await fileStorage.EnsureBucketExistsAsync("meb-forms");
        if (mebFormsBucketResult.IsFailure)
        {
            app.Logger.LogWarning("MinIO meb-forms bucket oluşturulamadı: {Error}", mebFormsBucketResult.Error.Description);
        }

        // Initialize business-documents bucket
        var businessDocsBucketResult = await fileStorage.EnsureBucketExistsAsync("business-documents");
        if (businessDocsBucketResult.IsFailure)
        {
            app.Logger.LogWarning("MinIO business-documents bucket oluşturulamadı: {Error}", businessDocsBucketResult.Error.Description);
        }
    }

    app.UseSerilogRequestLogging();

    // ──── Global Exception Handler → DomainException/ValidationException → 422, diğer → 500 ────
    app.UseExceptionHandler(errApp => errApp.Run(async ctx =>
    {
        var feature = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var ex = feature?.Error;

        // InnerException kontrolü — Wolverine bazen exception'ı wrap edebilir
        var domainEx = ex as MESNET.Common.Shared.DomainException
            ?? ex?.InnerException as MESNET.Common.Shared.DomainException;

        var validationEx = ex as FluentValidation.ValidationException
            ?? ex?.InnerException as FluentValidation.ValidationException;

        // Marten optimistic concurrency çakışması — inner zinciri tip ADIYLA yürü (namespace Marten
        // sürümleri arasında değişebilir): ConcurrentUpdateException (document) /
        // EventStreamUnexpectedMaxEventIdException ([AggregateHandler] event stream) / StreamLockedException.
        var isConcurrencyConflict = false;
        for (var cur = ex; cur is not null; cur = cur.InnerException)
        {
            if (cur.GetType().Name is "ConcurrentUpdateException" or "EventStreamUnexpectedMaxEventIdException"
                or "ConcurrencyException" or "StreamLockedException")
            {
                isConcurrencyConflict = true;
                break;
            }
        }

        if (domainEx is not null)
        {
            ctx.Response.StatusCode = 422;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(
                MESNET.Common.Shared.ResponseBuilder.Fail(422)
                    .AddMessage(domainEx.Error.Description)
                    .AddErrors(new { code = domainEx.Error.Code })
                    .Build());
        }
        else if (validationEx is not null)
        {
            var errors = validationEx.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            ctx.Response.StatusCode = 422;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(
                MESNET.Common.Shared.ResponseBuilder.Fail(422)
                    .AddMessage("Doğrulama hatası")
                    .AddErrors(errors)
                    .Build());
        }
        else if (ex is Microsoft.AspNetCore.Http.BadHttpRequestException
                 || ex?.InnerException is Microsoft.AspNetCore.Http.BadHttpRequestException)
        {
            // Eksik/hatalı istek gövdesi (zorunlu alan yok, JSON çözümlenemedi) → 400 (500 değil)
            ctx.Response.StatusCode = 400;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(
                MESNET.Common.Shared.ResponseBuilder.Fail(400)
                    .AddMessage("Geçersiz veya eksik istek gövdesi.")
                    .Build());
        }
        else if (ex is Wolverine.Persistence.Sagas.UnknownSagaException
                 || ex?.InnerException is Wolverine.Persistence.Sagas.UnknownSagaException)
        {
            // İlgili saga/kayıt bulunamadı (örn. olmayan staj için onay) → 404 (500 değil)
            ctx.Response.StatusCode = 404;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(
                MESNET.Common.Shared.ResponseBuilder.Fail(404)
                    .AddMessage("İlgili kayıt bulunamadı.")
                    .Build());
        }
        else if (isConcurrencyConflict)
        {
            // Optimistic concurrency çakışması (aynı aggregate/event stream'e eşzamanlı yazma) → 409 (500 değil)
            ctx.Response.StatusCode = 409;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(
                MESNET.Common.Shared.ResponseBuilder.Fail(409)
                    .AddMessage("Kayıt bu sırada başka bir işlemle değiştirildi. Lütfen sayfayı yenileyip tekrar deneyin.")
                    .Build());
        }
        else
        {
            // Beklenmeyen hatalar — 500 ama yine de yapısal ApiResponse dön
            // Structured property'ler Aspire dashboard'da filtrelenebilir
            var innerEx = ex?.InnerException;
            app.Logger.LogError(ex,
                "İşlenmeyen hata: {Path} | {ExceptionType} | {ExceptionMessage} | InnerException: {InnerExceptionType} — {InnerExceptionMessage}",
                ctx.Request.Path,
                ex?.GetType().FullName,
                ex?.Message,
                innerEx?.GetType().FullName,
                innerEx?.Message);

            ctx.Response.StatusCode = 500;
            ctx.Response.ContentType = "application/json";
            var builder500 = MESNET.Common.Shared.ResponseBuilder.Fail(500)
                .AddMessage("Beklenmeyen bir sunucu hatası oluştu.");
            // Dev: exception tipini/mesajını yanıta ekle (debugging — prod'da gizli)
            if (app.Environment.IsDevelopment())
                builder500 = builder500.AddErrors(new
                {
                    exception = ex?.GetType().FullName,
                    message = ex?.Message,
                    inner = innerEx?.GetType().FullName,
                    innerMessage = innerEx?.Message,
                });
            await ctx.Response.WriteAsJsonAsync(builder500.Build());
        }
    }));

    // ──── Security Headers ────
    app.Use(async (context, next) =>
    {
        var path = context.Request.Path.Value ?? "";

        // Scalar UI ve OpenAPI dokümanları CSP'den muaf — inline script gerektirir
        if (path.StartsWith("/scalar", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        var headers = context.Response.Headers;

        // XSS kaynağını kes: frontend ve Keycloak dışına istek gidemesin
        var keycloakUrl = builder.Configuration["Keycloak:auth-server-url"] ?? "http://localhost:8080";
        headers.Append("Content-Security-Policy",
            $"default-src 'self'; " +
            $"script-src 'self'; " +
            $"style-src 'self' 'unsafe-inline'; " +
            $"img-src 'self' data: blob:; " +
            $"font-src 'self'; " +
            $"connect-src 'self' {keycloakUrl}; " +
            $"frame-src {keycloakUrl}; " +
            $"object-src 'none'; " +
            $"base-uri 'self';");

        // Clickjacking koruması
        headers.Append("X-Frame-Options", "DENY");

        // MIME sniffing koruması
        headers.Append("X-Content-Type-Options", "nosniff");

        // Referrer bilgisi sızdırma
        headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // Hassas browser feature'larını kapat
        headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=(), payment=()");

        await next();
    });

    // ──── CORS & Rate Limiting ────
    app.UseCors("MesnetCors");
    app.UseRateLimiter();

    app.MapDefaultEndpoints();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapWolverineEndpoints();

    // ──── Modül Endpoint Registrations ────
    // Institution
    app.MapInstitutionEndpoints();
    app.MapFieldCatalogEndpoints();
    app.MapAcademicPeriodEndpoints();
    // Business
    app.MapBusinessEndpoints();
    app.MapBusinessQueryEndpoints();
    app.MapBusinessDocumentEndpoints();
    app.MapBusinessInstructorDocumentEndpoints();
    // Enrollment
    app.MapStudentEndpoints();
    app.MapTeacherEndpoints();
    app.MapApplicationEndpoints();
    app.MapPlacementEndpoints();
    // Contract
    app.MapContractEndpoints();
    // Attendance
    app.MapAttendanceEndpoints();
    app.MapWorkCalendarEndpoints();
    // Payment
    app.MapPaymentEndpoints();
    // Coordination
    app.MapCoordinationEndpoints();
    app.MapBusinessEvaluationEndpoints();
    app.MapGuidanceVisitEndpoints();
    app.MapMonthlyActivityReportEndpoints();
    app.MapSkillExamEndpoints();
    app.MapWeeklyVisitEndpoints();
    // Internship
    app.MapInternshipEndpoints();
    // Reporting
    app.MapReportingEndpoints();
    app.MapDocumentLifecycleEndpoints();
    // Security
    app.MapUserManagementEndpoints();
    app.MapInvitationEndpoints();
    app.MapRoleEndpoints();

    // Auth (kullanıcı bilgileri + permission listesi)
    app.MapAuthEndpoint();

    // SSE Notification Endpoint (Minimal API)
    app.MapSseNotificationEndpoint();

    // PostGIS extension — DBSCAN kümeleme (ST_ClusterDBSCAN) için zorunlu
    // Persistent container'da init-postgis.sql yalnızca ilk oluşturmada çalışır,
    // her startup'ta idempotent olarak garanti altına alıyoruz
    await using (var scope = app.Services.CreateAsyncScope())
    {
        var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();
        var conn = store.Storage.Database.CreateConnection();
        await conn.OpenAsync();
        await using (conn)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "CREATE EXTENSION IF NOT EXISTS postgis";
            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Npgsql.PostgresException ex) when (ex.SqlState is "23505" or "42710")
            {
                // CREATE EXTENSION IF NOT EXISTS PostgreSQL'de concurrency-safe değil:
                // Marten 9 şema uygulamasıyla eşzamanlı çalışınca pg_extension'da unique
                // ihlali (23505/42710) atabilir. Extension yine de mevcut → istenen son durum.
            }
        }
    }

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Uygulama başlatılırken beklenmeyen hata oluştu");
}
finally
{
    Log.CloseAndFlush();
}
