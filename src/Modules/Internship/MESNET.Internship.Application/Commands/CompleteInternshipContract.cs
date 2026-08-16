namespace MESNET.Internship.Application.Commands;

/// <summary>
/// Sözleşme tamamlandı, staj kapanır (#248). <c>ContractCompleted</c>'ın saga'ya çevrilmiş hâli.
/// Alan adı kısıtı için bkz. <see cref="LinkInternshipContract"/>.
/// </summary>
public sealed record CompleteInternshipContract(Guid InternshipId);
