using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Application.Errors;
using MESNET.Common.Infrastructure.Storage;
using MESNET.Common.Shared;
using Microsoft.Extensions.Logging;

namespace MESNET.Business.Application.Handlers;

public static class DeleteBusinessDocumentHandler
{
    public static async Task Handle(
        DeleteBusinessDocument command,
        IDocumentSession session,
        IFileStorageService fileStorage,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("DeleteBusinessDocumentHandler");

        var business = await session.LoadAsync<Core.Entities.Business>(command.BusinessId, cancellationToken);
        if (business is null)
            throw new DomainException(BusinessErrors.NotFound(command.BusinessId));

        var document = business.Documents.FirstOrDefault(d => d.Id == command.DocumentId);
        if (document is null)
            throw new DomainException(BusinessErrors.DocumentNotFound(command.DocumentId));

        // MinIO'dan sil (storagePath varsa)
        if (!string.IsNullOrEmpty(document.StoragePath))
        {
            var deleteResult = await fileStorage.DeleteFileAsync(
                UploadDocumentHandler.BusinessBucketName,
                document.StoragePath,
                cancellationToken);

            if (deleteResult.IsFailure)
                logger.LogWarning("MinIO dosya silme başarısız: {Error}", deleteResult.Error.Description);
        }

        // Entity'den kaldır
        business.Documents.Remove(document);
        session.Store(business);

        logger.LogInformation(
            "İşletme belgesi silindi: BusinessId={BusinessId}, DocumentId={DocumentId}",
            command.BusinessId, command.DocumentId);
    }
}
