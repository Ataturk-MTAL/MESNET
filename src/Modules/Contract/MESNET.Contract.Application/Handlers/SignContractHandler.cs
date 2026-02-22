using MESNET.Common.Shared;
using MESNET.Contract.Application.Commands;
using MESNET.Contract.Core.Aggregates;
using MESNET.Contract.Core.Enums;
using MESNET.Contract.Shared.Events;
using Wolverine.Marten;

namespace MESNET.Contract.Application.Handlers;

public static class SignContractHandler
{
    [AggregateHandler]
    public static object Handle(SignContract command, InternshipContract contract)
    {
        if (contract.Status != ContractStatus.AwaitingSignature)
            throw new DomainException("CONTRACT_INVALID_STATUS",
                $"İmzalama için sözleşme imza bekliyor durumunda olmalı. Mevcut durum: {contract.Status.Slug}.");

        if (!SignerParty.TryFromName(command.Party, true, out var party))
            throw new DomainException("CONTRACT_UNKNOWN_SIGNER", $"Bilinmeyen imzalayan taraf: {command.Party}.");

        return party.Name switch
        {
            nameof(SignerParty.Institution) => contract.InstitutionSignature.IsSigned
                ? throw new DomainException("CONTRACT_ALREADY_SIGNED", "Kurum zaten imzalamış.")
                : (object)new ContractSignedByInstitution(contract.Id, command.SignedBy, DateTime.UtcNow),

            nameof(SignerParty.Business) => contract.BusinessSignature.IsSigned
                ? throw new DomainException("CONTRACT_ALREADY_SIGNED", "İşletme zaten imzalamış.")
                : (object)new ContractSignedByBusiness(contract.Id, command.SignedBy, DateTime.UtcNow),

            nameof(SignerParty.Student) => contract.StudentSignature.IsSigned
                ? throw new DomainException("CONTRACT_ALREADY_SIGNED", "Öğrenci zaten imzalamış.")
                : (object)new ContractSignedByStudent(contract.Id, command.SignedBy, DateTime.UtcNow),

            nameof(SignerParty.Parent) => contract.ParentSignature.IsSigned
                ? throw new DomainException("CONTRACT_ALREADY_SIGNED", "Veli zaten imzalamış.")
                : (object)new ContractSignedByParent(contract.Id, command.SignedBy, DateTime.UtcNow),

            _ => throw new DomainException("CONTRACT_UNKNOWN_SIGNER", $"Bilinmeyen imzalayan taraf: {command.Party}.")
        };
    }
}
