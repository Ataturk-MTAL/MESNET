var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .AddDatabase("mesnet");

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin();

var keycloak = builder.AddKeycloak("keycloak", port: 8080)
    .WithRealmImport("./keycloak");

// MinIO (S3-compatible object storage)
var minio = builder.AddContainer("minio", "minio/minio", "latest")
    .WithHttpEndpoint(port: 9000, targetPort: 9000, name: "api")
    .WithHttpEndpoint(port: 9001, targetPort: 9001, name: "console")
    .WithEnvironment("MINIO_ROOT_USER", "minioadmin")
    .WithEnvironment("MINIO_ROOT_PASSWORD", "minioadmin")
    .WithArgs("server", "/data", "--console-address", ":9001")
    .WithVolume("minio-data", "/data");

builder.AddProject<Projects.MESNET_Presentation>("mesnet-api")
    .WithReference(postgres)
    .WithReference(rabbitmq)
    .WithReference(keycloak)
    .WaitFor(postgres)
    .WaitFor(rabbitmq)
    .WaitFor(minio);

builder.Build().Run();
