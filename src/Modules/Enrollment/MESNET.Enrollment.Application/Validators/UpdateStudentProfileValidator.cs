using FluentValidation;
using MESNET.Enrollment.Application.Commands;

namespace MESNET.Enrollment.Application.Validators;

public class UpdateStudentProfileValidator : AbstractValidator<UpdateStudentProfile>
{
    public UpdateStudentProfileValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty().WithMessage("Öğrenci belirtilmelidir.");
        RuleFor(x => x.FullName).NotEmpty().WithMessage("Ad soyad belirtilmelidir.");
        RuleFor(x => x.BranchCode).NotEmpty().WithMessage("Alan kodu belirtilmelidir.");
        RuleFor(x => x.BranchName).NotEmpty().WithMessage("Alan adı belirtilmelidir.");
        RuleFor(x => x.ClassYear).InclusiveBetween(9, 12).WithMessage("Sınıf 9-12 arasında olmalıdır.");
        RuleFor(x => x.Section).MaximumLength(5).WithMessage("Şube en fazla 5 karakter olmalıdır.");
    }
}
