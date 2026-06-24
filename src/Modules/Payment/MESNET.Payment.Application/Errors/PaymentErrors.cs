using MESNET.Common.Shared;

namespace MESNET.Payment.Application.Errors;

public static class PaymentErrors
{
    public static Error NotFound(Guid id) =>
        new("Payment.NotFound", $"Ödeme bulunamadı: {id}");

    public static Error InvalidPhase(string current, string required) =>
        new("Payment.InvalidPhase", $"Geçersiz durum. Mevcut: {current}, Gerekli: {required}");

    public static Error ApprovalRequired(string approver) =>
        new("Payment.ApprovalRequired", $"{approver} onayı gerekli.");

    public static Error OperationFailed(string operation, string message) =>
        new($"Payment.{operation}Failed", message);

    public static Error AcademicPeriodClosed(Guid id) =>
        new("Payment.AcademicPeriodClosed", $"Bu eğitim dönemi kapatılmıştır, ödeme işlemi yapılamaz: {id}");
}
