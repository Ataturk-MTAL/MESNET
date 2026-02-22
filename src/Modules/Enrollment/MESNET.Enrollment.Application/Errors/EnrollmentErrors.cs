using MESNET.Common.Shared;

namespace MESNET.Enrollment.Application.Errors;

public static class EnrollmentErrors
{
    public static Error StudentNotFound(Guid id) =>
        new("Enrollment.StudentNotFound", $"Öğrenci bulunamadı: {id}");

    public static Error TeacherNotFound(Guid id) =>
        new("Enrollment.TeacherNotFound", $"Öğretmen bulunamadı: {id}");

    public static Error PlacementNotFound(Guid id) =>
        new("Enrollment.PlacementNotFound", $"Yerleştirme bulunamadı: {id}");

    public static Error BusinessNotFound(Guid id) =>
        new("Enrollment.BusinessNotFound", $"İşletme bulunamadı: {id}");

    public static Error InvalidTransition(string entity, string from, string to) =>
        new("Enrollment.InvalidTransition", $"{entity} '{from}' durumundan '{to}' durumuna geçirilemez.");

    public static Error BusinessNotActive =>
        new("Enrollment.BusinessNotActive", "İşletme aktif değil, yerleştirme yapılamaz.");

    public static Error BusinessCapacityFull =>
        new("Enrollment.BusinessCapacityFull", "İşletme kapasitesi dolu, yerleştirme yapılamaz.");

    public static Error AcademicPeriodNotFound(Guid id) =>
        new("Enrollment.AcademicPeriodNotFound", $"Eğitim dönemi bulunamadı: {id}");

    public static Error AcademicPeriodClosed(Guid id) =>
        new("Enrollment.AcademicPeriodClosed", $"Bu eğitim dönemi kapatılmıştır, işlem yapılamaz: {id}");
}
