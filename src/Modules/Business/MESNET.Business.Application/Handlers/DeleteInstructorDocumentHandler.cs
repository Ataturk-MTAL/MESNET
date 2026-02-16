using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Core.Enums;
using MESNET.Business.Shared.Events;
using MESNET.Common.Shared;

namespace MESNET.Business.Application.Handlers;

public static class DeleteInstructorDocumentHandler
{
    public static async Task<Result<InstructorDocumentDeleted>> Handle(
        DeleteInstructorDocument command,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        // 1. İşletme ve belge kontrolü
        var business = await session.LoadAsync<Core.Entities.Business>(command.BusinessId, cancellationToken);
        if (business is null)
        {
            return Result<InstructorDocumentDeleted>.Failure(
                new Error("BUSINESS_NOT_FOUND", $"İşletme bulunamadı: {command.BusinessId}"));
        }

        var document = business.Documents.FirstOrDefault(d =>
            d.Id == command.DocumentId && d.Type == DocumentType.MasterInstructorCertificate);

        if (document is null)
        {
            return Result<InstructorDocumentDeleted>.Failure(
                new Error("DOCUMENT_NOT_FOUND", $"Usta öğretici belgesi bulunamadı: {command.DocumentId}"));
        }

        // 2. Belgeyi sil (listeden çıkar)
        business.Documents.Remove(document);

        session.Store(business);
        await session.SaveChangesAsync(cancellationToken);

        // 3. Event döndür
        var @event = new InstructorDocumentDeleted(
            command.BusinessId,
            command.DocumentId,
            command.DeletedBy,
            command.Reason,
            DateTime.UtcNow
        );

        return Result<InstructorDocumentDeleted>.Success(@event);
    }
}
