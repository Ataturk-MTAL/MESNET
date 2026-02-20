using MESNET.Common.Infrastructure.Storage;
using MESNET.Common.Shared;
using MESNET.Payment.Application.Commands;
using MESNET.Payment.Application.Errors;
using MESNET.Payment.Shared.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MESNET.Payment.Application.Handlers;

public static class UploadReceiptByBusinessHandler
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private const string AllowedContentType = "application/pdf";
    private static readonly byte[] PdfMagicBytes = "%PDF-"u8.ToArray();

    public static async Task<(Result<Guid>, ReceiptUploadedByBusiness?)> Handle(
        UploadReceiptByBusiness command,
        IFileStorageService fileStorage,
        IOptions<MinioStorageOptions> minioOptions,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        // 1. Dosya null kontrolü
        if (command.ReceiptFile is null || command.ReceiptFile.Length == 0)
        {
            return (Result<Guid>.Failure(FileUploadError.FileNull()), null);
        }

        // 2. Boyut kontrolü
        if (command.ReceiptFile.Length > MaxFileSizeBytes)
        {
            return (Result<Guid>.Failure(
                FileUploadError.FileTooLarge(command.ReceiptFile.Length, MaxFileSizeBytes)), null);
        }

        // 3. Content-Type kontrolü
        if (!command.ReceiptFile.ContentType.Equals(AllowedContentType, StringComparison.OrdinalIgnoreCase))
        {
            return (Result<Guid>.Failure(
                FileUploadError.InvalidFileType(command.ReceiptFile.ContentType)), null);
        }

        // 4. Magic byte kontrolü (ilk 5 byte: %PDF-)
        await using var stream = command.ReceiptFile.OpenReadStream();
        var buffer = new byte[PdfMagicBytes.Length];
        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

        if (bytesRead < PdfMagicBytes.Length || !buffer.AsSpan().SequenceEqual(PdfMagicBytes))
        {
            return (Result<Guid>.Failure(FileUploadError.InvalidFileContent()), null);
        }

        // 5. Object path oluştur: default/{studentId}/{year-MM}/{guid}_{timestamp}.pdf
        var receiptId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var objectPath = $"default/{command.StudentId:N}/{command.Year:D4}-{command.Month:D2}/{receiptId:N}_{timestamp}.pdf";

        // 6. Metadata hazırla
        var metadata = new Dictionary<string, string>
        {
            ["x-amz-meta-student-id"] = command.StudentId.ToString(),
            ["x-amz-meta-business-id"] = command.BusinessId.ToString(),
            ["x-amz-meta-month"] = command.Month.ToString(),
            ["x-amz-meta-year"] = command.Year.ToString(),
            ["x-amz-meta-uploaded-by"] = "Business",
            ["x-amz-meta-uploaded-at"] = DateTime.UtcNow.ToString("O")
        };

        // 7. MinIO'ya upload
        stream.Position = 0;
        var uploadResult = await fileStorage.UploadFileAsync(
            minioOptions.Value.DefaultBucket,
            objectPath,
            stream,
            AllowedContentType,
            metadata,
            cancellationToken);

        if (uploadResult.IsFailure)
        {
            var logger = loggerFactory.CreateLogger("UploadReceiptByBusiness");
            logger.LogError("MinIO upload başarısız: {Error}", uploadResult.Error.Description);
            return (Result<Guid>.Failure(uploadResult.Error), null);
        }

        // 8. Event döndür (objectPath ile, URL on-demand generate edilecek)
        var @event = new ReceiptUploadedByBusiness(
            command.SalaryPeriodId,
            receiptId,
            objectPath,
            "Business", // UploadedBy
            DateTime.UtcNow
        );

        var successLogger = loggerFactory.CreateLogger("UploadReceiptByBusiness");
        successLogger.LogInformation(
            "Dekont işletme tarafından yüklendi: SalaryPeriodId={SalaryPeriodId}, ReceiptId={ReceiptId}, StudentId={StudentId}",
            command.SalaryPeriodId, receiptId, command.StudentId);

        return (Result<Guid>.Success(receiptId), @event);
    }
}
