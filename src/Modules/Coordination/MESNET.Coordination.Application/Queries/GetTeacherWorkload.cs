namespace MESNET.Coordination.Application.Queries;

public sealed record GetTeacherWorkload(
    Guid TeacherId,
    Guid InstitutionId);
