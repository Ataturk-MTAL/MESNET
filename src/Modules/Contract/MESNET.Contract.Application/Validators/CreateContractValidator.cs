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
        RuleFor(x => x.EndDate).NotEmpty().WithMessage("Bitiş tarihi belirtilmelidir.")
            .GreaterThan(x => x.StartDate).WithMessage("Bitiş tarihi başlangıç tarihinden sonra olmalıdır.");
    }
}
