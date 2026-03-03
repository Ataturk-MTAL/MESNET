using System.Security.Claims;
using Marten;
using MESNET.Common.Shared;

namespace MESNET.Presentation;

public static class AuthEndpoint
{
    public static WebApplication MapAuthEndpoint(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth")
            .RequireAuthorization();

        group.MapGet("/me", GetCurrentUser);

        return app;
    }

    private static async Task<IResult> GetCurrentUser(ClaimsPrincipal user, IQuerySession session)
    {
        var sub = user.FindFirst("sub")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(sub))
            return Results.Unauthorized();

        var roles = user.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var permissions = user.FindAll("permissions")
            .Select(c => c.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // institution_id artık InstitutionClaimsTransformation tarafından claim olarak eklenir
        // (token'da yoksa DB fallback — 5dk cache)
        var institutionId = user.FindFirst("institution_id")?.Value;
        var businessId = user.FindFirst("business_id")?.Value;
        string? branchCode = null;

        // DepartmentHead → kendi branş kodu (alan şefi kendi alanını görür)
        if (roles.Contains("DepartmentHead", StringComparer.OrdinalIgnoreCase))
        {
            var institution = await session
                .Query<Institution.Core.Entities.Institution>()
                .FirstOrDefaultAsync(i => i.Staff.Any(s => s.KeycloakId == sub));

            var staffMember = institution?.Staff.FirstOrDefault(s => s.KeycloakId == sub);
            if (staffMember?.BranchCode is not null)
                branchCode = staffMember.BranchCode;
        }

        return Results.Ok(ResponseBuilder.Success()
            .AddData(new
            {
                id = sub,
                username = user.FindFirst("preferred_username")?.Value,
                email = user.FindFirst("email")?.Value
                    ?? user.FindFirst(ClaimTypes.Email)?.Value,
                firstName = user.FindFirst("given_name")?.Value
                    ?? user.FindFirst(ClaimTypes.GivenName)?.Value,
                lastName = user.FindFirst("family_name")?.Value
                    ?? user.FindFirst(ClaimTypes.Surname)?.Value,
                institutionId,
                businessId,
                branchCode,
                roles,
                permissions
            })
            .Build());
    }
}
