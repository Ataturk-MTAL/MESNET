using MESNET.Security.Core.Enums;

namespace MESNET.Security.Core.Entities;

/// <summary>
/// Kullanıcı davet kaydı.
/// Davet → Onay → Kayıt akışını yönetir.
/// Id aynı zamanda davet token'ı olarak kullanılır.
/// </summary>
public class UserInvitation
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string FullName => $"{FirstName} {LastName}";
    public required string TargetRole { get; set; }
    public Guid? InstitutionId { get; set; }
    public Guid? BusinessId { get; set; }
    private InvitationStatus _status = InvitationStatus.PendingApproval;
    public InvitationStatus Status
    {
        get => _status;
        set { _status = value; StatusName = value.Name; }
    }

    // SmartEnum LINQ tuzağı: Status JSON'a düz string serialize edilir; sorgular için düz string kopya.
    public string StatusName { get; private set; } = InvitationStatus.PendingApproval.Name;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public string? CreatedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectedByName { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public Guid? CreatedUserAccountId { get; set; }

    /// <summary>
    /// Rolün ihtiyacına göre ek bilgiler.
    /// Teacher: BranchCode, StaffRole
    /// Student: BranchCode, BranchName, ClassYear, TcKimlikNo
    /// CompanyManager: BusinessName, Phone
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = [];
}
