using MESNET.Common.Shared;
using MESNET.Institution.Core.ValueObjects;

namespace MESNET.Institution.Core.Entities;

public class Institution
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public int InstitutionCode { get; set; }
    public required string FullName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? WebUrl { get; set; }
    public Location? Location { get; set; }
    public ScheduleConfiguration? ScheduleConfig { get; set; }
    public List<InstitutionBranch> Branches { get; set; } = [];
    public List<StaffMember> Staff { get; set; } = [];
}

public sealed class ScheduleConfiguration
{
    /// <summary>
    /// Günlük ders sayısı (örn: 8 ders/gün)
    /// </summary>
    public int DailyPeriodCount { get; set; }

    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}
