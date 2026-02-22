using Marten;
using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Core.ReadModels;

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
        options.Schema.For<TeacherSchedule>().Index(x => x.AcademicPeriodId);

        // Composite index for common query pattern
        options.Schema.For<TeacherSchedule>()
            .Index(x => new { x.TeacherId, x.AcademicYear }, x => x.IsUnique = false);

        // MonthlyActivityReport
        options.Schema.For<MonthlyActivityReport>().DatabaseSchemaName("coordination");
        options.Schema.For<MonthlyActivityReport>().Index(x => x.StudentId);
        options.Schema.For<MonthlyActivityReport>().Index(x => x.BusinessId);
        options.Schema.For<MonthlyActivityReport>().Index(x => x.TeacherId);
        options.Schema.For<MonthlyActivityReport>().Index(x => x.AcademicPeriodId);
        options.Schema.For<MonthlyActivityReport>()
            .Index(x => new { x.StudentId, x.Year, x.Month }, x => x.IsUnique = false);

        // GuidanceVisit
        options.Schema.For<GuidanceVisit>().DatabaseSchemaName("coordination");
        options.Schema.For<GuidanceVisit>().Index(x => x.TeacherId);
        options.Schema.For<GuidanceVisit>().Index(x => x.BusinessId);
        options.Schema.For<GuidanceVisit>().Index(x => x.VisitDate);
        options.Schema.For<GuidanceVisit>().Index(x => x.AcademicPeriodId);

        // SkillExam
        options.Schema.For<SkillExam>().DatabaseSchemaName("coordination");
        options.Schema.For<SkillExam>().Index(x => x.StudentId);
        options.Schema.For<SkillExam>().Index(x => x.BusinessId);
        options.Schema.For<SkillExam>().Index(x => x.AcademicYear);
        options.Schema.For<SkillExam>().Index(x => x.AcademicPeriodId);

        // BusinessEvaluation
        options.Schema.For<BusinessEvaluation>().DatabaseSchemaName("coordination");
        options.Schema.For<BusinessEvaluation>().Index(x => x.BusinessId);
        options.Schema.For<BusinessEvaluation>().Index(x => x.InstitutionId);
        options.Schema.For<BusinessEvaluation>().Index(x => x.EvaluationDate);

        // AcademicPeriodView (cross-module read model)
        options.Schema.For<AcademicPeriodView>().DatabaseSchemaName("coordination");
        options.Schema.For<AcademicPeriodView>().Index(x => x.InstitutionId);
    }
}
