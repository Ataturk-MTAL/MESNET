using Marten;
using MESNET.Common.Shared;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Application.Errors;
using MESNET.Institution.Core.Entities;
using MESNET.Institution.Core.Enums;
using MESNET.Institution.Shared.Events;

namespace MESNET.Institution.Application.Handlers;

public static class SetGradeEntryWindowHandler
{
    public static async Task<GradeEntryWindowSet> Handle(
        SetGradeEntryWindow command, IDocumentSession session)
    {
        if (command.EndDate < command.StartDate)
            throw new DomainException(InstitutionErrors.InvalidGradeEntryWindow());

        var period = await session.LoadAsync<AcademicPeriod>(command.AcademicPeriodId)
            ?? throw new DomainException(InstitutionErrors.AcademicPeriodNotFound(command.AcademicPeriodId));

        // Kapalı döneme pencere açılamaz (geçmiş dönem = salt okunur)
        if (period.Status.Name == AcademicPeriodStatus.Closed.Name)
            throw new DomainException(InstitutionErrors.AcademicPeriodAlreadyClosed(command.AcademicPeriodId));

        period.GradeEntryStartDate = command.StartDate;
        period.GradeEntryEndDate = command.EndDate;
        session.Store(period);

        // Cascading event → Coordination AcademicPeriodView'ini günceller
        return new GradeEntryWindowSet(period.Id, period.InstitutionId, command.StartDate, command.EndDate);
    }
}
