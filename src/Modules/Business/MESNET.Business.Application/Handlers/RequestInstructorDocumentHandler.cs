using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Shared.Events;
using MESNET.Common.Shared;

namespace MESNET.Business.Application.Handlers;

public static class RequestInstructorDocumentHandler
{
    public static async Task<InstructorDocumentRequested> Handle(
        RequestInstructorDocument command,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        // 1. İşletme kontrolü
        var business = await session.LoadAsync<Core.Entities.Business>(command.BusinessId, cancellationToken);
        if (business is null)
        {
            throw new DomainException(new Error("BUSINESS_NOT_FOUND", $"İşletme bulunamadı: {command.BusinessId}"));
        }

        // 2. Event döndür (işletmeye bildirim gönderilir)
        return new InstructorDocumentRequested(
            command.BusinessId,
            command.RequestedBy,
            command.Reason,
            DateTime.UtcNow,
            command.Deadline
        );
    }
}
