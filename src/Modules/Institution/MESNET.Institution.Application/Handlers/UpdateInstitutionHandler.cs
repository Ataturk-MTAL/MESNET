using Marten;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Reference;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Application.Errors;
using MESNET.Institution.Shared.Events;

namespace MESNET.Institution.Application.Handlers;

public static class UpdateInstitutionHandler
{
    public static async Task<InstitutionUpdated> Handle(UpdateInstitution command, IDocumentSession session)
    {
        var institution = await session.LoadAsync<Core.Entities.Institution>(command.InstitutionId);
        if (institution is null)
            throw new DomainException(InstitutionErrors.NotFound(command.InstitutionId));

        if (command.FullName is not null) institution.FullName = command.FullName;
        if (command.Address is not null) institution.Address = command.Address;
        if (command.PhoneNumber is not null) institution.PhoneNumber = command.PhoneNumber;
        if (command.Email is not null) institution.Email = command.Email;
        if (command.WebUrl is not null) institution.WebUrl = command.WebUrl;
        if (command.Location is not null) institution.Location = command.Location;
        if (command.ProvinceCode is not null) institution.ProvinceCode = command.ProvinceCode;
        if (command.DistrictName is not null) institution.DistrictName = command.DistrictName;
        if (command.InstitutionCode is not null) institution.InstitutionCode = command.InstitutionCode.Value;

        // İl ve ilçe ayrı ayrı güncellenebildiği için nihai KOMBİNASYON burada doğrulanır.
        // Validator yalnız ikisi birlikte gönderildiğinde çapraz kontrol edebiliyor; ili
        // değiştirip ilçeyi bırakmak (ya da tersi) kuruma başka ilin ilçesini yapıştırırdı.
        if (institution.DistrictName is not null
            && !TurkishDistricts.IsValid(institution.ProvinceCode, institution.DistrictName))
        {
            throw new DomainException(InstitutionErrors.DistrictNotInProvince(
                institution.DistrictName, institution.ProvinceCode));
        }

        session.Store(institution);

        return new InstitutionUpdated(
            institution.Id, institution.FullName, institution.Location,
            institution.ScheduleConfig?.DailyPeriodCount ?? 0);
    }
}
