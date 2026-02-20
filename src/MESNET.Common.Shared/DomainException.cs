namespace MESNET.Common.Shared;

/// <summary>
/// Domain iş kuralı ihlallerinde fırlatılan exception.
/// Wolverine middleware tarafından yakalanır ve 422 Unprocessable Entity olarak döndürülür.
/// </summary>
public sealed class DomainException : Exception
{
    public Error Error { get; }

    public DomainException(Error error) : base(error.Description)
    {
        Error = error;
    }

    public DomainException(string code, string description)
        : this(new Error(code, description))
    {
    }
}
