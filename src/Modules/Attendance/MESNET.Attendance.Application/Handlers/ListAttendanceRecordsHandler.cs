using Marten;
using MESNET.Attendance.Application.Dtos;
using MESNET.Attendance.Application.Extensions;
using MESNET.Attendance.Application.Helpers;
using MESNET.Attendance.Application.Queries;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Core.Enums;
using MESNET.Attendance.Core.ReadModels;
using MESNET.Common.Infrastructure.Pagination;
using MESNET.Common.Shared.Pagination;

namespace MESNET.Attendance.Application.Handlers;

public static class ListAttendanceRecordsHandler
{
    public static async Task<PagedResult<AttendanceRecordDto>> Handle(
        ListAttendanceRecords query, IQuerySession session)
    {
        IQueryable<AttendanceRecord> queryable = session.Query<AttendanceRecord>()
            .Where(r => !r.IsDeleted);

        if (query.StudentId.HasValue)
            queryable = queryable.Where(r => r.StudentId == query.StudentId.Value);

        if (query.BusinessId.HasValue)
            queryable = queryable.Where(r => r.BusinessId == query.BusinessId.Value);

        if (query.InstitutionId.HasValue)
            queryable = queryable.Where(r => r.InstitutionId == query.InstitutionId.Value);

        if (query.AcademicPeriodId.HasValue)
            queryable = queryable.Where(r => r.AcademicPeriodId == query.AcademicPeriodId.Value);

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            AttendanceStatus.TryFromName(query.Status, true, out var attendanceStatus))
            queryable = queryable.Where(r => r.StatusName == attendanceStatus.Name);

        // Yıl/ay filtresi: Marten LINQ DateTime.Year/.Month üyelerini SQL'e çeviremez
        // (BadLinqExpressionException). Bunun yerine yarı-açık tarih aralığı [start, end) kullanılır.
        // Date alanı DateTime (UTC) olduğundan sınırlar da UTC Kind ile kurulur; gün içi saat bileşeni
        // varsa bile yarı-açık aralık tüm günü kapsar.
        if (query.Year.HasValue)
        {
            var year = query.Year.Value;

            // Ay yalnızca yıl ile birlikte anlamlıdır.
            if (query.Month.HasValue)
            {
                var month = query.Month.Value;
                var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
                var end = start.AddMonths(1);
                queryable = queryable.Where(r => r.Date >= start && r.Date < end);
            }
            else
            {
                var start = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var end = start.AddYears(1);
                queryable = queryable.Where(r => r.Date >= start && r.Date < end);
            }
        }

        // Arama: öğrenci ad/numara üzerinden (lokal StudentNameView → eşleşen öğrenci id'leri → devamsızlık filtresi)
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var matchingStudentIds = await session.Query<StudentNameView>()
                .ApplySearch(query.Search, s => s.FullName, s => s.StudentNumber)
                .Select(s => s.Id)
                .ToListAsync();
            queryable = queryable.Where(r => matchingStudentIds.Contains(r.StudentId));
        }

        // Alan (branch) filtresi: öğrencinin branşı devamsızlık kaydında değil, lokal StudentNameView'da
        // denormalize tutulur → o branştaki öğrenci id'leri → devamsızlık filtresi (2-adımlı sorgu).
        if (!string.IsNullOrWhiteSpace(query.BranchCode))
        {
            var branchStudentIds = await session.Query<StudentNameView>()
                .Where(s => s.BranchCode == query.BranchCode)
                .Select(s => s.Id)
                .ToListAsync();
            queryable = queryable.Where(r => branchStudentIds.Contains(r.StudentId));
        }

        queryable = queryable.ApplySort(query.SortBy, query.Descending, defaultSort: r => r.Date);

        var page = await queryable.ToPagedResultAsync(query, r => r);

        // Aktör adı saklanmaz, okuma anında çözülür (#139) — satır başına ayrı sorgu
        // atmamak için sayfadaki tüm kimlikler tek LoadMany ile çekilir.
        var names = await UserNameResolver.ResolveAsync(
            session, page.Items.SelectMany(r => r.ActorIds()));

        return new PagedResult<AttendanceRecordDto>
        {
            Items = [.. page.Items.Select(r => r.ToDto(names))],
            TotalCount = page.TotalCount,
            Page = page.Page,
            PageSize = page.PageSize
        };
    }
}
