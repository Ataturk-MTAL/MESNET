using System.Globalization;
using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Handlers;

public static class GenerateWeeklyVisitsHandler
{
    public static async Task<Guid> Handle(
        GenerateWeeklyVisits command,
        IDocumentSession session,
        CancellationToken ct)
    {
        // 1. Dönem kontrolü
        var period = await session.LoadAsync<AcademicPeriodView>(command.AcademicPeriodId, ct);
        if (period is null)
            throw new DomainException(CoordinationErrors.AcademicPeriodNotFound(command.AcademicPeriodId));
        if (!period.IsActive)
            throw new DomainException(CoordinationErrors.AcademicPeriodClosed(command.AcademicPeriodId));

        // 2. Kapsam doğrulama
        if (command.Scope is not ("Teacher" or "Branch" or "All"))
            throw new DomainException(CoordinationErrors.InvalidVisitScope(command.Scope));

        // 3. Aynı hafta+kapsam için mevcut plan var mı?
        IQueryable<WeeklyVisitPlan> planQuery = session.Query<WeeklyVisitPlan>()
            .Where(p => p.InstitutionId == command.InstitutionId
                     && p.AcademicPeriodId == command.AcademicPeriodId
                     && p.Year == command.Year
                     && p.WeekNumber == command.WeekNumber
                     && p.Scope == command.Scope);

        if (command.Scope == "Teacher" && command.TeacherId.HasValue)
            planQuery = planQuery.Where(p => p.ScopeTeacherId == command.TeacherId.Value);
        else if (command.Scope == "Branch" && !string.IsNullOrWhiteSpace(command.BranchCode))
            planQuery = planQuery.Where(p => p.ScopeBranchCode == command.BranchCode);

        var existingPlan = await planQuery.FirstOrDefaultAsync(ct);
        if (existingPlan is not null)
            throw new DomainException(CoordinationErrors.WeeklyVisitPlanAlreadyExists(
                command.Year, command.WeekNumber, command.Scope));

        // 4. İşletme atamalarını sorgula
        IQueryable<BusinessCoordinationView> viewQuery = session.Query<BusinessCoordinationView>()
            .Where(v => v.InstitutionId == command.InstitutionId
                     && v.AcademicPeriodId == command.AcademicPeriodId
                     && v.AssignedTeacherId != null);

        if (command.Scope == "Teacher" && command.TeacherId.HasValue)
            viewQuery = viewQuery.Where(v => v.AssignedTeacherId == command.TeacherId.Value);
        else if (command.Scope == "Branch" && !string.IsNullOrWhiteSpace(command.BranchCode))
            viewQuery = viewQuery.Where(v => v.BranchCode == command.BranchCode);

        var views = await viewQuery.ToListAsync(ct);

        if (views.Count == 0)
            throw new DomainException(CoordinationErrors.NoAssignmentsForScope(command.Scope));

        // 5. Hafta başlangıcı hesapla
        var weekStart = DateOnly.FromDateTime(
            ISOWeek.ToDateTime(command.Year, command.WeekNumber, DayOfWeek.Monday));
        var weekEnd = DateOnly.FromDateTime(
            ISOWeek.ToDateTime(command.Year, command.WeekNumber, DayOfWeek.Friday));

        // 6. Her işletme-gün çifti için tek ziyaret kaydı oluştur (1 günlük form)
        var assignments = new List<WeeklyVisitAssignment>();

        foreach (var view in views)
        {
            if (view.AssignedSlots.Count == 0) continue;

            // İşletme bazında gün grupla — her gün için tek kayıt
            var slotsByDay = view.AssignedSlots
                .GroupBy(s => s.Day)
                .Where(g => Enum.TryParse<DayOfWeek>(g.Key, true, out _));

            foreach (var dayGroup in slotsByDay)
            {
                Enum.TryParse<DayOfWeek>(dayGroup.Key, true, out var dayOfWeek);

                var visitDate = DateOnly.FromDateTime(
                    ISOWeek.ToDateTime(command.Year, command.WeekNumber, dayOfWeek));

                assignments.Add(new WeeklyVisitAssignment
                {
                    Id = Guid.NewGuid(),
                    PlanId = default, // set after plan creation
                    InstitutionId = command.InstitutionId,
                    AcademicPeriodId = command.AcademicPeriodId,
                    TeacherId = view.AssignedTeacherId!.Value,
                    TeacherName = view.AssignedTeacherName ?? "—",
                    BusinessId = view.Id,
                    BusinessName = view.Name,
                    BranchCode = view.BranchCode,
                    BranchName = view.BranchName,
                    VisitDate = visitDate,
                    Day = dayGroup.Key,
                    PeriodCount = dayGroup.Count(),
                    WeekNumber = command.WeekNumber,
                });
            }
        }

        if (assignments.Count == 0)
            throw new DomainException(CoordinationErrors.NoAssignmentsForScope(command.Scope));

        // 7. Plan oluştur
        var plan = new WeeklyVisitPlan
        {
            Id = Guid.NewGuid(),
            InstitutionId = command.InstitutionId,
            AcademicPeriodId = command.AcademicPeriodId,
            Year = command.Year,
            WeekNumber = command.WeekNumber,
            WeekStartDate = weekStart,
            WeekEndDate = weekEnd,
            Scope = command.Scope,
            ScopeTeacherId = command.Scope == "Teacher" ? command.TeacherId : null,
            ScopeBranchCode = command.Scope == "Branch" ? command.BranchCode : null,
            AssignmentCount = assignments.Count,
            GeneratedBy = command.GeneratedBy,
        };

        // PlanId set
        foreach (var a in assignments)
            a.PlanId = plan.Id;

        session.Store(plan);
        foreach (var a in assignments)
            session.Store(a);

        await session.SaveChangesAsync(ct);

        return plan.Id;
    }
}
