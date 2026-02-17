namespace MESNET.Reporting.Core.Models;

/// <summary>
/// Form 4: İşletme Devamsızlık Çizelgesi verileri
/// </summary>
public sealed class AttendanceSheetData
{
    public Guid DocumentId { get; init; } = Guid.NewGuid();

    // İlişkili entity ID'leri (GeneratedDocument'a kopyalanır)
    public Guid? StudentId { get; init; }
    public Guid? BusinessId { get; init; }
    public Guid? InstitutionId { get; init; }
    public Guid? TeacherId { get; init; }

    public required string InstitutionName { get; init; }
    public required string StudentFullName { get; init; }
    public required string StudentNumber { get; init; }
    public required string BranchName { get; init; }
    public required string BusinessName { get; init; }
    public int ClassYear { get; init; }
    public required string AcademicYear { get; init; }
    public int Year { get; init; }
    public int Month { get; init; }

    /// <summary>
    /// Devamsızlık yapılan günler (1-31)
    /// </summary>
    public List<int> AbsentDays { get; init; } = [];

    /// <summary>
    /// O ayın iş günleri (1-31)
    /// </summary>
    public List<int> WorkingDays { get; init; } = [];

    public required string MasterInstructorName { get; init; }
    public required string CoordinatorTeacherName { get; init; }
}
