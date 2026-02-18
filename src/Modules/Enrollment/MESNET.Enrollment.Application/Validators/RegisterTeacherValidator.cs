using FluentValidation;
using MESNET.Enrollment.Application.Commands;

namespace MESNET.Enrollment.Application.Validators;

public class RegisterTeacherValidator : AbstractValidator<RegisterTeacher>
{
    public RegisterTeacherValidator()
    {
        RuleFor(x => x.InstitutionId).NotEmpty().WithMessage("Kurum belirtilmelidir.");
        RuleFor(x => x.KeycloakUserId).NotEmpty().WithMessage("Kullanıcı kimliği belirtilmelidir.");
        RuleFor(x => x.FullName).NotEmpty().WithMessage("Ad soyad belirtilmelidir.");
    }
}
