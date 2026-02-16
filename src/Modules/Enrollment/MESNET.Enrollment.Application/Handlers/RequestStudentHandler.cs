using Marten;
using MESNET.Enrollment.Application.Commands;
using MESNET.Enrollment.Core.Enums;
using MESNET.Enrollment.Shared.Events;

namespace MESNET.Enrollment.Application.Handlers;

public static class RequestStudentHandler
{
    public static InternshipApplied Handle(RequestStudent command, IDocumentSession session)
    {
        return new InternshipApplied(
            Guid.Empty,
            command.BusinessId,
            command.BranchCode,
            ApplicationSource.BusinessRequest.Name);
    }
}
