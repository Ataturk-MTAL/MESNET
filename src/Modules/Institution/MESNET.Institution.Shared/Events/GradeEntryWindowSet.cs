namespace MESNET.Institution.Shared.Events;

/// <summary>
/// Dönem sonu not giriş penceresi müdür/müdür yardımcısı tarafından açıldı/güncellendi.
/// Coordination bu olayı dinleyip AcademicPeriodView'ini günceller (pencere kontrolü için).
/// </summary>
public sealed record GradeEntryWindowSet(
    Guid AcademicPeriodId,
    Guid InstitutionId,
    DateOnly StartDate,
    DateOnly EndDate);
