using Marten;
using MESNET.Coordination.Core.Entities;

namespace MESNET.Coordination.Persistence;

public static class MartenConfiguration
{
    public static void ConfigureCoordinationSchema(this StoreOptions options)
    {
        // Schema name
        options.Schema.For<TeacherSchedule>().DatabaseSchemaName("coordination");

        // Indexes for performance
        options.Schema.For<TeacherSchedule>().Index(x => x.TeacherId);
        options.Schema.For<TeacherSchedule>().Index(x => x.InstitutionId);
        options.Schema.For<TeacherSchedule>().Index(x => x.AcademicYear);

        // Composite index for common query pattern
        options.Schema.For<TeacherSchedule>()
            .Index(x => new { x.TeacherId, x.AcademicYear }, x => x.IsUnique = false);
    }
}
