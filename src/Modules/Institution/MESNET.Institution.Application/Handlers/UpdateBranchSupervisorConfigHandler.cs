using Marten;
using MESNET.Common.Shared;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Application.Errors;
using MESNET.Institution.Shared.Events;

namespace MESNET.Institution.Application.Handlers;

public static class UpdateBranchSupervisorConfigHandler
{
    public static async Task<BranchSupervisorConfigUpdated> Handle(
        UpdateBranchSupervisorConfig command, IDocumentSession session)
    {
        var institution = await session.LoadAsync<Core.Entities.Institution>(command.InstitutionId);
        if (institution is null)
            throw new DomainException(InstitutionErrors.NotFound(command.InstitutionId));

        var branch = institution.Branches.FirstOrDefault(b => b.FieldCode == command.FieldCode && b.IsActive);
        if (branch is null)
            throw new DomainException(InstitutionErrors.BranchNotFound(command.FieldCode));

        if (command.DepartmentHeadCount < 0 || command.WorkshopHeadCount < 0)
            throw new DomainException(InstitutionErrors.InvalidSupervisorCount());

        var updated = branch with
        {
            DepartmentHeadCount = command.DepartmentHeadCount,
            WorkshopHeadCount = command.WorkshopHeadCount
        };

        var index = institution.Branches.IndexOf(branch);
        institution.Branches[index] = updated;

        session.Store(institution);

        return new BranchSupervisorConfigUpdated(
            institution.Id,
            command.FieldCode,
            command.DepartmentHeadCount,
            command.WorkshopHeadCount);
    }
}
