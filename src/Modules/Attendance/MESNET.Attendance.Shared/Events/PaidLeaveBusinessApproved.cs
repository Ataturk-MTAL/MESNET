namespace MESNET.Attendance.Shared.Events;

/// <summary>
/// İşletme ücretli izin başvurusunu onayladı (#177) — zincirin 1. adımı.
///
/// <para>Hâlâ hüküm yoktur: başvuru okul onayına geçer, devamsızlık kaydı açılmaz. Onaylayanın
/// kimliği saklanır çünkü 2. adımı <b>aynı kullanıcı</b> yapamaz — iki taraflı onayın tek tarafa
/// çökmesini engelleyen kural budur.</para>
/// </summary>
public sealed record PaidLeaveBusinessApproved(
    Guid RequestId,
    Guid StudentId,
    Guid BusinessId,
    Guid ApprovedById,
    DateTime ApprovedAt);
