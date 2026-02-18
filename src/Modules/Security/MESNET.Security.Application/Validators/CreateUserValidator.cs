using FluentValidation;
using MESNET.Security.Application.Commands;

namespace MESNET.Security.Application.Validators;

public class CreateUserValidator : AbstractValidator<CreateUser>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("Kullanıcı adı belirtilmelidir.");
        RuleFor(x => x.Email).NotEmpty().WithMessage("E-posta belirtilmelidir.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.");
        RuleFor(x => x.FirstName).NotEmpty().WithMessage("Ad belirtilmelidir.");
        RuleFor(x => x.LastName).NotEmpty().WithMessage("Soyad belirtilmelidir.");
        RuleFor(x => x.Roles).NotEmpty().WithMessage("En az bir rol belirtilmelidir.");
    }
}
