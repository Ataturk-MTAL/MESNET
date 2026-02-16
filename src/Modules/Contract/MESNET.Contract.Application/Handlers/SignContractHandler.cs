using MESNET.Common.Shared;
using MESNET.Contract.Application.Commands;
using MESNET.Contract.Application.Errors;
using MESNET.Contract.Core.Aggregates;
using MESNET.Contract.Core.Enums;
using MESNET.Contract.Shared.Events;
using Wolverine.Marten;

namespace MESNET.Contract.Application.Handlers;

public static class SignContractHandler
{
    [AggregateHandler]
    public static Result<object> Handle(SignContract command, InternshipContract contract)
    {
        if (contract.Status != ContractStatus.AwaitingSignature)
            return Result<object>.Failure(
                ContractErrors.InvalidStatus(contract.Id, contract.Status.Slug, "İmzalama için sözleşme imza bekliyor durumunda olmalı."));

        if (!SignerParty.TryFromName(command.Party, true, out var party))
            return Result<object>.Failure(
                new Error("UNKNOWN_SIGNER_PARTY", $"Bilinmeyen imzalayan taraf: {command.Party}."));

        return party.Name switch
        {
            nameof(SignerParty.Institution) => contract.InstitutionSignature.IsSigned
                ? Result<object>.Failure(new Error("ALREADY_SIGNED", "Kurum zaten imzalamış."))
                : Result<object>.Success((object)new ContractSignedByInstitution(contract.Id, command.SignedBy, DateTime.UtcNow)),

            nameof(SignerParty.Business) => contract.BusinessSignature.IsSigned
                ? Result<object>.Failure(new Error("ALREADY_SIGNED", "İşletme zaten imzalamış."))
                : Result<object>.Success((object)new ContractSignedByBusiness(contract.Id, command.SignedBy, DateTime.UtcNow)),

            nameof(SignerParty.Student) => contract.StudentSignature.IsSigned
                ? Result<object>.Failure(new Error("ALREADY_SIGNED", "Öğrenci zaten imzalamış."))
                : Result<object>.Success((object)new ContractSignedByStudent(contract.Id, command.SignedBy, DateTime.UtcNow)),

            _ => Result<object>.Failure(new Error("UNKNOWN_SIGNER_PARTY", $"Bilinmeyen imzalayan taraf: {command.Party}."))
        };
    }
}
