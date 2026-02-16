using MESNET.Common.Shared;

namespace MESNET.Business.Application.Errors;

public static class BusinessErrors
{
    public static Error NotFound(Guid id) =>
        new("Business.NotFound", $"İşletme bulunamadı: {id}");

    public static Error InvalidTransition(string from, string to) =>
        new("Business.InvalidTransition", $"İşletme '{from}' durumundan '{to}' durumuna geçirilemez.");

    public static Error DocumentNotFound(Guid documentId) =>
        new("Business.DocumentNotFound", $"Belge bulunamadı: {documentId}");
}
