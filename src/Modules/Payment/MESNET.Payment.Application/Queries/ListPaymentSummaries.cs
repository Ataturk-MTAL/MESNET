namespace MESNET.Payment.Application.Queries;

public sealed record ListPaymentSummaries(
    Guid? StudentId,
    Guid? BusinessId,
    Guid? InstitutionId,
    string? Phase,
    string? Month);
