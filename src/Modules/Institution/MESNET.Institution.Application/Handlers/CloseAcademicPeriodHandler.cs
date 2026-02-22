using Marten;
using MESNET.Common.Shared;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Application.Errors;
using MESNET.Institution.Core.Entities;
using MESNET.Institution.Core.Enums;
using MESNET.Institution.Shared.Events;

namespace MESNET.Institution.Application.Handlers;

public static class CloseAcademicPeriodHandler
{
    public static async Task<AcademicPeriodClosed> Handle(
        CloseAcademicPeriod command, IDocumentSession session)
    {
        var period = await session.LoadAsync<AcademicPeriod>(command.AcademicPeriodId)
            ?? throw new DomainException(InstitutionErrors.AcademicPeriodNotFound(command.AcademicPeriodId));

        if (period.Status.Name == AcademicPeriodStatus.Closed.Name)
            throw new DomainException(InstitutionErrors.AcademicPeriodAlreadyClosed(command.AcademicPeriodId));

        period.Status = AcademicPeriodStatus.Closed;
        period.ClosedAt = DateTime.UtcNow;
        session.Store(period);

        return new AcademicPeriodClosed(period.Id, period.InstitutionId, period.ClosedAt.Value);
    }
}
