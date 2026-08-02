namespace MESNET.Attendance.Shared.Events;

/// <summary>
/// Ücretli izin başvurusu reddedildi (#177). Hangi adımda reddedildiği
/// <paramref name="RejectedStage"/> ile saklanır (<c>PendingBusiness</c> / <c>PendingSchool</c>).
///
/// <para>Ret başvuruyu kapatır; devamsızlık kaydı AÇILMAZ. Öğrenci yeni başvuru açabilir —
/// reddedilen başvuru yeniden onaya sokulamaz.</para>
/// </summary>
public sealed record PaidLeaveRejected(
    Guid RequestId,
    Guid StudentId,
    Guid BusinessId,
    Guid RejectedById,
    DateTime RejectedAt,
    string Reason,
    string RejectedStage);
