namespace MESNET.Contract.Core.ValueObjects;

public sealed record SignatureStatus(bool IsSigned, string? SignedBy, DateTime? SignedAt)
{
    public static SignatureStatus Unsigned => new(false, null, null);

    public static SignatureStatus Sign(string signedBy) => new(true, signedBy, DateTime.UtcNow);
}
