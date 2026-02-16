using Marten;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Core.ValueObjects;
using MESNET.Institution.Shared.Events;

namespace MESNET.Institution.Application.Handlers;

public static class AuthorizeStaffHandler
{
    public static async Task<StaffAuthorized> Handle(AuthorizeStaff command, IDocumentSession session)
    {
        var institution = await session.LoadAsync<Core.Entities.Institution>(command.InstitutionId)
            ?? throw new InvalidOperationException($"Institution {command.InstitutionId} not found.");

        var staff = new StaffMember
        {
            KeycloakId = command.KeycloakId,
            FullName = command.FullName,
            Role = command.Role,
            BranchCode = command.BranchCode
        };

        institution.Staff.Add(staff);
        session.Store(institution);

        return new StaffAuthorized(institution.Id, staff.Id, staff.Role, staff.BranchCode);
    }
}
