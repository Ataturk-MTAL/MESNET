using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Application.Dtos;
using MESNET.Business.Application.Extensions;
using MESNET.Business.Core.Enums;
using MESNET.Business.Core.ValueObjects;
using MESNET.Business.Shared.Events;
using Wolverine;

namespace MESNET.Business.Application.Handlers;

public static class SelfRegisterBusinessHandler
{
    public static async Task<BusinessDto> Handle(
        SelfRegisterBusiness command, IDocumentSession session, IMessageBus bus)
    {
        var business = new Core.Entities.Business
        {
            Id = Guid.NewGuid(),
            TenantId = command.TenantId,
            Name = command.BusinessName,
            Address = command.Address,
            PhoneNumber = command.PhoneNumber,
            Email = command.Email,
            Website = command.Website,
            PersonnelCount = command.PersonnelCount,
            Location = command.Location,
            Source = RegistrationSource.SelfRegistered,
            Status = BusinessStatus.PendingApproval,
            Sectors = command.Sectors ?? [],
            Capacity = new BusinessCapacity
            {
                TotalSlots = command.TotalSlots
            },
            Representatives =
            [
                new BusinessRepresentative
                {
                    KeycloakId = command.KeycloakId,
                    FullName = command.FullName,
                    PhoneNumber = command.RepresentativePhone,
                    Email = command.RepresentativeEmail
                }
            ]
        };

        session.Store(business);
        await session.SaveChangesAsync();

        await bus.PublishAsync(new BusinessRegistered(business.Id, business.TenantId, business.Name, business.Address, business.Location, business.Source, business.Capacity.TotalSlots, business.Sectors,
            business.PhoneNumber, business.Email, business.MasterInstructor?.FullName));
        await bus.PublishAsync(new BusinessCapacityChanged(business.Id, business.Capacity.TotalSlots, business.Capacity.OccupiedSlots));
        await bus.PublishAsync(new BusinessApprovalRequested(business.Id, business.Name));

        return business.ToDto();
    }
}
