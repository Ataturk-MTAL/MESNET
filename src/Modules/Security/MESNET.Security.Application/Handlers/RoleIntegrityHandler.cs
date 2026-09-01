using Marten;
using MESNET.Common.Infrastructure.Security;
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
/// <param name="RealmScanPermitted">
/// Aktör realm bacağını görmeye yetkili mi (#283).
///
/// <para><b>Neden <see cref="KeycloakChecked"/>'den ayrı bir alan:</b> ikisi de "bu bacak
/// taranmadı" der ama <b>nedenleri farklıdır</b> ve kullanıcının yapacağı şey de farklıdır —
/// biri "sunucu erişilemedi, tekrar dene", diğeri "bu kısım senin kapsamında değil".
/// Tek bayrakta birleştirilseydi ekran yetki sınırını geçici bir arıza gibi gösterirdi ve
/// kimse yetki istemezdi.</para>
/// </param>
public sealed record RoleIntegrityReport(
    IReadOnlyList<string> KnownRoles,
    IReadOnlyList<InvalidRoleInvitationDto> InvitationsWithUnknownRole,
    IReadOnlyList<InvalidRoleAccountDto> AccountsWithUnknownRole,
    bool KeycloakChecked,
    IReadOnlyList<RolelessAccountDto> AccountsWithoutRealmRole,
    string? KeycloakCheckError,
    bool RealmScanPermitted)
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
///
/// <para><b>Kapsam KURUM düzeyindedir (#283).</b> Yerel iki bacak (davetler ve hesaplar)
/// <c>UserScopeResolver</c>'dan geçer — kullanıcı ve davet listeleriyle aynı kapı.
/// <c>UserAccount</c>/<c>UserInvitation</c> kimlik katmanındadır ve conjoined kiracılık onları
/// SÜZMEZ; süzülmeseydi <c>user:roles:manage</c> taşıyan her müdür bütün okulların e-posta, ad
/// ve kullanıcı adı bilgisini görürdü — kullanıcı listeleri daraltıldıktan sonra aynı veri
/// <b>ikinci bir kapıdan</b> açık kalırdı.</para>
///
/// <para><b>Neden platform düzeyine çekilmedi:</b> raporu görmesi gereken kişi düzeltmeyi de
/// yapacak olandır, ve düzeltme ucu (<c>POST /api/security/users/{id}/roles</c>) kurum
/// kapsamlıdır. Rapor kurum üstü olsaydı gören ile düzeltebilen ayrılırdı: müdür kendi
/// okulundaki bozuk kaydı — düzeltebildiği tek kaydı — göremez olurdu.</para>
///
/// <para><b>Realm bacağı istisnadır ve ayrı izne bağlıdır.</b> Keycloak'ta kurum kavramı yoktur;
/// "hiç realm rolü olmayan hesap" sorgusu doğası gereği realm genelidir ve daraltılamaz. Bu
/// yüzden yalnız <c>platform:tenant:manage</c> taşıyan aktör için taranır. Yetkisi olmayanda
/// bacak <b>boş döner ve boş olduğu SÖYLENİR</b>
/// (<see cref="RoleIntegrityReport.RealmScanPermitted"/>) — sessiz boş liste "temiz" diye
/// okunurdu.</para>
/// </summary>
public static class GetRoleIntegrityReportHandler
{
    public static async Task<RoleIntegrityReport> Handle(
        GetRoleIntegrityReport query,
        IQuerySession session,
        UserScopeResolver scopeResolver,
        ICurrentUserService currentUser,
        IKeycloakAdminService keycloak,
        CancellationToken cancellationToken)
    {
        // KAPSAM — istekten HİÇ gelmez, aktörün claim'lerinden türer. null = süzgeç yok
        // (platform kapsamı); boş liste = yalnız kurum bağı OLMAYAN kayıtlar. İkisi zıt anlamlı.
        var visibleIds = await scopeResolver.ResolveAsync(cancellationToken);

        // Davetler: bekleyen/onaylanmış olanlar öncelikli — henüz Keycloak hesabına dönüşmemiş
        // ya da dönüşmek üzere olan bozuk kayıtlar en acil olanlardır. Tamamlanmış/iptal
        // edilmiş davetler de listelenir; geçmişin izini silmek tespitin işi değildir.
        IQueryable<UserInvitation> invitationQuery = session.Query<UserInvitation>();

        // Kurum bağı OLMAYAN davet görünür kalır — kapsamsız kayıt kimsenin listesinde
        // görünmezse hiç düzeltilemez. UserQueryHandler ile aynı yüklem.
        if (visibleIds is { } invitationIds)
            invitationQuery = invitationQuery.Where(
                i => i.InstitutionId == null || invitationIds.Contains(i.InstitutionId.Value));

        var invitations = await invitationQuery.ToListAsync(cancellationToken);
        var badInvitations = invitations
            .Where(i => !MesnetRoles.IsValid(i.TargetRole))
            .OrderBy(i => i.StatusName)
            .ThenByDescending(i => i.CreatedAt)
            .Select(i => new InvalidRoleInvitationDto(
                i.Id, i.Email, i.FullName, i.TargetRole, i.StatusName, SuggestRole(i.TargetRole)))
            .ToList();

        // Silinmiş hesabın rolü tutarsızsa da bildirilmez — düzeltilecek bir şey yok (#210).
        IQueryable<UserAccount> accountQuery = session.Query<UserAccount>().Where(u => u.DeletedAt == null);

        if (visibleIds is { } accountIds)
            accountQuery = accountQuery.Where(
                u => u.InstitutionId == null || accountIds.Contains(u.InstitutionId.Value));

        var accounts = await accountQuery.ToListAsync(cancellationToken);
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

        // Realm bacağı KURUM ÜSTÜDÜR ve daraltılamaz: Keycloak'ta kurum kavramı yok. Yetkisi
        // olmayana hiç sorulmaz — sorulup sonra süzmek, süzmeyi unutan bir sonraki düzenlemeye
        // açık kapı bırakırdı.
        if (!currentUser.HasPermission(Permissions.Platform.TenantManage))
            return new RoleIntegrityReport(
                MesnetRoles.All, badInvitations, badAccounts,
                KeycloakChecked: false, [], KeycloakCheckError: null, RealmScanPermitted: false);

        // Keycloak'ta sıfır realm rolü olan hesaplar — sessiz bozulmanın en net belirtisi.
        // Kimlik sunucusuna ulaşılamazsa tarama "temiz" sayılmaz, açıkça eksik işaretlenir.
        var kcResult = await keycloak.GetUsersAsync();
        if (kcResult.IsFailure)
            return new RoleIntegrityReport(
                MesnetRoles.All, badInvitations, badAccounts,
                KeycloakChecked: false, [], kcResult.Error.Description, RealmScanPermitted: true);

        var roleless = kcResult.Value
            .Where(u => u.Roles.Count == 0)
            .OrderBy(u => u.Username)
            .Select(u => new RolelessAccountDto(u.Id, u.Username, u.Email))
            .ToList();

        return new RoleIntegrityReport(
            MesnetRoles.All, badInvitations, badAccounts,
            KeycloakChecked: true, roleless, null, RealmScanPermitted: true);
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
