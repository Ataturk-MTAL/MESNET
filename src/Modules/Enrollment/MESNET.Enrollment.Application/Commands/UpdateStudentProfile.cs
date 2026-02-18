namespace MESNET.Enrollment.Application.Commands;

public sealed record UpdateStudentProfile(
    Guid StudentId,
    string FullName,
    string BranchCode,
    string BranchName,
    int ClassYear,
    string? Section);
