namespace MESNET.Common.Shared;

/// <summary>
/// Dosya yükleme ile ilgili ortak hatalar (Business, Contract, Payment, Reporting paylaşır).
/// </summary>
public static class FileUploadError
{
    /// <summary>
    /// Dosya boyutu maksimum limiti aşıyor.
    /// </summary>
    public static Error FileTooLarge(long actualSizeBytes, long maxSizeBytes) =>
        new("FILE_TOO_LARGE",
            $"Dosya boyutu {actualSizeBytes / 1024.0 / 1024.0:F2} MB. Maksimum izin verilen boyut: {maxSizeBytes / 1024 / 1024} MB.");

    /// <summary>
    /// Dosya tipi geçersiz (PDF olmalı).
    /// </summary>
    public static Error InvalidFileType(string actualContentType) =>
        new("INVALID_FILE_TYPE",
            $"Geçersiz dosya tipi: {actualContentType}. Sadece PDF dosyaları kabul edilir.");

    /// <summary>
    /// Dosya içeriği geçersiz veya bozuk.
    /// </summary>
    public static Error InvalidFileContent() =>
        new("INVALID_FILE_CONTENT",
            "Dosya içeriği geçersiz veya bozuk. PDF magic byte kontrol edilemedi.");

    /// <summary>
    /// Dosya yüklenemedi.
    /// </summary>
    public static Error FileNull() =>
        new("FILE_NULL",
            "Dosya yüklenmedi veya boş.");
}
