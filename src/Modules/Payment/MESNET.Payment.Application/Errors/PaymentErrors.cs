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

    // Geriye dönük yürürlük, eski config'i kendi başlangıcından önceye kapatır — ters
    // (imkânsız) aralık üretir ve o dönemin hesabı config bulamaz hale gelir (#75).
    public static Error SalaryConfigBackdated(DateTime requested, DateTime current) =>
        new("Payment.SalaryConfigBackdated",
            $"Yeni yürürlük tarihi ({requested:yyyy-MM-dd}) mevcut ayarın başlangıcından " +
            $"({current:yyyy-MM-dd}) önce olamaz.");

    // Config yoksa sessizce sabit bir tutarla hesaplamak yanlış para üretir — hata ver (#64).
    /// <summary>
    /// Hesaplanan ay için yürürlükte olan ulusal parametre yok (#147). Kurum kimliği
    /// TAŞIMAZ — parametre ulusaldır, "kuruma ait ayar" diye bir şey yoktur.
    /// </summary>
    public static Error SalaryConfigMissing(string month) =>
        new("Payment.SalaryConfigMissing",
            $"{month} ayında yürürlükte olan asgari ücret kaydı bulunamadı, ücret hesaplanamıyor.");

    public static Error AcademicPeriodClosed(Guid id) =>
        new("Payment.AcademicPeriodClosed", $"Bu eğitim dönemi kapatılmıştır, ödeme işlemi yapılamaz: {id}");
}
