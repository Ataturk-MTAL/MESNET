using MESNET.Internship.Core.Enums;

namespace MESNET.Internship.Core.Entities;

public class InternshipSummary
{
    public Guid Id { get; set; }
    public Guid PlacementId { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = "";
    /// <summary>İşletme — okulda stajda null (#159).</summary>
    public Guid? BusinessId { get; set; }
    public string BusinessName { get; set; } = "";
    public Guid InstitutionId { get; set; }
    public Guid AcademicPeriodId { get; set; }
    public Guid? ContractId { get; set; }
    private InternshipPhase _phase = InternshipPhase.Placed;
    public InternshipPhase Phase
    {
        get => _phase;
        set { _phase = value; PhaseName = value.Name; }
    }

    // SmartEnum LINQ tuzağı: Phase JSON'a düz string serialize edilir; sorgular için düz string kopya.
    public string PhaseName { get; private set; } = InternshipPhase.Placed.Name;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TotalAbsenceDays { get; set; }
    public int CompletedVisits { get; set; }
    public int ConfirmedPayments { get; set; }
    public DateTime LastUpdated { get; set; }
}
