namespace MESNET.Enrollment.Shared.Events;

public sealed record StudentRegistered(
    Guid StudentId,
    string FullName,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    string BranchCode,
    int ClassYear,
    string EducationType,
    string StudentNumber = "",
    // 3308 Madde 25: %50 oranı yalnız kalfalık yeterliğini kazanan MESEM 12. sınıf
    // öğrencilerine uygulanır. Payment bu bilgiyi başka modülün şemasından okuyamaz (#83).
    bool HasJourneymanQualification = false);
