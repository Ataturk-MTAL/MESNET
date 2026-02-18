using FluentValidation;
using MESNET.Payment.Application.Commands;

namespace MESNET.Payment.Application.Validators;

public class UploadReceiptByBusinessValidator : AbstractValidator<UploadReceiptByBusiness>
{
    public UploadReceiptByBusinessValidator()
    {
        RuleFor(x => x.SalaryPeriodId).NotEmpty().WithMessage("Maaş dönemi belirtilmelidir.");
        RuleFor(x => x.StudentId).NotEmpty().WithMessage("Öğrenci belirtilmelidir.");
        RuleFor(x => x.BusinessId).NotEmpty().WithMessage("İşletme belirtilmelidir.");
        RuleFor(x => x.Month).InclusiveBetween(1, 12).WithMessage("Ay 1-12 arasında olmalıdır.");
        RuleFor(x => x.Year).GreaterThan(0).WithMessage("Geçerli bir yıl belirtilmelidir.");
        RuleFor(x => x.ReceiptFile).NotNull().WithMessage("Dekont dosyası belirtilmelidir.");
    }
}
