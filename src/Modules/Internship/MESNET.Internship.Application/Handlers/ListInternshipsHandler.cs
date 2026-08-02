using MESNET.Common.Infrastructure.Pagination;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared.Pagination;
using MESNET.Common.Shared.Security;
using MESNET.Internship.Application.Dtos;
using MESNET.Internship.Application.Extensions;
using MESNET.Internship.Application.Queries;
using MESNET.Internship.Core.Entities;
using MESNET.Internship.Core.Enums;
using Marten;

namespace MESNET.Internship.Application.Handlers;

public static class ListInternshipsHandler
{
    public static async Task<PagedResult<InternshipSummaryDto>> Handle(
        ListInternships query, IQuerySession session, ICurrentUserService currentUser)
    {
        IQueryable<InternshipSummary> queryable = session.Query<InternshipSummary>();
        // Kapsam merdiveni (#182): geniş görüntüleme izni yoksa kullanıcı yalnız KENDİ verisini
        // görür — veli bağlı öğrencilerini, öğrenci kendisini. Kapsam çözülemezse boş sonuç;
        // kapsamsız kullanıcıya tüm kurumun verisini açmaktansa hiçbir şey göstermek doğrudur.
        var scope = OwnDataScope.Resolve(currentUser, Permissions.Internship.View);
        if (scope.IsEmpty)
            return EmptyPage(query);

        if (!scope.IsUnrestricted)
        {
            var scopedStudentIds = scope.StudentIds;
            queryable = queryable.Where(s => scopedStudentIds.Contains(s.StudentId));
        }

        if (query.StudentId.HasValue)
            queryable = queryable.Where(s => s.StudentId == query.StudentId.Value);

        if (query.BusinessId.HasValue)
            queryable = queryable.Where(s => s.BusinessId == query.BusinessId.Value);

        if (query.InstitutionId.HasValue)
            queryable = queryable.Where(s => s.InstitutionId == query.InstitutionId.Value);

        if (query.AcademicPeriodId.HasValue)
            queryable = queryable.Where(s => s.AcademicPeriodId == query.AcademicPeriodId.Value);

        if (!string.IsNullOrWhiteSpace(query.Phase) &&
            InternshipPhase.TryFromName(query.Phase, true, out var internshipPhase))
            queryable = queryable.Where(s => s.PhaseName == internshipPhase.Name);

        if (query.MinAbsenceDays.HasValue)
            queryable = queryable.Where(s => s.TotalAbsenceDays >= query.MinAbsenceDays.Value);

        queryable = queryable.ApplySearch(query.Search, s => s.StudentName);

        // Varsayılan: devamsızlığa göre azalan (en çok devamsız üste)
        var effectiveDescending = string.IsNullOrWhiteSpace(query.SortBy) ? true : query.Descending;
        queryable = queryable.ApplySort(query.SortBy, effectiveDescending,
            defaultSort: s => s.TotalAbsenceDays);

        return await queryable.ToPagedResultAsync(query, s => s.ToDto(s.StudentName, s.BusinessName));
    }

    /// <summary>Kapsam çözülemedi — sayfa bilgisi korunur, içerik boş döner (#182).</summary>
    private static PagedResult<InternshipSummaryDto> EmptyPage(ListInternships query) => new()
    {
        Items = [],
        TotalCount = 0,
        Page = query.Page,
        PageSize = query.PageSize
    };
}
