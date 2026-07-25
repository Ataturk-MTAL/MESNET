namespace MESNET.Enrollment.Application.Commands;

public sealed record RegisterStudent(
    Guid InstitutionId,
    Guid AcademicPeriodId,
    Guid KeycloakUserId,
    string FullName,
    string BranchCode,
    string BranchName,
    int ClassYear,
    string EducationType,
    string? Section,
    string? SpecializationCode = null,
    string? SpecializationName = null,
    string? StudentNumber = null,
    string? PhoneNumber = null,
    string? TcKimlikNo = null,
    string? GuardianName = null,
    string? GuardianPhone = null,
    // 3308 Madde 25: %50 oranının şartı — kalfalık yeterliği (#83)
    bool HasJourneymanQualification = false,
    // StudentProfile'da alan zaten vardı ama kayıt komutu doldurmuyordu; "yaşına uygun asgari
    // ücret" (3308 md.25) bu olmadan hesaplanamıyor (#85).
    DateTime? BirthDate = null,
    // Öğrenci / Aday Çırak / Çırak — StudentCategory.Name (#85)
    string Category = "Student");
