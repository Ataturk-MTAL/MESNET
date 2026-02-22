namespace MESNET.Contract.Application.Commands;

public sealed record CreateContract(
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    Guid? TeacherId,
    DateTime StartDate);
