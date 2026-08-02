namespace MESNET.Attendance.Shared.Events;

/// <summary>
/// Koordinatör öğretmen sağlık raporunu reddetti (#172).
/// Devamsızlık türü DEĞİŞMEZ — kesinti hangi türdeyse öyle kalır.
/// Belge kaydı silinmez; denetim izi için dosya yolu ve gerekçe saklanır.
/// </summary>
public sealed record HealthReportRejected(
    Guid AttendanceId,
    Guid StudentId,
    Guid RejectedById,
    DateTime RejectedAt,
    string Reason);
