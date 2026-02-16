using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Core.Enums;
using MESNET.Business.Shared.Events;

namespace MESNET.Business.Application.Handlers;

public static class ApproveDocumentHandler
{
    public static async Task<BusinessDocumentApproved> Handle(ApproveDocument command, IDocumentSession session)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(command.BusinessId)
            ?? throw new InvalidOperationException($"İşletme bulunamadı: {command.BusinessId}");

        var document = business.Documents.FirstOrDefault(d => d.Id == command.DocumentId)
            ?? throw new InvalidOperationException($"Belge bulunamadı: {command.DocumentId}");

        document.Status = DocumentStatus.Approved;
        document.ApprovedAt = DateTime.UtcNow;

        session.Store(business);

        return new BusinessDocumentApproved(business.Id, document.Id);
    }
}
