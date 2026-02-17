using MESNET.Common.Shared;

namespace MESNET.Coordination.Application.Errors;

public static class CoordinationErrors
{
    public static Error ScheduleNotFound(Guid teacherId, int year, string semester) =>
        new("Coordination.ScheduleNotFound",
            $"Öğretmen ders programı bulunamadı: TeacherId={teacherId}, Year={year}, Semester={semester}");

    public static Error ConfigurationMissing(string message) =>
        new("Coordination.ConfigurationMissing", message);

    public static Error InvalidPeriodCount(string day, int expected, int actual) =>
        new("Coordination.InvalidPeriodCount",
            $"{day} günü için {expected} ders bekleniyor, {actual} ders girildi.");

    public static Error InvalidPeriodSequence(string day) =>
        new("Coordination.InvalidPeriodSequence",
            $"{day} günü için ders numaraları 1'den başlayarak sıralı olmalıdır.");

    public static Error SlotNotFree(string day, int periodNumber) =>
        new("Coordination.SlotNotFree",
            $"{day} günü {periodNumber}. ders saati boş değil, işletme atanamaz.");

    public static Error SlotNotFound(string day, int periodNumber) =>
        new("Coordination.SlotNotFound",
            $"{day} günü {periodNumber}. ders saati bulunamadı.");

    public static Error InvalidSemester(string semester) =>
        new("Coordination.InvalidSemester",
            $"Geçersiz dönem: {semester}. Geçerli değerler: Fall, Spring");

    public static Error InvalidDay(string day) =>
        new("Coordination.InvalidDay",
            $"Geçersiz gün: {day}. Geçerli değerler: Monday, Tuesday, Wednesday, Thursday, Friday");

    public static Error InvalidSlotStatus(string status) =>
        new("Coordination.InvalidSlotStatus",
            $"Geçersiz ders durumu: {status}. Geçerli değerler: Occupied, Free");

    // MonthlyActivityReport
    public static Error ReportNotFound(Guid reportId) =>
        new("Coordination.ReportNotFound",
            $"Aylık faaliyet raporu bulunamadı: {reportId}");

    public static Error ReportNotDraft(Guid reportId) =>
        new("Coordination.ReportNotDraft",
            $"Rapor taslak durumunda değil, düzenlenemez: {reportId}");

    public static Error ReportNotSubmitted(Guid reportId) =>
        new("Coordination.ReportNotSubmitted",
            $"Rapor gönderilmiş durumda değil, onaylanamaz: {reportId}");

    // GuidanceVisit
    public static Error VisitNotFound(Guid visitId) =>
        new("Coordination.VisitNotFound",
            $"Rehberlik ziyareti bulunamadı: {visitId}");

    public static Error VisitNotDraft(Guid visitId) =>
        new("Coordination.VisitNotDraft",
            $"Ziyaret taslak durumunda değil, düzenlenemez: {visitId}");

    public static Error VisitNotSubmitted(Guid visitId) =>
        new("Coordination.VisitNotSubmitted",
            $"Ziyaret gönderilmiş durumda değil, onaylanamaz: {visitId}");

    // SkillExam
    public static Error ExamNotFound(Guid examId) =>
        new("Coordination.ExamNotFound",
            $"Beceri sınavı bulunamadı: {examId}");

    public static Error InvalidExamResult(string result) =>
        new("Coordination.InvalidExamResult",
            $"Geçersiz sınav sonucu: {result}. Geçerli değerler: Passed, Failed");

    // BusinessEvaluation
    public static Error EvaluationNotFound(Guid evaluationId) =>
        new("Coordination.EvaluationNotFound",
            $"İşletme değerlendirmesi bulunamadı: {evaluationId}");

    public static Error InvalidEvaluationResult(string result) =>
        new("Coordination.InvalidEvaluationResult",
            $"Geçersiz değerlendirme sonucu: {result}. Geçerli değerler: Suitable, Unsuitable, Conditional");
}
