using JasperFx;
using JasperFx.Events.Daemon;
using Marten;
using MESNET.Attendance.Api;
using MESNET.Business.Api;
using MESNET.Contract.Api;
using MESNET.Coordination.Api;
using MESNET.Enrollment.Api;
using MESNET.Institution.Api;
using MESNET.Common.Infrastructure.Notifications;
using MESNET.Institution.Persistence.SeedData;
using MESNET.Presentation;
using MESNET.Internship.Api;
using MESNET.Payment.Api;
using MESNET.Reporting.Api;
using Scalar.AspNetCore;
using Serilog;
using Wolverine;
using Wolverine.Http;
using Wolverine.Marten;
using Keycloak.AuthServices.Authentication;
using Keycloak.AuthServices.Sdk;
using MESNET.Common.Infrastructure.Security;
using MESNET.Security.Api;
using Wolverine.RabbitMQ;

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
    builder.Services.AddCoordinationModule();
    builder.Services.AddInternshipModule();
    builder.Services.AddReportingModule();
    builder.Services.AddSecurityModule();

    // ────────────────────────────────────────────────────────────────────────────────
    // Authentication + Authorization
    // ────────────────────────────────────────────────────────────────────────────────
    // 1. Keycloak JWT Bearer Authentication
    builder.Services
        .AddKeycloakWebApiAuthentication(builder.Configuration);

    // 2. Authorization Policies + Custom Permission Handler + Claims Transformation
    builder.Services.AddMesnetSecurity(builder.Configuration);

    // 3. Keycloak Admin SDK (client credentials flow — Admin API erişimi)
    builder.Services.AddDistributedMemoryCache();
    builder.Services
        .AddKeycloakAdminHttpClient(builder.Configuration);

    // OpenAPI
    builder.Services.AddOpenApi();

    // Wolverine — CQRS + Messaging
    builder.Host.UseWolverine(opts =>
    {
        opts.MultipleHandlerBehavior = MultipleHandlerBehavior.Separated;
        opts.Durability.MessageStorageSchemaName = "wolverine";
        opts.Policies.AutoApplyTransactions();
        opts.Policies.UseDurableLocalQueues();

        // RabbitMQ Transport
        opts.UseRabbitMq(rabbit =>
        {
            rabbit.HostName = builder.Configuration["RabbitMQ:HostName"] ?? "localhost";
            rabbit.UserName = builder.Configuration["RabbitMQ:UserName"] ?? "mesnet";
            rabbit.Password = builder.Configuration["RabbitMQ:Password"] ?? "mesnet_dev";
        }).AutoProvision();
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
    }

    app.UseSerilogRequestLogging();

    app.MapDefaultEndpoints();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapWolverineEndpoints();

    // SSE Notification Endpoint (Minimal API)
    app.MapSseNotificationEndpoint();

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
