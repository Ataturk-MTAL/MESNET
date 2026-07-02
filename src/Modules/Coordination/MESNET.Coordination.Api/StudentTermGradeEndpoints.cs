using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Coordination.Api;

public static class StudentTermGradeEndpoints
{
    public static IEndpointRouteBuilder MapStudentTermGradeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/coordination/term-grades")
            .WithTags("StudentTermGrade").RequireAuthorization();

        // İşletme yetkilisi (CompanyManager) — kendi öğrencilerinin notlarını girer/gönderir.
        // BusinessId token'daki business_id claim'inden alınır (kullanıcı-girişli değil).
        group.MapPost("/", PostEnter).RequireAuthorization(Permissions.Company.EnterGrade);
        group.MapPost("/{id:guid}/submit", PostSubmit).RequireAuthorization(Permissions.Company.EnterGrade);
        // İşletme: kendi öğrencileri + not durumu
        group.MapGet("/my-students", GetMyStudents).RequireAuthorization(Permissions.Company.EnterGrade);
        // Koordinatör/okul: gönderilmiş notlar (fiş üretilecekler)
        group.MapGet("/submitted", GetSubmitted).RequireAuthorization(Permissions.Coordinator.Report);

        return app;
    }

    private static async Task<IResult> GetMyStudents(
        Guid academicPeriodId, IMessageBus bus, HttpContext http)
    {
        var result = await bus.InvokeAsync<TermGradeRowsResult>(
            new GetMyStudentsForGrading(GetBusinessId(http), academicPeriodId));
        return Results.Ok(ResponseBuilder.Success().AddData(result).Build());
    }

    private static async Task<IResult> GetSubmitted(
        Guid academicPeriodId, IMessageBus bus, HttpContext http)
    {
        var result = await bus.InvokeAsync<TermGradeRowsResult>(
            new GetSubmittedTermGrades(GetInstitutionId(http), academicPeriodId));
        return Results.Ok(ResponseBuilder.Success().AddData(result).Build());
    }

    private static Guid GetInstitutionId(HttpContext http) =>
        Guid.TryParse(http.User.FindFirst("institution_id")?.Value, out var id) ? id : Guid.Empty;

    private static async Task<IResult> PostEnter(
        EnterStudentTermGrade command, IMessageBus bus, HttpContext http)
    {
        var id = await bus.InvokeAsync<Guid>(command with
        {
            BusinessId = GetBusinessId(http),
            EnteredByName = GetUserName(http)
        });

        return Results.Created(
            $"/api/coordination/term-grades/{id}",
            ResponseBuilder.Success(201)
                .AddData(new { id })
                .AddMessage("Dönem notu kaydedildi (taslak).")
                .Build());
    }

    private static async Task<IResult> PostSubmit(
        Guid id, IMessageBus bus, HttpContext http)
    {
        await bus.InvokeAsync(new SubmitStudentTermGrade(id) { BusinessId = GetBusinessId(http) });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Dönem notu gönderildi.")
            .Build());
    }

    // Token'daki business_id claim'i — yoksa Empty (handler yerleştirme kontrolünde reddeder)
    private static Guid GetBusinessId(HttpContext http) =>
        Guid.TryParse(http.User.FindFirst("business_id")?.Value, out var id) ? id : Guid.Empty;

    private static string? GetUserName(HttpContext http) =>
        http.User.FindFirst("name")?.Value ?? http.User.FindFirst("preferred_username")?.Value;
}
