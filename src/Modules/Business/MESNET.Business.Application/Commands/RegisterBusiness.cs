using MESNET.Common.Shared;

namespace MESNET.Business.Application.Commands;

public sealed record RegisterBusiness(
    Guid TenantId,
    string Name,
    string Address,
    string? PhoneNumber,
    string? Email,
    string? Website,
    int PersonnelCount,
    Location? Location,
    int TotalSlots);
