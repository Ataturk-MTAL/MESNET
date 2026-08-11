using MESNET.Institution.Core.Enums;

using MESNET.Institution.Application.Security;
namespace MESNET.Institution.Application.Commands;

public sealed record AuthorizeStaff(
    Guid InstitutionId,
    string KeycloakId,
    string FullName,
    StaffRole Role,
    string? BranchCode) : IInstitutionScoped;
