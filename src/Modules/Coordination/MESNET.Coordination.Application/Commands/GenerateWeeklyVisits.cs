namespace MESNET.Coordination.Application.Commands;

public sealed record GenerateWeeklyVisits(
    Guid InstitutionId,
    Guid AcademicPeriodId,
    int Year,
    int WeekNumber,
    string Scope,             // "Teacher" | "Branch" | "All"
    Guid? TeacherId,          // required if Scope="Teacher"
    string? BranchCode,       // required if Scope="Branch"
    string GeneratedBy);
