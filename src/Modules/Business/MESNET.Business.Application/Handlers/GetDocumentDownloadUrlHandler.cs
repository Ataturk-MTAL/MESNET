using Marten;
using MESNET.Business.Application.Errors;
using MESNET.Business.Application.Queries;
using MESNET.Common.Infrastructure.Storage;
using MESNET.Common.Shared;

namespace MESNET.Business.Application.Handlers;

public static class GetDocumentDownloadUrlHandler
{
    public static async Task<string> Handle(
        GetDocumentDownloadUrl query,
        IQuerySession session,
        IFileStorageService fileStorage,
        CancellationToken cancellationToken)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(query.BusinessId, cancellationToken);
        if (business is null)
            throw new DomainException(BusinessErrors.NotFound(query.BusinessId));

        var document = business.Documents.FirstOrDefault(d => d.Id == query.DocumentId);
        if (document is null)
            throw new DomainException(BusinessErrors.DocumentNotFound(query.DocumentId));

        if (string.IsNullOrEmpty(document.StoragePath))
            throw new DomainException(BusinessErrors.DocumentHasNoFile(query.DocumentId));

        var urlResult = await fileStorage.GetPresignedDownloadUrlAsync(
            UploadDocumentHandler.BusinessBucketName,
            document.StoragePath,
            3600, // 1 saat
            cancellationToken);

        if (urlResult.IsFailure)
            throw new DomainException(urlResult.Error);

        return urlResult.Value;
    }
}
