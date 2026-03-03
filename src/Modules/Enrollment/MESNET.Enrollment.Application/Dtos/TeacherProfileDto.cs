namespace MESNET.Enrollment.Application.Dtos;

public sealed record TeacherProfileDto(
    Guid Id,
    Guid KeycloakUserId,
    string FullName,
    Guid InstitutionId,
    DateTime RegisteredAt,
    string? BranchCode = null);
