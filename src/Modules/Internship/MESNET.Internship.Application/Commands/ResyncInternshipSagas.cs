namespace MESNET.Internship.Application.Commands;

/// <summary>Kopya saga'ları birleştirir (#251). Bkz. <c>ResyncInternshipSagasHandler</c>.</summary>
public sealed record ResyncInternshipSagas;

/// <param name="Merged">Silinen kopya sayısı.</param>
/// <param name="Placements">Tekilleştirilen yerleştirme sayısı.</param>
/// <param name="AlreadyCanonical">Zaten doğru kimlikte olan saga sayısı.</param>
/// <param name="TenantsProcessed">
/// Dolaşılan kiracı (okul) sayısı (#292). <b>Sıfır kiracı, sıfır bulgudan farklıdır</b> ve
/// operatörün bunu ayırt edebilmesi gerekir: biri "onaracak bir şey yoktu", diğeri "hiçbir okul
/// bulunamadı — kurulum eksik". Alan olmasaydı iki durum da aynı boş yanıtla dönerdi.
/// </param>
public sealed record ResyncInternshipSagasResult(
    int Merged, int Placements, int AlreadyCanonical, int TenantsProcessed);
