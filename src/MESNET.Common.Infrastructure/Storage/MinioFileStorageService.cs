using MESNET.Common.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace MESNET.Common.Infrastructure.Storage;

/// <summary>
/// MinIO (S3-compatible) object storage implementation.
/// </summary>
public sealed class MinioFileStorageService : IFileStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly MinioStorageOptions _options;
    private readonly ILogger<MinioFileStorageService> _logger;

    public MinioFileStorageService(
        IMinioClient minioClient,
        IOptions<MinioStorageOptions> options,
        ILogger<MinioFileStorageService> logger)
    {
        _minioClient = minioClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<string>> UploadFileAsync(
        string bucketName,
        string objectPath,
        Stream stream,
        string contentType,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Bucket kontrolü
            var bucketExistsResult = await EnsureBucketExistsAsync(bucketName, cancellationToken);
            if (bucketExistsResult.IsFailure)
            {
                return Result<string>.Failure(bucketExistsResult.Error);
            }

            // Stream pozisyonunu başa al (idempotent upload)
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            var putObjectArgs = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectPath)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(contentType);

            // Metadata ekle (S3 standard: x-amz-meta-* prefix)
            if (metadata is not null && metadata.Count > 0)
            {
                putObjectArgs.WithHeaders(metadata);
            }

            await _minioClient.PutObjectAsync(putObjectArgs, cancellationToken);

            _logger.LogInformation(
                "Dosya başarıyla yüklendi: Bucket={Bucket}, Path={Path}, Size={Size}",
                bucketName, objectPath, stream.Length);

            return Result<string>.Success(objectPath);
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "MinIO upload hatası: Bucket={Bucket}, Path={Path}", bucketName, objectPath);
            return Result<string>.Failure(
                new Error("MINIO_UPLOAD_ERROR", $"Dosya yükleme başarısız: {ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Beklenmeyen upload hatası: Bucket={Bucket}, Path={Path}", bucketName, objectPath);
            return Result<string>.Failure(
                new Error("UPLOAD_ERROR", $"Dosya yükleme sırasında hata: {ex.Message}"));
        }
    }

    public async Task<Result<string>> GetPresignedDownloadUrlAsync(
        string bucketName,
        string objectPath,
        int expiresInSeconds = 604800,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var presignedGetObjectArgs = new PresignedGetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectPath)
                .WithExpiry(expiresInSeconds);

            var url = await _minioClient.PresignedGetObjectAsync(presignedGetObjectArgs);

            _logger.LogDebug(
                "Presigned URL oluşturuldu: Bucket={Bucket}, Path={Path}, ExpiresIn={Expires}s",
                bucketName, objectPath, expiresInSeconds);

            return Result<string>.Success(url);
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "MinIO presigned URL hatası: Bucket={Bucket}, Path={Path}", bucketName, objectPath);
            return Result<string>.Failure(
                new Error("MINIO_PRESIGNED_URL_ERROR", $"Presigned URL oluşturulamadı: {ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Beklenmeyen presigned URL hatası: Bucket={Bucket}, Path={Path}", bucketName, objectPath);
            return Result<string>.Failure(
                new Error("PRESIGNED_URL_ERROR", $"Presigned URL oluşturma hatası: {ex.Message}"));
        }
    }

    public async Task<Result> DeleteFileAsync(
        string bucketName,
        string objectPath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectPath);

            await _minioClient.RemoveObjectAsync(removeObjectArgs, cancellationToken);

            _logger.LogInformation("Dosya silindi: Bucket={Bucket}, Path={Path}", bucketName, objectPath);

            return Result.Success();
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "MinIO delete hatası: Bucket={Bucket}, Path={Path}", bucketName, objectPath);
            return Result.Failure(
                new Error("MINIO_DELETE_ERROR", $"Dosya silinemedi: {ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Beklenmeyen delete hatası: Bucket={Bucket}, Path={Path}", bucketName, objectPath);
            return Result.Failure(
                new Error("DELETE_ERROR", $"Dosya silme hatası: {ex.Message}"));
        }
    }

    public async Task<Result> EnsureBucketExistsAsync(
        string bucketName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var bucketExistsArgs = new BucketExistsArgs().WithBucket(bucketName);
            bool exists = await _minioClient.BucketExistsAsync(bucketExistsArgs, cancellationToken);

            if (!exists)
            {
                var makeBucketArgs = new MakeBucketArgs().WithBucket(bucketName);
                await _minioClient.MakeBucketAsync(makeBucketArgs, cancellationToken);

                _logger.LogInformation("Bucket oluşturuldu: {Bucket}", bucketName);
            }

            return Result.Success();
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "MinIO bucket kontrolü/oluşturma hatası: Bucket={Bucket}", bucketName);
            return Result.Failure(
                new Error("MINIO_BUCKET_ERROR", $"Bucket kontrolü başarısız: {ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Beklenmeyen bucket hatası: Bucket={Bucket}", bucketName);
            return Result.Failure(
                new Error("BUCKET_ERROR", $"Bucket hatası: {ex.Message}"));
        }
    }
}
