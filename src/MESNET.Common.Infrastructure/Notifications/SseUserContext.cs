namespace MESNET.Common.Infrastructure.Notifications;

/// <summary>
/// SSE bağlantısı açan kullanıcının JWT claim bilgileri.
/// Bağlantı açılırken parse edilir ve Channel ile ilişkilendirilir.
/// </summary>
public sealed record SseUserContext(
    Guid UserId,
    string FullName,
    Guid? InstitutionId,
    Guid? BusinessId,
    Guid? StudentId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    /// <summary>
    /// Velinin bağlı olduğu öğrenciler — <c>linked_student_ids</c> claim'i (#174).
    ///
    /// <para><b>Sona ve varsayılanlı eklendi (#247):</b> bu alandan önce yazılmış çağrılar
    /// bozulmadan derlenir. Alan yokken <c>NotificationTarget.StudentIds</c> yalnız öğrencinin
    /// KENDİSİYLE eşleşiyordu; veliye gönderilen hiçbir bildirim ulaşmıyordu ve bu
    /// <b>sessizdi</b> — hedef kitle boş çıkınca servis yalnız <c>LogDebug</c> yazar.</para>
    /// </summary>
    IReadOnlyList<Guid>? LinkedStudentIds = null)
{
    /// <summary>Veli bağı — null-güvenli okuma.</summary>
    public IReadOnlyList<Guid> LinkedStudents => LinkedStudentIds ?? [];
}
