namespace MESNET.Coordination.Application.Commands;

public sealed record UpsertBranchWorkloadConfig(
    Guid InstitutionId,
    Guid AcademicPeriodId,
    string BranchCode,
    string EducationType,
    int DepartmentHeadCount,
    int WorkshopHeadCount,
    int DepartmentHeadHours,
    int WorkshopHeadHours,
    List<ClassLevelInput> ClassLevels,
    string UpdatedBy);

public sealed record ClassLevelInput(
    int ClassYear,
    int WeeklyLessonHours);
