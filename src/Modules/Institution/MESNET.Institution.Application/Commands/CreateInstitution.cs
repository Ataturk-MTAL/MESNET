using MESNET.Common.Shared;

namespace MESNET.Institution.Application.Commands;

public sealed record CreateInstitution(
    Guid TenantId,
    int InstitutionCode,
    string FullName,
    string? Address,
    string? PhoneNumber,
    string? Email,
    string? WebUrl,
    Location? Location,
    Guid? Id = null);
