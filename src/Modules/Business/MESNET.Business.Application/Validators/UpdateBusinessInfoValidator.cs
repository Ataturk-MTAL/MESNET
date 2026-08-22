using MESNET.Business.Core.Services;
using FluentValidation;
using MESNET.Business.Application.Commands;

namespace MESNET.Business.Application.Validators;

public class UpdateBusinessInfoValidator : AbstractValidator<UpdateBusinessInfo>
{
    public UpdateBusinessInfoValidator()
    {
        RuleFor(x => x.TaxNumber)
            .Must(TaxNumberPolicy.IsValid)
            .When(x => x.TaxNumber is not null)
            .WithMessage(TaxNumberPolicy.FormatMessage);
        // KISMİ güncelleme (PATCH): komuttaki tüm alanlar nullable ve handler yalnız null
        // OLMAYAN alanları yazıyor. Bu yüzden Name/Address koşulsuz zorunlu tutulamaz —
        // sadece sektör güncelleyen bir çağrı 422 alıyordu (#80). null = "dokunma",
        // boş string = geçersiz değer; ikisi ayrı.
        RuleFor(x => x.BusinessId).NotEmpty().WithMessage("İşletme belirtilmelidir.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("İşletme adı boş olamaz.")
            .MaximumLength(200).WithMessage("İşletme adı en fazla 200 karakter olmalıdır.")
            .When(x => x.Name is not null);
        RuleFor(x => x.Address).NotEmpty().WithMessage("Adres boş olamaz.")
            .When(x => x.Address is not null);
        RuleFor(x => x.PersonnelCount).GreaterThan(0).WithMessage("Personel sayısı sıfırdan büyük olmalıdır.");
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Geçerli bir e-posta adresi giriniz.");
    }
}
