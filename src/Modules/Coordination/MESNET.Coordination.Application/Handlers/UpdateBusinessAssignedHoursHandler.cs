using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Application.Helpers;
using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Handlers;

public static class UpdateBusinessAssignedHoursHandler
{
    public static async Task Handle(
        UpdateBusinessAssignedHours command,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        var view = await CoordinationViewLookup.LoadBranchRowAsync(
            session, command.BusinessId, command.BranchCode, command.AcademicPeriodId, cancellationToken);

        if (view is null)
        {
            throw new DomainException(
                CoordinationErrors.BusinessBranchNotFound(command.BusinessId, command.BranchCode));
        }

        if (command.AssignedHours <= 0)
            throw new DomainException(CoordinationErrors.InvalidAssignedHours(command.AssignedHours));

        if (command.AssignedHours > view.MaxCoordinationHours)
        {
            throw new DomainException(
                CoordinationErrors.AssignedHoursExceedMax(command.AssignedHours, view.MaxCoordinationHours));
        }

        // Havuz kısıtı: alandaki toplam takdir edilen saat ≤ TotalWorkloadPool
        var workloadConfig = await session.Query<BranchWorkloadConfig>()
            .FirstOrDefaultAsync(c =>
                c.InstitutionId == view.InstitutionId &&
                c.BranchCode == view.BranchCode &&
                c.AcademicPeriodId == view.AcademicPeriodId,
                cancellationToken);

        if (workloadConfig is not null)
        {
            var otherAssigned = await session.Query<BusinessCoordinationView>()
                .Where(b =>
                    b.InstitutionId == view.InstitutionId &&
                    b.BranchCode == view.BranchCode &&
                    b.AcademicPeriodId == view.AcademicPeriodId &&
                    b.Id != view.Id)
                .SumAsync(b => b.AssignedHours, cancellationToken);

            var totalAssigned = otherAssigned + command.AssignedHours;

            if (totalAssigned > workloadConfig.TotalWorkloadPool)
            {
                throw new DomainException(
                    CoordinationErrors.WorkloadPoolExceeded(totalAssigned, workloadConfig.TotalWorkloadPool));
            }
        }

        // Öğretmen başına azami saat kontrolü (öğretmen atanmışsa)
        if (view.AssignedTeacherId.HasValue)
        {
            var config = await session.Query<CoordinationConfig>()
                .FirstOrDefaultAsync(c => c.InstitutionId == view.InstitutionId, cancellationToken);

            if (config is not null)
            {
                // Öğretmenin diğer işletmelerindeki takdir edilen saatleri topla
                var otherBusinesses = await session.Query<BusinessCoordinationView>()
                    .Where(b =>
                        b.AssignedTeacherId == view.AssignedTeacherId &&
                        b.Id != view.Id)
                    .Select(b => new { b.AssignedHours, b.MaxCoordinationHours })
                    .ToListAsync(cancellationToken);

                var otherTeacherHours = otherBusinesses
                    .Sum(b => b.AssignedHours > 0 ? b.AssignedHours : b.MaxCoordinationHours);

                var teacherTotal = otherTeacherHours + command.AssignedHours;

                if (teacherTotal > config.MaxWeeklyExtraHours)
                {
                    throw new DomainException(
                        CoordinationErrors.TeacherHoursExceedMax(
                            view.AssignedTeacherId.Value, teacherTotal, config.MaxWeeklyExtraHours));
                }
            }
        }

        var oldHours = view.AssignedHours;
        view.AssignedHours = command.AssignedHours;

        // Audit trail
        view.History.Insert(0, new AssignmentHistoryEntry(
            DateTime.UtcNow,
            "HoursUpdated",
            command.UpdatedBy,
            view.AssignedTeacherName,
            null,
            null,
            command.AssignedHours,
            $"Takdir edilen saat {oldHours} → {command.AssignedHours} olarak değiştirildi"));
        view.LastModifiedAt = DateTime.UtcNow;
        view.LastModifiedBy = command.UpdatedBy;

        session.Store(view);
    }
}
