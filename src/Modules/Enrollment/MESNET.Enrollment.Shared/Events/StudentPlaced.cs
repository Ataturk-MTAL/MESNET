namespace MESNET.Enrollment.Shared.Events;

/// <param name="BusinessId">
/// İşletme — <b>okulda stajda null</b> (#159). Tüketiciler kararı bu alandan okur: işletme
/// yoksa ücret, devlet katkısı, dekont yükümlülüğü ve koordinasyon saati doğmaz.
/// </param>
/// <param name="PlacementType">
/// <c>Business</c> veya <c>School</c>. Modüller arası olaylarda SmartEnum değil string taşınır
/// (CLAUDE.md kuralı). Eski olaylarda alan yoktur ve boş gelir — o kayıtların hepsi işletmede
/// stajdır, çünkü okulda staj bu alanla birlikte doğdu.
/// </param>
public sealed record StudentPlaced(
    Guid PlacementId,
    Guid StudentId,
    Guid? BusinessId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    Guid? TeacherId,
    DateTime PlacedAt,
    string StudentName = "",
    string BusinessName = "",
    string BranchCode = "",
    string BranchName = "",
    string PlacementType = "Business");
