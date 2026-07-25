using FluentValidation;
using MESNET.Contract.Application.Commands;

namespace MESNET.Contract.Application.Validators;

public class CreateContractValidator : AbstractValidator<CreateContract>
{
    public CreateContractValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty().WithMessage("Öğrenci belirtilmelidir.");
        RuleFor(x => x.BusinessId).NotEmpty().WithMessage("İşletme belirtilmelidir.");
        RuleFor(x => x.InstitutionId).NotEmpty().WithMessage("Kurum belirtilmelidir.");
        RuleFor(x => x.StartDate).NotEmpty().WithMessage("Başlangıç tarihi belirtilmelidir.");

        // Yasal alt sınır (3308 Madde 25) burada DOĞRULANAMAZ: taban, asgari ücret ve oranlardan
        // hesaplanıyor, ikisi de Payment modülünün SalaryCalculationConfig'inde. Modüller arası
        // doğrudan sorgu yasak olduğu için Contract yalnız "pozitif olmalı" der; alt sınırı
        // SalaryCalculator uyguluyor — sözleşme ücreti tabandan düşükse taban ödenir (#84).
        RuleFor(x => x.AgreedMonthlyWage)
            .GreaterThan(0).WithMessage("Anlaşılan aylık ücret sıfırdan büyük olmalıdır.")
            .When(x => x.AgreedMonthlyWage is not null);
    }
}
