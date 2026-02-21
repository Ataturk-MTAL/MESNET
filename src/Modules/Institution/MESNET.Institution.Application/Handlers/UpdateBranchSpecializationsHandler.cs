using Marten;
using MESNET.Common.Shared;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Application.Errors;
using MESNET.Institution.Shared.Events;

namespace MESNET.Institution.Application.Handlers;

public static class UpdateBranchSpecializationsHandler
{
    public static async Task<BranchSpecializationsUpdated> Handle(
        UpdateBranchSpecializations command, IDocumentSession session)
    {
        var institution = await session.LoadAsync<Core.Entities.Institution>(command.InstitutionId);
        if (institution is null)
            throw new DomainException(InstitutionErrors.NotFound(command.InstitutionId));

        var branch = institution.Branches.FirstOrDefault(b => b.FieldCode == command.FieldCode && b.IsActive);
        if (branch is null)
            throw new DomainException(InstitutionErrors.BranchNotFound(command.FieldCode));

        var updated = branch with { ActiveSpecializations = command.ActiveSpecializations };
        var index = institution.Branches.IndexOf(branch);
        institution.Branches[index] = updated;

        session.Store(institution);

        return new BranchSpecializationsUpdated(institution.Id, command.FieldCode, command.ActiveSpecializations);
    }
}
