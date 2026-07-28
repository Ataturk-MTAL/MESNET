namespace MESNET.Coordination.Application.Commands;

/// <remarks>
/// İşlemi yapan kullanıcı komutta TAŞINMAZ (#137) — handler token'dan damgalar.
/// </remarks>
public sealed record UpsertBranchWorkloadConfig(
    Guid InstitutionId,
    Guid AcademicPeriodId,
    string BranchCode,
    string EducationType,
    int DepartmentHeadCount,
    int WorkshopHeadCount,
    int DepartmentHeadHours,
    int WorkshopHeadHours,
    List<ClassLevelInput> ClassLevels);

public sealed record ClassLevelInput(
    int ClassYear,
    int WeeklyLessonHours);
