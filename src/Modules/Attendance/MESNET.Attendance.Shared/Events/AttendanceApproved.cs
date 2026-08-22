namespace MESNET.Attendance.Shared.Events;

/// <param name="ApprovedById">
/// Onaylayan kullanıcının kimliği — token'dan gelir (#139).
/// Bkz. <see cref="AttendanceMarked.MarkedById"/> — aynı yeniden adlandırma gerekçesi.
/// </param>
public sealed record AttendanceApproved(
    Guid AttendanceId,
    Guid StudentId,
    Guid ApprovedById,
    DateTime ApprovedAt);
