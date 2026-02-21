using Marten;
using MESNET.Common.Shared;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Application.Errors;
using MESNET.Institution.Core.Entities;
using MESNET.Institution.Core.ValueObjects;
using MESNET.Institution.Shared.Events;

namespace MESNET.Institution.Application.Handlers;

public static class ActivateBranchHandler
{
    public static async Task<BranchActivated> Handle(ActivateBranch command, IDocumentSession session)
    {
        var institution = await session.LoadAsync<Core.Entities.Institution>(command.InstitutionId);
        if (institution is null)
            throw new DomainException(InstitutionErrors.NotFound(command.InstitutionId));

        if (institution.Branches.Any(b => b.FieldCode == command.FieldCode && b.IsActive))
            throw new DomainException(InstitutionErrors.BranchAlreadyActive(command.FieldCode));

        var field = await session.Query<FieldOfStudy>()
            .FirstOrDefaultAsync(f => f.Code == command.FieldCode);
        if (field is null)
            throw new DomainException(InstitutionErrors.FieldOfStudyNotFound(command.FieldCode));

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
