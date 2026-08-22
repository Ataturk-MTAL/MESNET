using Marten;
using MESNET.Coordination.Application.Dtos;
using MESNET.Coordination.Application.Helpers;
using MESNET.Coordination.Application.Queries;
using MESNET.Coordination.Core.Aggregates;
using MESNET.Coordination.Core.Enums;

namespace MESNET.Coordination.Application.Handlers;

public static class GetScheduleHistoryHandler
{
    public static async Task<ScheduleHistoryDto?> Handle(
        GetScheduleHistory query,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        // 1. Öğretmenin schedule'larını bul
        TeacherSchedule? schedule;

        if (query.ScheduleId.HasValue)
        {
            schedule = await session.LoadAsync<TeacherSchedule>(query.ScheduleId.Value, cancellationToken);
            if (schedule is null || schedule.TeacherId != query.TeacherId) return null;
        }
        else
        {
            // En son güncellenen schedule'ı al
            var schedules = await session.Query<TeacherSchedule>()
                .Where(s => s.TeacherId == query.TeacherId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync(cancellationToken);

            schedule = schedules
                .OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt)
                .FirstOrDefault();

            if (schedule is null) return null;
        }

        // 2. Event stream'den tüm event'leri al
        var events = await session.Events.FetchStreamAsync(schedule.Id, token: cancellationToken);

        // 3. Event'leri versiyonlara dönüştür
        var versions = new List<ScheduleVersionDto>();

        foreach (var e in events)
        {
            List<DailyScheduleDto>? weeklySchedule = null;
            string eventType;
            Guid updatedById;
            DateTime timestamp;

            switch (e.Data)
            {
                case ScheduleCreated created:
                    eventType = "Oluşturuldu";
                    updatedById = created.UpdatedById;
                    timestamp = created.Timestamp;
                    weeklySchedule = MapWeeklyData(created.WeeklySchedule);
                    break;

                case ScheduleUpdated updated:
                    eventType = "Güncellendi";
                    updatedById = updated.UpdatedById;
                    timestamp = updated.Timestamp;
                    weeklySchedule = MapWeeklyData(updated.WeeklySchedule);
                    break;

                default:
                    continue;
            }

            versions.Add(new ScheduleVersionDto(
                (int)e.Version,
                eventType,
                timestamp,
                updatedById,
                UpdatedByName: null, // aşağıda tek sorguda toplu çözülür
                weeklySchedule ?? []));
        }

        // Aktör adı saklanmaz, okuma anında çözülür (#137) — versiyon başına ayrı sorgu
        // atmamak için tüm kimlikler tek LoadMany ile çekilir.
        var names = await UserNameResolver.ResolveAsync(
            session, versions.Select(v => v.UpdatedById), cancellationToken);

        for (var i = 0; i < versions.Count; i++)
            versions[i] = versions[i] with { UpdatedByName = names.NameOf(versions[i].UpdatedById) };

        return new ScheduleHistoryDto(
            schedule.Id,
            schedule.TeacherId,
            schedule.AcademicYear,
            schedule.SemesterName,
            schedule.Version,
            versions);
    }

    /// <summary>
    /// Öğretmenin tüm schedule stream'lerinin listesini getir
    /// </summary>
    public static async Task<List<ScheduleStreamSummaryDto>> Handle(
        GetScheduleStreams query,
        IQuerySession session,
        CancellationToken cancellationToken)
    {
        var schedules = await session.Query<TeacherSchedule>()
            .Where(s => s.TeacherId == query.TeacherId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

        // Aktör adı saklanmaz, okuma anında çözülür (#137).
        var names = await UserNameResolver.ResolveAsync(
            session,
            schedules.SelectMany(s => new[] { s.CreatedById, s.UpdatedById ?? Guid.Empty }),
            cancellationToken);

        return schedules
            .OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt)
            .Select(s => new ScheduleStreamSummaryDto(
                s.Id,
                s.AcademicYear,
                s.SemesterName,
                s.Version,
                s.CreatedAt,
                s.UpdatedAt,
                s.CreatedById,
                names.NameOf(s.CreatedById),
                s.UpdatedById,
                names.NameOf(s.UpdatedById)))
            .ToList();
    }

    private static List<DailyScheduleDto> MapWeeklyData(List<DailyScheduleData> data)
    {
        return data.Select(d => new DailyScheduleDto(
            d.Day,
            d.Periods.Select(p => new PeriodSlotDto(
                p.PeriodNumber,
                p.Status,
                p.CourseName,
                p.AssignedBusinessId)).ToList()
        )).ToList();
    }
}
