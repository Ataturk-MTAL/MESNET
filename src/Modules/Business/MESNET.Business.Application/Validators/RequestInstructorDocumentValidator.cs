using FluentValidation;
using MESNET.Business.Application.Commands;

namespace MESNET.Business.Application.Validators;

public class RequestInstructorDocumentValidator : AbstractValidator<RequestInstructorDocument>
{
    public RequestInstructorDocumentValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty().WithMessage("İşletme belirtilmelidir.");
        RuleFor(x => x.RequestedBy).NotEmpty().WithMessage("Talep eden kişi belirtilmelidir.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Talep gerekçesi belirtilmelidir.");
        RuleFor(x => x.Deadline).GreaterThan(DateTime.UtcNow).WithMessage("Son tarih gelecekte olmalıdır.");
    }
}
