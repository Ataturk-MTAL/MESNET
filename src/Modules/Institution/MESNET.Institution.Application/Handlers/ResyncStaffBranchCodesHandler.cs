using Marten;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Shared.Events;
using Wolverine;

namespace MESNET.Institution.Application.Handlers;

/// <summary>
/// Mevcut personel kayıtlarını <see cref="StaffAuthorized"/> olarak yeniden yayınlar
/// (#126, ADR-0003 adım 2.1). Security modülü olayı tüketip kullanıcı kaydının <b>kurum
/// (kiracı anahtarı) ve alan kapsamı</b> boşluklarını doldurur — modüller arası doğrudan
/// veri yazma yoktur.
///
/// <para><b>Alanı olmayan personel de yayınlanır.</b> Eskiden atlanıyordu ve gerekçesi
/// makuldü: yayınlanacak bir alan yoktu. Ama olay <b>kurum kimliğini de</b> taşıyor ve
/// kiracı anahtarı backfill'i ona bağlı. Atlamak, tam da hiçbir alana bağlı olmayan iki rolü —
/// okul müdürü ve müdür yardımcısı — kiracı anahtarsız bırakırdı; canlıda ölçüldü.</para>
/// </summary>
public static class ResyncStaffBranchCodesHandler
{
    public static async Task<ResyncStaffBranchCodesResult> Handle(
        ResyncStaffBranchCodes command,
        IQuerySession session,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        // TODO(Faz 2): Kurum filtresi yok — tüm kurumların personeli taranıyor. Faz 1 tek
        // kurumlu olduğu için pratik etkisi yoktur; çok kurumluya geçmeden önce komut
        // InstitutionId almalı ve çağıran kullanıcının kurum kapsamıyla sınırlanmalıdır.
        var institutions = await session
            .Query<Core.Entities.Institution>()
            .ToListAsync(cancellationToken);

        int total = 0, published = 0, noBranch = 0, noKeycloakId = 0;

        foreach (var institution in institutions)
        {
            foreach (var staff in institution.Staff)
            {
                total++;

                // Alanı olmayan personel (müdür, müdür yrd.) SAYILIR ama ATLANMAZ: olayın
                // taşıdığı kurum kimliği kiracı anahtarı backfill'i için gerekli. Tüketici
                // boş branşı zaten "eksik veri değil" diye ele alıyor (#126).
                if (string.IsNullOrWhiteSpace(staff.BranchCode))
                    noBranch++;

                // Keycloak kimliği olmayan personel eşleştirilemez — yayın anlamsız.
                if (string.IsNullOrWhiteSpace(staff.KeycloakId))
                {
                    noKeycloakId++;
                    continue;
                }

                await bus.PublishAsync(new StaffAuthorized(
                    institution.Id, staff.Id, staff.Role.Name, staff.BranchCode, staff.KeycloakId));

                published++;
            }
        }

        return new ResyncStaffBranchCodesResult(total, published, noBranch, noKeycloakId);
    }
}
