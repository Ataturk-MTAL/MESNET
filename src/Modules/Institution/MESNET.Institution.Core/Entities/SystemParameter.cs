namespace MESNET.Institution.Core.Entities;

public class SystemParameter
{
    public Guid Id { get; set; }
    public required string Key { get; set; }
    public required string Value { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
