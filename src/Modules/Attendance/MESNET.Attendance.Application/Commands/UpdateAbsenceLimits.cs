namespace MESNET.Attendance.Application.Commands;

/// <summary>
/// Devamsızlık sınırlarını günceller — <b>ulusal parametre</b> (#183).
///
/// <para>Kurum kimliği <b>taşımaz</b>: sınır md. 36'dan türer ve okul başına değişemez. Yazma
/// izni <c>platform:parameter:manage</c>'dir; hiçbir okul rolünde yoktur.</para>
/// </summary>
public sealed record UpdateAbsenceLimits(int FormalUnexcusedDayLimit, int MesemUnexcusedDayLimit);
