using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Helpers;

/// <summary>
/// Takdir edilen saatin satıra yazılması ve geçmiş kaydının üretilmesi.
/// Tekil (<c>UpdateBusinessAssignedHours</c>) ve toplu (<c>UpdateBranchAssignedHours</c>)
/// uçlar aynı yazma davranışını paylaşsın diye tek yerde durur (#117).
/// </summary>
public static class AssignedHoursMutation
{
    /// <summary>
    /// Satırı yeni saat/fahri durumuna taşır ve geçmişe bir kayıt ekler.
    /// Doğrulama yapmaz — çağıran zaten doğrulamış olmalıdır.
    /// </summary>
    public static void Apply(
        BusinessCoordinationView view,
        int newHours,
        bool isHonoraryVisit,
        Guid updatedById)
    {
        var oldHours = view.AssignedHours;
        var wasHonorary = view.IsHonoraryVisit;

        view.AssignedHours = newHours;
        view.IsHonoraryVisit = isHonoraryVisit;

        view.History.Insert(0, new AssignmentHistoryEntry(
            DateTime.UtcNow,
            "HoursUpdated",
            updatedById,
            view.AssignedTeacherName,
            null,
            null,
            newHours,
            DescribeChange(oldHours, wasHonorary, newHours, isHonoraryVisit)));

        view.LastModifiedAt = DateTime.UtcNow;
        view.LastModifiedById = updatedById;
    }

    /// <summary>Geçmiş kaydı açıklaması — fahri geçişleri saat değişiminden ayrı okunur olsun.</summary>
    public static string DescribeChange(int oldHours, bool wasHonorary, int newHours, bool isHonorary)
    {
        if (isHonorary && !wasHonorary)
            return $"Fahri (ücretsiz) ziyaret olarak işaretlendi; takdir edilen saat {oldHours} → 0";

        if (!isHonorary && wasHonorary)
            return $"Fahri ziyaret işareti kaldırıldı; takdir edilen saat 0 → {newHours}";

        if (isHonorary)
            return "Fahri (ücretsiz) ziyaret olarak korundu; takdir edilen saat 0";

        return $"Takdir edilen saat {oldHours} → {newHours} olarak değiştirildi";
    }
}
