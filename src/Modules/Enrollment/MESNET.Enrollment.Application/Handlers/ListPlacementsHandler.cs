using Marten;
using MESNET.Common.Infrastructure.Pagination;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared.Pagination;
using MESNET.Enrollment.Application.Dtos;
using MESNET.Enrollment.Application.Extensions;
using MESNET.Enrollment.Application.Queries;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Core.Enums;
using MESNET.Enrollment.Core.ReadModels;

namespace MESNET.Enrollment.Application.Handlers;

public static class ListPlacementsHandler
{
    public static async Task<PagedResult<InternshipPlacementDto>> Handle(
        ListPlacements query, IQuerySession session, ICurrentUserService currentUser)
    {
        // Kapsam merdiveni — liste ve sayım sorgularında ortak (ADR-0001, #184).
        // Çözülemeyen kapsam BOŞ sayfadır; sessizce kurum geneline düşmez.
        if (await PlacementQueryScope.ResolveAsync(currentUser, session, query.BusinessId)
            is not { } scope)
        {
            return new PagedResult<InternshipPlacementDto>
            {
                Items = [], TotalCount = 0, Page = query.Page, PageSize = query.PageSize
            };
        }

        var (institutionId, teacherId, effectiveBusinessId) = scope;

        IQueryable<InternshipPlacement> queryable = session.Query<InternshipPlacement>();

        if (institutionId.HasValue)
            queryable = queryable.Where(p => p.InstitutionId == institutionId.Value);

        if (effectiveBusinessId.HasValue)
            queryable = queryable.Where(p => p.BusinessId == effectiveBusinessId.Value);

        if (query.StudentId.HasValue)
            queryable = queryable.Where(p => p.StudentId == query.StudentId.Value);

        if (query.AcademicPeriodId.HasValue)
            queryable = queryable.Where(p => p.AcademicPeriodId == query.AcademicPeriodId.Value);

        if (teacherId.HasValue)
            queryable = queryable.Where(p => p.TeacherId == teacherId.Value);

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            PlacementStatus.TryFromName(query.Status, true, out var status))
            queryable = queryable.Where(p => p.StatusName == status.Name);

        if (!string.IsNullOrWhiteSpace(query.BranchCode))
            queryable = queryable.Where(p => p.BranchCode == query.BranchCode);

        queryable = queryable.ApplySearch(query.Search, p => p.StudentName);
        queryable = queryable.ApplySort(query.SortBy, query.Descending, defaultSort: p => p.PlacedAt);

        // Sayfalama — önce count, sonra skip/take
        var totalCount = await queryable.CountAsync();
        var placements = await queryable
            .Skip(query.Skip)
            .Take(query.SafePageSize)
            .ToListAsync();

        if (placements.Count == 0)
            return PagedResult<InternshipPlacementDto>.Create(
                [], totalCount, query.SafePage, query.SafePageSize);

        // Batch isim yükleme — sadece mevcut sayfa
        // Okulda staj yerleştirmeleri işletme taşımaz (#159) — isim aramasına girmez.
        var businessIds = placements
            .Where(p => p.BusinessId.HasValue)
            .Select(p => p.BusinessId!.Value)
            .Distinct()
            .ToList();
        var teacherIds = placements.Where(p => p.TeacherId.HasValue).Select(p => p.TeacherId!.Value).Distinct().ToList();

        var businesses = businessIds.Count > 0
            ? await session.LoadManyAsync<BusinessProfileView>(businessIds)
            : new List<BusinessProfileView>();
        var teachers = teacherIds.Count > 0
            ? await session.LoadManyAsync<TeacherProfile>(teacherIds)
            : new List<TeacherProfile>();

        var businessNames = businesses.ToDictionary(b => b.Id, b => b.BusinessName);
        var teacherNames = teachers.ToDictionary(t => t.Id, t => t.FullName);

        var items = placements.Select(p => p.ToDto(
            p.BusinessId.HasValue ? businessNames.GetValueOrDefault(p.BusinessId.Value, "") : "",
            p.TeacherId.HasValue ? teacherNames.GetValueOrDefault(p.TeacherId.Value) : null
        )).ToList();

        return PagedResult<InternshipPlacementDto>.Create(
            items, totalCount, query.SafePage, query.SafePageSize);
    }
}
