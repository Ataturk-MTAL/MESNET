namespace MESNET.Enrollment.Application.Queries;

public sealed record ListStudents(
    Guid? InstitutionId,
    string? BranchCode,
    string? Section,
    string? Status);
