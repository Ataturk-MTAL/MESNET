using FluentValidation;
using MESNET.Common.Shared.Security;
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

        // Veli–öğrenci bağı yalnız VELİ rolünde anlamlıdır (#271). Başka bir role öğrenci
        // bağlamak, o kullanıcıya ParentScopePolicy üzerinden öğrenci verisine erişim açardı —
        // izin erişimi açar, kapsamı BAĞ belirler (ADR-0001).
        RuleFor(x => x.StudentIds)
            .Must((command, studentIds) =>
                studentIds is null || studentIds.Count == 0
                || string.Equals(command.TargetRole, MesnetRoles.Parent, StringComparison.Ordinal))
            .WithMessage("Öğrenci bağı yalnız veli rolünde kurulabilir.");

        RuleForEach(x => x.StudentIds)
            .NotEqual(Guid.Empty).WithMessage("Geçersiz öğrenci kimliği.");
    }
}
