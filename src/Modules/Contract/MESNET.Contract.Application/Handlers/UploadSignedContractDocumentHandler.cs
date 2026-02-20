using Marten;
using MESNET.Common.Infrastructure.Storage;
using MESNET.Common.Shared;
using MESNET.Contract.Application.Commands;
using MESNET.Contract.Core.Aggregates;
using MESNET.Contract.Shared.Events;
using MESNET.Payment.Application.Errors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MESNET.Contract.Application.Handlers;

public static class UploadSignedContractDocumentHandler
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB (sözleşme daha büyük olabilir)
    private const string AllowedContentType = "application/pdf";
    private static readonly byte[] PdfMagicBytes = "%PDF-"u8.ToArray();

    public static async Task<SignedContractDocumentUploaded> Handle(
        UploadSignedContractDocument command,
        IDocumentSession session,
        IFileStorageService fileStorage,
        IOptions<MinioStorageOptions> minioOptions,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        // 1. Sözleşme mevcut mu kontrol et
        var contract = await session.LoadAsync<InternshipContract>(command.ContractId, cancellationToken);
        if (contract is null)
            throw new DomainException("CONTRACT_NOT_FOUND", $"Sözleşme bulunamadı: {command.ContractId}");

        // 2. Dosya validasyonu
        if (command.DocumentFile is null || command.DocumentFile.Length == 0)
            throw new DomainException(FileUploadError.FileNull());

        if (command.DocumentFile.Length > MaxFileSizeBytes)
            throw new DomainException(FileUploadError.FileTooLarge(command.DocumentFile.Length, MaxFileSizeBytes));

        if (!command.DocumentFile.ContentType.Equals(AllowedContentType, StringComparison.OrdinalIgnoreCase))
            throw new DomainException(FileUploadError.InvalidFileType(command.DocumentFile.ContentType));

        // 3. Magic byte kontrolü
        await using var stream = command.DocumentFile.OpenReadStream();
        var buffer = new byte[PdfMagicBytes.Length];
        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

        if (bytesRead < PdfMagicBytes.Length || !buffer.AsSpan().SequenceEqual(PdfMagicBytes))
            throw new DomainException(FileUploadError.InvalidFileContent());

        // 4. Object path: contracts/{contractId}/signed-contract_{timestamp}.pdf
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var objectPath = $"contracts/{command.ContractId:N}/signed-contract_{timestamp}.pdf";

        // 5. Metadata
        var metadata = new Dictionary<string, string>
        {
            ["x-amz-meta-contract-id"] = command.ContractId.ToString(),
            ["x-amz-meta-document-type"] = "SignedContract",
            ["x-amz-meta-uploaded-by"] = command.UploadedBy,
            ["x-amz-meta-uploaded-at"] = DateTime.UtcNow.ToString("O")
        };

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
            loggerFactory.CreateLogger("UploadSignedContractDocument")
                .LogError("MinIO upload başarısız: {Error}", uploadResult.Error.Description);
            throw new DomainException(uploadResult.Error);
        }

        loggerFactory.CreateLogger("UploadSignedContractDocument")
            .LogInformation(
                "Islak imzalı sözleşme yüklendi: ContractId={ContractId}, UploadedBy={UploadedBy}",
                command.ContractId, command.UploadedBy);

        return new SignedContractDocumentUploaded(
            command.ContractId,
            objectPath,
            command.UploadedBy,
            DateTime.UtcNow);
    }
}
