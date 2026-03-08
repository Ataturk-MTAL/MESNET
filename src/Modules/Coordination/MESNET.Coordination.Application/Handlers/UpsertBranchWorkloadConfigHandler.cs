using Marten;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Core.ReadModels;
using MESNET.Coordination.Core.Services;

namespace MESNET.Coordination.Application.Handlers;

public static class UpsertBranchWorkloadConfigHandler
{
    public static async Task Handle(
        UpsertBranchWorkloadConfig command,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        // Öğrenci sayılarını BranchStudentCountView'dan al
        var countViewId = BranchStudentCountView.CreateId(
            command.InstitutionId, command.AcademicPeriodId, command.BranchCode, command.EducationType);
        var countView = await session.LoadAsync<BranchStudentCountView>(countViewId, cancellationToken);

        var classLevels = command.ClassLevels.Select(cl =>
        {
            var studentCount = countView?.StudentCountByClassYear.GetValueOrDefault(cl.ClassYear, 0) ?? 0;
            var groupCount = GroupCalculator.CalculateGroups(
                command.EducationType, cl.ClassYear, studentCount);
            return new ClassLevelConfig
            {
                ClassYear = cl.ClassYear,
                WeeklyLessonHours = cl.WeeklyLessonHours,
                StudentCount = studentCount,
                GroupCount = groupCount,
                SubTotal = cl.WeeklyLessonHours * groupCount
            };
        }).ToList();

        var existing = await session.Query<BranchWorkloadConfig>()
            .FirstOrDefaultAsync(c =>
                c.InstitutionId == command.InstitutionId &&
                c.BranchCode == command.BranchCode &&
                c.AcademicPeriodId == command.AcademicPeriodId &&
                c.EducationType == command.EducationType,
                cancellationToken);

        if (existing is not null)
        {
            existing.DepartmentHeadCount = command.DepartmentHeadCount;
            existing.WorkshopHeadCount = command.WorkshopHeadCount;
            existing.DepartmentHeadHours = command.DepartmentHeadHours;
            existing.WorkshopHeadHours = command.WorkshopHeadHours;
            existing.ClassLevels = classLevels;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = command.UpdatedBy;
            existing.Recalculate();
            session.Store(existing);
        }
        else
        {
            var config = new BranchWorkloadConfig
            {
                Id = Guid.NewGuid(),
                InstitutionId = command.InstitutionId,
                AcademicPeriodId = command.AcademicPeriodId,
                BranchCode = command.BranchCode,
                EducationType = command.EducationType,
                DepartmentHeadCount = command.DepartmentHeadCount,
                WorkshopHeadCount = command.WorkshopHeadCount,
                DepartmentHeadHours = command.DepartmentHeadHours,
                WorkshopHeadHours = command.WorkshopHeadHours,
                ClassLevels = classLevels,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = command.UpdatedBy,
            };
            config.Recalculate();
            session.Store(config);
        }
    }
}
