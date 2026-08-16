namespace MESNET.Contract.Application.Commands;

/// <summary>Aktif sözleşmeleri staj saga'sına yeniden bağlar (#248). Bkz. handler.</summary>
public sealed record ResyncInternshipLinks;

/// <param name="Republished">Yeniden yayınlanan <c>ContractActivated</c> sayısı.</param>
/// <param name="SkippedNonActive">Atlanan sözleşme sayısı (aktif olmayan).</param>
public sealed record ResyncInternshipLinksResult(int Republished, int SkippedNonActive);
