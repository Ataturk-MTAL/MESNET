namespace MESNET.Attendance.Application.Dtos;

public sealed record AttendanceRecordDto(
    Guid Id,
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    DateTime Date,
    string AbsenceType,
    string AbsenceTypeSlug,
    string? Reason,
    string Status,
    string StatusSlug,
    string? HealthReportUrl,
    // Aktör alanları hem kimlik hem çözümlenmiş ad taşır (#139): kimlik saklanan değerdir,
    // ad okuma anında UserNameView'dan türetilir. Ad bilinmiyorsa null — hata değil
    // (silinmiş kullanıcı ya da resync-display-names henüz koşmamış).
    Guid MarkedById,
    string? MarkedByName,
    DateTime MarkedAt,
    Guid? ApprovedById,
    string? ApprovedByName,
    DateTime? ApprovedAt,
    Guid? VerifiedById,
    string? VerifiedByName,
    DateTime? VerifiedAt,
    // Sağlık raporu onay zinciri (#172). Rapor yüklenmiş olması tek başına hüküm doğurmaz;
    // arayüz "onay bekliyor" ile "onaylandı" ayrımını bu alanlardan gösterir.
    string HealthReportStatus,
    string HealthReportStatusSlug,
    Guid? HealthReportAttachedById,
    string? HealthReportAttachedByName,
    DateTime? HealthReportAttachedAt,
    Guid? HealthReportReviewedById,
    string? HealthReportReviewedByName,
    DateTime? HealthReportReviewedAt,
    string? HealthReportRejectionReason);
