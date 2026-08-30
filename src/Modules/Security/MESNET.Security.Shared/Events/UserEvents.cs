namespace MESNET.Security.Shared.Events;

/// <summary>
/// Bir kullanıcının <b>görüntülenecek adı</b> yazıldı ya da değişti (#137).
///
/// <para>Denetim alanları artık yalnız kullanıcı <b>kimliğini</b> saklar; ad değişebilir,
/// kimlik değişmez. Adı gösterebilmek için her modül bu olayı dinleyip kendi şemasındaki
/// <c>UserNameView</c> read-model'ini besler — başka modülün şemasına sorgu atmak yasaktır.</para>
///
/// <para><paramref name="UserId"/> Keycloak kullanıcı kimliğidir; token'daki <c>sub</c> claim'i
/// ve <c>ICurrentUserService.GetUserId()</c> ile aynı değerdir. Kimliği Guid olarak
/// ayrıştırılamayan hesaplar için bu olay hiç yayınlanmaz.</para>
/// </summary>
public sealed record UserDisplayNameUpserted(
    Guid UserId,
    string FullName);

public sealed record UserCreated(
    Guid UserAccountId,
    string KeycloakUserId,
    string Username,
    string FullName,
    string Email,
    IReadOnlyList<string> Roles,
    Guid? InstitutionId,
    Guid? BusinessId,
    Dictionary<string, string> Metadata);

public sealed record UserUpdated(
    Guid UserAccountId,
    string KeycloakUserId,
    string FullName,
    string Email);

public sealed record UserRolesChanged(
    Guid UserAccountId,
    string KeycloakUserId,
    IReadOnlyList<string> PreviousRoles,
    IReadOnlyList<string> NewRoles);

public sealed record UserPermissionsChanged(
    Guid UserAccountId,
    string KeycloakUserId,
    IReadOnlyList<string> DirectPermissions);

/// <summary>
/// Kullanıcının kurum (kiracı) bağı değişti (ADR-0003 adım 2). <c>null</c> geçerli bir
/// sonuçtur — bağ çözülmüş, kullanıcı kurum kapsamsız kalmıştır.
/// </summary>
public sealed record UserInstitutionChanged(
    Guid UserAccountId,
    string KeycloakUserId,
    Guid? PreviousInstitutionId,
    Guid? InstitutionId);

/// <summary>
/// Kullanıcının işletme kapsamı değişti (#229). <c>null</c> geçerli bir sonuçtur — bağ
/// çözülmüş, kullanıcı işletme kapsamsız kalmıştır.
/// </summary>
public sealed record UserBusinessChanged(
    Guid UserAccountId,
    string KeycloakUserId,
    Guid? PreviousBusinessId,
    Guid? BusinessId);

/// <summary>Kullanıcının alan (branş) kapsamı değişti (#126). Boş liste geçerli bir sonuçtur.</summary>
public sealed record UserBranchesChanged(
    Guid UserAccountId,
    string KeycloakUserId,
    IReadOnlyList<string> PreviousBranchCodes,
    IReadOnlyList<string> NewBranchCodes);

/// <summary>
/// Velinin bağlı olduğu öğrenciler değişti (#174). Kapsam kaydı değiştiği için permission
/// cache'i geçersiz kılınır — aksi hâlde eski kapsam cache süresi boyunca geçerli kalırdı.
/// </summary>
public sealed record UserStudentsChanged(
    Guid UserAccountId,
    string KeycloakUserId,
    IReadOnlyList<Guid> PreviousStudentIds,
    IReadOnlyList<Guid> StudentIds);

public sealed record UserActivated(
    Guid UserAccountId,
    string KeycloakUserId);

public sealed record UserDeactivated(
    Guid UserAccountId,
    string KeycloakUserId,
    string Reason);

public sealed record UserDeleted(
    Guid UserAccountId,
    string KeycloakUserId);

/// <summary>
/// Bir kullanıcı hesabının yeniden yayınlanan (replay) anlık görüntüsü (D2, I-2).
///
/// <para><b>Bu bir OLUŞTURMA olayı DEĞİLDİR</b> ve hiçbir tüketici onu kayıt <b>yaratmak</b>
/// için kullanmamalıdır — yalnız daha önce var olan (ör. <c>UserCreated</c> ile kurulmuş) bir
/// yerel görünüm satırını eksiksiz olarak yeniden yazar. Amacı yalnız başka modüllerin yerel
/// görünümlerini geriye dönük onarmaktır.</para>
///
/// <para><b>Neden <c>UserCreated</c> değil (I-2):</b> <c>UserCreated</c>'ın diğer tüketicileri
/// (Business temsilci, Enrollment öğrenci/öğretmen, Institution personel kaydı) onu "yeni
/// kayıt" sanır; boş <c>Metadata</c> ile yeniden yayınlamak silinmiş bir öğrenci profilini boş
/// branş alanları ve <c>ClassYear = 0</c> ile, silinmiş bir personel kaydını boş branş koduyla
/// diriltir ve kaldırılmış bir işletme temsilcisini yeniden ekler; ayrıca Enrollment'ın
/// <c>Guid.Parse(KeycloakUserId)</c> çağrısı boş Keycloak id'sinde patlayıp mesajı
/// dead-letter'a düşürür. Bu olayı yalnız Institution modülünün
/// <c>InstitutionManagerLinkConsumer</c>'ı dinler.</para>
///
/// <para><b>İdempotenttir:</b> tüketici satırı mutlak olarak yazar — artırma/azaltma yoktur,
/// aynı olay kaç kez işlenirse işlensin sonuç aynıdır.</para>
///
/// <para><paramref name="IsEnabled"/> etkinlik durumunu <b>olayın kendisiyle</b> taşır — ayrı
/// bir <c>UserDeactivated</c> yayınına gerek yoktur, bu yüzden iki olay arasında sıra
/// garantisi aranmaz.</para>
/// </summary>
public sealed record UserAccountReplayed(
    Guid UserAccountId,
    string KeycloakUserId,
    IReadOnlyList<string> Roles,
    Guid? InstitutionId,
    bool IsEnabled);
