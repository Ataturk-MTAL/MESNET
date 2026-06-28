namespace MESNET.Institution.Shared.Events;

public sealed record BranchActivated(Guid InstitutionId, string FieldCode, string FieldName, string Type);
