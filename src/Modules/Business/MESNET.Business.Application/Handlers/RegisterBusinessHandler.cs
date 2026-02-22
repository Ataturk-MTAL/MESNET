using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Application.Dtos;
using MESNET.Business.Application.Extensions;
using MESNET.Business.Core.Enums;
using MESNET.Business.Core.ValueObjects;
using MESNET.Business.Shared.Events;
using MESNET.Enrollment.Core.ReadModels;
using Wolverine;

namespace MESNET.Business.Application.Handlers;

public static class RegisterBusinessHandler
{
    public static async Task<BusinessDto> Handle(
        RegisterBusiness command, IDocumentSession session, IMessageBus bus)
    {
        var business = new Core.Entities.Business
        {
            Id = Guid.NewGuid(),
            TenantId = command.TenantId,
            Name = command.Name,
            Address = command.Address,
            PhoneNumber = command.PhoneNumber,
            Email = command.Email,
            Website = command.Website,
            PersonnelCount = command.PersonnelCount,
            Location = command.Location,
            Source = RegistrationSource.InstitutionRegistered,
            Status = BusinessStatus.Active,
            Capacity = new BusinessCapacity
            {
                TotalSlots = command.TotalSlots
            },
            ApprovedAt = DateTime.UtcNow
        };

        // Enrollment modülünün PlaceStudent için ihtiyaç duyduğu view — aynı transaction'da garantili oluştur
        var profileView = new BusinessProfileView
        {
            Id = business.Id,
            BusinessName = business.Name,
            TotalSlots = business.Capacity.TotalSlots,
            OccupiedSlots = 0,
            Location = business.Location,
            IsActive = true,
            LastUpdated = DateTime.UtcNow
        };

        session.Store(business);
        session.Store(profileView);
        await session.SaveChangesAsync();

        await bus.PublishAsync(new BusinessRegistered(business.Id, business.Name, business.Location, business.Source, business.Capacity.TotalSlots));
        await bus.PublishAsync(new BusinessCapacityChanged(business.Id, business.Capacity.TotalSlots, business.Capacity.OccupiedSlots));

        return business.ToDto();
    }
}
