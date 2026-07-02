using Marten;
using MESNET.Reporting.Core.Entities;
using MESNET.Reporting.Core.ReadModels;

namespace MESNET.Reporting.Persistence;

public class ReportingMartenConfig : IConfigureMarten
{
    public void Configure(IServiceProvider services, StoreOptions options)
    {
        var schema = options.Schema.For<GeneratedDocument>();
        schema.DatabaseSchemaName("reporting");

        // Duplicate string alanlar (SmartEnum LINQ sorgularında kullanılır)
        schema.Index(x => x.FormTypeName);
        schema.Index(x => x.StatusName);
        schema.Index(x => x.GeneratedAt);

        // İlişkili entity ID'leri
        schema.Index(x => x.StudentId);
        schema.Index(x => x.BusinessId);
        schema.Index(x => x.InstitutionId);
        schema.Index(x => x.TeacherId);

        // Composite index'ler — Müdür Yardımcısı bekleyen dokümanları görmek için
        schema.Index(x => new { x.TeacherId, x.StatusName },
            x => x.Name = "idx_gendoc_teacher_status");
        schema.Index(x => new { x.InstitutionId, x.StatusName },
            x => x.Name = "idx_gendoc_inst_status");

        // ─── Reporting Read Models ───

        // StudentAttendanceReportView — öğrenci-ay bazlı devamsızlık
        options.Schema.For<StudentAttendanceReportView>().DatabaseSchemaName("reporting");
        options.Schema.For<StudentAttendanceReportView>()
            .Index(x => x.StudentId)
            .Index(x => new { x.InstitutionId, x.Year, x.Month },
                x => x.Name = "idx_att_rpt_inst_year_month");

        // StudentPlacementReportView — öğrenci-işletme eşleşmesi
        options.Schema.For<StudentPlacementReportView>().DatabaseSchemaName("reporting");
        options.Schema.For<StudentPlacementReportView>()
            .Index(x => x.StudentId)
            .Index(x => x.BusinessId)
            .Index(x => new { x.InstitutionId, x.AcademicPeriodId, x.BusinessId },
                x => x.Name = "idx_plc_rpt_inst_period_biz");

        // StudentTermGradeView — işletmenin gönderdiği dönem notları (Form 8 / Dönem Not Fişi kaynağı)
        options.Schema.For<StudentTermGradeView>().DatabaseSchemaName("reporting");
        options.Schema.For<StudentTermGradeView>()
            .Index(x => x.StudentId)
            .Index(x => new { x.StudentId, x.AcademicPeriodId },
                x => x.Name = "idx_term_grade_rpt_student_period");

        // VisitAssignmentReportView — haftalık ziyaret atamaları (Form 3 batch için)
        options.Schema.For<VisitAssignmentReportView>().DatabaseSchemaName("reporting");
        options.Schema.For<VisitAssignmentReportView>()
            .Index(x => new { x.InstitutionId, x.TeacherId },
                x => x.Name = "idx_visit_rpt_inst_teacher")
            .Index(x => x.VisitDate);

        // WorkCalendarReportView — iş takvimi
        options.Schema.For<WorkCalendarReportView>().DatabaseSchemaName("reporting");
        options.Schema.For<WorkCalendarReportView>()
            .Index(x => new { x.InstitutionId, x.Year },
                x => x.Name = "idx_cal_rpt_inst_year");
    }
}
