using FluentValidation;
using MESNET.Coordination.Application.Commands;

namespace MESNET.Coordination.Application.Validators;

public class UpsertBranchWorkloadConfigValidator : AbstractValidator<UpsertBranchWorkloadConfig>
{
    public UpsertBranchWorkloadConfigValidator()
    {
        // Zorunlu kimlik alanları — boş gövde (Guid.Empty / null) reddedilir
        RuleFor(x => x.InstitutionId).NotEmpty().WithMessage("Kurum belirtilmelidir.");
        RuleFor(x => x.AcademicPeriodId).NotEmpty().WithMessage("Akademik dönem belirtilmelidir.");
        RuleFor(x => x.BranchCode).NotEmpty().WithMessage("Alan kodu belirtilmelidir.");
        RuleFor(x => x.EducationType).NotEmpty().WithMessage("Eğitim türü belirtilmelidir.");
        RuleFor(x => x.UpdatedBy).NotEmpty().WithMessage("Güncelleyen kişi belirtilmelidir.");

        // Şeflik sayıları/saatleri — negatif değer geçersizdir
        RuleFor(x => x.DepartmentHeadCount).GreaterThanOrEqualTo(0).WithMessage("Alan şefi sayısı negatif olamaz.");
        RuleFor(x => x.WorkshopHeadCount).GreaterThanOrEqualTo(0).WithMessage("Atölye şefi sayısı negatif olamaz.");
        RuleFor(x => x.DepartmentHeadHours).GreaterThanOrEqualTo(0).WithMessage("Alan şefi saati negatif olamaz.");
        RuleFor(x => x.WorkshopHeadHours).GreaterThanOrEqualTo(0).WithMessage("Atölye şefi saati negatif olamaz.");

        // Sınıf seviyeleri koleksiyonu — null gövdede NRE'yi önler
        RuleFor(x => x.ClassLevels).NotNull().WithMessage("Sınıf seviyeleri belirtilmelidir.");

        // Her sınıf seviyesi girdisi geçerli aralıkta olmalı
        RuleForEach(x => x.ClassLevels).ChildRules(cl =>
        {
            cl.RuleFor(c => c.ClassYear).GreaterThan(0).WithMessage("Sınıf seviyesi sıfırdan büyük olmalıdır.");
            cl.RuleFor(c => c.WeeklyLessonHours).GreaterThanOrEqualTo(0).WithMessage("Haftalık ders saati negatif olamaz.");
        });
    }
}
