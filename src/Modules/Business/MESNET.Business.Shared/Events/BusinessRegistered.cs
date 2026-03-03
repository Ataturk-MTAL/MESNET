using MESNET.Business.Core.Enums;
using MESNET.Common.Shared;

namespace MESNET.Business.Shared.Events;

public sealed record BusinessRegistered(
    Guid BusinessId,
    Guid TenantId,
    string Name,
    string? Address,
    Location? Location,
    RegistrationSource Source,
    int TotalSlots = 0,
    List<string>? Sectors = null);
