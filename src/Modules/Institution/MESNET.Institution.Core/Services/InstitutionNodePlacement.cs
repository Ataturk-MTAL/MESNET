using MESNET.Common.Shared.Security;
using MESNET.Institution.Core.Enums;

namespace MESNET.Institution.Core.Services;

/// <summary>Yeni düğümün ağaçtaki yerleşim kararının sonucu.</summary>
public enum NodePlacementOutcome
{
    /// <summary>Yerleşim geçerli; <see cref="NodePlacement.Path"/> kullanılabilir.</summary>
    Ok,

    /// <summary>Verilen üst düğüm yok — kayıt YARATILMAZ.</summary>
    ParentMissing,

    /// <summary>
    /// Üst düğümün yolu boş (geçiş ucu o kayıt için henüz koşmadı). Kayıt YARATILMAZ:
    /// çocuğun yolu da kurulamaz ve İKİSİ de hiçbir kapsamda görünmez — hata değil,
    /// sessiz boşluk.
    /// </summary>
    ParentHasNoPath
}

/// <param name="Path">
/// Yerleşim geçerliyse düğümün yolu. Üstü olmayan OKUL/İLÇE için <c>null</c>'dır ve bu
/// normaldir — geçiş ucu (<c>rebuild-hierarchy</c>) sonradan doldurur.
/// </param>
public readonly record struct NodePlacement(NodePlacementOutcome Outcome, string? Path);

/// <summary>
/// Yeni bir kurum düğümünün ağaçtaki yerini belirler. <b>Saf</b> — veritabanı bilmez.
///
/// <para><b>Neden ayrı bir fonksiyon:</b> karar handler'ın içinde kalsaydı ancak canlı bir
/// Marten oturumuyla sınanabilirdi ve depoda mock kütüphanesi yok. Aynı ayrım
/// <c>InstitutionScopePolicy.Decide</c> ve <c>InstitutionHierarchyPlanner.Plan</c> içinde de
/// var: karar saf, yan etki çağıranın.</para>
/// </summary>
public static class InstitutionNodePlacement
{
    /// <param name="nodeType">Yaratılacak düğümün tipi.</param>
    /// <param name="id">Yaratılacak düğümün kimliği.</param>
    /// <param name="parentId">İstekte verilen üst düğüm; verilmediyse <c>null</c>.</param>
    /// <param name="parentExists">
    /// <paramref name="parentId"/> verildiyse o kaydın bulunup bulunmadığı. Üst verilmediğinde
    /// değeri önemsizdir.
    /// </param>
    /// <param name="parentPath">Bulunan üst düğümün yolu; yoksa <c>null</c>.</param>
    public static NodePlacement Resolve(
        InstitutionNodeType nodeType, Guid id, Guid? parentId, bool parentExists, string? parentPath)
    {
        if (parentId is null)
        {
            // Üst verilmedi. Yalnız İL kök olarak yol alır; okul ve ilçe yolsuz doğar ve
            // geçiş ucu doldurur (bugünkü kayıtlarla aynı durum).
            return nodeType == InstitutionNodeType.Province
                ? new NodePlacement(NodePlacementOutcome.Ok, InstitutionPath.Root(id))
                : new NodePlacement(NodePlacementOutcome.Ok, null);
        }

        if (!parentExists)
            return new NodePlacement(NodePlacementOutcome.ParentMissing, null);

        if (string.IsNullOrWhiteSpace(parentPath))
            return new NodePlacement(NodePlacementOutcome.ParentHasNoPath, null);

        return new NodePlacement(NodePlacementOutcome.Ok, InstitutionPath.Child(parentPath, id));
    }
}
