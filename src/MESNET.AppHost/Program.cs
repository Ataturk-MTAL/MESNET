var builder = DistributedApplication.CreateBuilder(args);

// Docker Compose publisher — aspire publish ile docker-compose.yml üretir
builder.AddDockerComposeEnvironment("mesnet-compose");

// Sabit şifreler — Persistent container'larla uyumlu olması için parametrize edildi
var postgresPassword = builder.AddParameter("postgres-password", secret: true);
var rabbitmqUser = builder.AddParameter("rabbitmq-user", secret: false);
var rabbitmqPassword = builder.AddParameter("rabbitmq-password", secret: true);
var keycloakPassword = builder.AddParameter("keycloak-password", secret: true);
var minioUser = builder.AddParameter("minio-user", secret: false);
var minioPassword = builder.AddParameter("minio-password", secret: true);

// Altyapı servisleri — Persistent: AppHost kapansa bile container'lar ayakta kalır
// Dev'de her restart'ta yeniden oluşturulmazlar, veri ve state korunur
var postgres = builder.AddPostgres("postgres", password: postgresPassword)
    .WithImage("kartoza/postgis", "18-3.6")
    .WithBindMount("./postgres", "/docker-entrypoint-initdb.d")
    .WithPgAdmin(pgAdmin => pgAdmin.WithLifetime(ContainerLifetime.Persistent))
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("mesnet");

var rabbitmq = builder.AddRabbitMQ("rabbitmq", userName: rabbitmqUser, password: rabbitmqPassword)
    .WithImage("rabbitmq", "4-management-alpine")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var keycloak = builder.AddKeycloak("keycloak", port: 8080, adminPassword: keycloakPassword)
    .WithRealmImport("./keycloak")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

// Mailpit (Dev email sunucusu — SMTP:1025, Web UI:8025)
var mailpit = builder.AddMailPit("mailpit")
    .WithDataVolume("mailpit-data")
    .WithLifetime(ContainerLifetime.Persistent);

// MinIO (S3-compatible object storage)
var minio = builder.AddContainer("minio", "minio/minio", "latest")
    .WithHttpEndpoint(port: 9000, targetPort: 9000, name: "api")
    .WithHttpEndpoint(port: 9001, targetPort: 9001, name: "console")
    .WithEnvironment("MINIO_ROOT_USER", minioUser)
    .WithEnvironment("MINIO_ROOT_PASSWORD", minioPassword)
    .WithArgs("server", "/data", "--console-address", ":9001")
    .WithVolume("minio-data", "/data")
    .WithHttpHealthCheck("/minio/health/live", endpointName: "api")
    .WithLifetime(ContainerLifetime.Persistent);

// OSRM — Open Source Routing Machine (rota bazlı mesafe hesaplama)
// Veriler Mac'te osmium + osrm-extract + osrm-contract ile hazırlandı (osrm/data/)
// Sadece Mersin bölgesi — container anında başlar, ~300 MB RAM
var osrm = builder.AddContainer("osrm", "ghcr.io/project-osrm/osrm-backend", "latest")
    .WithHttpEndpoint(port: 5002, targetPort: 5000, name: "osrm")
    .WithBindMount("./osrm/data", "/data")
    .WithArgs("osrm-routed", "--algorithm", "CH", "/data/mersin.osrm")
    .WithLifetime(ContainerLifetime.Persistent);

var api = builder.AddProject<Projects.MESNET_Presentation>("mesnet-api")
    .WithExternalHttpEndpoints()
    .WithReference(postgres)
    .WithReference(rabbitmq)
    .WithReference(keycloak)
    .WithEnvironment("MinioStorage__Endpoint", minio.GetEndpoint("api"))
    .WithEnvironment("MinioStorage__AccessKey", minioUser)
    .WithEnvironment("MinioStorage__SecretKey", minioPassword)
    .WithEnvironment("SmtpSettings__Host", mailpit.Resource.Host)
    .WithEnvironment("SmtpSettings__Port", mailpit.Resource.Port)
    .WithEnvironment("Osrm__BaseUrl", osrm.GetEndpoint("osrm"))
    .WaitFor(postgres)
    .WaitFor(rabbitmq)
    .WaitFor(minio)
    .WaitFor(mailpit);

// Seeder — sadece dev modunda çalışır, API + Keycloak hazır olduktan sonra
if (!builder.ExecutionContext.IsPublishMode)
{
    builder.AddProject<Projects.MESNET_Seeder>("mesnet-seeder")
        .WithReference(api)
        .WithReference(keycloak)
        .WithEnvironment("Seeder__ApiBaseUrl", api.GetEndpoint("http"))
        .WithEnvironment("Seeder__KeycloakTokenUrl", () =>
            $"{keycloak.GetEndpoint("http").Url}/realms/mesnet/protocol/openid-connect/token")
        .WaitFor(api)
        .WaitFor(keycloak);
}

// Mimari Dokümanlar (Docusaurus + Kroki — sadece dev modunda)
if (!builder.ExecutionContext.IsPublishMode)
{
    // Kroki — diyagram rendering servisi (PlantUML, C4, Mermaid vb.)
    var kroki = builder.AddContainer("kroki", "yuzutech/kroki", "latest")
        .WithHttpEndpoint(port: 9222, targetPort: 8000, name: "kroki")
        .WithEnvironment("KROKI_SAFE_MODE", "safe")
        .WithEnvironment("KROKI_PLANTUML_ALLOW_INCLUDE", "true")
        .WithEnvironment("KROKI_COMMAND_TIMEOUT", "60s")
        .WithLifetime(ContainerLifetime.Persistent);

    // Docusaurus docs site
    builder.AddNpmApp("docs", "../../src/Docs", scriptName: "start")
        .WithHttpEndpoint(port: 8100, env: "PORT")
        .WithEnvironment("KROKI_SERVER", kroki.GetEndpoint("kroki"))
        .WaitFor(kroki);
}

// Frontend
if (builder.ExecutionContext.IsPublishMode)
{
    // Publish: Dockerfile ile nginx container — docker-compose'a dahil edilir
    builder.AddDockerfile("frontend", "../../src/WebUI")
        .WithHttpEndpoint(port: 80, targetPort: 80);
}
else
{
    // Dev: Vite dev server — Aspire dashboard'dan izlenir
    builder.AddNpmApp("frontend", "../../src/WebUI", scriptName: "dev")
        .WithExternalHttpEndpoints()
        .WithReference(api)
        .WithEnvironment("VITE_API_URL", api.GetEndpoint("http"))
        .WithHttpEndpoint(port: 5173, env: "PORT")
        .WaitFor(api);
}

builder.Build().Run();
