using Marten;
using MESNET.Attendance.Core.ReadModels;

namespace MESNET.Attendance.Application.Helpers;

/// <summary>
/// Denetim alanlarındaki kullanıcı kimliğini görüntülenecek ada çevirir (#137).
///
/// <para>Ad, yazma anında SAKLANMAZ — saklansaydı işlemi yapan istemci kendi denetim
/// satırındaki aktörü yazardı. Bunun yerine yazmada yalnız token'dan gelen kimlik
/// damgalanır, ad okuma anında modülün kendi şemasındaki <see cref="UserNameView"/>'ından
/// çözülür (Security şemasına sorgu atılmaz — modüller arası şema izolasyonu).</para>
///
/// <para>Ad bulunamazsa <c>null</c> döner; bu bir hata değildir. Kullanıcı silinmiş
/// olabilir ya da <c>POST /api/security/users/resync-display-names</c> henüz
/// çalıştırılmamış olabilir.</para>
/// </summary>
public static class UserNameResolver
{
    /// <summary>Verilen kimlikler için kimlik → ad sözlüğü. Boş/bilinmeyen kimlikler atlanır.</summary>
    public static async Task<IReadOnlyDictionary<Guid, string>> ResolveAsync(
        IQuerySession session,
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.Where(id => id != Guid.Empty).Distinct().ToArray();

        if (ids.Length == 0)
            return new Dictionary<Guid, string>();

        var views = await session.LoadManyAsync<UserNameView>(cancellationToken, ids);

        return views.ToDictionary(v => v.Id, v => v.FullName);
    }

    /// <summary>Tek kimlik için ad; bilinmiyorsa <c>null</c>.</summary>
    public static async Task<string?> ResolveOneAsync(
        IQuerySession session,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty) return null;

        var view = await session.LoadAsync<UserNameView>(userId, cancellationToken);
        return view?.FullName;
    }

    /// <summary>Tek kimlik için ad; bilinmiyorsa <c>null</c>.</summary>
    public static string? NameOf(this IReadOnlyDictionary<Guid, string> names, Guid? userId) =>
        userId is { } id && id != Guid.Empty && names.TryGetValue(id, out var name)
            ? name
            : null;
}
