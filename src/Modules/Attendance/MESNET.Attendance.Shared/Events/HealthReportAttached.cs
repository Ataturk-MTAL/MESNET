namespace MESNET.Attendance.Shared.Events;

/// <param name="AttachedById">
/// Raporu sisteme giren kullanıcının kimliği (#139/#172). Ad saklanmaz, okuma anında
/// <c>UserNameView</c>'dan çözülür.
/// </param>
/// <param name="RequiresApproval">
/// Raporun koordinatör öğretmen onayı bekleyip beklemediği (#172). Karar giriş anında
/// <c>attendance:health-report:direct</c> izniyle verilir ve olaya YAZILIR — sonradan izin
/// haritası değişse bile geçmiş kaydın hangi kurala göre işlendiği okunabilir kalır.
///
/// <para><b>Eski olaylarda alan yoktur</b> ve <c>false</c> olarak deserialize olur; yani #172
/// öncesinde eklenmiş raporlar bugünkü gibi doğrudan geçerli sayılır. Bu bilinçlidir: geçmiş
/// kayıtların türü sonradan onaysız duruma düşürülüp ücret kesintisi geriye dönük
/// canlandırılmaz. Mutabakat gerekiyorsa ayrıca yürütülür.</para>
/// </param>
public sealed record HealthReportAttached(
    Guid AttendanceId,
    Guid StudentId,
    string ReportUrl,
    DateTime AttachedAt,
    Guid AttachedById = default,
    bool RequiresApproval = false);
