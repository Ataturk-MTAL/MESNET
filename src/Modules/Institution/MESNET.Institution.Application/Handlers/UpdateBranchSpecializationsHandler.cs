using Marten;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Shared.Events;

namespace MESNET.Institution.Application.Handlers;

public static class UpdateBranchSpecializationsHandler
{
    public static async Task<BranchSpecializationsUpdated> Handle(
        UpdateBranchSpecializations command, IDocumentSession session)
    {
        var institution = await session.LoadAsync<Core.Entities.Institution>(command.InstitutionId)
            ?? throw new InvalidOperationException($"Institution {command.InstitutionId} not found.");

        var branch = institution.Branches.FirstOrDefault(b => b.FieldCode == command.FieldCode && b.IsActive)
            ?? throw new InvalidOperationException($"Active branch {command.FieldCode} not found.");

        // InstitutionBranch is a record — replace it with updated specializations
        var updated = branch with { ActiveSpecializations = command.ActiveSpecializations };
        var index = institution.Branches.IndexOf(branch);
        institution.Branches[index] = updated;

        session.Store(institution);

        return new BranchSpecializationsUpdated(institution.Id, command.FieldCode, command.ActiveSpecializations);
    }
}
