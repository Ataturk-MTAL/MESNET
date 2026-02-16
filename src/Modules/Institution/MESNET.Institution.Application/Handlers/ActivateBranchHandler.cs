using Marten;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Core.Entities;
using MESNET.Institution.Core.ValueObjects;
using MESNET.Institution.Shared.Events;

namespace MESNET.Institution.Application.Handlers;

public static class ActivateBranchHandler
{
    public static async Task<BranchActivated> Handle(ActivateBranch command, IDocumentSession session)
    {
        var institution = await session.LoadAsync<Core.Entities.Institution>(command.InstitutionId)
            ?? throw new InvalidOperationException($"Institution {command.InstitutionId} not found.");

        if (institution.Branches.Any(b => b.FieldCode == command.FieldCode && b.IsActive))
            throw new InvalidOperationException($"Branch {command.FieldCode} is already active.");

        var field = await session.Query<FieldOfStudy>()
            .FirstOrDefaultAsync(f => f.Code == command.FieldCode)
            ?? throw new InvalidOperationException($"Field of study {command.FieldCode} not found in catalog.");

        var branch = new InstitutionBranch
        {
            FieldCode = field.Code,
            FieldName = field.Name,
            Type = field.Type
        };

        institution.Branches.Add(branch);
        session.Store(institution);

        return new BranchActivated(institution.Id, field.Code, field.Name, field.Type);
    }
}
