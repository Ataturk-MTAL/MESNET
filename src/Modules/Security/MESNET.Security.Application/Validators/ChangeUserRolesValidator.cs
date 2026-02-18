using FluentValidation;
using MESNET.Security.Application.Commands;

namespace MESNET.Security.Application.Validators;

public class ChangeUserRolesValidator : AbstractValidator<ChangeUserRoles>
{
    public ChangeUserRolesValidator()
    {
        RuleFor(x => x.UserAccountId).NotEmpty().WithMessage("Kullanıcı belirtilmelidir.");
        RuleFor(x => x.NewRoles).NotEmpty().WithMessage("En az bir rol belirtilmelidir.");
    }
}
