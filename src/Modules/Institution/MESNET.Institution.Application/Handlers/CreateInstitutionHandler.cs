using Marten;
using MESNET.Common.Shared;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Application.Errors;
using MESNET.Institution.Core.Enums;
using MESNET.Institution.Core.Services;
using MESNET.Institution.Shared.Events;
using Wolverine;

namespace MESNET.Institution.Application.Handlers;

public static class CreateInstitutionHandler
{
    public static async Task<Guid> Handle(
        CreateInstitution command, IDocumentSession session, IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var id = command.Id ?? Guid.NewGuid();
        var nodeType = InstitutionNodeType.Resolve(command.NodeType);

        // Üst düğüm varsa yüklenir; yol kararı InstitutionNodePlacement.Resolve'a (saf
        // fonksiyon) devredilir — veritabanı erişimi burada, karar orada.
        Core.Entities.Institution? parent = null;
        if (command.ParentId is { } parentId)
            parent = await session.LoadAsync<Core.Entities.Institution>(parentId, cancellationToken);

        var placement = InstitutionNodePlacement.Resolve(
            nodeType, id, command.ParentId, parentExists: parent is not null, parentPath: parent?.Path);

        var path = placement.Outcome switch
        {
            NodePlacementOutcome.Ok => placement.Path,
            NodePlacementOutcome.ParentMissing =>
                throw new DomainException(InstitutionErrors.ParentNotFound(command.ParentId!.Value)),
            NodePlacementOutcome.ParentHasNoPath =>
                throw new DomainException(InstitutionErrors.ParentHasNoPath(command.ParentId!.Value)),
            _ => throw new InvalidOperationException($"Tanınmayan yerleşim sonucu: {placement.Outcome}")
        };

        var institution = new Core.Entities.Institution
        {
            Id = id,
            InstitutionCode = command.InstitutionCode,
            FullName = command.FullName,
            Address = command.Address,
            PhoneNumber = command.PhoneNumber,
            Email = command.Email,
            WebUrl = command.WebUrl,
            Location = command.Location,
            ProvinceCode = command.ProvinceCode,
            DistrictName = command.DistrictName,
            ParentId = command.ParentId,
            NodeTypeName = nodeType.Name,
            Path = path
        };

        session.Store(institution);

        await bus.PublishAsync(new InstitutionUpdated(
            institution.Id, institution.FullName, institution.Location,
            institution.ScheduleConfig?.DailyPeriodCount ?? 0));

        return institution.Id;
    }
}
