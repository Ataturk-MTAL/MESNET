namespace MESNET.Contract.Application.Queries;

public sealed record ListContracts(Guid? StudentId, Guid? BusinessId, Guid? InstitutionId, Guid? AcademicPeriodId, string? Status);
