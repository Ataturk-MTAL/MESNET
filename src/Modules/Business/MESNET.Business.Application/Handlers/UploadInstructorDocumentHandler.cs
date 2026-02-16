using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Core.Enums;
using MESNET.Business.Core.ValueObjects;
using MESNET.Business.Shared.Events;
using MESNET.Common.Infrastructure.Storage;
using MESNET.Common.Shared;
using MESNET.Payment.Application.Errors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MESNET.Business.Application.Handlers;

public static class UploadInstructorDocumentHandler
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    private const string AllowedContentType = "application/pdf";
    private static readonly byte[] PdfMagicBytes = "%PDF-"u8.ToArray();

    public static async Task<Result<InstructorDocumentUploaded>> Handle(
        UploadInstructorDocument command,
        IDocumentSession session,
        IFileStorageService fileStorage,
        IOptions<MinioStorageOptions> minioOptions,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        // 1. İşletme mevcut mu kontrol et
        var business = await session.LoadAsync<Core.Entities.Business>(command.BusinessId, cancellationToken);
        if (business is null)
        {
            return Result<InstructorDocumentUploaded>.Failure(
                new Error("BUSINESS_NOT_FOUND", $"İşletme bulunamadı: {command.BusinessId}"));
        }

        // 2. Dosya validasyonu
        if (command.DocumentFile is null || command.DocumentFile.Length == 0)
        {
            return Result<InstructorDocumentUploaded>.Failure(FileUploadError.FileNull());
        }

        if (command.DocumentFile.Length > MaxFileSizeBytes)
        {
            return Result<InstructorDocumentUploaded>.Failure(
                FileUploadError.FileTooLarge(command.DocumentFile.Length, MaxFileSizeBytes));
        }

        if (!command.DocumentFile.ContentType.Equals(AllowedContentType, StringComparison.OrdinalIgnoreCase))
        {
            return Result<InstructorDocumentUploaded>.Failure(
                FileUploadError.InvalidFileType(command.DocumentFile.ContentType));
        }

        // 3. Magic byte kontrolü
        await using var stream = command.DocumentFile.OpenReadStream();
        var buffer = new byte[PdfMagicBytes.Length];
        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

        if (bytesRead < PdfMagicBytes.Length || !buffer.AsSpan().SequenceEqual(PdfMagicBytes))
        {
            return Result<InstructorDocumentUploaded>.Failure(FileUploadError.InvalidFileContent());
        }

        // 4. Object path: businesses/{businessId}/instructor-cert_{timestamp}.pdf
        var documentId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var objectPath = $"businesses/{command.BusinessId:N}/instructor-cert_{timestamp}.pdf";

        // 5. Metadata
        var metadata = new Dictionary<string, string>
        {
            ["x-amz-meta-business-id"] = command.BusinessId.ToString(),
            ["x-amz-meta-document-id"] = documentId.ToString(),
            ["x-amz-meta-document-type"] = "MasterInstructorCertificate",
            ["x-amz-meta-uploaded-by"] = command.UploadedBy,
            ["x-amz-meta-uploaded-at"] = DateTime.UtcNow.ToString("O")
        };

        if (command.ExpiresAt.HasValue)
        {
            metadata["x-amz-meta-expires-at"] = command.ExpiresAt.Value.ToString("O");
        }

        // 6. MinIO upload
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
            var logger = loggerFactory.CreateLogger("UploadInstructorDocument");
            logger.LogError("MinIO upload başarısız: {Error}", uploadResult.Error.Description);
            return Result<InstructorDocumentUploaded>.Failure(uploadResult.Error);
        }

        // 7. Business entity'ye document ekle (objectPath ile, URL on-demand generate edilecek)
        var document = new BusinessDocument
        {
            Id = documentId,
            Type = DocumentType.MasterInstructorCertificate,
            Status = DocumentStatus.Uploaded,
            FileName = command.DocumentFile.FileName,
            StoragePath = objectPath,
            UploadedAt = DateTime.UtcNow,
            ExpiresAt = command.ExpiresAt
        };

        business.Documents.Add(document);
        session.Store(business);
        await session.SaveChangesAsync(cancellationToken);

        // 8. Event döndür (objectPath ile, URL on-demand generate edilecek)
        var @event = new InstructorDocumentUploaded(
            command.BusinessId,
            documentId,
            objectPath,
            command.UploadedBy,
            DateTime.UtcNow,
            command.ExpiresAt
        );

        var successLogger = loggerFactory.CreateLogger("UploadInstructorDocument");
        successLogger.LogInformation(
            "Usta öğretici belgesi yüklendi: BusinessId={BusinessId}, DocumentId={DocumentId}",
            command.BusinessId, documentId);

        return Result<InstructorDocumentUploaded>.Success(@event);
    }
}
