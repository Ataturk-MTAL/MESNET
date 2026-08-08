using Marten;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Internship.Application.Dtos;
using MESNET.Internship.Application.Errors;
using MESNET.Internship.Application.Extensions;
using MESNET.Internship.Application.Queries;
using MESNET.Internship.Application.Sagas;
using MESNET.Internship.Core.Policies;

namespace MESNET.Internship.Application.Handlers;

/// <summary>
/// Fesih onay zincirinin durumunu döndürür (#191).
///
/// <para><b>Kaynak saga'dır.</b> Zincir <c>InternshipSaga.ApprovalChain</c> içinde yaşıyor;
/// <c>InternshipSummary</c> read-model'inde karşılığı yok. Saga kimliği staj kimliğiyle aynıdır
/// (<c>InternshipSaga.Start</c> ürettiği id'yi <c>InternshipStarted</c> ile yayınlar).</para>
///
/// <para><b>Kapsam burada uygulanır.</b> Uç hem okul tarafına hem veri sahibine açık
/// (<c>InternshipViewOrOwn</c>); kapsam olmadan bir veli, kimliğini bildiği herhangi bir stajın
/// fesih sürecini okuyabilirdi. Merdiven <see cref="OwnDataScope"/> ile aynı — üç listede
/// tekrarlanan kararın tek yerden çözümü (#182).</para>
/// </summary>
public static class GetTerminationChainHandler
{
    public static async Task<TerminationChainStatusDto> Handle(
        GetTerminationChain query, IQuerySession session, ICurrentUserService currentUser)
    {
        var saga = await session.LoadAsync<InternshipSaga>(query.InternshipId);
        if (saga is null)
            throw new DomainException(InternshipErrors.NotFound(query.InternshipId));

        EnsureInScope(currentUser, saga.StudentId, saga.BusinessId, query.InternshipId);

        var next = TerminationChainPolicy.NextStep(saga.ApprovalChain);

        return new TerminationChainStatusDto(
            IsActive: saga.ApprovalChain is not null,
            Chain: saga.ApprovalChain?.ToDto(),
            NextStep: next is null
                ? null
                : new TerminationStepDto(next.Name, next.Slug, next.Endpoint, next.Permission),
            TerminationReason: saga.TerminationReason,
            TerminationReasonType: saga.TerminationReasonType);
    }

    /// <summary>
    /// Geniş görüntüleme izni yoksa çağıran yalnız <b>kendi</b> öğrencisinin stajını okuyabilir.
    /// Kapsam dışı istek "bulunamadı" döner — "yetkin yok" deseydik, kimliğin var olduğunu
    /// doğrulamış olurduk.
    /// </summary>
    private static void EnsureInScope(
        ICurrentUserService currentUser, Guid studentId, Guid? businessId, Guid internshipId)
    {
        // İşletme basamağı açık (#191): işletme yetkilisi kendi stajının zincirini okuyabilmeli,
        // yoksa listede görüp detayını açamaz ve kendi onay adımını yapamazdı.
        var scope = OwnDataScope.Resolve(
            currentUser, Permissions.Internship.View, includeBusinessScope: true);

        if (scope.IsUnrestricted) return;

        // Kapsamlar birleşir — öğrenci bağı YA DA işletme eşleşmesi yeter.
        if (scope.StudentIds.Contains(studentId)) return;
        if (scope.BusinessId is { } b && businessId == b) return;

        throw new DomainException(InternshipErrors.NotFound(internshipId));
    }
}
