namespace MESNET.Business.Shared.Events;

public sealed record BusinessApprovalRequested(
    Guid BusinessId,
    string Name);
