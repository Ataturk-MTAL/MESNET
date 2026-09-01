using Marten;
using MESNET.Institution.Core.ReadModels;
using MESNET.Security.Shared.Events;
using Wolverine.Configuration;
using Wolverine.Transports.Local;

namespace MESNET.Institution.Application.Consumers;

/// <summary>
/// Security kullanıcı olaylarından <see cref="InstitutionManagerLink"/> satırını besler (D2).
///
/// <para><b>Neden <c>static class</c> DEĞİL (#262):</b> kuyruk yapılandırması
/// <c>IConfigureLocalQueue</c> ile yapılıyor ve statik sınıf arayüz uygulayamaz. Metotlar
/// statik kalır; Wolverine statik handler metotlarını örnek oluşturmadan çağırır.</para>
/// </summary>
public sealed class InstitutionManagerLinkConsumer : IConfigureLocalQueue
{
    /// <summary>
    /// Bu tüketicinin yerel kuyruğu <b>sıralı</b> çalışır (#262).
    ///
    /// <para><c>MultipleHandlerBehavior.Separated</c> her handler tipine ayrı bir "sticky" yerel
    /// kuyruk verir, ama o kuyruk varsayılan olarak <b>paralel ve sırasızdır</b>. Bu sınıfın
    /// metotlarının hepsi aynı kuyruğa düşer, yani aynı kullanıcıya ait olaylar birbirini
    /// geçebilir.</para>
    ///
    /// <para><b>Kırılma:</b> <c>UserRolesChanged</c>, satırı <b>kuran</b> <c>UserCreated</c>'ı
    /// geçerse satır henüz yoktur; load-modify-store sessizce düşer ve kullanıcı yönetici
    /// sayılmaz — okul sonsuza kadar "yöneticisiz" görünür.
    /// <c>UseDurableLocalQueues()</c> dayanıklılık verir, <b>sıra vermez</b>.</para>
    /// </summary>
    public static void Configure(LocalQueueConfiguration configuration)
    {
        configuration.Sequential();
    }

    public static void Consume(UserCreated e, IDocumentSession session)
    {
        session.Store(new InstitutionManagerLink
        {
            Id = e.UserAccountId,
            InstitutionId = e.InstitutionId,
            IsEnabled = true,
            HasManagePermission = InstitutionManagerLink.HasManage(e.Roles),
            UpdatedAt = DateTime.UtcNow,
        });
    }

    public static async Task Consume(UserInstitutionChanged e, IDocumentSession session)
    {
        var link = await LoadOrCreate(e.UserAccountId, session);
        link.InstitutionId = e.InstitutionId;
        link.UpdatedAt = DateTime.UtcNow;
        session.Store(link);
    }

    public static async Task Consume(UserRolesChanged e, IDocumentSession session)
    {
        var link = await LoadOrCreate(e.UserAccountId, session);
        link.HasManagePermission = InstitutionManagerLink.HasManage(e.NewRoles);
        link.UpdatedAt = DateTime.UtcNow;
        session.Store(link);
    }

    public static async Task Consume(UserActivated e, IDocumentSession session)
    {
        var link = await LoadOrCreate(e.UserAccountId, session);
        link.IsEnabled = true;
        link.UpdatedAt = DateTime.UtcNow;
        session.Store(link);
    }

    public static async Task Consume(UserDeactivated e, IDocumentSession session)
    {
        var link = await LoadOrCreate(e.UserAccountId, session);
        link.IsEnabled = false;
        link.UpdatedAt = DateTime.UtcNow;
        session.Store(link);
    }

    public static void Consume(UserDeleted e, IDocumentSession session)
    {
        session.Delete<InstitutionManagerLink>(e.UserAccountId);
    }

    /// <summary>
    /// <c>ReplayUserAccountsHandler</c>'dan gelen anlık görüntüyü yazar. <c>UserCreated</c>
    /// tüketicisinin bir kopyası DEĞİLDİR: bu satırı <b>mutlak</b> olarak yazar — etkinlik
    /// durumu dahil olayın kendisinden gelir, ayrı bir <c>UserDeactivated</c> beklenmez.
    /// </summary>
    public static void Consume(UserAccountReplayed e, IDocumentSession session)
    {
        session.Store(new InstitutionManagerLink
        {
            Id = e.UserAccountId,
            InstitutionId = e.InstitutionId,
            IsEnabled = e.IsEnabled,
            HasManagePermission = InstitutionManagerLink.HasManage(e.Roles),
            UpdatedAt = DateTime.UtcNow,
        });
    }

    /// <summary>
    /// Satır yoksa boş bir tane kurar. <b>Sessizce vazgeçmez:</b> satırsız bir kullanıcı için
    /// güncellemeyi düşürmek, o kullanıcının bağlı olduğu okulu kalıcı olarak "yöneticisiz"
    /// gösterirdi. Eksik alanlar (kurum, roller) sonraki olayla ya da resync ile dolar.
    /// </summary>
    private static async Task<InstitutionManagerLink> LoadOrCreate(
        Guid userAccountId, IDocumentSession session) =>
        await session.LoadAsync<InstitutionManagerLink>(userAccountId)
        ?? new InstitutionManagerLink { Id = userAccountId };
}
