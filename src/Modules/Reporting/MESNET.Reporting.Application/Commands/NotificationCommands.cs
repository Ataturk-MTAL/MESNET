namespace MESNET.Reporting.Application.Commands;

/// <summary>
/// Doküman oluşturuldu notification'ı — handler'dan cascading message olarak publish edilir.
/// </summary>
public sealed record NotifyDocumentGenerated(
    Guid DocumentId,
    string FormType,
    string FormTypeSlug,
    Guid? InstitutionId,
    Guid? TeacherId,
    Guid? StudentId,
    string GeneratedByName);

/// <summary>
/// Doküman durumu değişti notification'ı.
/// </summary>
public sealed record NotifyDocumentStatusChanged(
    Guid DocumentId,
    string FormType,
    string NewStatus,
    string NewStatusSlug,
    Guid? InstitutionId,
    Guid? TeacherId,
    Guid? StudentId,
    string ChangedByName);

/// <summary>
/// Doküman silindi notification'ı.
/// </summary>
public sealed record NotifyDocumentDeleted(
    Guid DocumentId,
    string FormType,
    Guid? InstitutionId,
    Guid? TeacherId,
    string DeletedByName);
