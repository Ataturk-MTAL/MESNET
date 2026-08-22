namespace MESNET.Payment.Application.Commands;

/// <summary>
/// Aktif dönemdeki her aktif yerleştirme için verilen aya ait maaş dönemini açar (#63).
/// </summary>
/// <remarks>
/// Normalde <c>MonthlySalarySchedulerService</c> tarafından ayın son günü yayınlanır. Elle de
/// tetiklenebilir: sisteme geçişin ilk ayı, kaçırılmış bir koşu veya sonradan eklenen
/// yerleştirmeler için. Zaten ödeme kaydı olan öğrenciler atlanır, yani tekrar çalıştırmak
/// güvenlidir.
/// </remarks>
public sealed record OpenMonthlySalaryPeriods(
    string Month,
    DateTime ReferenceDate);

/// <summary>Açılan ve atlanan maaş dönemi sayıları.</summary>
public sealed record OpenMonthlySalaryPeriodsResult(
    string Month,
    int Opened,
    int Skipped,
    int ActivePlacements);
