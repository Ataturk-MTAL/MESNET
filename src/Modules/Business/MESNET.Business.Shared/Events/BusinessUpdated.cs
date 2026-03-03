using MESNET.Common.Shared;

namespace MESNET.Business.Shared.Events;

public sealed record BusinessUpdated(
    Guid BusinessId,
    string Name,
    string? Address,
    Location? Location,
    List<string>? Sectors = null);
