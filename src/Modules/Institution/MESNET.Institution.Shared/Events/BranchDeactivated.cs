namespace MESNET.Institution.Shared.Events;

public sealed record BranchDeactivated(Guid InstitutionId, string FieldCode);
