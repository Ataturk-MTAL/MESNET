namespace MESNET.Business.Shared.Events;

public sealed record BusinessTerminationRequested(
    Guid BusinessId,
    Guid StudentId,
    string Reason);
