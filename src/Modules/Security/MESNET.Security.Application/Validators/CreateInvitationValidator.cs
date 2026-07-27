using FluentValidation;
using MESNET.Security.Application.Commands;

namespace MESNET.Security.Application.Validators;

public class CreateInvitationValidator : AbstractValidator<CreateInvitation>
{
    public CreateInvitationValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("E-posta belirtilmelidir.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.");
        RuleFor(x => x.FirstName).NotEmpty().WithMessage("Ad belirtilmelidir.");
        RuleFor(x => x.LastName).NotEmpty().WithMessage("Soyad belirtilmelidir.");
        // Hedef rol sistemde tanımlı olmalıdır (#129) — tanınmayan ad Keycloak'ta çözülemez ve
        // davet tamamlandığında kullanıcı sıfır realm rolüyle, hiçbir izin almadan açılırdı.
        RuleFor(x => x.TargetRole).NotEmpty().WithMessage("Hedef rol belirtilmelidir.")
            .MustBeKnownRole();
        RuleFor(x => x.CreatedByName).NotEmpty().WithMessage("Oluşturan kişi belirtilmelidir.");
    }
}
