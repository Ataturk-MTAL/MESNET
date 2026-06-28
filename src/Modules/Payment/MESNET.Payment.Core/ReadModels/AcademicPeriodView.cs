namespace MESNET.Payment.Core.ReadModels;

/// <summary>
/// Institution modülünden AcademicPeriodCreated/Closed event'leri ile beslenen read model.
/// Kapalı dönem yazma engellemesi için kullanılır (modüller arası doğrudan DB erişimi YASAK).
/// </summary>
public class AcademicPeriodView
{
    public Guid Id { get; set; }
    public Guid InstitutionId { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public DateTime? ClosedAt { get; set; }
}
