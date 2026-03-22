using System.Text.Json;
using Marten;
using MESNET.Common.Infrastructure.Storage;
using MESNET.Common.Shared.Security;
using MESNET.Reporting.Application.Commands;
using MESNET.Reporting.Application.Templates;
using MESNET.Reporting.Core.Entities;
using MESNET.Reporting.Core.Enums;
using MESNET.Reporting.Core.Models;
using QuestPDF.Fluent;

namespace MESNET.Reporting.Application.Handlers;

public static class GenerateDocumentHandler
{
    private const string BucketName = "meb-forms";

    // ─── Form 1: Staj Sözleşmesi ───
    public static async Task<(Guid, NotifyDocumentGenerated)> Handle(
        GenerateInternshipContractDocument command, IDocumentSession session, IFileStorageService storage)
    {
        var doc = CreateDocument(
            command.Data.DocumentId, MebFormType.InternshipContract, command.Data, command.User,
            studentId: command.Data.StudentId,
            businessId: command.Data.BusinessId,
            institutionId: command.Data.InstitutionId,
            teacherId: command.Data.TeacherId,
            academicYear: command.Data.AcademicYear);

        var pdf = new InternshipContractDocument(command.Data);
        await UploadPdfSnapshot(storage, doc, pdf);

        session.Store(doc);
        return (doc.Id, BuildNotification(doc, command.User));
    }

    // ─── Form 2: Aylık Eğitim Faaliyeti Formu ───
    public static async Task<(Guid, NotifyDocumentGenerated)> Handle(
        GenerateMonthlyActivityDocument command, IDocumentSession session, IFileStorageService storage)
    {
        var doc = CreateDocument(
            command.Data.DocumentId, MebFormType.MonthlyActivityReport, command.Data, command.User,
            studentId: command.Data.StudentId,
            businessId: command.Data.BusinessId,
            institutionId: command.Data.InstitutionId,
            teacherId: command.Data.TeacherId,
            academicYear: command.Data.AcademicYear);

        var pdf = new MonthlyActivityFormDocument(command.Data);
        await UploadPdfSnapshot(storage, doc, pdf);

        session.Store(doc);
        return (doc.Id, BuildNotification(doc, command.User));
    }

    // ─── Form 3: Günlük Rehberlik Formu (tekil) ───
    public static async Task<(Guid, NotifyDocumentGenerated)> Handle(
        GenerateGuidanceVisitDocument command, IDocumentSession session, IFileStorageService storage)
    {
        var doc = CreateDocument(
            command.Data.DocumentId, MebFormType.GuidanceVisit, command.Data, command.User,
            businessId: command.Data.BusinessId,
            institutionId: command.Data.InstitutionId,
            teacherId: command.Data.TeacherId);

        var pdf = new GuidanceVisitFormDocument(command.Data);
        await UploadPdfSnapshot(storage, doc, pdf);

        session.Store(doc);
        return (doc.Id, BuildNotification(doc, command.User));
    }

    // ─── Form 3: Günlük Rehberlik Formu (toplu — öğretmen bazlı) ───
    // Bir öğretmenin tüm haftalık ziyaretleri tek PDF'te toplanır
    public static async Task<(Guid, NotifyDocumentGenerated)> Handle(
        GenerateGuidanceVisitBatchDocument command, IDocumentSession session, IFileStorageService storage)
    {
        if (command.Forms.Count == 0) throw new InvalidOperationException("Batch boş olamaz.");

        var batchId = Guid.NewGuid();
        var doc = CreateDocument(
            batchId, MebFormType.GuidanceVisit, command.Forms, command.User,
            institutionId: command.InstitutionId,
            teacherId: command.TeacherId,
            description: command.Description);

        var pdf = new GuidanceVisitFormDocument(command.Forms);
        await UploadPdfSnapshot(storage, doc, pdf);

        session.Store(doc);
        return (doc.Id, BuildNotification(doc, command.User));
    }

    // ─── Form 7: Aylık Devamsızlık — öğretmen bazlı toplu ───
    public static async Task<(Guid, NotifyDocumentGenerated)> Handle(
        GenerateMonthlyAttendanceBatchDocument command, IDocumentSession session, IFileStorageService storage)
    {
        if (command.Pages.Count == 0) throw new InvalidOperationException("Batch boş olamaz.");

        var batchId = Guid.NewGuid();
        var doc = CreateDocument(
            batchId, MebFormType.MonthlyAttendanceReport, command.Pages, command.User,
            institutionId: command.InstitutionId,
            teacherId: command.TeacherId,
            description: command.Description);

        var pdf = new MonthlyAttendanceReportDocument(command.Pages);
        await UploadPdfSnapshot(storage, doc, pdf);

        session.Store(doc);
        return (doc.Id, BuildNotification(doc, command.User));
    }

