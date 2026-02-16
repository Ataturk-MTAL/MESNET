namespace MESNET.Business.Shared.Events;

public sealed record BusinessClosed(
    Guid BusinessId,
    DateTime ClosedAt);
