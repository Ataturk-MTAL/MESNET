using System.Security.Claims;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Reporting.Application.Commands;
using MESNET.Reporting.Core.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Reporting.Api;

public static class ReportingEndpoints
{
    public static void MapReportingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports").WithTags("Reporting").RequireAuthorization();

        group.MapPost("/internship-contract", PostInternshipContract).RequireAuthorization(Permissions.Internship.Report);
        group.MapPost("/monthly-activity", PostMonthlyActivity).RequireAuthorization(Permissions.Coordinator.Report);
        group.MapPost("/guidance-visit", PostGuidanceVisit).RequireAuthorization(Permissions.Coordinator.Report);
        group.MapPost("/attendance-sheet", PostAttendanceSheet).RequireAuthorization(Permissions.Attendance.Report);
        group.MapPost("/skill-exam", PostSkillExam).RequireAuthorization(Permissions.Coordinator.Report);
        group.MapPost("/business-evaluation", PostBusinessEvaluation).RequireAuthorization(Permissions.Coordinator.Visit);

        // Form 8: Dönem Not Fişi
        group.MapPost("/term-grade-slip", PostTermGradeSlip).RequireAuthorization(Permissions.Coordinator.Report);
        group.MapGet("/term-grade-slip/preview", GetTermGradeSlipPreview);
        // Gerçek (işletmenin gönderdiği) notlardan üret — okul/koordinatör
        group.MapPost("/term-grade-slip/generate", PostGenerateTermGradeSlipFromGrades).RequireAuthorization(Permissions.Coordinator.Report);

