namespace MESNET.Institution.Shared.Events;

public sealed record BranchSpecializationsUpdated(Guid InstitutionId, string FieldCode, List<string> ActiveSpecializations);
