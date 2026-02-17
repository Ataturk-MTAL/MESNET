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
        var group = app.MapGroup("/api/reports").WithTags("Reporting");

        group.MapPost("/internship-contract", PostInternshipContract);
        group.MapPost("/monthly-activity", PostMonthlyActivity);
        group.MapPost("/guidance-visit", PostGuidanceVisit);
        group.MapPost("/attendance-sheet", PostAttendanceSheet);
        group.MapPost("/skill-exam", PostSkillExam);
        group.MapPost("/business-evaluation", PostBusinessEvaluation);
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

    private static UserContext ExtractUserContext(HttpContext http)
    {
        var userId = Guid.TryParse(http.User.FindFirst("sub")?.Value, out var id) ? id : Guid.Empty;
        var fullName = http.User.FindFirst("name")?.Value
                       ?? http.User.FindFirst("preferred_username")?.Value
                       ?? "Bilinmeyen Kullanıcı";
        return new UserContext(userId, fullName);
    }
}
