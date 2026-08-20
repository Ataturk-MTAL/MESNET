namespace MESNET.Attendance.Application.Commands;

/// <param name="AcademicPeriodId">
/// Boşsa kiracının <b>tüm</b> devamsızlık kayıtları yayılır. Dönem verilerek etki alanı
/// daraltılabilir — onarım tek bir dönemi ilgilendiriyorsa binlerce mesaj üretmenin anlamı yok.
/// </param>
public sealed record ResyncAttendanceSnapshots(Guid? AcademicPeriodId = null);

public sealed record ResyncAttendanceSnapshotsResult(int RecordCount, int DeletedCount);
