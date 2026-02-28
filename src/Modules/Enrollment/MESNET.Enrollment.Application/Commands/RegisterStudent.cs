namespace MESNET.Enrollment.Application.Commands;

public sealed record RegisterStudent(
    Guid InstitutionId,
    Guid AcademicPeriodId,
    Guid KeycloakUserId,
    string FullName,
    string BranchCode,
    string BranchName,
    int ClassYear,
    string? Section,
    string? SpecializationCode = null,
    string? SpecializationName = null,
    string? StudentNumber = null,
    string? PhoneNumber = null,
    string? TcKimlikNo = null,
    string? GuardianName = null,
    string? GuardianPhone = null);
