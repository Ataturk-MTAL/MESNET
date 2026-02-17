namespace MESNET.Security.Core.Entities;

/// <summary>
/// Keycloak kullanıcısının yerel gölge kopyası.
/// Document storage — CRUD ağırlıklı, event sourcing kullanılmaz.
/// </summary>
public class UserAccount
{
    public Guid Id { get; set; }
    public required string KeycloakUserId { get; init; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string FullName => $"{FirstName} {LastName}";
    public bool IsEnabled { get; set; } = true;
    public Guid? InstitutionId { get; set; }
    public Guid? BusinessId { get; set; }
    public Guid? StudentId { get; set; }
    public List<string> Roles { get; set; } = [];
    public List<string> DirectPermissions { get; set; } = [];
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
