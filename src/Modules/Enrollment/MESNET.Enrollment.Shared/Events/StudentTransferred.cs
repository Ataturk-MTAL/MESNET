namespace MESNET.Enrollment.Shared.Events;

public sealed record StudentTransferred(
    Guid PlacementId,
    Guid StudentId,
    Guid OldBusinessId,
    Guid NewBusinessId,
    string Reason);
