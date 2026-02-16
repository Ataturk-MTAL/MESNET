namespace MESNET.Institution.Application.Commands;

public sealed record ActivateBranch(
    Guid InstitutionId,
    string FieldCode);
