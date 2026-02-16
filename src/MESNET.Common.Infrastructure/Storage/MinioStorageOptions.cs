namespace MESNET.Common.Infrastructure.Storage;

/// <summary>
/// MinIO konfigürasyon ayarları.
/// appsettings.json'dan "MinioStorage" section'ından bind edilir.
/// </summary>
public sealed class MinioStorageOptions
{
    public const string SectionName = "MinioStorage";

    /// <summary>
    /// MinIO endpoint (örn: localhost:9000)
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Access Key (S3-compatible)
    /// </summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>
    /// Secret Key (S3-compatible)
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// HTTPS kullan (production: true, local dev: false)
    /// </summary>
    public bool UseSSL { get; set; } = false;

    /// <summary>
    /// Default bucket adı (örn: payment-receipts)
    /// </summary>
    public string DefaultBucket { get; set; } = "payment-receipts";
}
