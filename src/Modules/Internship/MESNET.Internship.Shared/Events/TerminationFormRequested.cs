namespace MESNET.Internship.Shared.Events;

/// <summary>
/// Onay zinciri tamamlandı (ya da override edildi) — ıslak imza formu üretilebilir.
/// </summary>
/// <param name="BusinessId">
/// İşletme — <b>okulda stajda null</b> (#159, #218).
///
/// <para>Alan önceden zorunluydu ve saga onu <c>BusinessIdForContractFlow</c> ile dolduruyordu;
/// o özellik işverensiz stajda <b>istisna fırlatır</b>. Sonuç: okulda staj yapan öğrencinin
/// zincirinde müdür onayı 500 döndürüyordu — zincir kapanmak üzereyken patlıyordu.</para>
/// </summary>
public sealed record TerminationFormRequested(
    Guid InternshipId,
    Guid StudentId,
    Guid? BusinessId,
    Guid InstitutionId);
