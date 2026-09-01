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
    string PlacementType = "Business")
{
    /// <summary>
    /// Görünüm besleyen tüketicilerin <b>tek</b> girdi tipine çevirir (#291).
    ///
    /// <para>Yön bilerek tek taraflıdır: yaşam döngüsü olayı anlık görüntüye çevrilir, tersi
    /// <b>yoktur</b>. Ters çevrim olsaydı bir onarım yolu yanlışlıkla saga başlatabilirdi —
    /// düzeltilen hatanın ta kendisi.</para>
    /// </summary>
    public PlacementSnapshotResynced ToSnapshot() => new(
        PlacementId, StudentId, BusinessId, InstitutionId, AcademicPeriodId, TeacherId,
        PlacedAt, StudentName, BusinessName, BranchCode, BranchName, PlacementType);
}
