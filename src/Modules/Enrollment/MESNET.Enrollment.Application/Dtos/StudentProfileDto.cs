namespace MESNET.Enrollment.Application.Dtos;

public sealed record StudentProfileDto(
    Guid Id,
    Guid KeycloakUserId,
    string FullName,
    Guid InstitutionId,
    string BranchCode,
    string BranchName,
    string? SpecializationCode,
    string? SpecializationName,
    int ClassYear,
    string? Section,
    string? StudentNumber,
    string? PhoneNumber,
    string? TcKimlikNo,
    string? GuardianName,
    string? GuardianPhone,
    string Status,
    string StatusSlug,
    DateTime RegisteredAt);
