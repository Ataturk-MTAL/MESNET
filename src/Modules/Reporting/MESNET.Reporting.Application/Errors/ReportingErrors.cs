using MESNET.Common.Shared;

namespace MESNET.Reporting.Application.Errors;

public static class ReportingErrors
{
    public static Error DocumentNotFound(Guid documentId) =>
        new("Reporting.DocumentNotFound",
            $"Doküman bulunamadı: {documentId}");

    public static Error InvalidStatusTransition(string currentStatus, string targetStatus) =>
        new("Reporting.InvalidStatusTransition",
            $"Geçersiz durum geçişi: '{currentStatus}' → '{targetStatus}'. Sadece bir sonraki adıma geçilebilir.");

    public static Error DocumentNotPrinted(Guid documentId) =>
        new("Reporting.DocumentNotPrinted",
            $"Doküman henüz yazdırılmadı, imzalanıp teslim edildi olarak işaretlenemez: {documentId}");

    public static Error DocumentNotSignedAndReturned(Guid documentId) =>
        new("Reporting.DocumentNotSignedAndReturned",
            $"Doküman henüz imzalanıp teslim edilmedi, arşivlenemez: {documentId}");

    public static Error InvalidFormType(string formType) =>
        new("Reporting.InvalidFormType",
            $"Geçersiz form tipi: {formType}");

    public static Error InvalidDocumentStatus(string status) =>
        new("Reporting.InvalidDocumentStatus",
            $"Geçersiz doküman durumu: {status}");

    public static Error PdfStorageError(string message) =>
        new("Reporting.PdfStorageError",
            $"PDF depolama hatası: {message}");

    public static Error DocumentDeleteFailed(Guid documentId, string reason) =>
        new("Reporting.DocumentDeleteFailed",
            $"Doküman silinemedi ({documentId}): {reason}");
}
