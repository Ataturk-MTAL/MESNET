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

        // MonthlyActivityReport
        options.Schema.For<MonthlyActivityReport>().DatabaseSchemaName("coordination");
        options.Schema.For<MonthlyActivityReport>().Index(x => x.StudentId);
        options.Schema.For<MonthlyActivityReport>().Index(x => x.BusinessId);
        options.Schema.For<MonthlyActivityReport>().Index(x => x.TeacherId);
        options.Schema.For<MonthlyActivityReport>()
            .Index(x => new { x.StudentId, x.Year, x.Month }, x => x.IsUnique = false);

        // GuidanceVisit
        options.Schema.For<GuidanceVisit>().DatabaseSchemaName("coordination");
        options.Schema.For<GuidanceVisit>().Index(x => x.TeacherId);
        options.Schema.For<GuidanceVisit>().Index(x => x.BusinessId);
        options.Schema.For<GuidanceVisit>().Index(x => x.VisitDate);

        // SkillExam
        options.Schema.For<SkillExam>().DatabaseSchemaName("coordination");
        options.Schema.For<SkillExam>().Index(x => x.StudentId);
        options.Schema.For<SkillExam>().Index(x => x.BusinessId);
        options.Schema.For<SkillExam>().Index(x => x.AcademicYear);

        // BusinessEvaluation
        options.Schema.For<BusinessEvaluation>().DatabaseSchemaName("coordination");
        options.Schema.For<BusinessEvaluation>().Index(x => x.BusinessId);
        options.Schema.For<BusinessEvaluation>().Index(x => x.InstitutionId);
        options.Schema.For<BusinessEvaluation>().Index(x => x.EvaluationDate);
    }
}
