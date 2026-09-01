namespace MESNET.Internship.Application.Commands;

/// <summary>
/// Tıkanma eşiğini yazar — <b>ulusal parametre</b>. Kurum kimliği taşımaz; yazma izni
/// <c>platform:parameter:manage</c>'dir.
/// </summary>
public sealed record UpdateApprovalConfig(int StuckApprovalDays);
