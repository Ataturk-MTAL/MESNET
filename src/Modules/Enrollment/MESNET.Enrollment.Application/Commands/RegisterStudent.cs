namespace MESNET.Enrollment.Application.Commands;

public sealed record RegisterStudent(
    Guid InstitutionId,
    Guid AcademicPeriodId,
    Guid KeycloakUserId,
    string FullName,
    string BranchCode,
    string BranchName,
    int ClassYear,
    string? Section);
