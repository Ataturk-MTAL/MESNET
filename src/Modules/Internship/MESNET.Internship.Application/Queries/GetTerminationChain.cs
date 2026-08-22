namespace MESNET.Internship.Application.Queries;

/// <summary>
/// Fesih onay zincirinin durumunu okur (#191).
///
/// <para>Zincir <b>saga state'inde</b> yaşar; <c>InternshipSummary</c> read-model'inde karşılığı
/// yoktur. Bu yüzden sorgu saga belgesini yükler — aynı modül içi olduğu için şema izolasyonu
/// ihlal edilmez.</para>
/// </summary>
public sealed record GetTerminationChain(Guid InternshipId);
