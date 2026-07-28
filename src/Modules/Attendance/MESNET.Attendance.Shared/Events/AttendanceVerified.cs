namespace MESNET.Attendance.Shared.Events;

/// <param name="VerifiedById">
/// Doğrulayan kullanıcının kimliği — token'dan gelir (#139).
/// Bkz. <see cref="AttendanceMarked.MarkedById"/> — aynı yeniden adlandırma gerekçesi.
/// </param>
public sealed record AttendanceVerified(
    Guid AttendanceId,
    Guid StudentId,
    Guid VerifiedById,
    DateTime VerifiedAt);
