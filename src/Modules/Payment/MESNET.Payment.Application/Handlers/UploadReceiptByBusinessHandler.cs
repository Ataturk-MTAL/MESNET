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

    public static async Task<(Guid, ReceiptUploadedByBusiness)> Handle(
        UploadReceiptByBusiness command,
        IFileStorageService fileStorage,
        IOptions<MinioStorageOptions> minioOptions,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        // 1. Dosya null kontrolü
        if (command.ReceiptFile is null || command.ReceiptFile.Length == 0)
            throw new DomainException(FileUploadError.FileNull());

        // 2. Boyut kontrolü
        if (command.ReceiptFile.Length > MaxFileSizeBytes)
            throw new DomainException(FileUploadError.FileTooLarge(command.ReceiptFile.Length, MaxFileSizeBytes));

        // 3. Content-Type kontrolü
        if (!command.ReceiptFile.ContentType.Equals(AllowedContentType, StringComparison.OrdinalIgnoreCase))
            throw new DomainException(FileUploadError.InvalidFileType(command.ReceiptFile.ContentType));

        // 4. Magic byte kontrolü (ilk 5 byte: %PDF-)
        await using var stream = command.ReceiptFile.OpenReadStream();
        var buffer = new byte[PdfMagicBytes.Length];
        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

        if (bytesRead < PdfMagicBytes.Length || !buffer.AsSpan().SequenceEqual(PdfMagicBytes))
            throw new DomainException(FileUploadError.InvalidFileContent());

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
            minioOptions.Value.DefaultBucket, objectPath, stream,
            AllowedContentType, metadata, cancellationToken);

        if (uploadResult.IsFailure)
        {
            loggerFactory.CreateLogger("UploadReceiptByBusiness")
                .LogError("MinIO upload başarısız: {Error}", uploadResult.Error.Description);
            throw new DomainException(uploadResult.Error);
        }

        loggerFactory.CreateLogger("UploadReceiptByBusiness")
            .LogInformation("Dekont işletme tarafından yüklendi: SalaryPeriodId={SalaryPeriodId}, ReceiptId={ReceiptId}",
                command.SalaryPeriodId, receiptId);

        return (receiptId, new ReceiptUploadedByBusiness(
            command.SalaryPeriodId, receiptId, objectPath, "Business", DateTime.UtcNow));
    }
}
