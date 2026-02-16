using Marten;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Shared.Events;

namespace MESNET.Institution.Application.Handlers;

public static class DeactivateBranchHandler
{
    public static async Task<BranchDeactivated> Handle(DeactivateBranch command, IDocumentSession session)
    {
        var institution = await session.LoadAsync<Core.Entities.Institution>(command.InstitutionId)
            ?? throw new InvalidOperationException($"Institution {command.InstitutionId} not found.");

        var branch = institution.Branches.FirstOrDefault(b => b.FieldCode == command.FieldCode && b.IsActive)
            ?? throw new InvalidOperationException($"Active branch {command.FieldCode} not found.");

        branch.IsActive = false;
        session.Store(institution);

        return new BranchDeactivated(institution.Id, command.FieldCode);
    }
}
