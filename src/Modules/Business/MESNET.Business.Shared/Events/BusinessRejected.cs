namespace MESNET.Business.Shared.Events;

public sealed record BusinessRejected(
    Guid BusinessId,
    string Reason);
