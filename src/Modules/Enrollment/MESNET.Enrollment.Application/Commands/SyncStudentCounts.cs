namespace MESNET.Enrollment.Application.Commands;

public sealed record SyncStudentCounts(Guid InstitutionId, Guid AcademicPeriodId);
