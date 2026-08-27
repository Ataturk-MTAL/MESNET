using Marten;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Application.Errors;
using MESNET.Institution.Core.Enums;
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

        // Yol üst düğümden türetilir. Üst yoksa yalnız İL düğümü kök olarak yol alır; okul ve
        // ilçe yolsuz doğar ve geçiş ucu doldurur (bugünkü kayıtlarla aynı durum).
        string? path = null;

        if (command.ParentId is { } parentId)
        {
            var parent = await session
                .LoadAsync<Core.Entities.Institution>(parentId, cancellationToken)
                ?? throw new DomainException(InstitutionErrors.ParentNotFound(parentId));

            // Yolsuz bir üstün altına düğüm eklenirse çocuğun yolu da kurulamaz ve İKİSİ de
            // hiçbir kapsamda görünmez — hata değil, sessiz boşluk. Bu yüzden reddedilir.
            if (string.IsNullOrWhiteSpace(parent.Path))
                throw new DomainException(InstitutionErrors.ParentHasNoPath(parentId));

            path = InstitutionPath.Child(parent.Path, id);
        }
        else if (nodeType == InstitutionNodeType.Province)
        {
            path = InstitutionPath.Root(id);
        }

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