        // Form 7: Aylık Devam - Devamsızlık Bildirim Çizelgesi
        group.MapGet("/monthly-attendance/preview", GetMonthlyAttendancePreview).RequireAuthorization(Permissions.Attendance.Report);
        group.MapGet("/monthly-attendance/preview-batch", GetMonthlyAttendanceBatchPreview).RequireAuthorization(Permissions.Attendance.Report);
        group.MapPost("/monthly-attendance", PostMonthlyAttendance).RequireAuthorization(Permissions.Attendance.Report);
    }

    // --- Form 1: Isletme Staj Sozlesmesi ---
    private static async Task<IResult> PostInternshipContract(
        InternshipContractFormData data, IMessageBus bus, HttpContext http)
    {
        var user = ExtractUserContext(http);
        var command = new GenerateInternshipContractDocument(data, user);
        var documentId = await bus.InvokeAsync<Guid>(command);

        return Results.Created(
            $"/api/reports/documents/{documentId}",
            ResponseBuilder.Success(201)
                .AddData(new { documentId })
                .AddMessage("Staj sözleşmesi oluşturuldu.")
                .Build());
    }

    // --- Form 2: Aylik Egitim Faaliyeti Formu ---
    private static async Task<IResult> PostMonthlyActivity(
        MonthlyActivityFormData data, IMessageBus bus, HttpContext http)
    {
        var user = ExtractUserContext(http);
        var command = new GenerateMonthlyActivityDocument(data, user);
        var documentId = await bus.InvokeAsync<Guid>(command);

        return Results.Created(
            $"/api/reports/documents/{documentId}",
            ResponseBuilder.Success(201)
                .AddData(new { documentId })
                .AddMessage("Aylık eğitim faaliyeti formu oluşturuldu.")
                .Build());
    }

    // --- Form 3: Gunluk Rehberlik Formu ---
    private static async Task<IResult> PostGuidanceVisit(
        GuidanceVisitFormData data, IMessageBus bus, HttpContext http)
    {
        var user = ExtractUserContext(http);
        var command = new GenerateGuidanceVisitDocument(data, user);
        var documentId = await bus.InvokeAsync<Guid>(command);

        return Results.Created(
            $"/api/reports/documents/{documentId}",
            ResponseBuilder.Success(201)
                .AddData(new { documentId })
                .AddMessage("Günlük rehberlik formu oluşturuldu.")
                .Build());
    }

    // --- Form 4: Isletme Devamsizlik Cizelgesi ---
    private static async Task<IResult> PostAttendanceSheet(
        AttendanceSheetData data, IMessageBus bus, HttpContext http)
    {
        var user = ExtractUserContext(http);
        var command = new GenerateAttendanceSheetDocument(data, user);
        var documentId = await bus.InvokeAsync<Guid>(command);

        return Results.Created(
            $"/api/reports/documents/{documentId}",
            ResponseBuilder.Success(201)
                .AddData(new { documentId })
                .AddMessage("Devamsızlık çizelgesi oluşturuldu.")
                .Build());
    }

    // --- Form 5: Donem Sonu Beceri Sinavi Not Fisi ---
    private static async Task<IResult> PostSkillExam(
        SkillExamFormData data, IMessageBus bus, HttpContext http)
    {
        var user = ExtractUserContext(http);
        var command = new GenerateSkillExamDocument(data, user);
        var documentId = await bus.InvokeAsync<Guid>(command);

        return Results.Created(
            $"/api/reports/documents/{documentId}",
            ResponseBuilder.Success(201)
                .AddData(new { documentId })
                .AddMessage("Beceri sınavı not fişi oluşturuldu.")
                .Build());
    }

    // --- Form 6: Isletme Degerlendirme Formu ---
    private static async Task<IResult> PostBusinessEvaluation(
        BusinessEvaluationFormData data, IMessageBus bus, HttpContext http)
    {
        var user = ExtractUserContext(http);
        var command = new GenerateBusinessEvaluationDocument(data, user);
        var documentId = await bus.InvokeAsync<Guid>(command);

        return Results.Created(
            $"/api/reports/documents/{documentId}",
            ResponseBuilder.Success(201)
                .AddData(new { documentId })
                .AddMessage("İşletme değerlendirme formu oluşturuldu.")
                .Build());
    }

    // --- Form 8: Donem Not Fisi ---
    private static async Task<IResult> PostTermGradeSlip(
        TermGradeSlipFormData data, IMessageBus bus, HttpContext http)
    {
        var user = ExtractUserContext(http);
        var command = new GenerateTermGradeSlipDocument(data, user);
        var documentId = await bus.InvokeAsync<Guid>(command);

        return Results.Created(
            $"/api/reports/documents/{documentId}",
            ResponseBuilder.Success(201)
                .AddData(new { documentId })
                .AddMessage("Dönem not fişi oluşturuldu.")
                .Build());
    }

    // --- Form 8: Donem Not Fisi (layout önizleme — örnek veri) ---
    private static async Task<IResult> GetTermGradeSlipPreview(IMessageBus bus)
    {
        var pdfBytes = await bus.InvokeAsync<byte[]>(new GenerateTermGradeSlipPreview());
        return Results.File(pdfBytes, "application/pdf", "donem-not-fisi-onizleme.pdf");
    }

    // --- Form 8: Donem Not Fisi — işletmenin gönderdiği gerçek notlardan üret ---
    private static async Task<IResult> PostGenerateTermGradeSlipFromGrades(
        GenerateTermGradeSlipFromGrades command, IMessageBus bus, HttpContext http)
    {
        var user = ExtractUserContext(http);
        var documentId = await bus.InvokeAsync<Guid>(command with { User = user });

        return Results.Created(
            $"/api/reports/documents/{documentId}",
            ResponseBuilder.Success(201)
                .AddData(new { documentId })
                .AddMessage("Dönem not fişi, işletmenin gönderdiği notlardan üretildi.")
                .Build());
    }

    // --- Form 7: Aylik Devam - Devamsizlik Bildirim Cizelgesi (Preview) ---
    private static async Task<IResult> GetMonthlyAttendancePreview(
        Guid institutionId, Guid academicPeriodId, Guid businessId,
        int year, int month, string institutionName, string academicYear,
        IMessageBus bus)
    {
        var command = new GenerateMonthlyAttendancePreview(
            institutionId, academicPeriodId, businessId, year, month,
            institutionName, academicYear);

        var pdfBytes = await bus.InvokeAsync<byte[]>(command);

        return Results.File(pdfBytes, "application/pdf",
            $"aylik-devamsizlik-{year}-{month:D2}.pdf");
    }

    // --- Form 7: Aylık Devam - Devamsızlık Bildirim Çizelgesi (Batch Preview — tüm öğretmenler) ---
    private static async Task<IResult> GetMonthlyAttendanceBatchPreview(
        Guid institutionId, Guid academicPeriodId,
        int year, int month, string institutionName, string academicYear,
        IMessageBus bus)
    {
        var command = new GenerateMonthlyAttendanceBatchPreview(
            institutionId, academicPeriodId, year, month, institutionName, academicYear);

        var pdfBytes = await bus.InvokeAsync<byte[]>(command);

        return Results.File(pdfBytes, "application/pdf",
            $"aylik-devamsizlik-toplu-{year}-{month:D2}.pdf");
    }

    // --- Form 7: Aylik Devam - Devamsizlik Bildirim Cizelgesi (Archive) ---
    private static async Task<IResult> PostMonthlyAttendance(
        MonthlyAttendanceReportData data, IMessageBus bus, HttpContext http)
    {
        var user = ExtractUserContext(http);
        var command = new GenerateMonthlyAttendanceDocument(data, user);
        var documentId = await bus.InvokeAsync<Guid>(command);

        return Results.Created(
            $"/api/reports/documents/{documentId}",
            ResponseBuilder.Success(201)
                .AddData(new { documentId })
                .AddMessage("Aylık devamsızlık formu oluşturuldu.")
                .Build());
    }

    private static UserContext ExtractUserContext(HttpContext http)
    {
        var userId = Guid.TryParse(http.User.FindFirst("sub")?.Value, out var id) ? id : Guid.Empty;
        var fullName = http.User.FindFirst("name")?.Value
                       ?? http.User.FindFirst("preferred_username")?.Value
                       ?? "Bilinmeyen Kullanıcı";
        return new UserContext(userId, fullName);
    }
}
