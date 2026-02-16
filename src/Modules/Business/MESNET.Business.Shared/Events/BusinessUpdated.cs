using MESNET.Common.Shared;

namespace MESNET.Business.Shared.Events;

public sealed record BusinessUpdated(
    Guid BusinessId,
    string Name,
    Location? Location);
