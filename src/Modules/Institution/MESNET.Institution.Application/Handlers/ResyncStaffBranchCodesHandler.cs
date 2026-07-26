using Marten;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Shared.Events;
using Wolverine;

namespace MESNET.Institution.Application.Handlers;

/// <summary>
/// Mevcut personel kayıtlarındaki alan bilgisini <see cref="StaffAuthorized"/> olarak
/// yeniden yayınlar (#126). Security modülü olayı tüketip kullanıcı kaydının alan
/// kapsamını doldurur — modüller arası doğrudan veri yazma yoktur.
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

                // Alanı olmayan personel (müdür, müdür yrd.) ATLANIR — eksik veri değildir.
                if (string.IsNullOrWhiteSpace(staff.BranchCode))
                {
                    noBranch++;
                    continue;
                }

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
