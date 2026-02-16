namespace MESNET.Common.Shared;

public sealed record ApiResponse
{
    public int Code { get; init; }
    public string Type { get; init; } = default!;
    public string? Message { get; init; }
    public object? Data { get; init; }
    public object? Errors { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
