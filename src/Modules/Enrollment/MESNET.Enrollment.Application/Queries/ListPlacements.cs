using MESNET.Common.Shared.Pagination;

namespace MESNET.Enrollment.Application.Queries;

// Yetki-kapsam (kurum + rol bazlı teacher/işletme daraltma) artık endpoint'te değil,
// ListPlacementsHandler içinde ICurrentUserService'ten türetilir — bu yüzden query yalnız
// kullanıcının verdiği ham filtreleri taşır.
public sealed record ListPlacements(
    Guid? BusinessId,
    Guid? StudentId,
    Guid? AcademicPeriodId,
    string? Status,
    string? BranchCode = null) : PagedQuery;
