using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Pagination;
using MESNET.Common.Shared.Security;
using MESNET.Security.Application.Commands;
using MESNET.Security.Application.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Security.Api;

public static class UserManagementEndpoints
{
    public static void MapUserManagementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/security/users").WithTags("UserManagement");

        group.MapPost("/", CreateUser).RequireAuthorization(Permissions.UserManagement.Create);
        group.MapGet("/", GetUsers).RequireAuthorization(Permissions.UserManagement.View);
        group.MapGet("/{userAccountId:guid}", GetUser).RequireAuthorization(Permissions.UserManagement.View);
        group.MapPut("/{userAccountId:guid}", UpdateUser).RequireAuthorization(Permissions.UserManagement.Update);
        group.MapPost("/{userAccountId:guid}/roles", ChangeRoles).RequireAuthorization(Permissions.UserManagement.RolesManage);
        group.MapPost("/{userAccountId:guid}/permissions", ChangePermissions).RequireAuthorization(Permissions.UserManagement.RolesManage);
        // Alan (branş) kapsamı bir YETKİ kapsamı kararıdır → roller/yetkiler ile aynı izin (#126)
        group.MapPost("/{userAccountId:guid}/branches", ChangeBranches).RequireAuthorization(Permissions.UserManagement.RolesManage);
        // Kurum (kiracı) bağı da yetki kapsamı kararıdır → aynı izin (ADR-0003 adım 2).
        // YENİ İZİN TANIMLANMADI: "hangi kuruma" sorusunu izin değil aktörün kendi kurum
        // kapsamı cevaplar (UserInstitutionScopePolicy). Ayrı bir izin, "user:*" wildcard'ı
        // üzerinden zaten aynı iki role düşerdi (ADR-0002) — erişimi hiç daraltmazdı.
        group.MapPost("/{userAccountId:guid}/institution", ChangeInstitution).RequireAuthorization(Permissions.UserManagement.RolesManage);
        // Veli–öğrenci bağı (#174) — kapsam kararıdır, kimlik güncellemesi değil; bu yüzden
        // "user:update" değil "user:roles:manage" ister (ChangeBranches ile aynı çizgi).
        group.MapPost("/{userAccountId:guid}/students", ChangeStudents).RequireAuthorization(Permissions.UserManagement.RolesManage);
        group.MapPost("/{userAccountId:guid}/toggle-status", ToggleStatus).RequireAuthorization(Permissions.UserManagement.Update);
        group.MapDelete("/{userAccountId:guid}", DeleteUser).RequireAuthorization(Permissions.UserManagement.Delete);
        group.MapPost("/sync", SyncUsers).RequireAuthorization(Permissions.UserManagement.Create);
        group.MapPost("/resync-display-names", ResyncDisplayNames).RequireAuthorization(Permissions.UserManagement.Create);

        // Keycloak'taki artık institution_id özniteliğini siler (ADR-0003 adım 3). Kiracı
        // anahtarını yazan uçla aynı yetki seviyesi: ikisi de kiracı kapsamına dokunur.
        group.MapPost("/purge-institution-attribute", PurgeInstitutionAttribute)
            .RequireAuthorization(Permissions.UserManagement.RolesManage);