    // ─── Form 4: Devamsızlık Çizelgesi ───
    public static async Task<(Guid, NotifyDocumentGenerated)> Handle(
        GenerateAttendanceSheetDocument command, IDocumentSession session, IFileStorageService storage)
    {
        var doc = CreateDocument(
            command.Data.DocumentId, MebFormType.AttendanceSheet, command.Data, command.User,
            studentId: command.Data.StudentId,
            businessId: command.Data.BusinessId,
            institutionId: command.Data.InstitutionId,
            teacherId: command.Data.TeacherId,
            academicYear: command.Data.AcademicYear);

        var pdf = new AttendanceSheetDocument(command.Data);
        await UploadPdfSnapshot(storage, doc, pdf);

        session.Store(doc);
        return (doc.Id, BuildNotification(doc, command.User));
    }

    // ─── Form 5: Beceri Sınavı Not Fişi ───
    public static async Task<(Guid, NotifyDocumentGenerated)> Handle(
        GenerateSkillExamDocument command, IDocumentSession session, IFileStorageService storage)
    {
        var doc = CreateDocument(
            command.Data.DocumentId, MebFormType.SkillExam, command.Data, command.User,
            studentId: command.Data.StudentId,
            businessId: command.Data.BusinessId,
            institutionId: command.Data.InstitutionId,
            teacherId: command.Data.TeacherId,
            academicYear: command.Data.AcademicYear);

        var pdf = new SkillExamFormDocument(command.Data);
        await UploadPdfSnapshot(storage, doc, pdf);

        session.Store(doc);
        return (doc.Id, BuildNotification(doc, command.User));
    }

    // ─── Form 6: İşletme Değerlendirme Formu ───
    public static async Task<(Guid, NotifyDocumentGenerated)> Handle(
        GenerateBusinessEvaluationDocument command, IDocumentSession session, IFileStorageService storage)
    {
        var doc = CreateDocument(
            command.Data.DocumentId, MebFormType.BusinessEvaluation, command.Data, command.User,
            businessId: command.Data.BusinessId,
            institutionId: command.Data.InstitutionId,
            teacherId: command.Data.TeacherId);

        var pdf = new BusinessEvaluationFormDocument(command.Data);
        await UploadPdfSnapshot(storage, doc, pdf);

        session.Store(doc);
        return (doc.Id, BuildNotification(doc, command.User));
    }

    // ─── PDF snapshot üret ve MinIO'ya yükle ───
    private static async Task UploadPdfSnapshot(
        IFileStorageService storage, GeneratedDocument doc, QuestPDF.Infrastructure.IDocument pdfDocument)
    {
        var pdfBytes = pdfDocument.GeneratePdf();
        var objectPath = $"{doc.FormType.Name}/{doc.GeneratedAt:yyyy-MM}/{doc.Id}.pdf";

        using var stream = new MemoryStream(pdfBytes);
        var metadata = new Dictionary<string, string>
        {
            ["x-amz-meta-form-type"] = doc.FormType.Name,
            ["x-amz-meta-generated-by"] = doc.GeneratedByName,
            ["x-amz-meta-generated-at"] = doc.GeneratedAt.ToString("O")
        };

        var result = await storage.UploadFileAsync(BucketName, objectPath, stream, "application/pdf", metadata);

        if (result.IsSuccess)
            doc.PdfStoragePath = objectPath;
    }

    private static NotifyDocumentGenerated BuildNotification(GeneratedDocument doc, UserContext user) =>
        new(doc.Id, doc.FormType.Name, doc.FormType.Slug, doc.InstitutionId, doc.TeacherId, doc.StudentId, user.FullName);

    private static GeneratedDocument CreateDocument(
        Guid documentId, MebFormType formType, object formData, UserContext user,
        Guid? studentId = null, Guid? businessId = null,
        Guid? institutionId = null, Guid? teacherId = null,
        string? academicYear = null, string? description = null)
    {
        return new GeneratedDocument
        {
            Id = documentId,
            FormType = formType,
            FormTypeName = formType.Name,
            FormDataJson = JsonSerializer.Serialize(formData, formData.GetType()),
            GeneratedByName = user.FullName,
            GeneratedByUserId = user.UserId,
            GeneratedAt = DateTime.UtcNow,
            StudentId = studentId,
            BusinessId = businessId,
            InstitutionId = institutionId,
            TeacherId = teacherId,
            AcademicYear = academicYear,
            Description = description
        };
    }
}
