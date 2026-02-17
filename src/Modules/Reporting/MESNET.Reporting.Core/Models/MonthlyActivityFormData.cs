namespace MESNET.Reporting.Core.Models;

/// <summary>
/// Form 2: Aylık Eğitim Faaliyeti Formu verileri
/// </summary>
public sealed class MonthlyActivityFormData
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
    /// Günlük faaliyetler (gün numarası → açıklama)
    /// </summary>
    public List<DailyActivityEntry> Activities { get; init; } = [];

    public string? InstructorComment { get; init; }
    public string? TeacherComment { get; init; }
    public required string MasterInstructorName { get; init; }
    public required string CoordinatorTeacherName { get; init; }
}

public sealed record DailyActivityEntry(int DayNumber, string Description);
