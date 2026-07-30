using FluentValidation;
using MESNET.Payment.Application.Commands;

namespace MESNET.Payment.Application.Validators;

public class UpdateMinimumWageValidator : AbstractValidator<UpdateMinimumWage>
{
    public UpdateMinimumWageValidator()
    {
        RuleFor(x => x.InstitutionId).NotEmpty().WithMessage("Kurum belirtilmelidir.");
        RuleFor(x => x.NewMinimumWage).GreaterThan(0).WithMessage("Asgari ücret sıfırdan büyük olmalıdır.");

        // Alan gönderilmezse 0001-01-01 gelir ve yürürlük zinciri yılbaşından değil tarihin
        // başından açılır. Doğrulanmadığı sürece hata sessiz kalıyordu.
        RuleFor(x => x.EffectiveFrom).NotEmpty().WithMessage("Yürürlük tarihi belirtilmelidir.");

        // 16 yaş altı tutarı yaşa uygun (daha düşük) asgari ücrettir; genel tutarı aşarsa
        // küçük öğrenci büyükten fazla kazanır ve taban hesabı ters döner (#85).
        When(x => x.NewMinimumWageUnder16 is not null, () =>
        {
            RuleFor(x => x.NewMinimumWageUnder16!.Value)
                .GreaterThan(0).WithMessage("16 yaş altı asgari ücret sıfırdan büyük olmalıdır.");

            RuleFor(x => x.NewMinimumWageUnder16!.Value)
                .LessThanOrEqualTo(x => x.NewMinimumWage)
                .WithMessage("16 yaş altı asgari ücret, genel asgari ücretten yüksek olamaz.");
        });
    }
}
