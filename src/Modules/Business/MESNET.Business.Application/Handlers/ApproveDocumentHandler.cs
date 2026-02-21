using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Application.Errors;
using MESNET.Business.Core.Enums;
using MESNET.Business.Shared.Events;
using MESNET.Common.Shared;

namespace MESNET.Business.Application.Handlers;

public static class ApproveDocumentHandler
{
    public static async Task<BusinessDocumentApproved> Handle(ApproveDocument command, IDocumentSession session)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(command.BusinessId);
        if (business is null)
            throw new DomainException(BusinessErrors.NotFound(command.BusinessId));

        var document = business.Documents.FirstOrDefault(d => d.Id == command.DocumentId);
        if (document is null)
            throw new DomainException(BusinessErrors.DocumentNotFound(command.DocumentId));

        document.Status = DocumentStatus.Approved;
        document.ApprovedAt = DateTime.UtcNow;

        session.Store(business);

        return new BusinessDocumentApproved(business.Id, document.Id);
    }
}
