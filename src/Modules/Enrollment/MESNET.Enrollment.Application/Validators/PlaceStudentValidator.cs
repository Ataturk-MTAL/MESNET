using FluentValidation;
using MESNET.Enrollment.Application.Commands;

namespace MESNET.Enrollment.Application.Validators;

public class PlaceStudentValidator : AbstractValidator<PlaceStudent>
{
    public PlaceStudentValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty().WithMessage("Öğrenci belirtilmelidir.");

        // İşletme artık ZORUNLU DEĞİL: null = okulda staj (#159). Ama boş Guid gönderilmesi
        // hâlâ hatadır — "işletme seçilmedi" ile "okulda staj" ayrı şeylerdir ve boş Guid
        // ikisini de temsil ediyormuş gibi görünüp yanlış tarafa düşerdi.
        RuleFor(x => x.BusinessId)
            .Must(id => id != Guid.Empty)
            .WithMessage("İşletme geçersiz. Okulda staj için işletme alanı boş bırakılmalıdır.");
        RuleFor(x => x.InstitutionId).NotEmpty().WithMessage("Kurum belirtilmelidir.");
    }
}
