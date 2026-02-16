using MESNET.Common.Shared;

namespace MESNET.Internship.Application.Errors;

public static class InternshipErrors
{
    public static Error NotFound(Guid id) =>
        new("Internship.NotFound", $"Staj bulunamadı: {id}");

    public static Error TerminationNotInProgress(Guid id) =>
        new("Internship.TerminationNotInProgress", $"Staj fesih sürecinde değil: {id}");

    public static Error OperationFailed(string operation, string message) =>
        new($"Internship.{operation}Failed", message);
}
