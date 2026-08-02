using Marten;
using MESNET.Common.Infrastructure.Pagination;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Pagination;
using MESNET.Common.Shared.Security;
using MESNET.Security.Application.Commands;
using MESNET.Security.Application.Errors;
using MESNET.Security.Core.Entities;

namespace MESNET.Security.Application.Handlers;

/// <param name="BranchCodes">Kullanıcının sorumlu olduğu alan kodları (#126). Boş olabilir.</param>
/// <param name="BranchRequired">
/// Bu kullanıcı için alan girilmesi zorunlu mu? Permission'dan türetilir, rol adından değil.
/// <c>false</c> ise boş <paramref name="BranchCodes"/> beklenen normal durumdur (müdür, müdür yrd.).
/// </param>
/// <param name="BranchMissing">
/// Alan zorunlu ama girilmemiş — arayüzde "branş atanmamış" rozetiyle gösterilir.
/// Bu kullanıcı hiçbir alana yazamaz; idare elle alan girmelidir.
/// </param>
public sealed record UserAccountDto(
    Guid Id, string KeycloakUserId, string Username, string Email,
    string FirstName, string LastName, string FullName,
    bool IsEnabled, Guid? InstitutionId, Guid? BusinessId,
    List<string> Roles, List<string> DirectPermissions,
    DateTime CreatedAt, DateTime? UpdatedAt,
    List<string> BranchCodes, bool BranchRequired, bool BranchMissing,
    /// <summary>
    /// Velinin bağlı olduğu öğrenciler (#174). Boş olması normaldir — veli olmayan her
    /// kullanıcıda boştur. Kapsam kararının kaynağıdır; izin değildir.
    /// </summary>
    List<Guid> LinkedStudentIds);

public static class GetUserAccountsHandler
{
    public static async Task<PagedResult<UserAccountDto>> Handle(
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

        queryable = queryable.ApplySearch(query.Search, u => u.FullName, u => u.Email);
        queryable = queryable.ApplySort(query.SortBy, query.Descending, defaultSort: u => u.FullName);

        // "Branş atanmamış" filtresi (#126): karar permission'dan türetilir ve rol listesine
        // bağlıdır — SQL'e çevrilemez, bu yüzden in-memory uygulanır. Sayfalama bu yüzden
        // filtre uygulandığında bellek üzerinden yapılır.
        if (query.MissingBranchOnly == true)
        {
            var all = await queryable.ToListAsync();
            var missing = all.Where(IsBranchMissing).ToList();

            var items = missing
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(ToDto)
                .ToList();

            return new PagedResult<UserAccountDto>
            {
                Items = items,
                TotalCount = missing.Count,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        return await queryable.ToPagedResultAsync(query, ToDto);
    }

    internal static bool IsBranchMissing(UserAccount a) =>
        BranchRequirement.IsRequiredForRoles(a.Roles) && a.BranchCodes.Count == 0;

    internal static UserAccountDto ToDto(UserAccount a)
    {
        var required = BranchRequirement.IsRequiredForRoles(a.Roles);

        return new UserAccountDto(
            a.Id, a.KeycloakUserId, a.Username, a.Email,
            a.FirstName, a.LastName, a.FullName,
            a.IsEnabled, a.InstitutionId, a.BusinessId,
            a.Roles, a.DirectPermissions,
            a.CreatedAt, a.UpdatedAt,
            a.BranchCodes, required, required && a.BranchCodes.Count == 0,
            a.LinkedStudentIds);
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

        return GetUserAccountsHandler.ToDto(account);
    }
}
