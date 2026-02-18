using FluentValidation;
using MESNET.Coordination.Application.Commands;

namespace MESNET.Coordination.Application.Validators;

public class CreateSkillExamValidator : AbstractValidator<CreateSkillExam>
{
    public CreateSkillExamValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty().WithMessage("Öğrenci belirtilmelidir.");
        RuleFor(x => x.BusinessId).NotEmpty().WithMessage("İşletme belirtilmelidir.");
        RuleFor(x => x.InstitutionId).NotEmpty().WithMessage("Kurum belirtilmelidir.");
        RuleFor(x => x.AcademicYear).GreaterThan(0).WithMessage("Geçerli bir öğretim yılı belirtilmelidir.");
        RuleFor(x => x.Semester).NotEmpty().WithMessage("Dönem belirtilmelidir.");
        RuleFor(x => x.ExamDate).NotEmpty().WithMessage("Sınav tarihi belirtilmelidir.");
        RuleFor(x => x.Score).InclusiveBetween(0, 100).WithMessage("Puan 0-100 arasında olmalıdır.");
        RuleFor(x => x.Criteria).NotEmpty().WithMessage("En az bir değerlendirme kriteri belirtilmelidir.");
        RuleFor(x => x.CommitteeMembers).NotEmpty().WithMessage("En az bir komisyon üyesi belirtilmelidir.");
        RuleFor(x => x.Result).NotEmpty().WithMessage("Sınav sonucu belirtilmelidir.");
    }
}
