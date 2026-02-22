using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using MESNET.Seeder;
using MESNET.Seeder.Seeders;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var options = new SeederOptions();

// appsettings.json → "Seeder" section'ından bind et
config.GetSection("Seeder").Bind(options);

// Aspire env var'ları: Seeder__ApiBaseUrl → override eder
var envApiUrl = config["Seeder:ApiBaseUrl"];
if (!string.IsNullOrEmpty(envApiUrl))
    options.ApiBaseUrl = envApiUrl;

var envTokenUrl = config["Seeder:KeycloakTokenUrl"];
if (!string.IsNullOrEmpty(envTokenUrl))
    options.KeycloakTokenUrl = envTokenUrl;

Console.WriteLine();
Console.WriteLine("═══ MESNET API Seeder ═══");
Console.WriteLine($"API: {options.ApiBaseUrl}");
Console.WriteLine($"Keycloak: {options.KeycloakTokenUrl}");

// HTTP client'ları kur
var httpClient = new HttpClient();
var tokenHttp = new HttpClient();
var tokenService = new KeycloakTokenService(tokenHttp, options);

// Token al
try
{
    var token = await tokenService.GetTokenAsync();
    Console.WriteLine("[AUTH] Token alındı ✓");
}
catch (Exception ex)
{
    Console.WriteLine($"[AUTH] Token alınamadı: {ex.Message}");
    Console.WriteLine("  Backend ve Keycloak'ın çalıştığından emin olun.");
    return 1;
}

httpClient.BaseAddress = new Uri(options.ApiBaseUrl);
var api = new MesnetApiClient(httpClient, tokenService);
var ctx = new SeedContext();

// Keycloak Admin API — token URL'inden base URL'i çıkar (örn. http://localhost:8080)
var keycloakBaseUrl = new Uri(options.KeycloakTokenUrl).GetLeftPart(UriPartial.Authority);
var adminHttp = new HttpClient();
var keycloak = new KeycloakAdminService(adminHttp, keycloakBaseUrl);

var sw = Stopwatch.StartNew();

try
{
    // Step 1: Institution (kurum, branch, personel, schedule) + Keycloak güncelleme
    await InstitutionSeeder.SeedAsync(api, ctx, keycloak);

    // Her seeder kendi GET kontrolünü yapar — bağımsız idempotency
    // Step 2: Businesses
    await BusinessSeeder.SeedAsync(api, ctx);

    // Step 3-5: Teachers, Students, Placements
    await EnrollmentSeeder.SeedAsync(api, ctx);

    // Step 6: Contracts (event-sourced multi-step)
    await ContractSeeder.SeedAsync(api, ctx);

    // Step 7: Attendance (event-sourced multi-step)
    await AttendanceSeeder.SeedAsync(api, ctx);

    // Step 8: Payment config
    await PaymentSeeder.SeedAsync(api, ctx);

    // Step 9: Coordination
    await CoordinationSeeder.SeedAsync(api, ctx);
}
catch (HttpRequestException ex)
{
    Console.WriteLine();
    Console.WriteLine($"✗ HTTP hatası: {ex.Message}");
    Console.WriteLine("  Backend'in çalıştığından emin olun.");
    return 1;
}

sw.Stop();
Console.WriteLine();
Console.WriteLine($"═══ Tamamlandı ({sw.Elapsed.TotalSeconds:F1}s) ═══");
return 0;
