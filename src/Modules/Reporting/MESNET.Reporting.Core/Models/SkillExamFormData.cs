namespace MESNET.Reporting.Core.Models;

/// <summary>
/// Form 5: Dönem Sonu Beceri Sınavı Not Fişi verileri
/// </summary>
public sealed class SkillExamFormData
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
    public required string Semester { get; init; }
    public DateTime ExamDate { get; init; }
    public int Score { get; init; }
    public required string Result { get; init; }

    public List<ExamCriterionEntry> Criteria { get; init; } = [];
    public List<CommitteeMemberEntry> CommitteeMembers { get; init; } = [];
}

public sealed record ExamCriterionEntry(string Name, int MaxScore, int Score);
public sealed record CommitteeMemberEntry(string FullName, string Title);
