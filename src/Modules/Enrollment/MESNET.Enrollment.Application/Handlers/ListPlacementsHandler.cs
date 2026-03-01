using Marten;
using MESNET.Common.Infrastructure.Pagination;
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
    public static async Task<PagedResult<InternshipPlacementDto>> Handle(ListPlacements query, IQuerySession session)
    {
        IQueryable<InternshipPlacement> queryable = session.Query<InternshipPlacement>();

        if (query.InstitutionId.HasValue)
            queryable = queryable.Where(p => p.InstitutionId == query.InstitutionId.Value);

        if (query.BusinessId.HasValue)
            queryable = queryable.Where(p => p.BusinessId == query.BusinessId.Value);

        if (query.StudentId.HasValue)
            queryable = queryable.Where(p => p.StudentId == query.StudentId.Value);

        if (query.AcademicPeriodId.HasValue)
            queryable = queryable.Where(p => p.AcademicPeriodId == query.AcademicPeriodId.Value);

        if (query.TeacherId.HasValue)
            queryable = queryable.Where(p => p.TeacherId == query.TeacherId.Value);

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            PlacementStatus.TryFromName(query.Status, true, out var status))
            queryable = queryable.Where(p => p.Status.Name == status.Name);

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
        var studentIds = placements.Select(p => p.StudentId).Distinct().ToList();
        var businessIds = placements.Select(p => p.BusinessId).Distinct().ToList();
        var teacherIds = placements.Where(p => p.TeacherId.HasValue).Select(p => p.TeacherId!.Value).Distinct().ToList();

        var students = await session.LoadManyAsync<StudentProfile>(studentIds);
        var businesses = await session.LoadManyAsync<BusinessProfileView>(businessIds);
        var teachers = teacherIds.Count > 0
            ? await session.LoadManyAsync<TeacherProfile>(teacherIds)
            : new List<TeacherProfile>();

        var studentNames = students.ToDictionary(s => s.Id, s => s.FullName);
        var businessNames = businesses.ToDictionary(b => b.Id, b => b.BusinessName);
        var teacherNames = teachers.ToDictionary(t => t.Id, t => t.FullName);

        var items = placements.Select(p => p.ToDto(
            studentNames.GetValueOrDefault(p.StudentId, ""),
            businessNames.GetValueOrDefault(p.BusinessId, ""),
            p.TeacherId.HasValue ? teacherNames.GetValueOrDefault(p.TeacherId.Value) : null
        )).ToList();

        return PagedResult<InternshipPlacementDto>.Create(
            items, totalCount, query.SafePage, query.SafePageSize);
    }
}
