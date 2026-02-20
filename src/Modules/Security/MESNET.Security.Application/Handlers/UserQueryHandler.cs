using Marten;
using MESNET.Common.Shared;
using MESNET.Security.Application.Commands;
using MESNET.Security.Application.Errors;
using MESNET.Security.Core.Entities;

namespace MESNET.Security.Application.Handlers;

public sealed record UserAccountDto(
    Guid Id, string KeycloakUserId, string Username, string Email,
    string FirstName, string LastName, string FullName,
    bool IsEnabled, Guid? InstitutionId, Guid? BusinessId,
    List<string> Roles, List<string> DirectPermissions,
    DateTime CreatedAt, DateTime? UpdatedAt);

public static class GetUserAccountsHandler
{
    public static async Task<IReadOnlyList<UserAccountDto>> Handle(
        GetUserAccounts query, IQuerySession session)
    {
        IQueryable<UserAccount> queryable = session.Query<UserAccount>();

        if (query.InstitutionId.HasValue)
            queryable = queryable.Where(u => u.InstitutionId == query.InstitutionId.Value);

        if (query.BusinessId.HasValue)
            queryable = queryable.Where(u => u.BusinessId == query.BusinessId.Value);

        if (query.IsEnabled.HasValue)
            queryable = queryable.Where(u => u.IsEnabled == query.IsEnabled.Value);

        if (!string.IsNullOrEmpty(query.Role))
            queryable = queryable.Where(u => u.Roles.Contains(query.Role));

        var accounts = await queryable.ToListAsync();

        return accounts.Select(a => new UserAccountDto(
            a.Id, a.KeycloakUserId, a.Username, a.Email,
            a.FirstName, a.LastName, a.FullName,
            a.IsEnabled, a.InstitutionId, a.BusinessId,
            a.Roles, a.DirectPermissions,
            a.CreatedAt, a.UpdatedAt)).ToList();
    }
}

public static class GetUserAccountHandler
{
    public static async Task<UserAccountDto> Handle(
        GetUserAccount query, IQuerySession session)
    {
        var account = await session.LoadAsync<UserAccount>(query.UserAccountId);
        if (account is null)
            throw new DomainException(SecurityErrors.UserNotFound(query.UserAccountId));

        return new UserAccountDto(
            account.Id, account.KeycloakUserId, account.Username, account.Email,
            account.FirstName, account.LastName, account.FullName,
            account.IsEnabled, account.InstitutionId, account.BusinessId,
            account.Roles, account.DirectPermissions,
            account.CreatedAt, account.UpdatedAt);
    }
}
