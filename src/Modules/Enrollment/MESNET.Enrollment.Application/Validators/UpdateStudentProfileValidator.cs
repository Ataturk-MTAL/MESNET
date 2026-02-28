using FluentValidation;
using MESNET.Enrollment.Application.Commands;

namespace MESNET.Enrollment.Application.Validators;

public class UpdateStudentProfileValidator : AbstractValidator<UpdateStudentProfile>
{
    public UpdateStudentProfileValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty().WithMessage("Öğrenci belirtilmelidir.");

        When(x => x.FullName is not null, () =>
        {
            RuleFor(x => x.FullName).NotEmpty().WithMessage("Ad soyad boş olamaz.");
        });

        When(x => x.BranchCode is not null, () =>
        {
            RuleFor(x => x.BranchCode).NotEmpty().WithMessage("Alan kodu boş olamaz.");
        });

        When(x => x.BranchName is not null, () =>
        {
            RuleFor(x => x.BranchName).NotEmpty().WithMessage("Alan adı boş olamaz.");
        });

        When(x => x.ClassYear is not null, () =>
        {
            RuleFor(x => x.ClassYear).InclusiveBetween(9, 12).WithMessage("Sınıf 9-12 arasında olmalıdır.");
        });

        RuleFor(x => x.Section).MaximumLength(5).WithMessage("Şube en fazla 5 karakter olmalıdır.");

        When(x => x.StudentNumber is not null, () =>
        {
            RuleFor(x => x.StudentNumber).MaximumLength(20).WithMessage("Öğrenci numarası en fazla 20 karakter olmalıdır.");
        });

        When(x => x.PhoneNumber is not null, () =>
        {
            RuleFor(x => x.PhoneNumber).MaximumLength(20).WithMessage("Telefon numarası en fazla 20 karakter olmalıdır.");
        });

        When(x => x.TcKimlikNo is not null, () =>
        {
            RuleFor(x => x.TcKimlikNo).Length(11).WithMessage("T.C. Kimlik No 11 haneli olmalıdır.");
        });

        When(x => x.GuardianPhone is not null, () =>
        {
            RuleFor(x => x.GuardianPhone).MaximumLength(20).WithMessage("Veli telefon numarası en fazla 20 karakter olmalıdır.");
        });
    }
}
