using FluentValidation;
using MESNET.Business.Application.Commands;
using MESNET.Business.Core.Enums;

namespace MESNET.Business.Application.Validators;

public class SelfRegisterBusinessValidator : AbstractValidator<SelfRegisterBusiness>
{
    public SelfRegisterBusinessValidator()
    {
        RuleFor(x => x.KeycloakId).NotEmpty().WithMessage("Kullanıcı kimliği belirtilmelidir.");
        RuleFor(x => x.FullName).NotEmpty().WithMessage("Temsilci adı soyadı belirtilmelidir.");
        RuleFor(x => x.RepresentativePhone).NotEmpty().WithMessage("Temsilci telefonu belirtilmelidir.");
        RuleFor(x => x.RepresentativeEmail).NotEmpty().WithMessage("Temsilci e-postası belirtilmelidir.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.");
        RuleFor(x => x.BusinessName).NotEmpty().WithMessage("İşletme adı belirtilmelidir.");
        RuleFor(x => x.Address).NotEmpty().WithMessage("Adres belirtilmelidir.");
        RuleFor(x => x.PersonnelCount).GreaterThan(0).WithMessage("Personel sayısı sıfırdan büyük olmalıdır.");
        RuleFor(x => x.TotalSlots).GreaterThan(0).WithMessage("Toplam kontenjan sıfırdan büyük olmalıdır.");

        When(x => x.Sectors is not null, () =>
        {
            RuleFor(x => x.Sectors)
                .Must(s => s!.Count == s.Distinct().Count())
                .WithMessage("Aynı sektör birden fazla kez seçilemez.");

            RuleForEach(x => x.Sectors)
                .Must(s => BusinessSector.TryFromName(s, true, out _))
                .WithMessage(s => $"Geçersiz sektör: {s}");
        });
    }
}
