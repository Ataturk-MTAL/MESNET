using System.Security.Claims;
using Wolverine;
using MESNET.Common.Infrastructure.Security;
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

    /// <summary>
    /// <paramref name="bus"/> yalnız <b>çözülmüş kiracıyı okumak</b> için alınır (#149).
    /// Kiracıyı istek başına <c>TenantResolutionMiddleware</c> koyar; buradan yansıtmak, hattın
    /// gerçekten çalıştığını dışarıdan görülebilir kılar. Aksi hâlde middleware sessizce
    /// çalışmasa da hiçbir şey fark etmezdi — kiracılık açılana kadar.
    /// </summary>
    private static IResult GetCurrentUser(ClaimsPrincipal user, IMessageBus bus)
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
        // active_institution_id aktörün ADINA DAVRANDIĞI kurumdur (B parçası) — institutionId
        // EV kurumudur ve bundan etkilenmez. İkisi yan yana döner; ön yüz hangisinin geçerli
        // olduğuna authStore.currentInstitutionId ile karar verir.
        var activeInstitutionId = user.FindFirst(
            PermissionClaimsTransformation.ActiveInstitutionClaimType)?.Value;
        var businessId = user.FindFirst("business_id")?.Value;
        // #230 — kapsam claim'lerinin üçü de burada görünür olmalı; studentId eksikti ve
        // eksikliği kapsamın doğru çözülüp çözülmediğini gözlenemez kılıyordu.
        var studentId = user.FindFirst("student_id")?.Value;

        // Alan (branş) kapsamı da claim'den okunur (#126): PermissionClaimsTransformation
        // token'da branch_codes yoksa personel kaydından doldurur. Rol adına bakılmaz —
        // kapsam kararı rolden değil claim'den gelir.
        var branchCodes = BranchCodeClaims.Read(user);

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
                activeInstitutionId,
                businessId,
                studentId,
                // Geriye uyumluluk: tek alan bekleyen istemciler için ilk kod.
                branchCode = branchCodes.Count > 0 ? branchCodes[0] : null,
                branchCodes,
                roles,
                permissions,
                // Kiracı = okul (ADR-0003). null ise kullanıcı kapsamsızdır.
                tenantId = bus.TenantId
            })
            .Build());
    }
}
