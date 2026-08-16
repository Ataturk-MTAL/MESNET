namespace MESNET.Internship.Application.Commands;

/// <summary>
/// Sözleşme feshedildi, staj feshe kapanır ve yeniden yerleştirme istenir (#248).
/// <c>ContractTerminated</c>'ın saga'ya çevrilmiş hâli.
/// Alan adı kısıtı için bkz. <see cref="LinkInternshipContract"/>.
/// </summary>
public sealed record TerminateInternshipContract(Guid InternshipId, string Reason);
