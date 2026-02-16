using MESNET.Common.Shared;

namespace MESNET.Common.Infrastructure.Storage;

/// <summary>
/// Dosya depolama servisi için abstraction.
/// MinIO, Azure Blob, AWS S3 gibi farklı implementasyonlar kullanılabilir.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Dosyayı belirtilen bucket'a yükler.
    /// </summary>
    /// <param name="bucketName">Bucket adı (örn: payment-receipts)</param>
    /// <param name="objectPath">Object path (örn: tenantId/studentId/month/guid.pdf)</param>
    /// <param name="stream">Dosya stream'i</param>
    /// <param name="contentType">MIME type (örn: application/pdf)</param>
    /// <param name="metadata">Ek metadata (opsiyonel)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Upload edilen dosyanın object path'i</returns>
    Task<Result<string>> UploadFileAsync(
        string bucketName,
        string objectPath,
        Stream stream,
        string contentType,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dosya için presigned download URL oluşturur.
    /// </summary>
    /// <param name="bucketName">Bucket adı</param>
    /// <param name="objectPath">Object path</param>
    /// <param name="expiresInSeconds">URL geçerlilik süresi (saniye, default: 7 gün)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Presigned URL</returns>
    Task<Result<string>> GetPresignedDownloadUrlAsync(
        string bucketName,
        string objectPath,
        int expiresInSeconds = 604800, // 7 days
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dosyayı siler.
    /// </summary>
    Task<Result> DeleteFileAsync(
        string bucketName,
        string objectPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bucket'ın var olup olmadığını kontrol eder, yoksa oluşturur.
    /// </summary>
    Task<Result> EnsureBucketExistsAsync(
        string bucketName,
        CancellationToken cancellationToken = default);
}
