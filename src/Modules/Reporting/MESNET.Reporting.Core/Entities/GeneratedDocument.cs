using MESNET.Reporting.Core.Enums;

namespace MESNET.Reporting.Core.Entities;

/// <summary>
/// Üretilen MEB dokümanının yaşam döngüsünü takip eden entity.
/// Id = QR koddaki DocumentId (form data'daki DocumentId ile aynı).
/// </summary>
public sealed class GeneratedDocument
{
    public Guid Id { get; init; }

    // Form tipi ve durum
    public required MebFormType FormType { get; init; }
    public PhysicalDocumentStatus Status { get; set; } = PhysicalDocumentStatus.Generated;

    // Duplicate primitive alanlar — SmartEnum LINQ nested path tuzağı:
    // data->'FormType'->>'Name' NULL döner (SmartEnum string olarak serialize edilir, obje değil).
    // Bu alanlar Marten LINQ sorgularında kullanılır.
    public string FormTypeName { get; init; } = "";
    public string StatusName { get; set; } = "Generated";

    // İlişkili entity ID'leri
    public Guid? StudentId { get; init; }
    public Guid? BusinessId { get; init; }
    public Guid? InstitutionId { get; init; }
    public Guid? TeacherId { get; init; }

    // Form verisi — JSON olarak saklanır, veri erişimi ve yeniden üretim için
    public required string FormDataJson { get; init; }

    // PDF snapshot — MinIO object path (oluşturma anında sabitlenir)
    public string? PdfStoragePath { get; set; }

    // Oluşturma bilgileri
    public required string GeneratedByName { get; init; }
    public Guid GeneratedByUserId { get; init; }
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;

    // Yazdırma bilgileri
    public DateTime? PrintedAt { get; set; }
    public string? PrintedByName { get; set; }
    public Guid? PrintedByUserId { get; set; }

    // İmzalanıp teslim edilme bilgileri
    public DateTime? SignedAndReturnedAt { get; set; }
    public string? ReturnedByName { get; set; }
    public Guid? ReturnedByUserId { get; set; }

    // Arşivleme bilgileri
    public DateTime? ArchivedAt { get; set; }
    public string? ArchivedByName { get; set; }
    public Guid? ArchivedByUserId { get; set; }

    // Ek bilgiler
    public string? AcademicYear { get; init; }
    public string? Description { get; init; }

    /// <summary>
    /// Yazdırıldı olarak işaretle
    /// </summary>
    public void MarkAsPrinted(string userName, Guid userId)
    {
        Status = PhysicalDocumentStatus.Printed;
        StatusName = PhysicalDocumentStatus.Printed.Name;
        PrintedAt = DateTime.UtcNow;
        PrintedByName = userName;
        PrintedByUserId = userId;
    }

    /// <summary>
    /// İmzalanıp teslim edildi olarak işaretle
    /// </summary>
    public void MarkAsSignedAndReturned(string userName, Guid userId)
    {
        Status = PhysicalDocumentStatus.SignedAndReturned;
        StatusName = PhysicalDocumentStatus.SignedAndReturned.Name;
        SignedAndReturnedAt = DateTime.UtcNow;
        ReturnedByName = userName;
        ReturnedByUserId = userId;
    }

    /// <summary>
    /// Arşivlendi olarak işaretle
    /// </summary>
    public void MarkAsArchived(string userName, Guid userId)
    {
        Status = PhysicalDocumentStatus.Archived;
        StatusName = PhysicalDocumentStatus.Archived.Name;
        ArchivedAt = DateTime.UtcNow;
        ArchivedByName = userName;
        ArchivedByUserId = userId;
    }
}
