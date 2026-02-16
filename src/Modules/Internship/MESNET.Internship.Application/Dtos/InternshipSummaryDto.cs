namespace MESNET.Internship.Application.Dtos;

public sealed record InternshipSummaryDto(
    Guid Id,
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    Guid? ContractId,
    string Phase,
    string PhaseSlug,
    DateTime StartedAt,
    DateTime? CompletedAt,
    int TotalAbsenceDays,
    int CompletedVisits,
    int ConfirmedPayments,
    DateTime LastUpdated);
