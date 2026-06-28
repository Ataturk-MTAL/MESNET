using Marten;
using MESNET.Institution.Application.Queries;

namespace MESNET.Institution.Application.Handlers;

public static class GetStaffBranchCodeHandler
{
    public static async Task<StaffBranchCodeResult> Handle(GetStaffBranchCode query, IQuerySession session)
    {
        var institution = await session
            .Query<Core.Entities.Institution>()
            .FirstOrDefaultAsync(i => i.Staff.Any(s => s.KeycloakId == query.KeycloakId));

        var branchCode = institution?.Staff
            .FirstOrDefault(s => s.KeycloakId == query.KeycloakId)?.BranchCode;

        return new StaffBranchCodeResult(branchCode);
    }
}
