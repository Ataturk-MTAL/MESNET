namespace MESNET.Institution.Application.Commands;

/// <summary>
/// Dönem sonu not giriş penceresini açar/günceller (müdür / müdür yardımcısı).
/// Bu aralıkta işletme, öğrencilerinin dönem notlarını girebilir.
/// </summary>
public sealed record SetGradeEntryWindow(
    Guid AcademicPeriodId,
    DateOnly StartDate,
    DateOnly EndDate);
