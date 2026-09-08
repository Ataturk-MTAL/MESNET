using Marten;
using MESNET.Enrollment.Application.Commands;
using MESNET.Enrollment.Application.Dtos;
using MESNET.Enrollment.Application.Extensions;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Core.Policies;
using MESNET.Enrollment.Shared.Events;

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
    /// </summary>
    public static async Task<(TeacherProfileDto, TeacherRegistered?)> Handle(
        RegisterTeacher command,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        // Doğal anahtarı olmayan istek (KeycloakUserId boş) eşleştirilemez — politika null döner
        // ve aşağıdaki yol yeni kayıt açar. Sorgu yine de yalnız anahtar varken anlamlıdır.
        if (command.KeycloakUserId != Guid.Empty)
        {
            var candidates = await session.Query<TeacherProfile>()
                .Where(t => t.KeycloakUserId == command.KeycloakUserId
                            && t.InstitutionId == command.InstitutionId)
                .ToListAsync(cancellationToken);

            var existing = TeacherRegistrationPolicy.FindExisting(
                candidates, command.InstitutionId, command.KeycloakUserId);

            if (existing is not null) return (existing.ToDto(), null);
        }

        var teacher = new TeacherProfile
        {
            Id = Guid.NewGuid(),
            InstitutionId = command.InstitutionId,
            KeycloakUserId = command.KeycloakUserId,
            FullName = command.FullName,
            BranchCode = command.BranchCode
        };

        session.Store(teacher);

        return (teacher.ToDto(), new TeacherRegistered(teacher.Id, teacher.FullName, teacher.InstitutionId));
    }
}
