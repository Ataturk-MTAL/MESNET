namespace MESNET.Coordination.Application.Commands;

/// <summary>
/// Mevcut haftalık ziyaret planlarının event'lerini yeniden yayınlar.
/// Reporting modülünün VisitAssignmentReportView'ını sıfırdan doldurur.
/// Schema sıfırlanması veya consumer hatası sonrası veri tutarlılığını sağlar.
/// </summary>
public sealed record ResyncWeeklyVisitEvents(
    Guid InstitutionId,
    Guid AcademicPeriodId);
