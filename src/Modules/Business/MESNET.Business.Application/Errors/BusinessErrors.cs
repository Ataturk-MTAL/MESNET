using MESNET.Common.Shared;

namespace MESNET.Business.Application.Errors;

public static class BusinessErrors
{
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
}
