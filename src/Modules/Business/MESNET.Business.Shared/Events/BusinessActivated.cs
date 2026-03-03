using MESNET.Common.Shared;

namespace MESNET.Business.Shared.Events;

public sealed record BusinessActivated(
    Guid BusinessId,
    Guid TenantId,
    string Name,
    string? Address,
    Location? Location);
