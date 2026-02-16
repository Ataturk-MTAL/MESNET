namespace MESNET.Enrollment.Application.Commands;

public sealed record RequestStudent(
    Guid BusinessId,
    string BranchCode,
    Guid InstitutionId);
