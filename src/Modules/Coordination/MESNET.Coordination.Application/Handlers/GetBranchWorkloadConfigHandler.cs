using Marten;
using MESNET.Coordination.Application.Queries;
using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Core.ReadModels;
using MESNET.Coordination.Core.Services;

namespace MESNET.Coordination.Application.Handlers;

public static class GetBranchWorkloadConfigHandler
{
    public static async Task<BranchWorkloadConfig?> Handle(
        GetBranchWorkloadConfig query,
        IQuerySession session,
        CancellationToken cancellationToken)
    {
        var config = await session.Query<BranchWorkloadConfig>()
            .FirstOrDefaultAsync(c =>
                c.InstitutionId == query.InstitutionId &&
                c.BranchCode == query.BranchCode &&
                c.AcademicPeriodId == query.AcademicPeriodId &&
                c.EducationType == query.EducationType,
                cancellationToken);

        if (config is null) return null;

        // BranchStudentCountView'dan güncel öğrenci sayılarını al ve merge et
        var countViewId = BranchStudentCountView.CreateId(
            query.InstitutionId, query.AcademicPeriodId, query.BranchCode, query.EducationType);
        var countView = await session.LoadAsync<BranchStudentCountView>(countViewId, cancellationToken);

        if (countView is not null)
        {
            config.ClassLevels = config.ClassLevels.Select(cl =>
            {
                var studentCount = countView.StudentCountByClassYear.GetValueOrDefault(cl.ClassYear, 0);
                var groupCount = GroupCalculator.CalculateGroups(config.EducationType, cl.ClassYear, studentCount);
                return new ClassLevelConfig
                {
                    ClassYear = cl.ClassYear,
                    WeeklyLessonHours = cl.WeeklyLessonHours,
                    StudentCount = studentCount,
                    GroupCount = groupCount,
                    SubTotal = cl.WeeklyLessonHours * groupCount
                };
            }).ToList();

            config.Recalculate();
        }

        return config;
    }
}
