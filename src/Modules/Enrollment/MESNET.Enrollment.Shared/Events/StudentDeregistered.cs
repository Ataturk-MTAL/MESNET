namespace MESNET.Enrollment.Shared.Events;

public sealed record StudentDeregistered(
    Guid StudentId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    string BranchCode,
    int ClassYear,
    string EducationType,
    string Reason);
