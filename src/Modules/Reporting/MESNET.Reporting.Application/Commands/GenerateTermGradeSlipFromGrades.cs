using MESNET.Common.Shared.Security;

namespace MESNET.Reporting.Application.Commands;

/// <summary>
/// Dönem Not Fişi'ni işletmenin gönderdiği GERÇEK notlardan üretir (manuel POST yerine).
/// İşletme-payı puanlar + öğrenci/işletme bilgisi stored read-model'lerden (StudentTermGradeView,
/// StudentPlacementReportView); okul-payı (*) + müdür/müdür yrd. adı request'ten gelir.
/// User token'dan endpoint tarafından set edilir.
/// </summary>
public sealed record GenerateTermGradeSlipFromGrades(
    Guid StudentId,
    Guid AcademicPeriodId,
    string InstitutionName,
    string AcademicYear,
    string Semester,
    int? MakeupTrainingScore,
    int? SkillCompetitionScore,
    string? VicePrincipalName,
    string? PrincipalName)
{
    public UserContext User { get; init; } = default!;
}
