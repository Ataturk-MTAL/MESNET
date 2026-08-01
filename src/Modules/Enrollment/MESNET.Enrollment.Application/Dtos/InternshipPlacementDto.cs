namespace MESNET.Enrollment.Application.Dtos;

public sealed record InternshipPlacementDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    // Okulda stajda null (#159) — işveren yok. Arayüz türe bakarak "Okulda" yazar.
    Guid? BusinessId,
    string BusinessName,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    Guid? TeacherId,
    string? TeacherName,
    string BranchCode,
    string Status,
    string StatusSlug,
    string Source,
    string SourceSlug,
    DateTime PlacedAt,
    // Yerleştirme türü (#159): Business / School + Türkçe karşılığı.
    string PlacementType = "Business",
    string PlacementTypeSlug = "İşletmede");
