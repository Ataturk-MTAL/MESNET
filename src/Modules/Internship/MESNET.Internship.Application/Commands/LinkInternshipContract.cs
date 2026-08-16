namespace MESNET.Internship.Application.Commands;

/// <summary>
/// Sözleşme staja bağlanır (#248). <c>ContractActivated</c>'ın saga'ya çevrilmiş hâli.
///
/// <para><b>Alan adı <c>InternshipId</c> olmak ZORUNDA:</b> Wolverine saga kimliğini bu addan
/// çözer (<c>InternshipSaga</c> → <c>Internship</c> + <c>Id</c>). Yeniden adlandırmak saga'yı
/// sessizce ölü mektup kuyruğuna düşürür — hata değil, sessizlik üretir.</para>
/// </summary>
public sealed record LinkInternshipContract(Guid InternshipId, Guid ContractId);
