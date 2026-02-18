using FluentValidation;
using MESNET.Coordination.Application.Commands;

namespace MESNET.Coordination.Application.Validators;

public class AssignBusinessToFreeSlotValidator : AbstractValidator<AssignBusinessToFreeSlot>
{
    public AssignBusinessToFreeSlotValidator()
    {
        RuleFor(x => x.TeacherId).NotEmpty().WithMessage("Öğretmen belirtilmelidir.");
        RuleFor(x => x.AcademicYear).GreaterThan(0).WithMessage("Geçerli bir öğretim yılı belirtilmelidir.");
        RuleFor(x => x.Semester).NotEmpty().WithMessage("Dönem belirtilmelidir.");
        RuleFor(x => x.Day).NotEmpty().WithMessage("Gün belirtilmelidir.");
        RuleFor(x => x.PeriodNumber).GreaterThan(0).WithMessage("Ders saati sıfırdan büyük olmalıdır.");
        RuleFor(x => x.BusinessId).NotEmpty().WithMessage("İşletme belirtilmelidir.");
        RuleFor(x => x.AssignedBy).NotEmpty().WithMessage("Atayan kişi belirtilmelidir.");
    }
}
