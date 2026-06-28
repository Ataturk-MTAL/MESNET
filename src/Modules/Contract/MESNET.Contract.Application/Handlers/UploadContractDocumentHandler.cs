using Marten;
using MESNET.Common.Infrastructure.Storage;
using MESNET.Common.Shared;
using MESNET.Contract.Application.Commands;
using MESNET.Contract.Application.Errors;
using MESNET.Contract.Core.Aggregates;
using MESNET.Contract.Core.Enums;
using MESNET.Contract.Shared.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MESNET.Contract.Application.Handlers;

public static class UploadContractDocumentHandler
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    private const string AllowedContentType = "application/pdf";
    private static readonly byte[] PdfMagicBytes = "%PDF-"u8.ToArray();

    public static async Task<ContractDocumentUploaded> Handle(
        UploadContractDocument command,
        IDocumentSession session,
        IFileStorageService fileStorage,
        IOptions<MinioStorageOptions> minioOptions,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        // 1. DocumentType doğrula
        if (!ContractDocumentType.TryFromName(command.DocumentType, true, out var documentType))
            throw new DomainException(ContractErrors.OperationFailed("UPLOAD", $"Geçersiz evrak türü: {command.DocumentType}"));

        // 2. Sözleşme mevcut mu kontrol et
        var contract = await session.LoadAsync<InternshipContract>(command.ContractId, cancellationToken);
        if (contract is null)
            throw new DomainException(ContractErrors.NotFound(command.ContractId));

        // 3. Dosya validasyonu
        if (command.DocumentFile is null || command.DocumentFile.Length == 0)
            throw new DomainException(FileUploadError.FileNull());

        if (command.DocumentFile.Length > MaxFileSizeBytes)
            throw new DomainException(FileUploadError.FileTooLarge(command.DocumentFile.Length, MaxFileSizeBytes));

        if (!command.DocumentFile.ContentType.Equals(AllowedContentType, StringComparison.OrdinalIgnoreCase))
            throw new DomainException(FileUploadError.InvalidFileType(command.DocumentFile.ContentType));

        // 4. Magic byte kontrolü
        await using var stream = command.DocumentFile.OpenReadStream();
        var buffer = new byte[PdfMagicBytes.Length];
        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

        if (bytesRead < PdfMagicBytes.Length || !buffer.AsSpan().SequenceEqual(PdfMagicBytes))
            throw new DomainException(FileUploadError.InvalidFileContent());

        // 5. Object path: contracts/{contractId}/{documentType}_{timestamp}.pdf
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var objectPath = $"contracts/{command.ContractId:N}/{documentType.Name.ToLowerInvariant()}_{timestamp}.pdf";

        // 6. Metadata
        var metadata = new Dictionary<string, string>
        {
            ["x-amz-meta-contract-id"]    = command.ContractId.ToString(),
            ["x-amz-meta-document-type"]  = documentType.Name,
            ["x-amz-meta-uploaded-by"]    = command.UploadedBy,
            ["x-amz-meta-uploaded-at"]    = DateTime.UtcNow.ToString("O")
        };

        if (!string.IsNullOrWhiteSpace(command.Description))
            metadata["x-amz-meta-description"] = command.Description;

        // 7. MinIO upload
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
            loggerFactory.CreateLogger("UploadContractDocument")
                .LogError("MinIO upload başarısız: {Error}", uploadResult.Error.Description);
            throw new DomainException(uploadResult.Error);
        }

        loggerFactory.CreateLogger("UploadContractDocument")
            .LogInformation(
                "Evrak yüklendi: ContractId={ContractId}, Type={Type}, UploadedBy={UploadedBy}",
                command.ContractId, documentType.Name, command.UploadedBy);

        return new ContractDocumentUploaded(
            command.ContractId,
            Guid.NewGuid(),
            documentType.Name,
            documentType.Slug,
            command.Description,
            objectPath,
            command.UploadedBy,
            DateTime.UtcNow);
    }
}
