namespace MESNET.Payment.Shared.Events;

/// <summary>
/// Maaş dönemi tutarı ay içinde yeniden hesaplandı — devamsızlık kesintisi değiştiğinde
/// yayınlanır (#64). Yalnız dekont beklenirken (<c>AwaitingReceipt</c>) çıkar; onay süreci
/// başlamış ödemenin tutarı dondurulur.
/// </summary>
public sealed record SalaryRecalculated(
    Guid SalaryPeriodId,
    Guid StudentId,
    string Month,
    decimal NetAmount,
    decimal BaseWage,
    decimal Deduction,
    decimal GovContribution);
