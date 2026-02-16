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
}
