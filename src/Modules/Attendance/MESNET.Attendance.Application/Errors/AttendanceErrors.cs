using MESNET.Common.Shared;

namespace MESNET.Attendance.Application.Errors;

public static class AttendanceErrors
{
    public static Error NotFound(Guid id) =>
        new("Attendance.NotFound", $"Devamsızlık kaydı bulunamadı: {id}");

    public static Error CalendarNotFound(Guid institutionId, int year) =>
        new("Attendance.CalendarNotFound",
            $"Çalışma takvimi bulunamadı: kurum={institutionId}, yıl={year}");

    public static Error RestrictedDate(DateTime date) =>
        new("Attendance.RestrictedDate",
            $"Bu tarih kısıtlı bir gündür: {date:dd.MM.yyyy}");

    public static Error OperationFailed(string operation, string message) =>
        new($"Attendance.{operation}Failed", message);
}
