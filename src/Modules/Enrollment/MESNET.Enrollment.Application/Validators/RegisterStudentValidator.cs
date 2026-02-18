using FluentValidation;
using MESNET.Enrollment.Application.Commands;

namespace MESNET.Enrollment.Application.Validators;

public class RegisterStudentValidator : AbstractValidator<RegisterStudent>
{
    public RegisterStudentValidator()
    {
        RuleFor(x => x.InstitutionId).NotEmpty().WithMessage("Kurum belirtilmelidir.");
        RuleFor(x => x.KeycloakUserId).NotEmpty().WithMessage("Kullanıcı kimliği belirtilmelidir.");
        RuleFor(x => x.FullName).NotEmpty().WithMessage("Ad soyad belirtilmelidir.");
        RuleFor(x => x.BranchCode).NotEmpty().WithMessage("Alan kodu belirtilmelidir.");
        RuleFor(x => x.BranchName).NotEmpty().WithMessage("Alan adı belirtilmelidir.");
        RuleFor(x => x.ClassYear).InclusiveBetween(9, 12).WithMessage("Sınıf 9-12 arasında olmalıdır.");
        RuleFor(x => x.Section).MaximumLength(5).WithMessage("Şube en fazla 5 karakter olmalıdır.");
    }
}
