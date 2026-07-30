using FluentValidation;
using MESNET.Business.Application.Commands;
using MESNET.Business.Core.Enums;

namespace MESNET.Business.Application.Validators;

public class RegisterBusinessValidator : AbstractValidator<RegisterBusiness>
{
    public RegisterBusinessValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("İşletme adı belirtilmelidir.")
            .MaximumLength(200).WithMessage("İşletme adı en fazla 200 karakter olmalıdır.");
        RuleFor(x => x.Address).NotEmpty().WithMessage("Adres belirtilmelidir.");
        RuleFor(x => x.PersonnelCount).GreaterThan(0).WithMessage("Personel sayısı sıfırdan büyük olmalıdır.");
        RuleFor(x => x.TotalSlots).GreaterThan(0).WithMessage("Toplam kontenjan sıfırdan büyük olmalıdır.");
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Geçerli bir e-posta adresi giriniz.");

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
