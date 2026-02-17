namespace MESNET.Common.Infrastructure.Notifications;

/// <summary>
/// Notification'ın kimlere gönderileceğini belirler.
/// Kriterler OR mantığı ile çalışır: herhangi birine uyan kullanıcıya gönderilir.
/// </summary>
public sealed record NotificationTarget
{
    /// <summary>
    /// Doğrudan hedeflenen kullanıcı ID'leri
    /// </summary>
    public IReadOnlyList<Guid>? UserIds { get; init; }

    /// <summary>
    /// Belirli bir kuruma ait tüm kullanıcılar
    /// </summary>
    public Guid? InstitutionId { get; init; }

    /// <summary>
    /// Belirli rollere sahip kullanıcılar (OR: herhangi bir rol yeterli)
    /// </summary>
    public IReadOnlyList<string>? Roles { get; init; }

    /// <summary>
    /// Belirli permission'a sahip kullanıcılar
    /// </summary>
    public string? RequiredPermission { get; init; }
}
