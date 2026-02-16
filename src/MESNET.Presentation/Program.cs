using JasperFx;
using JasperFx.Events.Daemon;
using Marten;
using MESNET.Coordination.Persistence;
using MESNET.Institution.Persistence.SeedData;
using Scalar.AspNetCore;
using Serilog;
using Wolverine;
using Wolverine.Http;
using Wolverine.Marten;
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

    // Modül Persistence Registrations (Her modül kendi schema'sını kaydeder)
    builder.Services.AddCoordinationPersistence();

    // Keycloak Authentication (disabled in dev until realm is configured)
    // builder.Services
    //     .AddAuthentication()
    //     .AddKeycloakWebApi(builder.Configuration);
    // builder.Services.AddAuthorization();

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
    }

    app.UseSerilogRequestLogging();

    app.MapDefaultEndpoints();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    // app.UseAuthentication();
    // app.UseAuthorization();

    app.MapWolverineEndpoints();

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
