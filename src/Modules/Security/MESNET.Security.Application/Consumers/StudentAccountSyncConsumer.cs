using Marten;
using MESNET.Common.Infrastructure.Security;
using MESNET.Enrollment.Shared.Events;
using MESNET.Security.Core.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace MESNET.Security.Application.Consumers;

/// <summary>
/// Öğrenci kaydından <c>UserAccount.StudentId</c> otoritesini doldurur (#230).
///
/// <para><b>Neden gerekti:</b> <c>student_id</c> claim'i doğrudan token'dan okunuyordu ve
/// otoritesi olması gereken <c>UserAccount.StudentId</c> alanı <b>hiçbir yerde
/// yazılmıyordu</b> — canlıda 11 hesabın 0'ında doluydu. Yani alan vardı, otorite yoktu.</para>
///
/// <para><b>Neden tüketici:</b> bağ Enrollment'ta (<c>StudentProfile.KeycloakUserId</c>), claim
/// Security'de üretiliyor. Modüller arası doğrudan okuma yasak; olay tabanlı read-model tek
/// meşru yol (<c>StaffBranchSyncConsumer</c> ile aynı desen).</para>
///
/// <para><b>Uydurmaz.</b> Eşleşen hesap yoksa ya da olay Keycloak kimliği taşımıyorsa sessizce
/// atlanır: öğrencinin sistemde kullanıcı hesabı olmayabilir ve bu normaldir.</para>
/// </summary>
public static class StudentAccountSyncConsumer
{
    public static async Task Consume(
        StudentRegistered @event,
        IDocumentSession session,
        IMemoryCache cache,
        ILogger<StudentRegistered> logger,
        CancellationToken cancellationToken)
    {
        if (@event.KeycloakUserId == Guid.Empty)
            return;

        var keycloakId = @event.KeycloakUserId.ToString();

        var account = await session.Query<UserAccount>()
            .FirstOrDefaultAsync(u => u.KeycloakUserId == keycloakId && u.DeletedAt == null, cancellationToken);

        if (account is null)
            return;

        if (account.StudentId == @event.StudentId)
            return;

        account.StudentId = @event.StudentId;
        account.UpdatedAt = DateTime.UtcNow;
        session.Store(account);

        // Kapsam değişti — önbellek geçersiz kılınmazsa yeni bağ 5 dakika boyunca claim'e
        // yansımaz ve öğrenci kendi verisini göremez.
        PermissionClaimsTransformation.InvalidateCache(cache, account.KeycloakUserId);

        logger.LogInformation(
            "Öğrenci kapsamı kullanıcı kaydına yazıldı: {KeycloakUserId} → {StudentId}",
            keycloakId, @event.StudentId);
    }
}
