using MESNET.Common.Shared;
using MESNET.Contract.Application.Commands;
using MESNET.Contract.Core.Aggregates;
using MESNET.Contract.Core.Enums;
using MESNET.Contract.Shared.Events;
using Wolverine;
using Wolverine.Marten;

namespace MESNET.Contract.Application.Handlers;

public static class TerminateContractHandler
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
    /// <para><b>Kapanış yolu — aktivasyonla birlikte canlanmalı.</b> Bu olay
    /// <c>SagaRelayConsumer</c> ile stajı sonlandırır, <c>ContractEmploymentConsumer</c> ile
    /// istihdam penceresini kapatır ve <c>PlacementViewConsumer</c> ile yerleştirmeyi düşürür.
    /// Yalnız <c>ContractActivated</c> yayınlansaydı açan yol çalışır, kapatan yol ölü kalırdı:
    /// feshedilmiş sözleşme ücret hesabında açık görünmeye devam ederdi.</para>
    /// </summary>
    [AggregateHandler]
    public static (Events, OutgoingMessages) Handle(TerminateContract command, InternshipContract? contract)
    {
        if (contract is null)
            throw new DomainException("CONTRACT_NOT_FOUND", "Sözleşme bulunamadı.");

        if (!contract.Status.CanTransitionTo(ContractStatus.Terminated))
            throw new DomainException("CONTRACT_INVALID_STATUS",
                $"Sözleşme feshedilemez. Mevcut durum: {contract.Status.Slug}.");

        if (!TerminationReason.TryFromName(command.ReasonType, true, out _))
            throw new DomainException("CONTRACT_UNKNOWN_TERMINATION_REASON",
                $"Bilinmeyen fesih nedeni: {command.ReasonType}.");

        var endDate = command.EndDate ?? DateTime.UtcNow;
        var terminated = new ContractTerminated(
            contract.Id, contract.StudentId, contract.BusinessId,
            command.Reason, command.ReasonType, endDate, DateTime.UtcNow);

        return (new Events { terminated }, new OutgoingMessages { terminated });
    }
}
