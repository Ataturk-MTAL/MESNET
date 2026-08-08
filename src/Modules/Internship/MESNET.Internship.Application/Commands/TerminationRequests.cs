namespace MESNET.Internship.Application.Commands;

/// <summary>
/// <c>POST /api/internships/{id}/terminate</c> gövdesi.
///
/// <para><b>Aktör alanı bilerek YOKTUR.</b> <see cref="RequestTermination.RequestedBy"/> uçta
/// token'dan damgalanır. Alan istemciye açık kalsaydı, fesih talebini kimin açtığı
/// <b>istemcinin gönderdiği metinden</b> yazılırdı — denetim izi taklit edilebilirdi.
/// Aynı ilke #137'de dekont/onay akışlarında da uygulanmıştı.</para>
/// </summary>
public sealed record RequestTerminationRequest(string Reason, string ReasonType);

/// <summary>
/// <c>POST /api/internships/{id}/approve/override</c> gövdesi.
///
/// <para><b>Aktör alanı bilerek YOKTUR</b> — bkz. <see cref="RequestTerminationRequest"/>.
/// Override, onay zincirini tümüyle atlayan tek işlemdir; "kim atladı" sorusunun yanıtı
/// istemciden gelmemelidir.</para>
/// </summary>
public sealed record OverrideTerminationApprovalRequest(string Reason);
