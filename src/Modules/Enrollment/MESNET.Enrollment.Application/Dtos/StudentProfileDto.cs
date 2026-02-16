namespace MESNET.Enrollment.Application.Dtos;

public sealed record StudentProfileDto(
    Guid Id,
    Guid KeycloakUserId,
    string FullName,
    Guid InstitutionId,
    string BranchCode,
    string BranchName,
    int ClassYear,
    string Status,
    string StatusSlug,
    DateTime RegisteredAt);
