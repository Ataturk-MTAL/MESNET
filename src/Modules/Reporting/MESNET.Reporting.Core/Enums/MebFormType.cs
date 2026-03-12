using Ardalis.SmartEnum;

namespace MESNET.Reporting.Core.Enums;

/// <summary>
/// MEB standart form tipleri
/// </summary>
public sealed class MebFormType : SmartEnum<MebFormType>
{
    public static readonly MebFormType InternshipContract = new(nameof(InternshipContract), 1, "Beceri Eğitimi Sözleşmesi");
    public static readonly MebFormType MonthlyActivityReport = new(nameof(MonthlyActivityReport), 2, "Aylık Eğitim Faaliyeti Formu");
    public static readonly MebFormType GuidanceVisit = new(nameof(GuidanceVisit), 3, "Günlük Rehberlik Formu");
    public static readonly MebFormType AttendanceSheet = new(nameof(AttendanceSheet), 4, "Devamsızlık Çizelgesi");
    public static readonly MebFormType SkillExam = new(nameof(SkillExam), 5, "Beceri Sınavı Not Fişi");
    public static readonly MebFormType BusinessEvaluation = new(nameof(BusinessEvaluation), 6, "İşletme Değerlendirme Formu");
    public static readonly MebFormType MonthlyAttendanceReport = new(nameof(MonthlyAttendanceReport), 7, "Aylık Devamsızlık Formu");

    private MebFormType(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }

    public string Slug { get; }
}
