using Marten;
using MESNET.Payment.Application.Commands;
using MESNET.Payment.Core.Entities;
using MESNET.Payment.Core.ReadModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace MESNET.Payment.Application.Services;

/// <summary>
/// Her ayın son günü 01:00 UTC'de, aktif dönemdeki her aktif yerleştirme için maaş dönemi açar.
/// </summary>
/// <remarks>
/// Maaş eskiden yalnız <c>AttendanceMarked</c> ile tetikleniyordu: o ay hiç devamsızlık girilmeyen
/// öğrenci için süreç hiç başlamıyordu (#63). Devamsızlığı olmayan öğrenci maaşı hak eden
/// öğrencidir — akış tam tersti.
///
/// Ay sonunda çalışmasının ikinci faydası: devamsızlık o ay için kesinleşmiş olur, kesinti tek
/// seferde doğru hesaplanır. Reporting'in ay sonu raporu 00:30 UTC'de koştuğu için burada 01:00
/// seçildi — aynı gün, çakışmadan.
/// </remarks>
public class MonthlySalarySchedulerService(
    IServiceScopeFactory scopeFactory,
    ILogger<MonthlySalarySchedulerService> logger) : BackgroundService
{
    private const int RunHourUtc = 1;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRun = CalculateNextRun(now);

            logger.LogInformation(
                "Aylık maaş scheduler — sonraki çalışma: {NextRun:yyyy-MM-dd HH:mm} UTC ({Delay})",
                nextRun, nextRun - now);

            try
            {
                await Task.Delay(nextRun - now, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            try
            {
                await OpenSalaryPeriods(stoppingToken);
            }
            catch (Exception ex)
            {
                // Bir aylık koşu patlarsa servis ölmemeli — sonraki ay tekrar denenir.
                logger.LogError(ex, "Aylık maaş dönemi açma başarısız oldu.");
            }
        }
    }

    /// <summary>Ayın son günü 01:00 UTC.</summary>
    private static DateTime CalculateNextRun(DateTime now)
    {
        var lastDay = LastDayOfMonth(now.Year, now.Month);
        if (now < lastDay) return lastDay;

        var next = now.AddMonths(1);
        return LastDayOfMonth(next.Year, next.Month);
    }

    private static DateTime LastDayOfMonth(int year, int month)
        => new(year, month, DateTime.DaysInMonth(year, month), RunHourUtc, 0, 0, DateTimeKind.Utc);

    // İşin kendisi OpenMonthlySalaryPeriodsHandler'da: aynı mantık hem bu zamanlayıcıdan hem
    // elle tetikleme endpoint'inden çalışsın (kaçırılmış koşu, ilk ay, sonradan eklenen
    // yerleştirme). Zamanlayıcıda yalnız zamanlama kalıyor.
    private async Task OpenSalaryPeriods(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var referenceDate = DateTime.UtcNow;
        await bus.PublishAsync(new OpenMonthlySalaryPeriods(
            referenceDate.ToString("yyyy-MM"), referenceDate));
    }
}
