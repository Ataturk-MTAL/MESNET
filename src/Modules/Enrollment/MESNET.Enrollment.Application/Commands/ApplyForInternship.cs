namespace MESNET.Enrollment.Application.Commands;

public sealed record ApplyForInternship(
    Guid StudentId,
    Guid? BusinessId,
    string BranchCode);
