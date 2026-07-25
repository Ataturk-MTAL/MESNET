using System.Linq.Expressions;
using System.Reflection;
using Marten;
using MESNET.Common.Shared.Pagination;

namespace MESNET.Common.Infrastructure.Pagination;

public static class QueryableExtensions
{
    /// <summary>
    /// <c>sortBy</c> ile sıralanmasına izin verilen skaler tipler. Query-string'den gelen ad
    /// reflection ile eşlendiği için, izin listesi olmadan koleksiyon veya karmaşık tipteki bir
    /// alan da sıralamaya girebiliyordu; PostgreSQL bunu ORDER BY'da çeviremeyip HTTP 500
    /// üretiyordu (#65):
    /// <code>GET /api/businesses?sortBy=Sectors
    /// Npgsql.PostgresException: 22P02: malformed array literal: "[""Machinery""]"</code>
    /// SmartEnum alanları da liste dışıdır: JSON'da skaler string oldukları için sorgu patlamaz
    /// ama sıralama İngilizce <c>Name</c> değerine göre yapılır, kullanıcının gördüğü Türkçe
    /// <c>Slug</c> sırasına göre değil. Sıralama gerekiyorsa entity'deki duplicate primitive
    /// alan (<c>StatusName</c> vb.) kullanılmalıdır — bkz. CLAUDE.md SmartEnum LINQ kuralı.
    /// </summary>
    private static readonly HashSet<Type> SortableTypes =
    [
        typeof(string), typeof(bool), typeof(char), typeof(Guid),
        typeof(DateTime), typeof(DateTimeOffset), typeof(DateOnly), typeof(TimeOnly), typeof(TimeSpan),
        typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
        typeof(int), typeof(uint), typeof(long), typeof(ulong),
        typeof(decimal), typeof(double), typeof(float),
    ];

    private static bool IsSortable(PropertyInfo property)
        => property.CanRead
           && property.GetIndexParameters().Length == 0
           && SortableTypes.Contains(
               Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType);

    /// <summary>
    /// String property adına göre dinamik sıralama uygular.
    /// Marten LINQ uyumlu expression-based OrderBy üretir.
    /// sortBy null, eşleşme yok veya alan sıralanabilir tipte değilse defaultSort kullanılır.
    /// </summary>
    public static IQueryable<T> ApplySort<T>(
        this IQueryable<T> queryable,
        string? sortBy,
        bool descending,
        Expression<Func<T, object>>? defaultSort = null)
    {
        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            var property = typeof(T).GetProperties()
                .FirstOrDefault(p => p.Name.Equals(sortBy, StringComparison.OrdinalIgnoreCase)
                                     && IsSortable(p));

            if (property is not null)
            {
                var parameter = Expression.Parameter(typeof(T), "x");
                var propertyAccess = Expression.Property(parameter, property);
                var converted = Expression.Convert(propertyAccess, typeof(object));
                var lambda = Expression.Lambda<Func<T, object>>(converted, parameter);

                return descending
                    ? queryable.OrderByDescending(lambda)
                    : queryable.OrderBy(lambda);
            }
        }

        if (defaultSort is not null)
        {
            return descending
                ? queryable.OrderByDescending(defaultSort)
                : queryable.OrderBy(defaultSort);
        }

        return queryable;
    }

    /// <summary>
    /// Birden fazla string alanda OR-based case-insensitive arama uygular.
    /// Marten LINQ uyumlu: string.Contains(term, OrdinalIgnoreCase) PostgreSQL ILIKE'a çevrilir.
    /// </summary>
    public static IQueryable<T> ApplySearch<T>(
        this IQueryable<T> queryable,
        string? search,
        params Expression<Func<T, string?>>[] searchFields)
    {
        if (string.IsNullOrWhiteSpace(search) || searchFields.Length == 0)
            return queryable;

        var term = search.Trim();
        var parameter = Expression.Parameter(typeof(T), "x");

        Expression? combinedPredicate = null;

        foreach (var field in searchFields)
        {
            var body = new ParameterReplacer(field.Parameters[0], parameter).Visit(field.Body);

            // x.Field != null && x.Field.Contains(term, StringComparison.OrdinalIgnoreCase)
            var notNull = Expression.NotEqual(body, Expression.Constant(null, typeof(string)));
            var containsMethod = typeof(string).GetMethod("Contains", [typeof(string), typeof(StringComparison)])!;
            var contains = Expression.Call(
                body,
                containsMethod,
                Expression.Constant(term),
                Expression.Constant(StringComparison.OrdinalIgnoreCase));
            var safePredicate = Expression.AndAlso(notNull, contains);

            combinedPredicate = combinedPredicate is null
                ? safePredicate
                : Expression.OrElse(combinedPredicate, safePredicate);
        }

        if (combinedPredicate is null) return queryable;

        var lambda = Expression.Lambda<Func<T, bool>>(combinedPredicate, parameter);
        return queryable.Where(lambda);
    }

    /// <summary>
    /// Sayfalama (Skip/Take) uygular ve PagedResult döner.
    /// TEntity → TDto mapping ile. CountAsync + Skip/Take.
    /// Tüm Where/OrderBy'dan SONRA çağrılmalıdır.
    /// </summary>
    public static async Task<PagedResult<TDto>> ToPagedResultAsync<TEntity, TDto>(
        this IQueryable<TEntity> queryable,
        PagedQuery paging,
        Func<TEntity, TDto> mapper,
        CancellationToken ct = default)
        where TEntity : notnull
    {
        var totalCount = await queryable.CountAsync(ct);

        var items = await queryable
            .Skip(paging.Skip)
            .Take(paging.SafePageSize)
            .ToListAsync(ct);

        return PagedResult<TDto>.Create(
            items.Select(mapper).ToList(),
            totalCount,
            paging.SafePage,
            paging.SafePageSize);
    }

    /// <summary>
    /// Sayfalama — mapping gerekmediğinde (queryable zaten DTO tipindeyse).
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> queryable,
        PagedQuery paging,
        CancellationToken ct = default)
        where T : notnull
    {
        var totalCount = await queryable.CountAsync(ct);

        var items = await queryable
            .Skip(paging.Skip)
            .Take(paging.SafePageSize)
            .ToListAsync(ct);

        return PagedResult<T>.Create(
            items,
            totalCount,
            paging.SafePage,
            paging.SafePageSize);
    }

    private sealed class ParameterReplacer(
        ParameterExpression oldParam,
        ParameterExpression newParam) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == oldParam ? newParam : base.VisitParameter(node);
    }
}
