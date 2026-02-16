namespace MESNET.Business.Core.ValueObjects;

public sealed record BusinessRepresentative
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string KeycloakId { get; init; }
    public required string FullName { get; init; }
    public required string PhoneNumber { get; init; }
    public required string Email { get; init; }
    public DateTime AuthorizedAt { get; init; } = DateTime.UtcNow;
}
