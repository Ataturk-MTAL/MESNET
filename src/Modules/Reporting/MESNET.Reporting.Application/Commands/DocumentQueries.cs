using MESNET.Common.Shared.Pagination;
using MESNET.Common.Shared.Security;

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

/// <summary>
/// Toplu belge oluşturma — Müdür/müdür yardımcısı tarafından tetiklenir.
/// Aynı dönem için zaten oluşturulmuş belgeler atlanır (duplicate kontrolü).
/// </summary>
public sealed record GenerateBatchDocuments(
    string FormType,
    int Year,
    int Month,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    string AcademicYear,
    UserContext User,
    string? InstitutionName = null);

/// <summary>
/// Toplu belge oluşturma sonucu
/// </summary>
public sealed record BatchGenerateResult(int Generated, int Skipped, string FormTypeSlug);
