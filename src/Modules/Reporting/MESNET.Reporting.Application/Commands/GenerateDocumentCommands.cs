using MESNET.Common.Shared.Security;
using MESNET.Reporting.Core.Models;

namespace MESNET.Reporting.Application.Commands;

// ─── Form 1: Staj Sözleşmesi ───
public sealed record GenerateInternshipContractDocument(InternshipContractFormData Data, UserContext User);

// ─── Form 2: Aylık Eğitim Faaliyeti Formu ───
public sealed record GenerateMonthlyActivityDocument(MonthlyActivityFormData Data, UserContext User);

// ─── Form 3: Günlük Rehberlik Formu (tekil) ───
public sealed record GenerateGuidanceVisitDocument(GuidanceVisitFormData Data, UserContext User);

// ─── Form 3: Günlük Rehberlik Formu (toplu — öğretmen bazlı) ───
// Bir öğretmenin tüm haftalık ziyaretleri tek PDF'te toplanır (ikişerli A5 yerleşim)
public sealed record GenerateGuidanceVisitBatchDocument(
    List<GuidanceVisitFormData> Forms, UserContext User,
    Guid? InstitutionId = null, Guid? TeacherId = null,
    string? Description = null);

// ─── Form 4: Devamsızlık Çizelgesi ───
public sealed record GenerateAttendanceSheetDocument(AttendanceSheetData Data, UserContext User);

// ─── Form 5: Beceri Sınavı Not Fişi ───
public sealed record GenerateSkillExamDocument(SkillExamFormData Data, UserContext User);

// ─── Form 6: İşletme Değerlendirme Formu ───
public sealed record GenerateBusinessEvaluationDocument(BusinessEvaluationFormData Data, UserContext User);

// ─── Form 7: Aylık Devam - Devamsızlık Bildirim Çizelgesi ───
// Anlık önizleme — işletme bazlı (PDF byte[] döner, MinIO'ya yazılmaz)
public sealed record GenerateMonthlyAttendancePreview(
    Guid InstitutionId, Guid AcademicPeriodId,
    Guid BusinessId, int Year, int Month,
    string InstitutionName, string AcademicYear);

// Arşivleme — tekil işletme (MinIO'ya yazar + GeneratedDocument kaydeder)
public sealed record GenerateMonthlyAttendanceDocument(MonthlyAttendanceReportData Data, UserContext User);

// Arşivleme — öğretmen bazlı toplu (tüm işletmeler tek PDF, her işletme = 1 sayfa)
public sealed record GenerateMonthlyAttendanceBatchDocument(
    List<MonthlyAttendanceReportData> Pages, UserContext User,
    Guid? InstitutionId = null, Guid? TeacherId = null,
    string? Description = null);

// Önizleme — tüm öğretmenlerin birleşik PDF'i, MinIO'ya yazılmaz
public sealed record GenerateMonthlyAttendanceBatchPreview(
    Guid InstitutionId, Guid AcademicPeriodId,
    int Year, int Month,
    string InstitutionName, string AcademicYear);

// ─── Form 8: Dönem Not Fişi ───
public sealed record GenerateTermGradeSlipDocument(TermGradeSlipFormData Data, UserContext User);

// Layout önizleme — örnek veriyle PDF byte[] döner (MinIO'ya yazılmaz)
public sealed record GenerateTermGradeSlipPreview;
