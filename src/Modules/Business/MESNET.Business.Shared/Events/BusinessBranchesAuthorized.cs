namespace MESNET.Business.Shared.Events;

/// <summary>
/// İşletmenin öğrenci alabileceği alanlar güncellendi (#119).
///
/// <paramref name="ActiveBranchCodes"/> yalnız AKTİF (iptal edilmemiş) alan kodlarını taşır —
/// tüketiciler için tam liste, artımlı fark değil. Boş liste "hiçbir alandan öğrenci alamaz"
/// demektir.
///
/// Tüketiciler: Enrollment (yerleştirme guard'ının read-model'i), Coordination (alan bazlı
/// saat dağıtımının girdisi — #114).
/// </summary>
public sealed record BusinessBranchesAuthorized(
    Guid BusinessId,
    string BusinessName,
    List<string> ActiveBranchCodes,
    string AuthorizedBy,
    DateTime AuthorizedAt);
