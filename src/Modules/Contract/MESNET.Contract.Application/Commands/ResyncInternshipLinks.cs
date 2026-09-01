namespace MESNET.Contract.Application.Commands;

/// <summary>Aktif sözleşmeleri staj saga'sına yeniden bağlar (#248). Bkz. handler.</summary>
public sealed record ResyncInternshipLinks;

/// <param name="Republished">Yeniden yayınlanan <c>ContractActivated</c> sayısı.</param>
/// <param name="SkippedNonActive">Atlanan sözleşme sayısı (aktif olmayan).</param>
/// <param name="TenantsProcessed">
/// Dolaşılan kiracı (okul) sayısı (#292). <b>Sıfır kiracı, sıfır sözleşmeden farklıdır</b> —
/// biri "yayınlanacak bir şey yoktu", diğeri "hiçbir okul bulunamadı". Alan olmasaydı iki durum
/// da aynı boş yanıtla dönerdi.
/// </param>
public sealed record ResyncInternshipLinksResult(
    int Republished, int SkippedNonActive, int TenantsProcessed);
