namespace MESNET.Business.Shared.Events;

public sealed record BusinessDeactivated(
    Guid BusinessId,
    string Reason);
