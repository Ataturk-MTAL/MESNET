namespace MESNET.Internship.Application.Dtos;

public sealed record InternshipSummaryDto(
    Guid Id,
    Guid PlacementId,
    Guid StudentId,
    string StudentName,
    // Okulda stajda null (#159).
    Guid? BusinessId,
    string BusinessName,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    Guid? ContractId,
    string Phase,
    string PhaseSlug,
    DateTime StartedAt,
    DateTime? CompletedAt,
    int TotalAbsenceDays,
    int CompletedVisits,
    int ConfirmedPayments,
    DateTime LastUpdated);
