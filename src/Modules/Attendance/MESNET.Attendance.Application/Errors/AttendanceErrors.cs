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

    public static Error InvalidStatus(Guid attendanceId, string currentStatus, string message) =>
        new("Attendance.InvalidStatus",
            $"{message} Mevcut durum: {currentStatus}. Devamsızlık: {attendanceId}");

    public static Error AcademicPeriodNotFound(Guid id) =>
        new("Attendance.AcademicPeriodNotFound", $"Eğitim dönemi bulunamadı: {id}");

    public static Error AcademicPeriodClosed(Guid id) =>
        new("Attendance.AcademicPeriodClosed", $"Bu eğitim dönemi kapatılmıştır, işlem yapılamaz: {id}");

    // ─── Sağlık raporu onay zinciri (#172) ───

    public static Error HealthReportMissing(Guid attendanceId) =>
        new("Attendance.HealthReportMissing",
            $"Bu devamsızlık kaydında sağlık raporu yok: {attendanceId}");

    public static Error HealthReportNotPending(string currentStatus) =>
        new("Attendance.HealthReportNotPending",
            $"Sağlık raporu onay bekleyen durumda değil. Mevcut durum: {currentStatus}.");

    public static Error HealthReportAlreadyPending(Guid attendanceId) =>
        new("Attendance.HealthReportAlreadyPending",
            $"Bu kayıtta onay bekleyen bir sağlık raporu zaten var: {attendanceId}. " +
            "Önce mevcut rapor onaylanmalı ya da reddedilmelidir.");

    public static Error RejectionReasonRequired() =>
        new("Attendance.RejectionReasonRequired", "Ret gerekçesi zorunludur.");
}
