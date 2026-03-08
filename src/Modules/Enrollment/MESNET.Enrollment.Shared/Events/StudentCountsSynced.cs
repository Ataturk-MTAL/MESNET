namespace MESNET.Enrollment.Shared.Events;

/// <summary>
/// Mevcut öğrencilerin alan/sınıf bazında toplam sayılarını içerir.
/// Coordination modülü bu event'i consume ederek BranchStudentCountView'u sıfırdan günceller.
/// </summary>
public sealed record StudentCountsSynced(
    Guid InstitutionId,
    Guid AcademicPeriodId,
    string BranchCode,
    string EducationType,
    Dictionary<int, int> CountsByClassYear);
