using MESNET.Common.Shared;

namespace MESNET.Business.Application.Errors;

public static class BusinessErrors
{
    /// <summary>
    /// Kurum kapsamı token'dan gelir; claim yoksa işletmenin hangi okula bağlanacağı
    /// belirsizdir. İstekten kurum almak yerine hata vermek gerekir — aksi hâlde kapsam
    /// istemciye geçer ve yetkili bir kullanıcı başka okulun adına işletme kaydeder (#147).
    /// </summary>
    public static Error InstitutionScopeMissing() =>
        new("Business.InstitutionScopeMissing",
            "Kullanıcının kurum bilgisi bulunamadı, işletme kaydedilemiyor.");

    public static Error NotFound(Guid id) =>
        new("Business.NotFound", $"İşletme bulunamadı: {id}");

    public static Error InvalidTransition(string from, string to) =>
        new("Business.InvalidTransition", $"İşletme '{from}' durumundan '{to}' durumuna geçirilemez.");

    public static Error DocumentNotFound(Guid documentId) =>
        new("Business.DocumentNotFound", $"Belge bulunamadı: {documentId}");

    public static Error FileNull() =>
        new("Business.FileNull", "Dosya yüklenmedi veya boş.");

    public static Error FileTooLarge(long actualBytes, long maxBytes) =>
        new("Business.FileTooLarge",
            $"Dosya boyutu {actualBytes / 1024.0 / 1024.0:F2} MB. Maksimum: {maxBytes / 1024 / 1024} MB.");

    public static Error InvalidFileType(string contentType) =>
        new("Business.InvalidFileType",
            $"Geçersiz dosya tipi: {contentType}. Kabul edilen: PDF, JPEG, PNG.");

    public static Error InvalidFileContent() =>
        new("Business.InvalidFileContent", "Dosya içeriği geçersiz veya bozuk.");

    public static Error DocumentHasNoFile(Guid documentId) =>
        new("Business.DocumentHasNoFile", $"Belgenin dosyası bulunamadı: {documentId}");

    public static Error InvalidSector(string sectorName) =>
        new("Business.InvalidSector", $"Geçersiz sektör: {sectorName}");

    public static Error HasActiveStudents(Guid id) =>
        new("Business.HasActiveStudents", $"İşletmede aktif stajyerler var. Önce fesih yapılmalıdır: {id}");

    public static Error BranchCodeRequired() =>
        new("Business.BranchCodeRequired", "Alan kodu boş olamaz.");

    public static Error BranchNotOffered(string branchCode) =>
        new("Business.BranchNotOffered",
            $"Kurumda açık olmayan alan için yetki verilemez: {branchCode}");

    public static Error ClosedBusinessNotAuthorizable(Guid id) =>
        new("Business.ClosedBusinessNotAuthorizable",
            $"Kapatılmış işletmenin alan yetkileri düzenlenemez: {id}");

    public static Error HasAssignedTeacher(Guid id) =>
        new("Business.HasAssignedTeacher", $"İşletmeye atanmış öğretmen var. Önce atama kaldırılmalıdır: {id}");
}
