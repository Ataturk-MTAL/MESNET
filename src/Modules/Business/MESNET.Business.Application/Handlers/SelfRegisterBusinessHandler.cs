using MESNET.Business.Core.Services;
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

public static class SelfRegisterBusinessHandler
{
    public static async Task<BusinessDto> Handle(
        SelfRegisterBusiness command, IDocumentSession session, IMessageBus bus,
        ICurrentUserService currentUser)
    {
        // Kurum kapsamı token'dan okunur, istekten ALINMAZ (#147). Eskiden komutun
        // TenantId alanından geliyordu; WebUI o alanı hiç göndermediği için kayıt
        // 422 ile reddediliyordu ve gönderen bir istemci başka okulun adına kaydedebilirdi.
        var institutionId = currentUser.GetCurrentUser()?.InstitutionId;
        if (institutionId is not { } scopedInstitutionId || scopedInstitutionId == Guid.Empty)
            throw new DomainException(BusinessErrors.InstitutionScopeMissing());

        // Vergi kimliği paylaşımlı kataloğun doğal anahtarıdır (#150) — kendi kaydını yapan
        // işletme için de aynı kural geçerli.
        var taxNumber = TaxNumberPolicy.Normalize(command.TaxNumber);
        if (await session.Query<Core.Entities.Business>().AnyAsync(b => b.TaxNumber == taxNumber))
            throw new DomainException(BusinessErrors.TaxNumberAlreadyRegistered(taxNumber!));

        var business = new Core.Entities.Business
        {
            Id = Guid.NewGuid(),
            TaxNumber = taxNumber,
            // Provenance: kaydı GİREN okul. Kapsam alanı değildir (ADR-0003 adım 4).
            RegisteredByInstitutionId = scopedInstitutionId,
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

        await bus.PublishAsync(new BusinessRegistered(business.Id, business.RegisteredByInstitutionId, business.Name, business.Address, business.Location, business.Source.Name, business.Capacity.TotalSlots, business.Sectors,
            business.PhoneNumber, business.Email, business.MasterInstructor?.FullName, business.PersonnelCount,
            business.PrimaryRepresentativeName()));
        await bus.PublishAsync(new BusinessCapacityChanged(business.Id, business.Capacity.TotalSlots, business.Capacity.OccupiedSlots));
        await bus.PublishAsync(new BusinessApprovalRequested(business.Id, business.Name));

        return business.ToDto();
    }
}
