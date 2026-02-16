using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Minio;

namespace MESNET.Common.Infrastructure.Storage;

/// <summary>
/// MinIO bağlantısının sağlığını kontrol eden health check.
/// Aspire Dashboard'da görünür olacak.
/// </summary>
public sealed class MinioHealthCheck : IHealthCheck
{
    private readonly IMinioClient _minioClient;
    private readonly ILogger<MinioHealthCheck> _logger;

    public MinioHealthCheck(IMinioClient minioClient, ILogger<MinioHealthCheck> logger)
    {
        _minioClient = minioClient;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Basit bucket list ile connectivity check
            await _minioClient.ListBucketsAsync(cancellationToken);

            return HealthCheckResult.Healthy("MinIO bağlantısı aktif.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MinIO health check başarısız.");
            return HealthCheckResult.Unhealthy("MinIO bağlantısı başarısız.", ex);
        }
    }
}
