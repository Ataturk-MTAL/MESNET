using Marten;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared.Security;
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
/// <para><b>Uydurmaz.</b> Eşleşen hesap yoksa ya da olay Keycloak kimliği taşımıyorsa atlanır:
/// öğrencinin sistemde kullanıcı hesabı olmayabilir ve bu normaldir. Atlama artık <b>sessiz
/// değil</b> — "eşleşme yok" ile "kontrol hiç çalışmadı" aynı görünüyordu (#236).</para>
///
/// <para><b>Ezmez (#236).</b> Karar <see cref="StudentAccountBindingPolicy"/>'dedir: dolu ve
/// farklı bir <c>StudentId</c> üzerine yazılmaz, hesabın kurumu olayın kurumuyla eşleşmezse
/// bağlanmaz. Bu kod <c>UserAccount</c>'a yazar ve o belge kimlik katmanındadır — kiracı damgası
/// taşımaz, yani arama okullar arası globaldir ve ne kiracılık ne kapsam guard'ı burayı korur.</para>
/// </summary>
public static class StudentAccountSyncConsumer
{
    /// <summary>Öğrenci kaydı olayı — canlı yol.</summary>
    public static Task Consume(StudentRegistered @event,
        IDocumentSession session,
        IMemoryCache cache,
        ILogger<StudentRegistered> logger,
        CancellationToken cancellationToken)
        => Apply(@event.ToSnapshot(), session, cache, logger, cancellationToken);

    /// <summary>
    /// Onarım yolu (#290): <c>POST /api/students/resync-projections</c> bu olayı yayınlar.
    /// <c>StudentRegistered</c> yeniden yayınlanamaz — tüketicilerinden biri şube sayacını
    /// ARTIRIYOR ve her yeniden yayın sayacı şişirirdi.
    /// </summary>
    public static Task Consume(StudentSnapshotResynced @event,
        IDocumentSession session,
        IMemoryCache cache,
        ILogger<StudentRegistered> logger,
        CancellationToken cancellationToken)
        => Apply(@event, session, cache, logger, cancellationToken);

    private static async Task Apply(
        StudentSnapshotResynced @event,
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
        {
            // Öğrencinin hesabı olmaması normaldir; ama "eşleşme yok" ile "bu kod hiç koşmadı"
            // bugüne kadar aynı görünüyordu — kapsam dolmadığında bakılacak bir iz kalsın (#236).
            logger.LogDebug(
                "Öğrenci kapsamı yazılamadı, eşleşen kullanıcı hesabı yok: {KeycloakUserId}",
                keycloakId);
            return;
        }

        var decision = StudentAccountBindingPolicy.Decide(
            @event.StudentId, account.StudentId, @event.InstitutionId, account.InstitutionId);

        if (!decision.ShouldBind)
        {
            // Reddedilen bağ GÖRÜNÜR olmalı: çakışma sessiz kalırsa hem yanlış bağ hem de
            // gerçek sahibin kopan bağı fark edilmez. "İş yok" durumu ise gürültü üretmemeli.
            if (decision.IsWarning)
                logger.LogWarning(
                    "Öğrenci kapsamı bağlanmadı: {KeycloakUserId} → {StudentId}. {Reason} "
                    + "(hesaptaki öğrenci: {ExistingStudentId}, hesabın kurumu: {AccountInstitutionId}, "
                    + "olayın kurumu: {EventInstitutionId})",
                    keycloakId, @event.StudentId, decision.Reason,
                    account.StudentId, account.InstitutionId, @event.InstitutionId);
            else
                logger.LogDebug(
                    "Öğrenci kapsamı değişmedi: {KeycloakUserId}. {Reason}", keycloakId, decision.Reason);

            return;
        }

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
