using Marten;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Tenancy;
using MESNET.Enrollment.Application.Commands;
using MESNET.Enrollment.Application.Dtos;
using MESNET.Enrollment.Application.Errors;
using MESNET.Enrollment.Application.Extensions;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Core.Policies;
using MESNET.Enrollment.Shared.Events;
using Wolverine;

namespace MESNET.Enrollment.Application.Handlers;

public static class RegisterTeacherHandler
{
    /// <summary>
    /// Öğretmen kaydı — <b>idempotent</b> (#310).
    ///
    /// <para>Doğal anahtar <c>(InstitutionId, KeycloakUserId)</c> zaten varsa yeni profil
    /// AÇILMAZ, mevcut olan döner. Eskiden koşulsuz <c>Guid.NewGuid()</c> vardı ve seeder'ın
    /// her koşusu yeni bir kopya doğuruyordu; ölçüldü: 6 öğretmenin 2'şer kopyası.</para>
    ///
    /// <para><b>Tekrar çağrıda olay yayınlanmaz.</b> Dönüş <c>null</c>'dır: hiçbir şey
    /// değişmediyse tüketicileri yeniden tetiklemek yanlış olur.</para>
    ///
    /// <para><b>Kurum kiracıdan türetilir (#309).</b> Satırın kiracı damgası zaten
    /// <see cref="Envelope.TenantId"/>'den gelir; <c>InstitutionId</c> alanı da oradan
    /// gelince ikisi ayrışamaz. Okul olmayan kiracıda (platform, kapsamsız aktör) kayıt
    /// reddedilir — kurum uydurulmaz.</para>
    /// </summary>
    public static async Task<(TeacherProfileDto, TeacherRegistered?)> Handle(
        RegisterTeacher command,
        IDocumentSession session,
        Envelope envelope,
        CancellationToken cancellationToken)
    {
        var institutionId = TenantResolution.InstitutionOf(envelope.TenantId)
            ?? throw new DomainException(EnrollmentErrors.SchoolScopeMissing);

        // Doğal anahtarı olmayan istek (KeycloakUserId boş) eşleştirilemez — politika null döner
        // ve aşağıdaki yol yeni kayıt açar. Sorgu yine de yalnız anahtar varken anlamlıdır.
        if (command.KeycloakUserId != Guid.Empty)
        {
            var candidates = await session.Query<TeacherProfile>()
                .Where(t => t.KeycloakUserId == command.KeycloakUserId
                            && t.InstitutionId == institutionId)
                .ToListAsync(cancellationToken);

            var existing = TeacherRegistrationPolicy.FindExisting(
                candidates, institutionId, command.KeycloakUserId);

            if (existing is not null) return (existing.ToDto(), null);
        }

        var teacher = new TeacherProfile
        {
            Id = Guid.NewGuid(),
            InstitutionId = institutionId,
            KeycloakUserId = command.KeycloakUserId,
            FullName = command.FullName,
            BranchCode = command.BranchCode
        };

        session.Store(teacher);

        return (teacher.ToDto(), new TeacherRegistered(teacher.Id, teacher.FullName, teacher.InstitutionId));
    }
}
