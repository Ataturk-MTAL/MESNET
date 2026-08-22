using MESNET.Common.Shared;

namespace MESNET.Internship.Application.Errors;

public static class InternshipErrors
{
    public static Error NotFound(Guid id) =>
        new("Internship.NotFound", $"Staj bulunamadı: {id}");

    public static Error TerminationNotInProgress(Guid id) =>
        new("Internship.TerminationNotInProgress", $"Staj fesih sürecinde değil: {id}");

    /// <summary>Fesih süreci hiç açılmamışken onay adımı denendi (#218).</summary>
    public static Error TerminationNotStarted(Guid id) =>
        new("Internship.TerminationNotStarted", $"Staj için fesih süreci açılmamış: {id}");

    /// <summary>
    /// Onay sırası atlandı (#218). Mesaj hangi adımın beklendiğini taşır — yoksa kullanıcı
    /// neyi beklediğini bilemez.
    /// </summary>
    public static Error TerminationStepOutOfOrder(string description) =>
        new("Internship.TerminationStepOutOfOrder", description);

    public static Error OperationFailed(string operation, string message) =>
        new($"Internship.{operation}Failed", message);
}
