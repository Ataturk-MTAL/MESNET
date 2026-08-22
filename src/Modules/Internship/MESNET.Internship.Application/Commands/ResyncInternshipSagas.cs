namespace MESNET.Internship.Application.Commands;

/// <summary>Kopya saga'ları birleştirir (#251). Bkz. <c>ResyncInternshipSagasHandler</c>.</summary>
public sealed record ResyncInternshipSagas;

/// <param name="Merged">Silinen kopya sayısı.</param>
/// <param name="Placements">Tekilleştirilen yerleştirme sayısı.</param>
/// <param name="AlreadyCanonical">Zaten doğru kimlikte olan saga sayısı.</param>
public sealed record ResyncInternshipSagasResult(int Merged, int Placements, int AlreadyCanonical);
