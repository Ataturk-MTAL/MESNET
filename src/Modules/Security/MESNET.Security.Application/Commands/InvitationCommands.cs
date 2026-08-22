using MESNET.Common.Shared.Pagination;

namespace MESNET.Security.Application.Commands;

/// <remarks>
/// İşlemi yapan kullanıcı komutta TAŞINMAZ (#137) — handler token'dan
/// (<c>ICurrentUserService.GetUserId()</c>) damgalar. Daveti kimin oluşturduğunu/onayladığını,
/// işlemi yapan istemcinin kendisi yazamaz.
/// </remarks>
public sealed record CreateInvitation(
    string Email,
    string FirstName,
    string LastName,
    string TargetRole,
    Guid? InstitutionId = null,
    Guid? BusinessId = null,
    Dictionary<string, string>? Metadata = null,
    /// <summary>
    /// Velinin bağlı olacağı öğrenciler (#271). Davet <b>kabul edildiğinde</b>
    /// <c>UserAccount.LinkedStudentIds</c>'e yazılır ve Keycloak özniteliği kurulur.
    ///
    /// <para><b>Neden davet anında:</b> veli–öğrenci bağını dolduran başka hiçbir otomatik yol
    /// yoktu — ne consumer, ne seeder, ne backfill. Tek yazma yolu elle
    /// <c>POST /api/security/users/{id}/students</c>'ti. Bağ kurulmadan md. 36 (4) tebligatı
    /// (#247) veliye <b>hiç ulaşmıyor</b>.</para>
    ///
    /// <para><b>Neden otomatik eşleştirme değil:</b> ortak anahtar yok. <c>UserAccount</c>'ta TC
    /// alanı bulunmuyor ve <c>StudentRegistered</c> veli bilgisi taşımıyor; ad eşleştirmesi
    /// güvenilmez. Kapsam anahtarını davete koymak, <c>InstitutionId</c> ve <c>BusinessId</c>
    /// ile <b>birebir aynı</b> yerleşik desendir.</para>
    ///
    /// <para><b>Yalnız veli rolünde anlamlıdır</b> — başka rolde verilirse doğrulama reddeder.</para>
    /// </summary>
    IReadOnlyList<Guid>? StudentIds = null);

public sealed record ApproveInvitation(Guid InvitationId);

public sealed record RejectInvitation(Guid InvitationId, string Reason);

public sealed record CompleteInvitation(Guid InvitationId, string Username, string Password);

public sealed record GetInvitations(
    Guid? InstitutionId = null,
    string? Status = null,
    string? TargetRole = null) : PagedQuery;

public sealed record ResendInvitation(Guid InvitationId);
