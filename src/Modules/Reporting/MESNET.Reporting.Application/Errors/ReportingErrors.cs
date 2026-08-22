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

    public static Error EmptyDocumentList() =>
        new("Reporting.EmptyDocumentList",
            "İşlem için en az bir doküman seçilmelidir. Doküman listesi boş veya gönderilmedi.");

    public static Error TermGradesNotFound(Guid studentId) =>
        new("Reporting.TermGradesNotFound",
            $"Bu öğrenci için gönderilmiş dönem notu bulunamadı; fiş üretilemez: {studentId}");

    /// <summary>
    /// Okulda staj (#171) — Dönem Not Fişi üretilmez. Fiş işletme adı ve iki işletme imzası
    /// ister; işverensiz staj için MEB'in tanımladığı ayrı bir form yoktur. Not kaydı tutulur,
    /// belge üretilmez.
    /// </summary>
    public static Error TermGradeSlipNotAvailableForSchoolPlacement(Guid studentId) =>
        new("Reporting.TermGradeSlipNotAvailableForSchoolPlacement",
            "Okulda staj yapan öğrenci için Dönem Not Fişi üretilmez; not kaydı tutulur, " +
            $"belge düzenlenmez: {studentId}");
}
