using Marten;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared.Security;
using MESNET.Common.Infrastructure.Pagination;
using MESNET.Common.Shared.Pagination;
using MESNET.Payment.Application.Dtos;
using MESNET.Payment.Application.Extensions;
using MESNET.Payment.Application.Queries;
using MESNET.Payment.Core.Entities;
using MESNET.Payment.Core.Enums;

namespace MESNET.Payment.Application.Handlers;

public static class ListPaymentSummariesHandler
{
    public static async Task<PagedResult<PaymentSummaryDto>> Handle(
        ListPaymentSummaries query, IQuerySession session, ICurrentUserService currentUser)
    {
        IQueryable<PaymentSummary> q = session.Query<PaymentSummary>();

        // Kapsam merdiveni (#182): geniş görüntüleme izni yoksa kullanıcı yalnız KENDİ verisini
        // görür — veli bağlı öğrencilerini, öğrenci kendisini. Kapsam çözülemezse boş sonuç.
        var scope = OwnDataScope.Resolve(currentUser, Permissions.Salary.View);
        if (scope.IsEmpty)
            return EmptyPage(query);

        if (!scope.IsUnrestricted)
        {
            var scopedStudentIds = scope.StudentIds;
            q = q.Where(p => scopedStudentIds.Contains(p.StudentId));
        }

        if (query.StudentId.HasValue)
            q = q.Where(p => p.StudentId == query.StudentId.Value);

        if (query.BusinessId.HasValue)
            q = q.Where(p => p.BusinessId == query.BusinessId.Value);

        if (query.InstitutionId.HasValue)
            q = q.Where(p => p.InstitutionId == query.InstitutionId.Value);

        if (query.AcademicPeriodId.HasValue)
            q = q.Where(p => p.AcademicPeriodId == query.AcademicPeriodId.Value);

        if (!string.IsNullOrWhiteSpace(query.Month))
            q = q.Where(p => p.Month == query.Month);

        if (!string.IsNullOrWhiteSpace(query.MonthFrom))
            q = q.Where(p => string.Compare(p.Month, query.MonthFrom) >= 0);

        if (!string.IsNullOrWhiteSpace(query.MonthTo))
            q = q.Where(p => string.Compare(p.Month, query.MonthTo) <= 0);

        if (!string.IsNullOrWhiteSpace(query.Phase) && PaymentPhase.TryFromName(query.Phase, out var phase))
            q = q.Where(p => p.PhaseName == phase.Name);

        if (!string.IsNullOrWhiteSpace(query.BranchCode))
            q = q.Where(p => p.BranchCode == query.BranchCode);

        q = q.ApplySearch(query.Search, p => p.StudentName, p => p.StudentNumber);
        q = q.ApplySort(query.SortBy, query.Descending, defaultSort: p => p.Month);

        return await q.ToPagedResultAsync(query, s => s.ToDto());
    }

    /// <summary>Kapsam çözülemedi — sayfa bilgisi korunur, içerik boş döner (#182).</summary>
    private static PagedResult<PaymentSummaryDto> EmptyPage(ListPaymentSummaries query) => new()
    {
        Items = [],
        TotalCount = 0,
        Page = query.Page,
        PageSize = query.PageSize
    };
}
