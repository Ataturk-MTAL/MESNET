using FluentValidation;
using MESNET.Common.Shared.Security;
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
        // Her rol sistemde tanımlı olmalıdır (#129) — tanınmayan ad Keycloak'ta çözülemez ve
        // kullanıcı sıfır realm rolüyle, hiçbir izin almadan açılırdı.
        RuleFor(x => x.Roles).NotEmpty().WithMessage("En az bir rol belirtilmelidir.")
            .MustBeKnownRoles();

        // Alan (branş) zorunluluğu permission'dan türetilir, rol adından DEĞİL (#126).
        // Dağıtım iznine sahip ama kurum geneli muafiyeti olmayan kullanıcı (alan şefi) en az
        // bir alanla kaydedilmelidir — aksi hâlde oluşturulduğu anda yazmaya kilitlenir.
        // Muafiyeti olanda (müdür, müdür yardımcısı) alan İSTENMEZ ve boş bırakılabilir.
        RuleFor(x => x.BranchCodes)
            .Must(codes => codes is not null && codes.Any(c => !string.IsNullOrWhiteSpace(c)))
            .When(x => BranchRequirement.IsRequiredForRoles(x.Roles))
            .WithMessage("Bu yetkideki kullanıcı için en az bir alan (branş) seçilmelidir.");
    }
}
