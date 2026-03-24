namespace MESNET.Enrollment.Application.Commands;

public sealed record MarkAsFailedToComplete(Guid PlacementId, Guid InstitutionId);
