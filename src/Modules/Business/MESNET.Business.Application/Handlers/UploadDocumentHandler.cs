using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Application.Errors;
using MESNET.Business.Core.Enums;
using MESNET.Business.Core.ValueObjects;
using MESNET.Business.Shared.Events;
using MESNET.Common.Infrastructure.Storage;
using MESNET.Common.Shared;
using Microsoft.Extensions.Logging;

namespace MESNET.Business.Application.Handlers;

public static class UploadDocumentHandler
{
    internal const string BusinessBucketName = "business-documents";
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png"
    };

    private static readonly byte[] PdfMagicBytes = "%PDF-"u8.ToArray();
    private static readonly byte[] JpegMagicBytes = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngMagicBytes = [0x89, 0x50, 0x4E, 0x47];

    public static async Task<BusinessDocumentUploaded> Handle(
        UploadDocument command,
        IDocumentSession session,
        IFileStorageService fileStorage,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("UploadDocumentHandler");

        // 1. İşletme kontrolü
        var business = await session.LoadAsync<Core.Entities.Business>(command.BusinessId, cancellationToken);
        if (business is null)
            throw new DomainException(BusinessErrors.NotFound(command.BusinessId));

        // 2. Dosya validasyonu
        if (command.DocumentFile is null || command.DocumentFile.Length == 0)
            throw new DomainException(BusinessErrors.FileNull());

        if (command.DocumentFile.Length > MaxFileSizeBytes)
            throw new DomainException(BusinessErrors.FileTooLarge(command.DocumentFile.Length, MaxFileSizeBytes));

        if (!AllowedContentTypes.Contains(command.DocumentFile.ContentType))
            throw new DomainException(BusinessErrors.InvalidFileType(command.DocumentFile.ContentType));

        // 3. Magic byte kontrolü
        await using var stream = command.DocumentFile.OpenReadStream();
        var buffer = new byte[5]; // En uzun magic byte dizisi (PDF: 5 byte)
        var bytesRead = await stream.ReadAsync(buffer, cancellationToken);

        var isValidContent = command.DocumentFile.ContentType.ToLowerInvariant() switch
        {
            "application/pdf" => bytesRead >= PdfMagicBytes.Length && buffer.AsSpan(0, PdfMagicBytes.Length).SequenceEqual(PdfMagicBytes),
            "image/jpeg" => bytesRead >= JpegMagicBytes.Length && buffer.AsSpan(0, JpegMagicBytes.Length).SequenceEqual(JpegMagicBytes),
            "image/png" => bytesRead >= PngMagicBytes.Length && buffer.AsSpan(0, PngMagicBytes.Length).SequenceEqual(PngMagicBytes),
            _ => false
        };

        if (!isValidContent)
            throw new DomainException(BusinessErrors.InvalidFileContent());

        // 4. Object path oluştur — aylık klasörleme: businesses/{id}/{yyyy}/{MM}/{type}_{ts}.{ext}
        var documentId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var timestamp = now.ToString("yyyyMMddHHmmss");
        var extension = command.DocumentFile.ContentType.ToLowerInvariant() switch
        {
            "application/pdf" => "pdf",
            "image/jpeg" => "jpg",
            "image/png" => "png",
            _ => "bin"
        };
        var objectPath = $"businesses/{command.BusinessId:N}/{now:yyyy}/{now:MM}/{command.Type.Name}_{timestamp}.{extension}";

        // 5. MinIO upload
        stream.Position = 0;
        var metadata = new Dictionary<string, string>
        {
            ["x-amz-meta-business-id"] = command.BusinessId.ToString(),
            ["x-amz-meta-document-id"] = documentId.ToString(),
            ["x-amz-meta-document-type"] = command.Type.Name,
            ["x-amz-meta-uploaded-at"] = DateTime.UtcNow.ToString("O")
        };

        var uploadResult = await fileStorage.UploadFileAsync(
            BusinessBucketName,
            objectPath,
            stream,
            command.DocumentFile.ContentType,
            metadata,
            cancellationToken);

        if (uploadResult.IsFailure)
        {
            logger.LogError("MinIO upload başarısız: {Error}", uploadResult.Error.Description);
            throw new DomainException(uploadResult.Error);
        }

        // 6. Entity'ye belge ekle
        var document = new BusinessDocument
        {
            Id = documentId,
            Type = command.Type,
            Status = DocumentStatus.Uploaded,
            FileName = command.DocumentFile.FileName,
            StoragePath = objectPath,
            ExpiresAt = command.ExpiresAt
        };

        business.Documents.Add(document);
        session.Store(business);

        logger.LogInformation(
            "İşletme belgesi yüklendi: BusinessId={BusinessId}, DocumentId={DocumentId}, Path={Path}",
            command.BusinessId, documentId, objectPath);

        return new BusinessDocumentUploaded(business.Id, document.Id, document.Type.Name);
    }
}
