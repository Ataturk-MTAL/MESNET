namespace MESNET.Attendance.Application.Dtos;

/// <summary>
/// Devamsızlık sayacı görünümü. <b>Bugün hiçbir uç bunu döndürmüyor</b> — eşleme
/// (<c>AttendanceMappingExtensions.ToDto</c>) çağrılmıyor. Tip, görünümle uyumlu tutuldu (#242);
/// bir uca bağlanacaksa önce kimliğin dışa açılması gerekip gerekmediği kararlaştırılmalı.
/// </summary>
/// <param name="Id"><c>{studentId}:{academicPeriodId}</c> — bileşik anahtar (#242).</param>
public sealed record AttendanceViewDto(
    string Id,
    Guid StudentId,
    Guid AcademicPeriodId,
    Guid BusinessId,
    int TotalAbsenceDays,
    int ExcusedDays,
    int UnexcusedDays,
    bool LimitExceeded,
    DateTime LastUpdated);
