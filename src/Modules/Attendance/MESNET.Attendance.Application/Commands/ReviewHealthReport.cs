using JasperFx;
using MESNET.Attendance.Application.Guards;

namespace MESNET.Attendance.Application.Commands;

/// <summary>
/// Sağlık raporunu onayla (#172) — onay zincirinin 1. adımı, koordinatör öğretmen.
/// Devamsızlık türü ancak bu komutla <c>HealthReport</c>'a döner ve ücret kesintisi kalkar.
/// </summary>
public sealed record ApproveHealthReport([property: Identity] Guid AttendanceId) : IAttendancePeriodScoped;

/// <summary>
/// Sağlık raporunu reddet (#172). Tür değişmez, kesinti aynen sürer; gerekçe kayda yazılır.
/// </summary>
public sealed record RejectHealthReport(
    [property: Identity] Guid AttendanceId,
    string Reason) : IAttendancePeriodScoped;
