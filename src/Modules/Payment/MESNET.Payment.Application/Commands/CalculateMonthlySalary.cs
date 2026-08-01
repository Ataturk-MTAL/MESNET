namespace MESNET.Payment.Application.Commands;

/// <summary>
/// Bir öğrencinin belirli bir aya ait maaş sürecini başlatır — <c>PaymentSaga</c>'nın giriş mesajı.
/// </summary>
/// <remarks>
/// Saga eskiden doğrudan <c>AttendanceMarked</c> ile başlıyordu ve her devamsızlık girişinde
/// <c>Guid.NewGuid()</c> ile yeni bir saga açıyordu; aynı öğrenci/ay için onlarca ödeme kaydı
/// oluşuyordu (#62). Artık araya bu komut giriyor ve <see cref="SalaryPeriodId"/>
/// (öğrenci, ay) ikilisinden deterministik türetiliyor — aynı ay için tekrar tetiklense de
/// kimlik aynı kalır.
///
/// Aynı komut #63'teki aylık zamanlayıcının da giriş noktasıdır: maaş devamsızlığa değil aya
/// bağlı hesaplanmalı, devamsızlık yalnız kesintiyi etkilemeli.
///
/// Anahtar #154 ile (öğrenci, ay)'dan (sözleşme, ay)'a taşındı; komut bu yüzden
/// <see cref="ContractId"/> taşır. Gün oranlaması komutta DEĞİL hesap anında sözleşme
/// kaydından türetilir — yeniden hesap yolunun (<c>RecalculateMonthlySalary</c>) da aynı
/// oranı görmesi gerekir, komuta gömülseydi o yol tam ay hesaplardı.
/// </remarks>
public sealed record CalculateMonthlySalary(
    Guid SalaryPeriodId,
    Guid ContractId,
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    string Month,
    DateTime ReferenceDate);
