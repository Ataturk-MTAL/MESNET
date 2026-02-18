using FluentValidation;
using MESNET.Security.Application.Commands;

namespace MESNET.Security.Application.Validators;

public class ToggleUserStatusValidator : AbstractValidator<ToggleUserStatus>
{
    public ToggleUserStatusValidator()
    {
        RuleFor(x => x.UserAccountId).NotEmpty().WithMessage("Kullanıcı belirtilmelidir.");
    }
}
