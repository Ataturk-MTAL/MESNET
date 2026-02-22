namespace MESNET.Enrollment.Application.Commands;

public sealed record PlaceStudent(
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    Guid? TeacherId);
