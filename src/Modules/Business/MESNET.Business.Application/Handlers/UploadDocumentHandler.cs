using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Core.ValueObjects;
using MESNET.Business.Shared.Events;

namespace MESNET.Business.Application.Handlers;

public static class UploadDocumentHandler
{
    public static async Task<BusinessDocumentUploaded> Handle(UploadDocument command, IDocumentSession session)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(command.BusinessId)
            ?? throw new InvalidOperationException($"İşletme bulunamadı: {command.BusinessId}");

        var document = new BusinessDocument
        {
            Type = command.Type,
            FileName = command.FileName,
            StoragePath = command.StoragePath,
            ExpiresAt = command.ExpiresAt
        };

        business.Documents.Add(document);

        session.Store(business);

        return new BusinessDocumentUploaded(business.Id, document.Id, document.Type);
    }
}
