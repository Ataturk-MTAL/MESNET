using MESNET.Common.Shared;

namespace MESNET.Attendance.Application.Errors;

public static class AttendanceErrors
{
    /// <summary>
    /// Sıfır ya da negatif sınır, "her öğrenci ilk devamsızlıkta feshedilir" demektir (#183).
    /// Yapılandırma bir fesih tetikleyicisini sessizce sıfıra çekememeli.
    /// </summary>
    public static Error InvalidAbsenceLimit() =>
        new("Attendance.InvalidAbsenceLimit", "Devamsızlık sınırı en az 1 gün olmalıdır.");

    /// <summary>
    /// Toplam ayak mazeretsiz ayağı kapsar (#183): her mazeretsiz gün aynı zamanda toplam bir
    /// gündür. Ayrı mesaj — "en az 1 gün" uyarısı bu hatayı hiç anlatmıyor ve idare neyi
    /// düzelteceğini bilemiyordu.
    /// </summary>
    public static Error TotalLimitBelowUnexcused(int totalLimit, int unexcusedLimit) =>
        new("Attendance.TotalLimitBelowUnexcused",
            $"Toplam devamsızlık sınırı ({totalLimit} gün), özürsüz sınırından "
            + $"({unexcusedLimit} gün) küçük olamaz — özürsüz günler toplama da dâhildir.");

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

    // ─── Devamsızlık türü giriş kuralları (#175) ───

    public static Error TypeNotReportableByBusiness(string typeSlug) =>
        new("Attendance.TypeNotReportableByBusiness",
            $"İşletme yalnız mazeretsiz devamsızlık bildirebilir; \"{typeSlug}\" girilemez. " +
            "Mazeret, izin ve sağlık raporu sınıflandırması okul tarafındadır.");

    public static Error PaidLeaveNotAllowedForEducationType(string? educationTypeName) =>
        new("Attendance.PaidLeaveNotAllowed",
            educationTypeName is null
                ? "Öğrencinin eğitim türü bilinmiyor; ücretli izin girilemez."
                : "Ücretli izin hakkı yalnız mesleki eğitim merkezi (MESEM) öğrencilerindedir. " +
                  "Örgün eğitimde sağlık raporu ya da veli izni gerekir.");

    public static Error StudentNotFound(Guid studentId) =>
        new("Attendance.StudentNotFound", $"Öğrenci kaydı bulunamadı: {studentId}");

    // ─── Ücretli izin başvurusu (#177) ───

    public static Error PaidLeaveRequiresApprovedRequest() =>
        new("Attendance.PaidLeaveRequiresRequest",
            "Ücretli izin doğrudan girilemez. Öğrenci başvuru açar; başvuru işletme ve okul " +
            "onayından geçtiğinde izin günleri otomatik olarak kaydedilir.");

    public static Error PaidLeaveRequestNotFound(Guid requestId) =>
        new("Attendance.PaidLeaveRequestNotFound", $"Ücretli izin başvurusu bulunamadı: {requestId}");

    public static Error PaidLeaveInvalidRange(int maxDays) =>
        new("Attendance.PaidLeaveInvalidRange",
            $"İzin tarih aralığı geçersiz. Başlangıç bitişten sonra olamaz ve aralık en çok {maxDays} gün olabilir.");

    public static Error PaidLeaveReasonRequired() =>
        new("Attendance.PaidLeaveReasonRequired", "İzin gerekçesi zorunludur.");

    public static Error PaidLeaveStartsInPast() =>
        new("Attendance.PaidLeaveStartsInPast",
            "Ücretli izin önceden planlanır; geçmiş tarihli başvuru açılamaz.");

    public static Error PaidLeaveOverlappingRequest() =>
        new("Attendance.PaidLeaveOverlappingRequest",
            "Bu tarih aralığında açık ya da onaylanmış bir ücretli izin başvurunuz zaten var.");

    public static Error PaidLeaveInvalidStage(string currentStatusSlug) =>
        new("Attendance.PaidLeaveInvalidStage",
            $"Başvuru bu adım için uygun durumda değil. Mevcut durum: {currentStatusSlug}.");

    public static Error PaidLeaveBusinessScopeMismatch() =>
        new("Attendance.PaidLeaveBusinessScopeMismatch",
            "Bu başvuruyu yalnız öğrencinin yerleştirildiği işletme onaylayabilir.");

    public static Error PaidLeaveSameApprover() =>
        new("Attendance.PaidLeaveSameApprover",
            "İşletme onayını veren kullanıcı okul onayını da veremez; onay iki ayrı taraftandır.");

    public static Error PaidLeaveStudentScopeMissing() =>
        new("Attendance.PaidLeaveStudentScopeMissing",
            "Başvuru yalnız öğrencinin kendi hesabından açılabilir (student_id bilgisi yok).");

    public static Error PaidLeavePlacementNotFound(Guid studentId) =>
        new("Attendance.PaidLeavePlacementNotFound",
            $"Öğrencinin aktif işletme yerleştirmesi bulunamadı, izin başvurusu açılamaz: {studentId}");
}
