namespace MESNET.Payment.Application.Commands;

/// <summary>
/// Mevcut bir maaş döneminin tutarını yeniden hesaplatır — ay içinde yeni devamsızlık
/// onaylandığında kesintinin güncellenmesi için (#64).
/// </summary>
/// <remarks>
/// Maaş şu an ayın ilk devamsızlığıyla tetikleniyor; o anda kesinti tek gün üzerinden çıkar.
/// Sonraki her devamsızlık bu komutu doğurur ve saga tutarı günceller. Saga yalnız
/// <c>AwaitingReceipt</c> fazındayken yeniden hesaplar — onay süreci başlamış bir ödemenin
/// tutarı dondurulur.
///
/// #63'teki aylık zamanlayıcı devreye girince maaş ay sonunda tek seferde hesaplanacağı için
/// bu mekanizmaya gerek kalmayabilir.
/// </remarks>
public sealed record RecalculateMonthlySalary(
    Guid SalaryPeriodId,
    DateTime ReferenceDate);
