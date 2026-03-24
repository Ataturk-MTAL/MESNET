using MESNET.Internship.Core.Enums;

namespace MESNET.Internship.Core.Entities;

public class InternshipSummary
{
    public Guid Id { get; set; }
    public Guid PlacementId { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = "";
    public Guid BusinessId { get; set; }
    public string BusinessName { get; set; } = "";
    public Guid InstitutionId { get; set; }
    public Guid AcademicPeriodId { get; set; }
    public Guid? ContractId { get; set; }
    public InternshipPhase Phase { get; set; } = InternshipPhase.Placed;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TotalAbsenceDays { get; set; }
    public int CompletedVisits { get; set; }
    public int ConfirmedPayments { get; set; }
    public DateTime LastUpdated { get; set; }
}
