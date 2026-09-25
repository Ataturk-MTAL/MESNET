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
var openObserveUser = builder.AddParameter("openobserve-user", secret: false);
var openObservePassword = builder.AddParameter("openobserve-password", secret: true);

// Altyapı servisleri — Persistent: AppHost kapansa bile container'lar ayakta kalır
// Dev'de her restart'ta yeniden oluşturulmazlar, veri ve state korunur
// Not: Altyapı endpoint'lerinde IsProxied=false — Aspire DCP proxy'si yerine doğrudan podman port
// publish kullanılır (host:port → container). Host process'ler (API/seeder) ve sabit URL'ler
// (Keycloak authority localhost:8080) için öngörülebilir; proxy kaynaklı port/JWKS karışıklığını önler.
var postgres = builder.AddPostgres("postgres", password: postgresPassword)
    .WithImage("kartoza/postgis", "18-3.6")
    .WithBindMount("./postgres", "/docker-entrypoint-initdb.d")
    .WithPgAdmin(pgAdmin => pgAdmin.WithLifetime(ContainerLifetime.Persistent))
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEndpoint("tcp", e => e.IsProxied = false)
    .AddDatabase("mesnet");

var rabbitmq = builder.AddRabbitMQ("rabbitmq", userName: rabbitmqUser, password: rabbitmqPassword)
    .WithImage("rabbitmq", "4-management-alpine")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEndpoint("tcp", e => e.IsProxied = false);

// Keycloak proxy AÇIK kalır: çift http(8080)/https(8443) portu nedeniyle proxy kapatılınca
// host:8080 yanlışlıkla HTTPS'e (8443) bağlanıyor → ERR_EMPTY_RESPONSE. Proxy'de 8080→8080 HTTP doğru.
// WithoutHttpsCertificate deneysel API (ASPIRECERTIFICATES001) — tanı ifadenin başına bağlanır.
#pragma warning disable ASPIRECERTIFICATES001
var keycloak = builder.AddKeycloak("keycloak", port: 8080, adminPassword: keycloakPassword)
    // Dev, CI ve docker-compose AYNI Keycloak sürümünde tutulur — sürüm sapması, birinde
    // görünmeyen hatayı diğerinde doğurur. Önceden dev 26.6, CI ve compose 26.0 idi.
    .WithImageTag("26.7.0")
    // Aspire.Hosting.Keycloak 13.4+ geliştirici sertifikası bulunca ana uç noktayı HTTPS'e
    // (hedef 8443) çevirir; host:8080 HTTPS'e bağlanır ve http://localhost:8080 kullanan
    // frontend/API/seeder boş yanıt alır. Paket bu yüzden 13.1.2'de tutuluyordu — kök neden
    // sürüm değil otomatik sertifikaydı. Dev'de düz HTTP kalsın.
    .WithoutHttpsCertificate()
    .WithRealmImport("./keycloak")
    .WithBindMount("./keycloak/themes/mesnet", "/opt/keycloak/themes/mesnet")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);
#pragma warning restore ASPIRECERTIFICATES001

// Mailpit (Dev email sunucusu — SMTP:1025, Web UI:8025)
var mailpit = builder.AddMailPit("mailpit")
    .WithDataVolume("mailpit-data")
    .WithLifetime(ContainerLifetime.Persistent);

// RustFS (S3 uyumlu nesne deposu). MinIO imajları Docker Hub ve quay.io'dan kaldırıldı
// (Eylül 2026, ölçüldü: her etiket "pull access denied"). RustFS aynı portlarda (9000 API,
// 9001 konsol) çalışır; uygulama Minio .NET SDK ile standart S3 konuşur, kod değişmedi.
// Parametre adları (minio-user/minio-password) mevcut user-secrets bozulmasın diye korundu.
var rustfs = builder.AddContainer("rustfs", "rustfs/rustfs", "1.0.0")
    .WithHttpEndpoint(port: 9000, targetPort: 9000, name: "api")
    .WithHttpEndpoint(port: 9001, targetPort: 9001, name: "console")
    .WithEnvironment("RUSTFS_ACCESS_KEY", minioUser)
    .WithEnvironment("RUSTFS_SECRET_KEY", minioPassword)
    .WithVolume("rustfs-data", "/data")
    .WithHttpHealthCheck("/health", endpointName: "api")
    .WithEndpoint("api", e => e.IsProxied = false)
    .WithEndpoint("console", e => e.IsProxied = false)
    .WithLifetime(ContainerLifetime.Persistent);

// OSRM — Open Source Routing Machine (rota bazlı mesafe hesaplama)
// Veriler Mac'te osmium + osrm-extract + osrm-contract ile hazırlandı (osrm/data/)
// Sadece Mersin bölgesi — container anında başlar, ~300 MB RAM
var osrm = builder.AddContainer("osrm", "ghcr.io/project-osrm/osrm-backend", "latest")
    .WithHttpEndpoint(port: 5002, targetPort: 5000, name: "osrm")
    .WithBindMount("./osrm/data", "/data")
    .WithArgs("osrm-routed", "--algorithm", "CH", "/data/mersin.osrm")
    .WithEndpoint("osrm", e => e.IsProxied = false)
    .WithLifetime(ContainerLifetime.Persistent);

