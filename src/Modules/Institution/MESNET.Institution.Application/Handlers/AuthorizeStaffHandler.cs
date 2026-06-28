using Marten;
using MESNET.Common.Shared;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Application.Errors;
using MESNET.Institution.Core.ValueObjects;
using MESNET.Institution.Shared.Events;
using Wolverine;

namespace MESNET.Institution.Application.Handlers;

public static class AuthorizeStaffHandler
{
    public static async Task<Guid> Handle(AuthorizeStaff command, IDocumentSession session, IMessageBus bus)
    {
        var institution = await session.LoadAsync<Core.Entities.Institution>(command.InstitutionId);
        if (institution is null)
            throw new DomainException(InstitutionErrors.NotFound(command.InstitutionId));

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
        await bus.PublishAsync(new StaffAuthorized(institution.Id, staff.Id, staff.Role.Name, staff.BranchCode));

        return staff.Id;
    }
}
