using Marten;
using MESNET.Business.Application.Errors;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using MESNET.Business.Application.Commands;
using MESNET.Business.Application.Dtos;
using MESNET.Business.Application.Extensions;
using MESNET.Business.Core.Enums;
using MESNET.Business.Core.ValueObjects;
using MESNET.Business.Shared.Events;
using Wolverine;

namespace MESNET.Business.Application.Handlers;

public static class RegisterBusinessHandler
{
    public static async Task<BusinessDto> Handle(
        RegisterBusiness command, IDocumentSession session, IMessageBus bus,
        ICurrentUserService currentUser)
    {
        // Kurum kapsamı token'dan okunur, istekten ALINMAZ (#147). Eskiden komutun
        // TenantId alanından geliyordu; WebUI o alanı hiç göndermediği için kayıt
        // 422 ile reddediliyordu ve gönderen bir istemci başka okulun adına kaydedebilirdi.
        var institutionId = currentUser.GetCurrentUser()?.InstitutionId;
        if (institutionId is not { } scopedInstitutionId || scopedInstitutionId == Guid.Empty)
            throw new DomainException(BusinessErrors.InstitutionScopeMissing());

        var business = new Core.Entities.Business
        {
            Id = Guid.NewGuid(),
            InstitutionId = scopedInstitutionId,
            Name = command.Name,
            Address = command.Address,
            PhoneNumber = command.PhoneNumber,
            Email = command.Email,
            Website = command.Website,
            PersonnelCount = command.PersonnelCount,
            IsPublicInstitution = command.IsPublicInstitution,
            Location = command.Location,
            Source = RegistrationSource.InstitutionRegistered,
            Status = BusinessStatus.Active,
            Sectors = command.Sectors ?? [],
            Capacity = new BusinessCapacity
            {
                TotalSlots = command.TotalSlots
            },
            ApprovedAt = DateTime.UtcNow
        };

        session.Store(business);
        await session.SaveChangesAsync();

        await bus.PublishAsync(new BusinessRegistered(business.Id, business.InstitutionId, business.Name, business.Address, business.Location, business.Source.Name, business.Capacity.TotalSlots, business.Sectors,
            business.PhoneNumber, business.Email, business.MasterInstructor?.FullName, business.PersonnelCount,
            business.PrimaryRepresentativeName(), business.IsPublicInstitution));
        await bus.PublishAsync(new BusinessCapacityChanged(business.Id, business.Capacity.TotalSlots, business.Capacity.OccupiedSlots));

        return business.ToDto();
    }
}
