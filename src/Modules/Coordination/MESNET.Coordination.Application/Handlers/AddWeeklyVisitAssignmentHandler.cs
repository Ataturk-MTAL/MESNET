using System.Globalization;
using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Handlers;

public static class AddWeeklyVisitAssignmentHandler
{
    public static async Task<Guid> Handle(
        AddWeeklyVisitAssignment command,
        IDocumentSession session,
        CancellationToken ct)
    {
        // Plan kontrolü
        var plan = await session.LoadAsync<WeeklyVisitPlan>(command.PlanId, ct);
        if (plan is null)
            throw new DomainException(CoordinationErrors.WeeklyVisitPlanNotFound(command.PlanId));

        if (plan.InstitutionId != command.InstitutionId)
            throw new DomainException(CoordinationErrors.WeeklyVisitPlanNotFound(command.PlanId));

        // Dönem kontrolü
        var period = await session.LoadAsync<AcademicPeriodView>(plan.AcademicPeriodId, ct);
        if (period is not null && !period.IsActive)
            throw new DomainException(CoordinationErrors.AcademicPeriodClosed(plan.AcademicPeriodId));

        // Gün doğrulama
        if (!Enum.TryParse<DayOfWeek>(command.Day, true, out var dayOfWeek))
            throw new DomainException(CoordinationErrors.InvalidDay(command.Day));

        // Çift kayıt kontrolü — aynı plan + işletme + gün
        var duplicate = await session.Query<WeeklyVisitAssignment>()
            .Where(a => a.PlanId == command.PlanId
                     && a.BusinessId == command.BusinessId
                     && a.Day == command.Day)
            .AnyAsync(ct);

        if (duplicate)
            throw new DomainException(CoordinationErrors.WeeklyVisitAssignmentDuplicate(
                command.BusinessId, command.Day));

        // Ziyaret tarihi hesapla
        var visitDate = DateOnly.FromDateTime(
            ISOWeek.ToDateTime(plan.Year, plan.WeekNumber, dayOfWeek));

        var assignment = new WeeklyVisitAssignment
        {
            Id = Guid.NewGuid(),
            PlanId = command.PlanId,
            InstitutionId = command.InstitutionId,
            AcademicPeriodId = plan.AcademicPeriodId,
            TeacherId = command.TeacherId,
            TeacherName = command.TeacherName,
            BusinessId = command.BusinessId,
            BusinessName = command.BusinessName,
            BranchCode = command.BranchCode,
            BranchName = command.BranchName,
            VisitDate = visitDate,
            Day = command.Day,
            PeriodCount = command.PeriodCount,
            WeekNumber = plan.WeekNumber,
        };

        session.Store(assignment);
        plan.AssignmentCount += 1;
        session.Store(plan);

        await session.SaveChangesAsync(ct);

        return assignment.Id;
    }
}