        // Rol → atanabilir yetki domain kapsamı (yapılandırılabilir guardrail)
        var scopes = app.MapGroup("/api/security/permission-scopes").WithTags("PermissionScope");
        scopes.MapGet("/", GetScopes).RequireAuthorization(Permissions.UserManagement.RolesManage);
        scopes.MapPut("/", PutScopes).RequireAuthorization(Permissions.UserManagement.RolesManage);
    }

    private static async Task<IResult> GetScopes(IMessageBus bus)
    {
        var dto = await bus.InvokeAsync<PermissionScopeDto>(new GetPermissionScopes());
        return Results.Ok(ResponseBuilder.Success().AddData(dto).Build());
    }

    private static async Task<IResult> PutScopes(UpdatePermissionScopes command, IMessageBus bus)
    {
        await bus.InvokeAsync(command);
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Yetki kapsamları güncellendi.")
            .Build());
    }

    private static async Task<IResult> CreateUser(
        CreateUser command, ICurrentUserService currentUser, IMessageBus bus)
    {
        // Kapsam istekten ALINMAZ: aktörün kurumu claim'den, platform muafiyeti izinden gelir
        // (ADR-0003 adım 6). Gövdedeki InstitutionId yalnız HEDEFTİR, yetki değil.
        var userId = await bus.InvokeAsync<Guid>(command with
        {
            ActorInstitutionId = currentUser.GetCurrentUser()?.InstitutionId,
            ActorHasPlatformScope = currentUser.HasPermission(Permissions.Platform.TenantManage)
        });

        return Results.Created(
            $"/api/security/users/{userId}",
            ResponseBuilder.Success(201)
                .AddData(new { userId })
                .AddMessage("Kullanıcı oluşturuldu.")
                .Build());
    }

    private static async Task<IResult> GetUsers(
        Guid? institutionId, Guid? businessId, string? role, bool? isEnabled, bool? missingBranchOnly,
        int page = 1, int pageSize = 20, string? sortBy = null, bool descending = false, string? search = null,
        IMessageBus bus = default!)
    {
        var result = await bus.InvokeAsync<PagedResult<UserAccountDto>>(
            new GetUserAccounts(institutionId, businessId, role, isEnabled, missingBranchOnly)
            { Page = page, PageSize = pageSize, SortBy = sortBy, Descending = descending, Search = search });

        return Results.Ok(ResponseBuilder.Success()
            .AddData(result)
            .Build());
    }

    private static async Task<IResult> GetUser(
        Guid userAccountId, IMessageBus bus)
    {
        var user = await bus.InvokeAsync<UserAccountDto>(new GetUserAccount(userAccountId));

        return Results.Ok(ResponseBuilder.Success()
            .AddData(user)
            .Build());
    }

    private static async Task<IResult> UpdateUser(
        Guid userAccountId, UpdateUser command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { UserAccountId = userAccountId });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Kullanıcı güncellendi.")
            .Build());
    }

    private static async Task<IResult> ChangeRoles(
        Guid userAccountId, ChangeUserRoles command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { UserAccountId = userAccountId });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Kullanıcı rolleri güncellendi.")
            .Build());
    }

    private static async Task<IResult> ChangePermissions(
        Guid userAccountId, ChangeUserPermissions command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { UserAccountId = userAccountId });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Kullanıcı yetkileri güncellendi.")
            .Build());
    }

    private static async Task<IResult> ChangeBranches(
        Guid userAccountId, ChangeUserBranches command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { UserAccountId = userAccountId });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Kullanıcının alanları güncellendi.")
            .Build());
    }

    /// <summary>
    /// Kurum (kiracı) bağını yazar. <b>Aktörün kurum kapsamı token claim'inden okunur</b>,
    /// istekten ALINMAZ — aksi hâlde gönderen taraf kendi kapsamını beyan edip kontrolü
    /// anlamsız kılardı (ADR-0003 adım 2).
    /// </summary>
    private static async Task<IResult> ChangeInstitution(
        Guid userAccountId, ChangeUserInstitution command,
        ICurrentUserService currentUser, IMessageBus bus)
    {
        await bus.InvokeAsync(command with
        {
            UserAccountId = userAccountId,
            ActorInstitutionId = currentUser.GetCurrentUser()?.InstitutionId
        });

        var message = command.InstitutionId is null
            ? "Kullanıcının kurum bağı çözüldü."
            : "Kullanıcının kurum bağı güncellendi.";

        return Results.Ok(ResponseBuilder.Success().AddMessage(message).Build());
    }

    private static async Task<IResult> ChangeStudents(
        Guid userAccountId, ChangeUserStudents command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { UserAccountId = userAccountId });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Velinin bağlı olduğu öğrenciler güncellendi.")
            .Build());
    }

    private static async Task<IResult> ToggleStatus(
        Guid userAccountId, ToggleUserStatus command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { UserAccountId = userAccountId });

        var action = command.Enable ? "aktif" : "pasif";
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage($"Kullanıcı {action} yapıldı.")
            .Build());
    }

    private static async Task<IResult> DeleteUser(
        Guid userAccountId, IMessageBus bus)
    {
        await bus.InvokeAsync(new DeleteUser(userAccountId));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Kullanıcı silindi.")
            .Build());
    }

    /// <summary>
    /// Mevcut hesapların adlarını modüllerin <c>UserNameView</c> read-model'lerine yeniden
    /// yayınlar (#137). Dağıtımdan sonra bir kez çalıştırılmalıdır: bu değişiklikten önce
    /// var olan kullanıcılar için hiç ad olayı yayınlanmadığından denetim satırları adsız kalır.
    /// Tekrar çalıştırmak güvenlidir.
    /// </summary>
    /// <summary>
    /// Keycloak'taki artık <c>institution_id</c> özniteliğini siler. Idempotenttir; ikinci
    /// koşuda <c>purged = 0</c> döner.
    /// </summary>
    private static async Task<IResult> PurgeInstitutionAttribute(IMessageBus bus)
    {
        var result = await bus.InvokeAsync<PurgeInstitutionAttributeResult>(
            new PurgeKeycloakInstitutionAttribute());

        // Başarısızlık sayısı mesaja KOYULUR: sıfırdan farklıysa o kullanıcılarda artık duruyor
        // demektir ve sessiz kalması, temizliği yapılmış sanmaya yol açar.
        return Results.Ok(ResponseBuilder.Success()
            .AddData(result)
            .AddMessage(
                $"{result.Total} Keycloak kullanıcısı tarandı: {result.Purged} özniteliği silindi, "
                + $"{result.Skipped} zaten temizdi, {result.Failed} başarısız.")
            .Build());
    }

    private static async Task<IResult> ResyncDisplayNames(IMessageBus bus)
    {
        var result = await bus.InvokeAsync<ResyncUserDisplayNamesResult>(new ResyncUserDisplayNames());

        return Results.Ok(ResponseBuilder.Success()
            .AddData(result)
            .AddMessage($"{result.Published} kullanıcının adı yeniden yayınlandı ({result.Skipped} atlandı).")
            .Build());
    }

    private static async Task<IResult> SyncUsers(IMessageBus bus)
    {
        var result = await bus.InvokeAsync<SyncUsersResult>(new SyncUsersFromKeycloak());

        // Kapsamsız hesap sayısı mesaja BİLEREK eklenir (ADR-0003 adım 2): sync artık kiracı
        // anahtarını Keycloak'tan kopyalamaz, o yüzden "senkronize edildi" tek başına işin
        // bittiği anlamına gelmez.
        var scopeNote = result.WithoutInstitution > 0
            ? $" {result.WithoutInstitution} hesabın kurum bağı yok — kurum ataması gerekiyor."
            : string.Empty;

        return Results.Ok(ResponseBuilder.Success()
            .AddData(result)
            .AddMessage($"{result.Total} kullanıcı senkronize edildi ({result.Created} yeni, {result.Updated} güncellendi).{scopeNote}")
            .Build());
    }
}
