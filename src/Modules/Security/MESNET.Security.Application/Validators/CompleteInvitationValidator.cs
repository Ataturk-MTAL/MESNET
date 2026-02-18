using FluentValidation;
using MESNET.Security.Application.Commands;

namespace MESNET.Security.Application.Validators;

public class CompleteInvitationValidator : AbstractValidator<CompleteInvitation>
{
    public CompleteInvitationValidator()
    {
        RuleFor(x => x.InvitationId).NotEmpty().WithMessage("Davet belirtilmelidir.");
        RuleFor(x => x.Username).NotEmpty().WithMessage("Kullanıcı adı belirtilmelidir.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Şifre belirtilmelidir.")
            .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalıdır.");
    }
}
