using MESNET.Common.Shared.Security;
using MESNET.Reporting.Core.Models;

namespace MESNET.Reporting.Application.Commands;

// ─── Form 1: Staj Sözleşmesi ───
public sealed record GenerateInternshipContractDocument(InternshipContractFormData Data, UserContext User);

// ─── Form 2: Aylık Eğitim Faaliyeti Formu ───
public sealed record GenerateMonthlyActivityDocument(MonthlyActivityFormData Data, UserContext User);

// ─── Form 3: Günlük Rehberlik Formu ───
public sealed record GenerateGuidanceVisitDocument(GuidanceVisitFormData Data, UserContext User);

// ─── Form 4: Devamsızlık Çizelgesi ───
public sealed record GenerateAttendanceSheetDocument(AttendanceSheetData Data, UserContext User);

// ─── Form 5: Beceri Sınavı Not Fişi ───
public sealed record GenerateSkillExamDocument(SkillExamFormData Data, UserContext User);

// ─── Form 6: İşletme Değerlendirme Formu ───
public sealed record GenerateBusinessEvaluationDocument(BusinessEvaluationFormData Data, UserContext User);
