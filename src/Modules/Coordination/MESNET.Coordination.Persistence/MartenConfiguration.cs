using JasperFx.Events.Projections;
using Marten;
using MESNET.Coordination.Core.Aggregates;
using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Persistence;

public static class MartenConfiguration
{
    public static void ConfigureCoordinationSchema(this StoreOptions options)
    {
        // ── Event Sourcing: TeacherSchedule ──
        // Inline snapshot — her event sonrası aggregate otomatik güncellenir
        options.Projections.Snapshot<TeacherSchedule>(SnapshotLifecycle.Inline);

        // Schema + indexes (snapshot document'ı için)
        options.Schema.For<TeacherSchedule>().DatabaseSchemaName("coordination");
        options.Schema.For<TeacherSchedule>().Index(x => x.TeacherId);
        options.Schema.For<TeacherSchedule>().Index(x => x.InstitutionId);
        options.Schema.For<TeacherSchedule>().Index(x => x.AcademicYear);
        options.Schema.For<TeacherSchedule>().Index(x => x.AcademicPeriodId);
        options.Schema.For<TeacherSchedule>()
            .Index(x => new { x.TeacherId, x.AcademicYear }, x => x.IsUnique = false);

        // ── Document Storage ──

        // CoordinationConfig (kurum başına tek document)
        options.Schema.For<CoordinationConfig>().DatabaseSchemaName("coordination");
        options.Schema.For<CoordinationConfig>().Index(x => x.InstitutionId);

        // BusinessCoordinationView (işletme-öğretmen atama read model — satır başına alan)
        options.Schema.For<BusinessCoordinationView>().DatabaseSchemaName("coordination");
        options.Schema.For<BusinessCoordinationView>().Index(x => x.InstitutionId);
        options.Schema.For<BusinessCoordinationView>().Index(x => x.BusinessId);
        options.Schema.For<BusinessCoordinationView>().Index(x => x.BranchCode);
        options.Schema.For<BusinessCoordinationView>().Index(x => x.AssignedTeacherId);
        options.Schema.For<BusinessCoordinationView>().Index(x => x.AcademicPeriodId);
        options.Schema.For<BusinessCoordinationView>()
            .Index(x => new { x.InstitutionId, x.BranchCode }, x => x.IsUnique = false);
        // (BusinessId, BranchCode, AcademicPeriodId) — satır kimliğinin mantıksal karşılığı.
        // Kimlik zaten bu üçlüden deterministik üretildiği için benzersizlik Id ile garanti
        // edilir; buradaki index yalnız sorgu içindir. Kısa ad zorunlu (64 karakter sınırı).
        options.Schema.For<BusinessCoordinationView>()
            .Index(x => new { x.BusinessId, x.BranchCode, x.AcademicPeriodId },
                x =>
                {
                    x.IsUnique = false;
                    x.Name = "idx_biz_coord_view_biz_branch_period";
                });

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

        // StudentTermGrade (dönem notları — Dönem Not Fişi kaynağı)
        options.Schema.For<StudentTermGrade>().DatabaseSchemaName("coordination");
        options.Schema.For<StudentTermGrade>().Index(x => x.StudentId);
        options.Schema.For<StudentTermGrade>().Index(x => x.BusinessId);
        options.Schema.For<StudentTermGrade>().Index(x => x.InstitutionId);
        options.Schema.For<StudentTermGrade>().Index(x => x.AcademicPeriodId);
        options.Schema.For<StudentTermGrade>().Index(x => x.StatusName);
        options.Schema.For<StudentTermGrade>()
            .Index(x => new { x.StudentId, x.AcademicPeriodId },
                x =>
                {
                    x.IsUnique = true;
                    x.Name = "idx_student_term_grade_unique";
                });

        // CoordinationPlacedStudentView (işletme bazlı öğrenci listesi — Enrollment.StudentPlaced'den)
        options.Schema.For<CoordinationPlacedStudentView>().DatabaseSchemaName("coordination");
        options.Schema.For<CoordinationPlacedStudentView>().Index(x => x.StudentId);
        options.Schema.For<CoordinationPlacedStudentView>().Index(x => x.BusinessId);
        options.Schema.For<CoordinationPlacedStudentView>().Index(x => x.InstitutionId);
        options.Schema.For<CoordinationPlacedStudentView>().Index(x => x.AcademicPeriodId);

        // BusinessEvaluation
        options.Schema.For<BusinessEvaluation>().DatabaseSchemaName("coordination");
        options.Schema.For<BusinessEvaluation>().Index(x => x.BusinessId);
        options.Schema.For<BusinessEvaluation>().Index(x => x.InstitutionId);
        options.Schema.For<BusinessEvaluation>().Index(x => x.EvaluationDate);

        // BranchStudentCountView (Enrollment event'lerinden türetilen öğrenci sayıları)
        options.Schema.For<BranchStudentCountView>().DatabaseSchemaName("coordination");
        options.Schema.For<BranchStudentCountView>().Index(x => x.InstitutionId);
        options.Schema.For<BranchStudentCountView>().Index(x => x.BranchCode);
        options.Schema.For<BranchStudentCountView>()
            .Index(x => new { x.InstitutionId, x.BranchCode, x.AcademicPeriodId, x.EducationType },
                x =>
                {
                    x.IsUnique = true;
                    x.Name = "idx_branch_student_count_unique";
                });

        // BranchWorkloadConfig (alan bazlı ders yükü havuzu)
        options.Schema.For<BranchWorkloadConfig>().DatabaseSchemaName("coordination");
        options.Schema.For<BranchWorkloadConfig>().Index(x => x.InstitutionId);
        options.Schema.For<BranchWorkloadConfig>().Index(x => x.BranchCode);
        options.Schema.For<BranchWorkloadConfig>().Index(x => x.AcademicPeriodId);
        options.Schema.For<BranchWorkloadConfig>()
            .Index(x => new { x.InstitutionId, x.BranchCode, x.AcademicPeriodId },
                x =>
                {
                    x.IsUnique = true;
                    x.Name = "idx_branch_workload_config_unique";
                });

        // WeeklyVisitPlan (haftalık ziyaret planı)
        options.Schema.For<WeeklyVisitPlan>().DatabaseSchemaName("coordination");
        options.Schema.For<WeeklyVisitPlan>().Index(x => x.InstitutionId);
        options.Schema.For<WeeklyVisitPlan>().Index(x => x.AcademicPeriodId);
        options.Schema.For<WeeklyVisitPlan>()
            .Index(x => new { x.InstitutionId, x.AcademicPeriodId, x.WeekNumber },
                x =>
                {
                    x.IsUnique = false;
                    x.Name = "idx_visit_plan_inst_period_week";
                });

        // WeeklyVisitAssignment (tekil ziyaret kaydı — QR kod kaynağı)
        options.Schema.For<WeeklyVisitAssignment>().DatabaseSchemaName("coordination");
        options.Schema.For<WeeklyVisitAssignment>().Index(x => x.PlanId);
        options.Schema.For<WeeklyVisitAssignment>().Index(x => x.TeacherId);
        options.Schema.For<WeeklyVisitAssignment>().Index(x => x.BusinessId);
        options.Schema.For<WeeklyVisitAssignment>().Index(x => x.InstitutionId);

        // AcademicPeriodView (cross-module read model)
        options.Schema.For<AcademicPeriodView>().DatabaseSchemaName("coordination");
        options.Schema.For<AcademicPeriodView>().Index(x => x.InstitutionId);

        // InstitutionView (Institution modülünden InstitutionUpdated event'i ile beslenir)
        options.Schema.For<InstitutionView>().DatabaseSchemaName("coordination");

        // UserNameView (Security.UserDisplayNameUpserted ile beslenir) — denetim alanları
        // yalnız kullanıcı kimliğini saklar, ad sorgu tarafında buradan çözülür (#137)
        options.Schema.For<UserNameView>().DatabaseSchemaName("coordination");
    }
}