// OpenObserve — kalıcı log deposu (OTLP alıcısı)
//
// Neden ayrı bir depo: Aspire dashboard OTLP'yi yutar ama BELLEKTE tutar; AppHost kapanınca
// her şey gider. #136'da (süresi dolmuş token döngüsü) teşhis yalnız sunucu logu sayesinde
// mümkün oldu — o log kalıcı olmasaydı hata görülmeyecekti.
//
// İmaj: açık kaynak olan `public.ecr.aws/zinclabs/openobserve`. Resmî dokümandaki
// `docker run` komutu ENTERPRISE imajını (`o2cr.ai/.../openobserve-enterprise`) gösterir;
// bilerek kullanılmıyor.
//
// Etiket: `latest`. OpenObserve'un `slim` diye bir etiketi YOKTUR — iki seçenek var:
// `latest` (tek statik Rust binary, her ortamda çalışır) ve `latest-simd` (AVX512/NEON
// ister). Zaten ince olan `latest`; simd varyantı eski x86 sunucularda başlamayabilir.
var openObserve = builder.AddContainer("openobserve", "public.ecr.aws/zinclabs/openobserve", "latest")
    .WithHttpEndpoint(port: 5080, targetPort: 5080, name: "http")
    .WithEndpoint(port: 5081, targetPort: 5081, name: "grpc", scheme: "http")
    .WithEnvironment("ZO_DATA_DIR", "/data")
    .WithEnvironment("ZO_ROOT_USER_EMAIL", openObserveUser)
    .WithEnvironment("ZO_ROOT_USER_PASSWORD", openObservePassword)
    // Kapatılmazsa açılışta dışarıya kullanım telemetrisi gönderir
    // ("sending a track event OpenObserve - Starting server"). Öğrenci verisi işleyen bir
    // kurulumda gözlemlenebilirlik aracının kendisi dışarı veri sızdırmamalıdır.
    .WithEnvironment("ZO_TELEMETRY_ENABLED", "false")
    .WithVolume("openobserve-data", "/data")
    // Uç doğrulandı: container ayağa kalktıktan sonra GET /healthz → 200.
    .WithHttpHealthCheck("/healthz", endpointName: "http")
    .WithEndpoint("http", e => e.IsProxied = false)
    .WithEndpoint("grpc", e => e.IsProxied = false)
    .WithLifetime(ContainerLifetime.Persistent);

var api = builder.AddProject<Projects.MESNET_Presentation>("mesnet-api")
    .WithExternalHttpEndpoints()
    .WithReference(postgres)
    .WithReference(rabbitmq)
    .WithReference(keycloak)
    .WithEnvironment("MinioStorage__Endpoint", rustfs.GetEndpoint("api"))
    .WithEnvironment("MinioStorage__AccessKey", minioUser)
    .WithEnvironment("MinioStorage__SecretKey", minioPassword)
    .WithEnvironment("SmtpSettings__Host", mailpit.Resource.Host)
    .WithEnvironment("SmtpSettings__Port", mailpit.Resource.Port)
    .WithEnvironment("Osrm__BaseUrl", osrm.GetEndpoint("osrm"))
    // Serilog'un OTLP sink'i OpenObserve'a yazar. Aspire'ın OTEL_EXPORTER_OTLP_ENDPOINT'i
    // BİLEREK ezilmiyor: o değişken trace ve metric'i de taşır ve ezilirse dev'de Aspire
    // dashboard'un izleme sekmesi boşalır. Üretimde üç sinyalin de OpenObserve'a gitmesi
    // için o değişken dağıtım tarafında bu uca yönlendirilir (bkz. #144).
    .WithEnvironment("OpenObserve__Endpoint", openObserve.GetEndpoint("grpc"))
    .WithEnvironment("OpenObserve__User", openObserveUser)
    .WithEnvironment("OpenObserve__Password", openObservePassword)
    .WaitFor(postgres)
    .WaitFor(rabbitmq)
    .WaitFor(keycloak)
    .WaitFor(rustfs)
    .WaitFor(mailpit)
    // WaitFor DEĞİL: log deposu erişilemezse uygulama yine de açılmalıdır. Gözlemlenebilirlik
    // altyapısını başlangıç bağımlılığı yapmak, teşhis aracını arıza kaynağına çevirir.
    .WithReference(openObserve.GetEndpoint("grpc"));

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
        .WithEndpoint("kroki", e => e.IsProxied = false)
        .WithLifetime(ContainerLifetime.Persistent);

    // Docusaurus docs site
    // Aspire.Hosting.JavaScript (NodeJs paketinin Aspire 13 devamı). Depo pnpm kullanır;
    // install:false — eski AddNpmApp paket kurmuyordu, davranış korunur.
    builder.AddJavaScriptApp("docs", "../../src/Docs", runScriptName: "start")
        .WithPnpm(install: false)
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
    // AddViteApp kendi "http" uç noktasını ekler; aşağıdaki WithHttpEndpoint aynı adı
    // günceller (sabit 5173 — Keycloak redirect URI'leri buna bağlı).
    builder.AddViteApp("frontend", "../../src/WebUI")
        .WithPnpm(install: false)
        .WithExternalHttpEndpoints()
        .WithReference(api)
        .WithEnvironment("VITE_API_URL", api.GetEndpoint("http"))
        .WithHttpEndpoint(port: 5173, env: "PORT")
        .WaitFor(api);
}

builder.Build().Run();
