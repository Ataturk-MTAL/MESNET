namespace MESNET.Reporting.Core.Models;

/// <summary>
/// Form 6: Beceri Eğitimi İşletme Değerlendirme Formu verileri
/// </summary>
public sealed class BusinessEvaluationFormData
{
    public Guid DocumentId { get; init; } = Guid.NewGuid();

    // İlişkili entity ID'leri (GeneratedDocument'a kopyalanır)
    public Guid? BusinessId { get; init; }
    public Guid? InstitutionId { get; init; }
    public Guid? TeacherId { get; init; }

    public required string InstitutionName { get; init; }
    public required string BusinessName { get; init; }
    public required string BusinessAddress { get; init; }
    public required string BusinessPhone { get; init; }
    public required string ActivityField { get; init; }
    public required string EvaluatorName { get; init; }
    public DateTime EvaluationDate { get; init; }
    public required string Result { get; init; }
    public string? Notes { get; init; }

    public List<EvaluationCategory> Categories { get; init; } = [];
}

public sealed class EvaluationCategory
{
    public required string Name { get; init; }
    public List<EvaluationItemEntry> Items { get; init; } = [];
}

public sealed record EvaluationItemEntry(string Question, bool IsCompliant, string? Note);
