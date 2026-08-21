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
    /// Hedeflenen öğrenci profili ID'leri. Kullanıcının Keycloak ID'si yerine öğrenci
    /// profili ID'siyle hedefleme sağlar — bir modül, öğrencinin kullanıcı hesabını
    /// bilmeden ona bildirim gönderebilir (#69). SseUserContext.StudentId ile eşleşir.
    /// </summary>
    public IReadOnlyList<Guid>? StudentIds { get; init; }

    /// <summary>
    /// Hedeflenen öğrencilerin <b>velileri</b> (#247). Kullanıcının <c>linked_student_ids</c>
    /// bağından (#174) çözülür.
    ///
    /// <para><b>Neden <see cref="StudentIds"/>'den ayrı:</b> veli ile öğrenci ayrı
    /// hedeflenebilmeli. Md. 36 (4) veliye <b>koşulsuz</b>, öğrenciye yalnız 18 yaşını
    /// doldurmuşsa bildirim istiyor; tek boyut olsaydı 18 altı öğrenci velisinin tebligatını da
    /// görürdü.</para>
    /// </summary>
    public IReadOnlyList<Guid>? GuardianOfStudentIds { get; init; }

    /// <summary>
    /// Belirli işletmelere bağlı kullanıcılar (#247) — işletme yetkilisi, usta öğretici,
    /// işletme İK.
    ///
    /// <para><c>business_id</c> claim'i <b>sunucu tarafından</b> üretilir: token'daki değer her
    /// istekte silinip <c>UserAccount.BusinessId</c>'den yeniden yazılır (#229). Yani kullanıcı
    /// kendi hedefini belirleyemez.</para>
    /// </summary>
    public IReadOnlyList<Guid>? BusinessIds { get; init; }

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
