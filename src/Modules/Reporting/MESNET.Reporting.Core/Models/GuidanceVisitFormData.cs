namespace MESNET.Reporting.Core.Models;

/// <summary>
/// Form 3: Günlük Rehberlik Formu verileri
/// </summary>
public sealed class GuidanceVisitFormData
{
    public Guid DocumentId { get; init; } = Guid.NewGuid();

    // İlişkili entity ID'leri (GeneratedDocument'a kopyalanır)
    public Guid? BusinessId { get; init; }
    public Guid? InstitutionId { get; init; }
    public Guid? TeacherId { get; init; }

    public required string InstitutionName { get; init; }
    public required string TeacherName { get; init; }
    public required string BusinessName { get; init; }
    public required string BusinessAddress { get; init; }
    public DateTime VisitDate { get; init; }

    public List<StudentVisitEntry> StudentNotes { get; init; } = [];

    public string? InstructorMeetingNotes { get; init; }
    public string? IssuesIdentified { get; init; }
    public string? ActionsTaken { get; init; }
    public string? GeneralAssessment { get; init; }
}

public sealed record StudentVisitEntry(string StudentName, string PerformanceNote);
