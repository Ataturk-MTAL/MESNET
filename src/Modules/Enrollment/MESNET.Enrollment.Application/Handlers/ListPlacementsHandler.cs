using Marten;
using MESNET.Enrollment.Application.Dtos;
using MESNET.Enrollment.Application.Extensions;
using MESNET.Enrollment.Application.Queries;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Core.Enums;
using MESNET.Enrollment.Core.ReadModels;

namespace MESNET.Enrollment.Application.Handlers;

public static class ListPlacementsHandler
{
    public static async Task<IReadOnlyList<InternshipPlacementDto>> Handle(ListPlacements query, IQuerySession session)
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

        var placements = await queryable.ToListAsync();

        // SmartEnum LINQ kısıtı: in-memory filtrele
        if (!string.IsNullOrWhiteSpace(query.Status) &&
            PlacementStatus.TryFromName(query.Status, true, out var status))
            placements = placements.Where(p => p.Status.Name == status.Name).ToList();

        if (placements.Count == 0)
            return Array.Empty<InternshipPlacementDto>();

        // Batch isim yükleme
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

        return placements.Select(p => p.ToDto(
            studentNames.GetValueOrDefault(p.StudentId, ""),
            businessNames.GetValueOrDefault(p.BusinessId, ""),
            p.TeacherId.HasValue ? teacherNames.GetValueOrDefault(p.TeacherId.Value) : null
        )).ToList();
    }
}
