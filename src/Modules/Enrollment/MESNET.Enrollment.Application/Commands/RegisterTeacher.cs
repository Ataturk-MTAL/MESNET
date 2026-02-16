namespace MESNET.Enrollment.Application.Commands;

public sealed record RegisterTeacher(
    Guid InstitutionId,
    Guid KeycloakUserId,
    string FullName);
