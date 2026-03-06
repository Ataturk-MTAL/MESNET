using MESNET.Common.Shared;

namespace MESNET.Institution.Application.Errors;

public static class InstitutionErrors
{
    public static Error NotFound(Guid id) =>
        new("Institution.NotFound", $"Kurum bulunamadı: {id}");

    public static Error BranchAlreadyActive(string fieldCode) =>
        new("Institution.BranchAlreadyActive", $"Alan '{fieldCode}' zaten aktif.");

    public static Error FieldOfStudyNotFound(string fieldCode) =>
        new("Institution.FieldOfStudyNotFound", $"Eğitim alanı bulunamadı: {fieldCode}");

    public static Error BranchNotFound(string fieldCode) =>
        new("Institution.BranchNotFound", $"Aktif alan bulunamadı: {fieldCode}");

    public static Error InvalidPeriodCount(int count, int min, int max) =>
        new("Institution.InvalidPeriodCount",
            $"Günlük ders sayısı {min}-{max} arasında olmalıdır. Girilen: {count}");

    public static Error AcademicPeriodAlreadyExists(string name) =>
        new("Institution.AcademicPeriodAlreadyExists", $"Bu dönem zaten mevcut: {name}");

    public static Error AcademicPeriodNotFound(Guid id) =>
        new("Institution.AcademicPeriodNotFound", $"Dönem bulunamadı: {id}");

    public static Error AcademicPeriodAlreadyClosed(Guid id) =>
        new("Institution.AcademicPeriodAlreadyClosed", $"Dönem zaten kapatılmış: {id}");

    public static Error NoActiveAcademicPeriod(Guid institutionId) =>
        new("Institution.NoActiveAcademicPeriod", $"Kurumun aktif dönemi bulunmuyor: {institutionId}");

    public static Error InvalidSupervisorCount() =>
        new("Institution.InvalidSupervisorCount", "Şef sayısı negatif olamaz.");
}
