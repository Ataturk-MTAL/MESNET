using FluentValidation;
using MESNET.Security.Application.Commands;

namespace MESNET.Security.Application.Validators;

public class ChangeUserRolesValidator : AbstractValidator<ChangeUserRoles>
{
    public ChangeUserRolesValidator()
    {
        RuleFor(x => x.UserAccountId).NotEmpty().WithMessage("Kullanıcı belirtilmelidir.");
        // Her rol sistemde tanımlı olmalıdır (#129) — tanınmayan ad Keycloak'ta çözülemez.
        RuleFor(x => x.NewRoles).NotEmpty().WithMessage("En az bir rol belirtilmelidir.")
            .MustBeKnownRoles();
    }
}
