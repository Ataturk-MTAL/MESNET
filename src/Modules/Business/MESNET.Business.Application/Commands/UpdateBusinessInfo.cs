using MESNET.Common.Shared;

namespace MESNET.Business.Application.Commands;

public sealed record UpdateBusinessInfo(
    Guid BusinessId,
    string Name,
    string Address,
    string? PhoneNumber,
    string? Email,
    string? Website,
    int PersonnelCount,
    Location? Location);
