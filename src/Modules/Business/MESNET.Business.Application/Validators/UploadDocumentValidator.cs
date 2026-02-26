using FluentValidation;
using MESNET.Business.Application.Commands;

namespace MESNET.Business.Application.Validators;

public class UploadDocumentValidator : AbstractValidator<UploadDocument>
{
    public UploadDocumentValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty().WithMessage("İşletme belirtilmelidir.");
        RuleFor(x => x.Type).NotNull().WithMessage("Geçerli bir belge türü belirtilmelidir.");
        RuleFor(x => x.DocumentFile).NotNull().WithMessage("Dosya yüklenmedi.");
    }
}
