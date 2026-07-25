namespace MESNET.Payment.Core.ReadModels;

/// <summary>
/// Payment modülünün yerel devamsızlık kaydı — Attendance olaylarından beslenir.
/// Devamsızlık kesintisi bu kayıtlar sayılarak hesaplanır (#64).
/// </summary>
/// <remarks>
/// Attendance modülünün <c>AttendanceView</c>'ı zaten mazeretsiz gün sayısı tutuyor, ama başka
/// modülün şeması olduğu için okunamaz. Ayrıca oradaki sayaç ay kırılımı taşımıyor; maaş aylık
/// hesaplandığı için burada kayıt başına tutulup ay bazında sayılıyor.
///
/// Alan adları düz string: Marten LINQ'te SmartEnum karşılaştırması yapılamıyor
/// (bkz. CLAUDE.md — SmartEnum LINQ kuralları).
/// </remarks>
public class StudentAbsenceView
{
    public Guid Id { get; set; }            // AttendanceId
    public Guid StudentId { get; set; }
    public Guid BusinessId { get; set; }

    /// <summary>Ay, <c>yyyy-MM</c> formatında (ör. <c>2026-07</c>).</summary>
    public string Month { get; set; } = "";

    /// <summary><c>Excused</c> / <c>Unexcused</c> / <c>HealthReport</c>.</summary>
    public string AbsenceTypeName { get; set; } = "";

    /// <summary><c>Pending</c> / <c>Recorded</c> / <c>Verified</c> / <c>Corrected</c>.</summary>
    public string StatusName { get; set; } = "";
}
