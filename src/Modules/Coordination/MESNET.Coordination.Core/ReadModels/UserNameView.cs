namespace MESNET.Coordination.Core.ReadModels;

/// <summary>
/// Kullanıcı kimliği → görüntülenecek ad (#137).
///
/// <para>Denetim alanları (<c>UpdatedById</c>, <c>AssignedById</c> vb.) yalnız token'dan
/// gelen kullanıcı kimliğini saklar; serbest metin ad SAKLANMAZ — aksi hâlde işlemi yapan
/// istemci denetim satırındaki aktörü kendisi yazardı. Ad, sorgu tarafında bu view'dan
/// çözülür.</para>
///
/// <para>View salt <c>Security.UserDisplayNameUpserted</c> olayıyla beslenir; Security
/// şemasına doğrudan sorgu atılmaz (modüller arası şema izolasyonu).</para>
/// </summary>
public class UserNameView
{
    /// <summary>Keycloak kullanıcı kimliği (token <c>sub</c> claim'i).</summary>
    public Guid Id { get; set; }

    public string FullName { get; set; } = default!;
}
