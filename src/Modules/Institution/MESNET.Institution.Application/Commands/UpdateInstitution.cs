using MESNET.Common.Shared;

namespace MESNET.Institution.Application.Commands;

public sealed record UpdateInstitution(
    Guid InstitutionId,
    string FullName,
    string? Address,
    string? PhoneNumber,
    string? Email,
    string? WebUrl,
    Location? Location);
