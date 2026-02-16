using MESNET.Common.Infrastructure.Storage;
using MESNET.Common.Shared;
using MESNET.Payment.Application.Commands;
using MESNET.Payment.Application.Errors;
using MESNET.Payment.Shared.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MESNET.Payment.Application.Handlers;

public static class UploadReceiptByStudentHandler
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private const string AllowedContentType = "application/pdf";
    private static readonly byte[] PdfMagicBytes = "%PDF-"u8.ToArray();

    public static async Task<Result<ReceiptUploadedByStudent>> Handle(
        UploadReceiptByStudent command,
        IFileStorageService fileStorage,
        IOptions<MinioStorageOptions> minioOptions,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        // 1. Dosya null kontrolü
        if (command.ReceiptFile is null || command.ReceiptFile.Length == 0)
        {
            return Result<ReceiptUploadedByStudent>.Failure(FileUploadError.FileNull());
        }

        // 2. Boyut kontrolü
        if (command.ReceiptFile.Length > MaxFileSizeBytes)
        {
            return Result<ReceiptUploadedByStudent>.Failure(
                FileUploadError.FileTooLarge(command.ReceiptFile.Length, MaxFileSizeBytes));
        }

        // 3. Content-Type kontrolü
        if (!command.ReceiptFile.ContentType.Equals(AllowedContentType, StringComparison.OrdinalIgnoreCase))
        {
            return Result<ReceiptUploadedByStudent>.Failure(
                FileUploadError.InvalidFileType(command.ReceiptFile.ContentType));
        }

        // 4. Magic byte kontrolü (ilk 5 byte: %PDF-)
        await using var stream = command.ReceiptFile.OpenReadStream();
        var buffer = new byte[PdfMagicBytes.Length];
        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

        if (bytesRead < PdfMagicBytes.Length || !buffer.AsSpan().SequenceEqual(PdfMagicBytes))
        {
            return Result<ReceiptUploadedByStudent>.Failure(FileUploadError.InvalidFileContent());
        }

        // 5. Object path oluştur: default/{studentId}/{year-MM}/{guid}_{timestamp}.pdf
        var receiptId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var objectPath = $"default/{command.StudentId:N}/{command.Year:D4}-{command.Month:D2}/{receiptId:N}_{timestamp}.pdf";

        // 6. Metadata hazırla
        var metadata = new Dictionary<string, string>
        {
            ["x-amz-meta-student-id"] = command.StudentId.ToString(),
            ["x-amz-meta-month"] = command.Month.ToString(),
            ["x-amz-meta-year"] = command.Year.ToString(),
            ["x-amz-meta-uploaded-by"] = "Student",
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
            var logger = loggerFactory.CreateLogger("UploadReceiptByStudent");
            logger.LogError("MinIO upload başarısız: {Error}", uploadResult.Error.Description);
            return Result<ReceiptUploadedByStudent>.Failure(uploadResult.Error);
        }

        // 8. Event döndür (objectPath ile, URL on-demand generate edilecek)
        var @event = new ReceiptUploadedByStudent(
            command.SalaryPeriodId,
            receiptId,
            objectPath,
            DateTime.UtcNow
        );

        var successLogger = loggerFactory.CreateLogger("UploadReceiptByStudent");
        successLogger.LogInformation(
            "Dekont öğrenci tarafından yüklendi: SalaryPeriodId={SalaryPeriodId}, ReceiptId={ReceiptId}, StudentId={StudentId}",
            command.SalaryPeriodId, receiptId, command.StudentId);

        return Result<ReceiptUploadedByStudent>.Success(@event);
    }
}
