namespace MESNET.Attendance.Application.Guards;

/// <summary>
/// Belirli bir devamsızlık kaydına (AttendanceRecord) bağlı YAZMA command'larını işaretler.
/// Bu marker'ı taşıyan command'lar, kayıt ait olduğu akademik dönem kapalıysa
/// <see cref="AttendancePeriodGuardMiddleware"/> tarafından engellenir.
/// </summary>
public interface IAttendancePeriodScoped
{
    /// <summary>Hedef devamsızlık kaydının (AttendanceRecord stream) kimliği.</summary>
    Guid AttendanceId { get; }
}
