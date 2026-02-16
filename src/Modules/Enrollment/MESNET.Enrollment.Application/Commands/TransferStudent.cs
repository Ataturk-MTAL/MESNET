namespace MESNET.Enrollment.Application.Commands;

public sealed record TransferStudent(
    Guid PlacementId,
    Guid NewBusinessId,
    string Reason);
