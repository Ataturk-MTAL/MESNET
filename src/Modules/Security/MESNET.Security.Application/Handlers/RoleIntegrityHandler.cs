using Marten;
using MESNET.Common.Shared.Security;
using MESNET.Security.Application.Commands;
using MESNET.Security.Application.Services;
using MESNET.Security.Core.Entities;

namespace MESNET.Security.Application.Handlers;

/// <param name="SuggestedRole">
/// Yalnız <b>öneri</b>. Ad normalize edilip (<c>deputy_director</c> → <c>deputydirector</c>)
/// bilinen rollerle karşılaştırılır. Eşleşme yoksa <c>null</c> — kod uydurmaz.
/// Bu öneri hiçbir yerde otomatik uygulanmaz.
/// </param>
public sealed record InvalidRoleInvitationDto(
    Guid InvitationId, string Email, string FullName, string TargetRole,
    string Status, string? SuggestedRole);

public sealed record InvalidRoleAccountDto(
    Guid UserAccountId, string Username, string FullName,
    List<string> Roles, List<string> UnknownRoles, List<string> SuggestedRoles);

/// <summary>Keycloak'ta hiç realm rolü olmayan hesap — bozulmanın en net belirtisi.</summary>
public sealed record RolelessAccountDto(string KeycloakUserId, string Username, string Email);

/// <param name="KeycloakChecked">
/// Keycloak taraması yapılabildi mi? Kimlik sunucusuna ulaşılamazsa lokal bulgular yine döner;
/// eksik tarama <b>"sorun yok"</b> gibi gösterilmez.
/// </param>
public sealed record RoleIntegrityReport(
    IReadOnlyList<string> KnownRoles,
    IReadOnlyList<InvalidRoleInvitationDto> InvitationsWithUnknownRole,
    IReadOnlyList<InvalidRoleAccountDto> AccountsWithUnknownRole,
    bool KeycloakChecked,
    IReadOnlyList<RolelessAccountDto> AccountsWithoutRealmRole,
    string? KeycloakCheckError)
{
    /// <summary>Toplam bulgu sayısı — arayüzde "temiz mi" rozetini sürer.</summary>
    public int TotalFindings =>
        InvitationsWithUnknownRole.Count + AccountsWithUnknownRole.Count + AccountsWithoutRealmRole.Count;
}

/// <summary>
/// Rol modeli tutarlılık taraması (#129). Üç bulgu türü:
/// (1) <c>UserInvitation.TargetRole</c> tanınmıyor, (2) <c>UserAccount.Roles</c> tanınmayan değer
/// taşıyor, (3) Keycloak hesabının hiç realm rolü yok.
///
/// <para><b>Salt okunur.</b> Hiçbir kayıt değiştirilmez; liste idareye gösterilir, düzeltmeyi
/// idare yapar (rol değişimi <c>POST /api/security/users/{id}/roles</c>, davet için yeni davet).</para>
/// </summary>
public static class GetRoleIntegrityReportHandler
{
    public static async Task<RoleIntegrityReport> Handle(
        GetRoleIntegrityReport query, IQuerySession session, IKeycloakAdminService keycloak)
    {
        // Davetler: bekleyen/onaylanmış olanlar öncelikli — henüz Keycloak hesabına dönüşmemiş
        // ya da dönüşmek üzere olan bozuk kayıtlar en acil olanlardır. Tamamlanmış/iptal
        // edilmiş davetler de listelenir; geçmişin izini silmek tespitin işi değildir.
        var invitations = await session.Query<UserInvitation>().ToListAsync();
        var badInvitations = invitations
            .Where(i => !MesnetRoles.IsValid(i.TargetRole))
            .OrderBy(i => i.StatusName)
            .ThenByDescending(i => i.CreatedAt)
            .Select(i => new InvalidRoleInvitationDto(
                i.Id, i.Email, i.FullName, i.TargetRole, i.StatusName, SuggestRole(i.TargetRole)))
            .ToList();

        // Silinmiş hesabın rolü tutarsızsa da bildirilmez — düzeltilecek bir şey yok (#210).
        var accounts = await session.Query<UserAccount>().Where(u => u.DeletedAt == null).ToListAsync();
        var badAccounts = accounts
            .Where(a => a.Roles.Any(r => !MesnetRoles.IsValid(r)))
            .OrderBy(a => a.FullName)
            .Select(a =>
            {
                var unknown = a.Roles.Where(r => !MesnetRoles.IsValid(r)).ToList();
                return new InvalidRoleAccountDto(
                    a.Id, a.Username, a.FullName, a.Roles, unknown,
                    [.. unknown.Select(SuggestRole).Where(s => s is not null).Cast<string>().Distinct()]);
            })
            .ToList();

        // Keycloak'ta sıfır realm rolü olan hesaplar — sessiz bozulmanın en net belirtisi.
        // Kimlik sunucusuna ulaşılamazsa tarama "temiz" sayılmaz, açıkça eksik işaretlenir.
        var kcResult = await keycloak.GetUsersAsync();
        if (kcResult.IsFailure)
            return new RoleIntegrityReport(
                MesnetRoles.All, badInvitations, badAccounts,
                KeycloakChecked: false, [], kcResult.Error.Description);

        var roleless = kcResult.Value
            .Where(u => u.Roles.Count == 0)
            .OrderBy(u => u.Username)
            .Select(u => new RolelessAccountDto(u.Id, u.Username, u.Email))
            .ToList();

        return new RoleIntegrityReport(
            MesnetRoles.All, badInvitations, badAccounts,
            KeycloakChecked: true, roleless, null);
    }

    /// <summary>
    /// Tanınmayan ad için <b>öneri</b> üretir: harf/rakam dışını atıp büyük/küçük harf duyarsız
    /// karşılaştırır (<c>deputy_director</c> → <c>DeputyDirector</c>). Eşleşme yoksa <c>null</c>
    /// (ör. <c>coordinator_teacher</c> — hangi role karşılık geldiği kodun bilebileceği bir şey
    /// değildir). Öneri hiçbir yerde otomatik uygulanmaz.
    /// </summary>
    internal static string? SuggestRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return null;

        var normalized = Normalize(role);
        return MesnetRoles.All.FirstOrDefault(known =>
            string.Equals(Normalize(known), normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static string Normalize(string value) =>
        new([.. value.Where(char.IsLetterOrDigit)]);
}
