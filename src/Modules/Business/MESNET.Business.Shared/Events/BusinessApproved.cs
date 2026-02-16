namespace MESNET.Business.Shared.Events;

public sealed record BusinessApproved(
    Guid BusinessId,
    string ApprovedBy,
    DateTime ApprovedAt);
