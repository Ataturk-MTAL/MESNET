using Marten;
using MESNET.Common.Shared;
using MESNET.Security.Application.Errors;
using MESNET.Security.Core.Policies;
using MESNET.Security.Core.ReadModels;

namespace MESNET.Security.Application.Services;

/// <summary>
/// Veli–öğrenci bağının kiracı kontrolü — karar
/// <see cref="GuardianLinkScopePolicy"/>'de, veri toplama burada (#271).
///
/// <para><b>Kiracı süzgeci sorgunun kendisindedir:</b> <c>GuardianLinkView</c> kiracı damgalıdır,
/// yani istek bağlamındaki okulun öğrencileri dışında hiçbir satır dönmez. Ayrıca
/// <c>InstitutionId</c> karşılaştırması yapmaya gerek yok — damga zaten süzüyor.</para>
/// </summary>
public static class GuardianLinkScopeGuard
{
    /// <summary>
    /// İstenen öğrencilerin hepsi kiracıya ait mi; değilse <c>DomainException</c> (422).
    /// </summary>
    public static async Task EnsureInScopeAsync(
        IQuerySession session, IReadOnlyList<Guid> studentIds, CancellationToken ct = default)
    {
        if (studentIds.Count == 0) return;

        var known = await session.Query<GuardianLinkView>()
            .Where(v => studentIds.Contains(v.Id))
            .Select(v => v.Id)
            .ToListAsync(ct);

        var outOfScope = GuardianLinkScopePolicy.FindOutOfScope(studentIds, known.ToHashSet());
        if (outOfScope.Count == 0) return;

        throw new DomainException(SecurityErrors.GuardianLinkOutOfScope(outOfScope));
    }
}
