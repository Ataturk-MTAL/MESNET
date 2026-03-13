namespace MESNET.Reporting.Core.Models;

/// <summary>
/// Form 3: Günlük Rehberlik Görev Formu verileri
/// MEB standardı — koordinatör öğretmenin işletme ziyaret raporu
/// </summary>
public sealed class GuidanceVisitFormData
{
    public Guid DocumentId { get; init; } = Guid.NewGuid();

    // İlişkili entity ID'leri (GeneratedDocument'a kopyalanır)
    public Guid? BusinessId { get; init; }
    public Guid? InstitutionId { get; init; }
    public Guid? TeacherId { get; init; }

    // Form üst bilgi alanları
    public required string BusinessName { get; init; }
    public int StudentCount { get; init; }
    public required string BranchName { get; init; }
    public DateTime VisitDate { get; init; }

    // İmza alanları
    public required string TeacherName { get; init; }
    public string? BusinessContactName { get; init; }
    public string? VicePrincipalName { get; init; }

    // Serbest metin alanları (öğretmen yazdırıp elle dolduracak)
    public string? NegativeFactors { get; init; }
    public string? GuidanceActions { get; init; }
    public string? ReportNotes { get; init; }

    // Eski alanlar — geriye uyumluluk (JSON deserialize için)
    public string? InstitutionName { get; init; }
    public string? BusinessAddress { get; init; }
    public List<StudentVisitEntry> StudentNotes { get; init; } = [];
    public string? InstructorMeetingNotes { get; init; }
    public string? IssuesIdentified { get; init; }
    public string? ActionsTaken { get; init; }
    public string? GeneralAssessment { get; init; }
}

public sealed record StudentVisitEntry(string StudentName, string PerformanceNote);
