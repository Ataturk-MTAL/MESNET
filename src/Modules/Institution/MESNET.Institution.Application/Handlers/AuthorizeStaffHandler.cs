using Marten;
using MESNET.Common.Shared;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Application.Errors;
using MESNET.Institution.Core.ValueObjects;
using MESNET.Institution.Shared.Events;
using Wolverine;

namespace MESNET.Institution.Application.Handlers;

/// <summary>
/// Kurum personeli yetkilendirir.
///
/// <para><b>Mükerrer kaydı reddeder (#190).</b> Bu kontrol yokken her çağrı koşulsuz
/// <c>Staff.Add(...)</c> yapıyordu; canlı veride <b>5 kişi 41'er kez</b> (205 satır) birikmişti.
/// Çağıran tarafın kendi kontrolüne güvenilemez — seeder'ınki sessizce çalışmıyordu ve kimse
/// fark etmedi. Kontrol otoriter katmanda, yani burada olmalı.</para>
///
/// <para><b>Neden hata, sessiz geçiş ya da güncelleme değil:</b> aynı kişiyi ikinci kez
/// yetkilendirmek bir niyet değil bir yanlışlıktır. Sessizce geçmek hatayı yine görünmez
/// kılardı; güncelleme saymak ise rol değişikliğini kaza eseri yapılabilir hâle getirirdi.
/// Rol/alan değişimi ayrı ve açık bir işlem olmalıdır.</para>
/// </summary>
public static class AuthorizeStaffHandler
{
    public static async Task<Guid> Handle(AuthorizeStaff command, IDocumentSession session, IMessageBus bus)
    {
        var institution = await session.LoadAsync<Core.Entities.Institution>(command.InstitutionId);
        if (institution is null)
            throw new DomainException(InstitutionErrors.NotFound(command.InstitutionId));

        // UserCreatedConsumer'da bu kontrol zaten vardı; yalnız bu yolda yoktu (#190).
        if (institution.Staff.Any(s => s.KeycloakId == command.KeycloakId))
            throw new DomainException(InstitutionErrors.StaffAlreadyAuthorized(command.KeycloakId));

        var staff = new StaffMember
        {
            KeycloakId = command.KeycloakId,
            FullName = command.FullName,
            Role = command.Role,
            BranchCode = command.BranchCode
        };

        institution.Staff.Add(staff);
        session.Store(institution);

        // Modüller arası event'te SmartEnum yerine Name string'i gönderilir
        await bus.PublishAsync(new StaffAuthorized(
            institution.Id, staff.Id, staff.Role.Name, staff.BranchCode, staff.KeycloakId));

        return staff.Id;
    }
}
