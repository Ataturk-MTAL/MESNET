using Marten;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Core.Entities;
using MESNET.Attendance.Core.ReadModels;

namespace MESNET.Attendance.Persistence;

public class AttendanceMartenConfig : IConfigureMarten
{
    public void Configure(IServiceProvider services, StoreOptions options)
    {
        options.Schema.For<WorkCalendar>().DatabaseSchemaName("attendance");
        options.Schema.For<WorkCalendar>().Index(x => x.InstitutionId);

        // Read model schema (projection Application'da register edilir)
        options.Schema.For<AttendanceView>().DatabaseSchemaName("attendance");
        options.Schema.For<AttendanceView>().Index(x => x.StudentId);
        options.Schema.For<AttendanceView>().Index(x => x.BusinessId);

        options.Schema.For<AcademicPeriodView>().DatabaseSchemaName("attendance");
        options.Schema.For<AcademicPeriodView>().Index(x => x.InstitutionId);

        options.Schema.For<InternshipPlacementView>().DatabaseSchemaName("attendance");
        options.Schema.For<InternshipPlacementView>().Index(x => x.StudentId);
        options.Schema.For<InternshipPlacementView>().Index(x => x.BusinessId);
        options.Schema.For<InternshipPlacementView>().Index(x => x.AcademicPeriodId);

        // Öğrenci ad/numara araması için denormalize view (Enrollment.StudentRegistered ile beslenir)
        options.Schema.For<StudentNameView>().DatabaseSchemaName("attendance");

        // UserNameView (Security.UserDisplayNameUpserted ile beslenir) — denetim alanları
        // yalnız kullanıcı kimliğini saklar, ad sorgu tarafında buradan çözülür (#137)
        options.Schema.For<UserNameView>().DatabaseSchemaName("attendance");
    }
}
