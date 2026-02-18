using FluentValidation;
using MESNET.Institution.Application.Commands;

namespace MESNET.Institution.Application.Validators;

public class UpdateScheduleConfigurationValidator : AbstractValidator<UpdateScheduleConfiguration>
{
    public UpdateScheduleConfigurationValidator()
    {
        RuleFor(x => x.InstitutionId).NotEmpty().WithMessage("Kurum belirtilmelidir.");
        RuleFor(x => x.DailyPeriodCount).InclusiveBetween(1, 12)
            .WithMessage("Günlük ders sayısı 1-12 arasında olmalıdır.");
        RuleFor(x => x.UpdatedBy).NotEmpty().WithMessage("Güncelleyen kişi belirtilmelidir.");
    }
}
