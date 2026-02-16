using System.Text.Json.Serialization;
using Ardalis.SmartEnum.SystemTextJson;
using MESNET.Institution.Core.Enums;

namespace MESNET.Institution.Core.ValueObjects;

public sealed record StaffMember
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string KeycloakId { get; init; }
    public required string FullName { get; init; }

    [JsonConverter(typeof(SmartEnumNameConverter<StaffRole, int>))]
    public required StaffRole Role { get; init; }

    public string? BranchCode { get; init; }
    public DateTime AuthorizedAt { get; init; } = DateTime.UtcNow;
}
