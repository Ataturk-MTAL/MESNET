using Marten;
using MESNET.Common.Shared.Security;
using MESNET.Institution.Core.Entities;
using MESNET.Institution.Core.Enums;
using MESNET.Institution.Core.ValueObjects;
using MESNET.Security.Shared.Events;

namespace MESNET.Institution.Application.Consumers;

public static class UserCreatedConsumer
{
    public static async Task Consume(UserCreated @event, IDocumentSession session)
    {
        if (!@event.InstitutionId.HasValue) return;

        // Realm rolü → kurum kadro rolü (StaffRole). Sıra önemlidir: sonraki eşleşme öncekini
        // ezer, yani en yetkili rol kazanır.
        //
        // #129: müdür yardımcısı artık ayrı realm rolüdür (DeputyDirector). Öncesinde
        // InstitutionManager → VicePrincipal eşleniyordu; okul müdürünü müdür yardımcısı
        // kadrosuna yazan bu satır, #129'un düzelttiği "müdür/müdür yardımcısı aynı rol"
        // karışıklığının aynısıydı. DeputyDirector eklenmeseydi, o role sahip kullanıcı için
        // HİÇBİR kadro kaydı oluşmaz ve kişi kurum personeli listesinde görünmezdi.
        StaffRole? staffRole = null;

        if (@event.Roles.Contains(MesnetRoles.Teacher))
            staffRole = StaffRole.Coordinator; // Varsayılan — Metadata'dan override edilebilir
        if (@event.Roles.Contains(MesnetRoles.DepartmentHead))
            staffRole = StaffRole.DepartmentHead;
        if (@event.Roles.Contains(MesnetRoles.InstitutionStaff))
            staffRole = StaffRole.Staff;
        if (@event.Roles.Contains(MesnetRoles.DeputyDirector))
            staffRole = StaffRole.VicePrincipal;
        if (@event.Roles.Contains(MesnetRoles.InstitutionManager))
            staffRole = StaffRole.Principal;

        // Metadata'da StaffRole belirtilmişse onu kullan
        if (@event.Metadata.TryGetValue("StaffRole", out var staffRoleName)
            && StaffRole.TryFromName(staffRoleName, out var parsedRole))
        {
            staffRole = parsedRole;
        }

        if (staffRole is null) return;

        var institution = await session.LoadAsync<Core.Entities.Institution>(@event.InstitutionId.Value);
        if (institution is null) return;

        // Aynı KeycloakId ile zaten kayıtlı mı kontrol
        if (institution.Staff.Any(s => s.KeycloakId == @event.KeycloakUserId))
            return;

        var member = new StaffMember
        {
            KeycloakId = @event.KeycloakUserId,
            FullName = @event.FullName,
            Role = staffRole,
            BranchCode = @event.Metadata.GetValueOrDefault("BranchCode")
        };

        institution.Staff.Add(member);
        session.Store(institution);
    }
}
