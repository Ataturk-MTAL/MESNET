using MESNET.Common.Shared;

namespace MESNET.Contract.Application.Errors;

public static class ContractErrors
{
    public static Error NotFound(Guid id) =>
        new("CONTRACT_NOT_FOUND", $"Sözleşme bulunamadı: {id}");

    public static Error InvalidStatus(Guid contractId, string currentStatus, string message) =>
        new("INVALID_CONTRACT_STATUS", $"{message} Mevcut durum: {currentStatus}. Sözleşme: {contractId}");

    public static Error OperationFailed(string operation, string message) =>
        new($"CONTRACT_{operation.ToUpperInvariant()}_FAILED", message);

    public static Error ActiveContractExists(Guid studentId) =>
        new("CONTRACT_ALREADY_ACTIVE", $"Öğrencinin zaten devam eden bir sözleşmesi var. Öğrenci: {studentId}");

    public static Error AcademicPeriodNotFound(Guid id) =>
        new("CONTRACT_ACADEMIC_PERIOD_NOT_FOUND", $"Eğitim dönemi bulunamadı: {id}");

    public static Error AcademicPeriodClosed(Guid id) =>
        new("CONTRACT_ACADEMIC_PERIOD_CLOSED", $"Bu eğitim dönemi kapatılmıştır, işlem yapılamaz: {id}");
}
