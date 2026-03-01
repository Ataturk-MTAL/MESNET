namespace MESNET.Attendance.Core.ReadModels;

/// <summary>
/// Enrollment modülünün StudentPlaced event'inden beslenen lokal read model.
/// Devamsızlık girişinde koordinatör öğretmen bilgisini çözmek için kullanılır.
/// </summary>
public class InternshipPlacementView
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid BusinessId { get; set; }
    public Guid InstitutionId { get; set; }
    public Guid? TeacherId { get; set; }
    public Guid AcademicPeriodId { get; set; }
}
