namespace MESNET.Audit.Application.Auditing;

/// <summary>
/// Denetim satırını <b>komutun işleminden AYRI</b> bir oturumda yazar.
/// </summary>
public interface IAuditWriter
{
    /// <param name="exception">
    /// Başarısızlık yolunda istisna; başarı yolunda <c>null</c>.
    /// </param>
    Task WriteAsync(AuditContext context, Exception? exception, CancellationToken cancellationToken = default);
}
