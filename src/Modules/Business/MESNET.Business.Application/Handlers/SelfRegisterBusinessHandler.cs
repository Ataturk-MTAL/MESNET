using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Core.Enums;
using MESNET.Business.Core.ValueObjects;
using MESNET.Business.Shared.Events;

namespace MESNET.Business.Application.Handlers;

public static class SelfRegisterBusinessHandler
{
    public static object[] Handle(SelfRegisterBusiness command, IDocumentSession session)
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

        return
        [
            new BusinessRegistered(business.Id, business.Name, business.Location, business.Source),
            new BusinessApprovalRequested(business.Id, business.Name)
        ];
    }
}
