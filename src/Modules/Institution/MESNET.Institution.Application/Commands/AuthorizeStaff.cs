using MESNET.Institution.Core.Enums;

namespace MESNET.Institution.Application.Commands;

public sealed record AuthorizeStaff(
    Guid InstitutionId,
    string KeycloakId,
    string FullName,
    StaffRole Role,
    string? BranchCode);
