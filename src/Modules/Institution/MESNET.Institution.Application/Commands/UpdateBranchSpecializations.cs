namespace MESNET.Institution.Application.Commands;

public sealed record UpdateBranchSpecializations(
    Guid InstitutionId,
    string FieldCode,
    List<string> ActiveSpecializations);
