using MESNET.Common.Shared.Pagination;

namespace MESNET.Reporting.Application.Commands;

/// <summary>
/// Doküman detayını getir (PdfContent hariç)
/// </summary>
public sealed record GetDocumentById(Guid DocumentId);

/// <summary>
/// Doküman PDF'ini getir
/// </summary>
public sealed record GetDocumentPdf(Guid DocumentId);

/// <summary>
/// Filtrelere göre doküman listesi getir
/// </summary>
public sealed record GetPendingDocuments(
    string? Status,
    string? FormType,
    Guid? TeacherId,
    Guid? InstitutionId) : PagedQuery;

/// <summary>
/// Öğrenciye ait dokümanları getir
/// </summary>
public sealed record GetDocumentsByStudent(Guid StudentId);

/// <summary>
/// Seçili dokümanları ZIP olarak indir. Handler byte[] döner.
/// </summary>
public sealed record DownloadDocumentsZip(List<Guid> DocumentIds);
