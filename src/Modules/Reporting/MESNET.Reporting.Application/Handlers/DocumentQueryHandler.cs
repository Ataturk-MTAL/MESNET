using System.Text.Json;
using Marten;
using MESNET.Common.Infrastructure.Storage;
using MESNET.Common.Shared;
using MESNET.Reporting.Application.Commands;
using MESNET.Reporting.Application.Errors;
using MESNET.Reporting.Application.Templates;
using MESNET.Reporting.Core.Entities;
using MESNET.Reporting.Core.Enums;
using MESNET.Reporting.Core.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace MESNET.Reporting.Application.Handlers;

public static class DocumentQueryHandler
{
    private const string BucketName = "meb-forms";

    // ─── Doküman detayı ───
    public static async Task<Result<GeneratedDocumentSummary>> Handle(GetDocumentById query, IQuerySession session)
    {
        var doc = await session.LoadAsync<GeneratedDocument>(query.DocumentId);
        if (doc is null)
            return Result<GeneratedDocumentSummary>.Failure(ReportingErrors.DocumentNotFound(query.DocumentId));

        return MapToSummary(doc);
    }

    // ─── PDF indir (presigned URL veya on-demand fallback) ───
    public static async Task<Result<PdfDownloadResult>> Handle(
        GetDocumentPdf query, IQuerySession session, IFileStorageService storage)
    {
        var doc = await session.LoadAsync<GeneratedDocument>(query.DocumentId);
        if (doc is null)
            return Result<PdfDownloadResult>.Failure(ReportingErrors.DocumentNotFound(query.DocumentId));

        // MinIO'da PDF snapshot varsa presigned URL döndür
        if (!string.IsNullOrEmpty(doc.PdfStoragePath))
        {
            var urlResult = await storage.GetPresignedDownloadUrlAsync(BucketName, doc.PdfStoragePath, 3600);
            if (urlResult.IsSuccess)
                return new PdfDownloadResult(PresignedUrl: urlResult.Value);
        }

        // Fallback: on-demand üretim (eski kayıtlar veya MinIO hatası)
        var pdfDocument = DeserializeToDocument(doc.FormType, doc.FormDataJson);
        var pdfBytes = pdfDocument.GeneratePdf();
        return new PdfDownloadResult(PdfBytes: pdfBytes);
    }

    // ─── Filtreli liste ───
    public static async Task<IReadOnlyList<GeneratedDocumentSummary>> Handle(GetPendingDocuments query, IQuerySession session)
    {
        IQueryable<GeneratedDocument> queryable = session.Query<GeneratedDocument>();

        if (!string.IsNullOrEmpty(query.Status) && PhysicalDocumentStatus.TryFromName(query.Status, out var status))
            queryable = queryable.Where(d => d.Status == status);

        if (!string.IsNullOrEmpty(query.FormType) && MebFormType.TryFromName(query.FormType, out var formType))
            queryable = queryable.Where(d => d.FormType == formType);

        if (query.TeacherId.HasValue)
            queryable = queryable.Where(d => d.TeacherId == query.TeacherId.Value);

        if (query.InstitutionId.HasValue)
            queryable = queryable.Where(d => d.InstitutionId == query.InstitutionId.Value);

        var docs = await queryable
            .OrderByDescending(d => d.GeneratedAt)
            .ToListAsync();

        return docs.Select(MapToSummary).ToList();
    }

    // ─── Öğrenciye göre dokümanlar ───
    public static async Task<IReadOnlyList<GeneratedDocumentSummary>> Handle(GetDocumentsByStudent query, IQuerySession session)
    {
        var docs = await session.Query<GeneratedDocument>()
            .Where(d => d.StudentId == query.StudentId)
            .OrderByDescending(d => d.GeneratedAt)
            .ToListAsync();

        return docs.Select(MapToSummary).ToList();
    }

    private static IDocument DeserializeToDocument(MebFormType formType, string json)
    {
        if (formType == MebFormType.InternshipContract)
            return new InternshipContractDocument(JsonSerializer.Deserialize<InternshipContractFormData>(json)!);
        if (formType == MebFormType.MonthlyActivityReport)
            return new MonthlyActivityFormDocument(JsonSerializer.Deserialize<MonthlyActivityFormData>(json)!);
        if (formType == MebFormType.GuidanceVisit)
            return new GuidanceVisitFormDocument(JsonSerializer.Deserialize<GuidanceVisitFormData>(json)!);
        if (formType == MebFormType.AttendanceSheet)
            return new AttendanceSheetDocument(JsonSerializer.Deserialize<AttendanceSheetData>(json)!);
        if (formType == MebFormType.SkillExam)
            return new SkillExamFormDocument(JsonSerializer.Deserialize<SkillExamFormData>(json)!);
        if (formType == MebFormType.BusinessEvaluation)
            return new BusinessEvaluationFormDocument(JsonSerializer.Deserialize<BusinessEvaluationFormData>(json)!);

        throw new InvalidOperationException($"Bilinmeyen form tipi: {formType.Name}");
    }

    private static GeneratedDocumentSummary MapToSummary(GeneratedDocument doc) => new()
    {
        Id = doc.Id,
        FormType = doc.FormType,
        Status = doc.Status,
        StudentId = doc.StudentId,
        BusinessId = doc.BusinessId,
        InstitutionId = doc.InstitutionId,
        TeacherId = doc.TeacherId,
        GeneratedByName = doc.GeneratedByName,
        GeneratedAt = doc.GeneratedAt,
        PrintedAt = doc.PrintedAt,
        PrintedByName = doc.PrintedByName,
        SignedAndReturnedAt = doc.SignedAndReturnedAt,
        ReturnedByName = doc.ReturnedByName,
        ArchivedAt = doc.ArchivedAt,
        ArchivedByName = doc.ArchivedByName,
        AcademicYear = doc.AcademicYear,
        Description = doc.Description
    };
}

/// <summary>
/// PDF indirme sonucu — ya presigned URL ya da byte array (on-demand fallback)
/// </summary>
public sealed record PdfDownloadResult(string? PresignedUrl = null, byte[]? PdfBytes = null)
{
    public bool HasPresignedUrl => !string.IsNullOrEmpty(PresignedUrl);
}
