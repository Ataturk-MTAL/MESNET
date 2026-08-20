using MESNET.Common.Shared;
using MESNET.Contract.Application.Commands;
using MESNET.Contract.Core.Aggregates;
using MESNET.Contract.Core.Enums;
using MESNET.Contract.Shared.Events;
using Wolverine;
using Wolverine.Marten;

namespace MESNET.Contract.Application.Handlers;

public static class ActivateContractHandler
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
    /// <para><b>Ölçülen sonuç:</b> bu olay hiç teslim edilmediği için <c>SagaRelayConsumer</c>
    /// çağrılmıyor ve <c>InternshipSaga</c> <c>AwaitingContract</c>'ta çakılı kalıyordu;
    /// <c>ContractEmploymentConsumer</c> de çalışmadığı için Payment'ın
    /// <c>ContractEmploymentView</c>'ı doğmuyor ve <c>PaymentSaga</c>
    /// <c>ContractEmploymentMissing</c> fırlatarak o öğrenci için maaş dönemini hiç
    /// açmıyordu. Tek çalışan yol elden tetiklenen <c>ResyncInternshipLinksHandler</c>
    /// backfill'iydi.</para>
    /// </summary>
    [AggregateHandler]
    public static (Events, OutgoingMessages) Handle(ActivateContract command, InternshipContract? contract)
    {
        if (contract is null)
            throw new DomainException("CONTRACT_NOT_FOUND", "Sözleşme bulunamadı.");

        if (!contract.Status.CanTransitionTo(ContractStatus.Active))
            throw new DomainException("CONTRACT_INVALID_STATUS",
                $"Sözleşme aktif edilemez. Mevcut durum: {contract.Status.Slug}.");

        if (!contract.AllSignaturesComplete)
            throw new DomainException("CONTRACT_SIGNATURES_INCOMPLETE", "Tüm tarafların imzası tamamlanmalı.");

        var activated = new ContractActivated(
            contract.Id, contract.StudentId, contract.BusinessId, DateTime.UtcNow);

        return (new Events { activated }, new OutgoingMessages { activated });
    }
}
