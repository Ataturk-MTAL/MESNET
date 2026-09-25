using JasperFx.Events.Projections;
using Marten;
using Marten.Events.Projections;
using Microsoft.Extensions.DependencyInjection;
using MESNET.Attendance.Application.Projections;
using MESNET.Attendance.Core.Aggregates;

namespace MESNET.Attendance.Application;

public static class ServiceRegistration
{
    /// <summary>
    /// Attendance Application katmanı servislerini DI container'a ekler.
    /// Wolverine handlers otomatik keşfedilir (convention-based).
    /// Marten projection'ları register eder.
    /// </summary>
    public static IServiceCollection AddAttendanceApplication(this IServiceCollection services)
    {
        // Marten projections + snapshot schema
        services.ConfigureMarten(opts =>
        {
            opts.Projections.Snapshot<AttendanceRecord>(SnapshotLifecycle.Inline);
            opts.Schema.For<AttendanceRecord>().DatabaseSchemaName("attendance");
            opts.Schema.For<AttendanceRecord>().Index(x => x.StudentId);
            opts.Schema.For<AttendanceRecord>().Index(x => x.BusinessId);
            opts.Schema.For<AttendanceRecord>().Index(x => x.InstitutionId);
            opts.Schema.For<AttendanceRecord>().Index(x => x.Date);
            // INLINE (#317): row-level security açıkken async daemon olayları kendi bağlantısında,
            // kiracı ayarı OLMADAN okur — politika onları süzer, daemon boş aralığı "işlendi"
            // sayıp ilerler. Ölçüldü: 6 devamsızlık kaydı, ilerleme 29'a taşındı, görünüm 0 belge,
            // logda hata yok. Olaylar KALICI atlanır. Inline projeksiyon yazan session'da, kiracısı
            // kurulu bağlantıda çalışır.
            // Not: AttendanceView hiçbir handler'da OKUNMUYOR (#249 sayacı AttendanceRecord'a taşıdı).
            opts.Projections.Add<AttendanceViewProjection>(ProjectionLifecycle.Inline);

            // Ücretli izin başvurusu (#177) — durum geçişi olan varlık, event sourcing.
            opts.Projections.Snapshot<PaidLeaveRequest>(SnapshotLifecycle.Inline);
            opts.Schema.For<PaidLeaveRequest>().DatabaseSchemaName("attendance");
            opts.Schema.For<PaidLeaveRequest>().Index(x => x.StudentId);
            opts.Schema.For<PaidLeaveRequest>().Index(x => x.BusinessId);
            opts.Schema.For<PaidLeaveRequest>().Index(x => x.InstitutionId);
            opts.Schema.For<PaidLeaveRequest>().Index(x => x.StartDate);
        });

        return services;
    }
}
