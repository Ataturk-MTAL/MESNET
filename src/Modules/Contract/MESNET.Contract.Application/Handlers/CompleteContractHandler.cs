using MESNET.Common.Shared;
using MESNET.Contract.Application.Commands;
using MESNET.Contract.Core.Aggregates;
using MESNET.Contract.Core.Enums;
using MESNET.Contract.Shared.Events;
using Wolverine;
using Wolverine.Marten;

namespace MESNET.Contract.Application.Handlers;

public static class CompleteContractHandler
{
    /// <summary>
    /// Olay hem <b>akışa yazılır</b> hem <b>mesaj olarak yayınlanır</b> (#253).
    ///
    /// <para><b>Bare dönüş YETMEZ:</b> <c>[AggregateHandler]</c> iş akışında Wolverine.Marten
    /// handler'ın dönüş eylemini <c>EventCaptureActionSource</c> ile değiştirir ve dönen nesneyi
    /// yalnız <c>IEventStream&lt;T&gt;.AppendOne</c> ile akışa ekler — hiçbir tüketiciye
    /// yönlendirmez. Olay yönlendirmesi (<c>EventForwardingToWolverine</c>) bu projede kapalıdır
    /// ve <b>açılmamalıdır</b>: elle yayınlanan olaylar (ör. <c>AttendanceMarked</c>) çift
    /// işlenirdi.</para>
    ///
    /// <para><b>Kapanış yolu</b> — <see cref="TerminateContractHandler"/> ile aynı gerekçe;
    /// aynı üç tüketiciye gider.</para>
    /// </summary>
    [AggregateHandler]
    public static (Events, OutgoingMessages) Handle(CompleteContract command, InternshipContract? contract)
    {
        if (contract is null)
            throw new DomainException("CONTRACT_NOT_FOUND", "Sözleşme bulunamadı.");

        if (!contract.Status.CanTransitionTo(ContractStatus.Completed))
            throw new DomainException("CONTRACT_INVALID_STATUS",
                $"Sözleşme tamamlanamaz. Mevcut durum: {contract.Status.Slug}.");

        var endDate = command.EndDate ?? DateTime.UtcNow;
        var completed = new ContractCompleted(
            contract.Id, contract.StudentId, contract.BusinessId, endDate, DateTime.UtcNow);

        return (new Events { completed }, new OutgoingMessages { completed });
    }
}
