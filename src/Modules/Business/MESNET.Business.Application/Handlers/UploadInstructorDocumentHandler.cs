using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Core.Enums;
using MESNET.Business.Core.ValueObjects;
using MESNET.Business.Shared.Events;
using MESNET.Common.Infrastructure.Storage;
using MESNET.Common.Shared;
using Microsoft.Extensions.Logging;

namespace MESNET.Business.Application.Handlers;

public static class UploadInstructorDocumentHandler
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    private const string AllowedContentType = "application/pdf";
    private static readonly byte[] PdfMagicBytes = "%PDF-"u8.ToArray();

    public static async Task<InstructorDocumentUploaded> Handle(
        UploadInstructorDocument command,
        IDocumentSession session,
        IFileStorageService fileStorage,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        // 1. İşletme mevcut mu kontrol et
        var business = await session.LoadAsync<Core.Entities.Business>(command.BusinessId, cancellationToken);
        if (business is null)
        {
            throw new DomainException(new Error("BUSINESS_NOT_FOUND", $"İşletme bulunamadı: {command.BusinessId}"));
        }

        // 2. Dosya validasyonu
        if (command.DocumentFile is null || command.DocumentFile.Length == 0)
        {
            throw new DomainException(FileUploadError.FileNull());
        }

        if (command.DocumentFile.Length > MaxFileSizeBytes)
        {
            throw new DomainException(FileUploadError.FileTooLarge(command.DocumentFile.Length, MaxFileSizeBytes));
        }

        if (!command.DocumentFile.ContentType.Equals(AllowedContentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException(FileUploadError.InvalidFileType(command.DocumentFile.ContentType));
        }

        // 3. Magic byte kontrolü
        await using var stream = command.DocumentFile.OpenReadStream();
        var buffer = new byte[PdfMagicBytes.Length];
        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

        if (bytesRead < PdfMagicBytes.Length || !buffer.AsSpan().SequenceEqual(PdfMagicBytes))
        {
            throw new DomainException(FileUploadError.InvalidFileContent());
        }

        // 4. Object path — aylık klasörleme: businesses/{id}/{yyyy}/{MM}/instructor-cert_{ts}.pdf
        var documentId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var timestamp = now.ToString("yyyyMMddHHmmss");
        var objectPath = $"businesses/{command.BusinessId:N}/{now:yyyy}/{now:MM}/instructor-cert_{timestamp}.pdf";

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
            UploadDocumentHandler.BusinessBucketName,
            objectPath,
            stream,
            AllowedContentType,
            metadata,
            cancellationToken);

        if (uploadResult.IsFailure)
        {
            var logger = loggerFactory.CreateLogger("UploadInstructorDocument");
            logger.LogError("MinIO upload başarısız: {Error}", uploadResult.Error.Description);
            throw new DomainException(uploadResult.Error);
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

        return @event;
    }
}
