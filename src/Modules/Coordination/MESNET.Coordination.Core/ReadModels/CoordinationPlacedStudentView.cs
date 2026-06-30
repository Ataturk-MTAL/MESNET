namespace MESNET.Coordination.Core.ReadModels;

/// <summary>
/// İşletmeye yerleştirilmiş öğrenci (Enrollment.StudentPlaced'den denormalize). İşletmenin
/// "benim öğrencilerim" listesini ve not girişinde yerleştirme doğrulamasını besler.
/// </summary>
public class CoordinationPlacedStudentView
{
    public Guid Id { get; set; }            // PlacementId
    public Guid StudentId { get; set; }
    public Guid BusinessId { get; set; }
    public Guid InstitutionId { get; set; }
    public Guid AcademicPeriodId { get; set; }
    public Guid? TeacherId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public DateTime PlacedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
