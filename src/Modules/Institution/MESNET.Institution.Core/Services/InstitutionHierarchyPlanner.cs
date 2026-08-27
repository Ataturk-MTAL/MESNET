using MESNET.Common.Shared.Reference;
using MESNET.Common.Shared.Security;
using MESNET.Institution.Core.Enums;
using InstitutionRecord = MESNET.Institution.Core.Entities.Institution;

namespace MESNET.Institution.Core.Services;

/// <summary>Kurulacak yeni üst düğüm (il ya da ilçe müdürlüğü).</summary>
public sealed record HierarchyNodeToCreate(
    Guid Id,
    Guid? ParentId,
    string NodeTypeName,
    string Path,
    string FullName,
    string ProvinceCode,
    string? DistrictName);

/// <summary>Var olan bir kayda yazılacak ağaç alanları.</summary>
public sealed record HierarchyAssignment(Guid Id, Guid? ParentId, string NodeTypeName, string Path);

/// <summary>
/// Geçiş planı.
/// </summary>
/// <param name="Created">Kurulacak yeni üst düğümler.</param>
/// <param name="Assignments">
/// <b>Bütün</b> düğümlere yazılacak ağaç alanları — yenilere de, var olanlara da. Yalnız eksik
/// satırlara yazılsaydı, elle bozulmuş bir yol kalıcı olurdu.
/// </param>
/// <param name="SkippedNoProvince">İl kodu olmadığı için kapsamsız bırakılan okullar.</param>
/// <remarks>
/// <b>Bilinen sınır:</b> hiçbir okulun referans vermediği bir üst düğüm (ör. son okulu
/// kapanmış bir ilçe müdürlüğü) atama listesine girmez ve yolu olduğu gibi kalır. Zararsızdır —
/// altında kimse yoktur — ama yolu bozulmuşsa bu koşu onu onarmaz.
/// </remarks>
public sealed record HierarchyPlan(
    IReadOnlyList<HierarchyNodeToCreate> Created,
    IReadOnlyList<HierarchyAssignment> Assignments,
    IReadOnlyList<Guid> SkippedNoProvince);

/// <summary>
/// Mevcut okul künyelerinden (<c>ProvinceCode</c> / <c>DistrictName</c>) kurum ağacını üretir.
///
/// <para><b>Saf — veritabanı bilmez.</b> Bu geçişin tek kritik özelliği idempotanlıktır ve
/// mantık handler'ın içinde kalsaydı bunu ancak iki kez yazarak sınayabilirdik.</para>
///
/// <para><b>İl kodu olmayan okul köke BAĞLANMAZ.</b> Bağlansaydı, herhangi bir il yetkilisinin
/// alt ağacına düşen sahipsiz bir kayıt olurdu. Kapsamsız kalır ve sayılır — sonuçtaki sayı
/// boşluğu görünür kılar (aynı desen <c>SyncUsersFromKeycloak</c>'un <c>WithoutInstitution</c>
/// sayısında da var).</para>
/// </summary>
public static class InstitutionHierarchyPlanner
{
    /// <summary>
    /// Üst düğümlerin kurum kodu. MEB müdürlüklerinin kendi kodları vardır ama bu geçişin
    /// elinde o veri yoktur ve <b>uydurulmuş bir kod gerçek veri gibi görünürdü</b> (aynı
    /// gerekçe <c>Institution.DistrictName</c> yorumunda ilçe kodu için de yazılı). Sıfır,
    /// "girilmedi" demektir; B parçasında bu düğümler düzenlenebilir olacak.
    /// </summary>
    public const int UnknownInstitutionCode = 0;

    public static HierarchyPlan Plan(IReadOnlyList<InstitutionRecord> all, Func<Guid> newId)
    {
        var created = new List<HierarchyNodeToCreate>();
        var assignments = new List<HierarchyAssignment>();
        var skipped = new List<Guid>();

        // Var olan üst düğümler. Anahtarlar künyeden gelir, addan değil: ilçe adı düzeltilse
        // bile aynı düğüm bulunur.
        var provinces = all
            .Where(i => i.NodeType == InstitutionNodeType.Province && !string.IsNullOrWhiteSpace(i.ProvinceCode))
            .GroupBy(i => i.ProvinceCode!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.OrderBy(i => i.Id).First().Id, StringComparer.OrdinalIgnoreCase);

        var districts = all
            .Where(i => i.NodeType == InstitutionNodeType.District
                        && !string.IsNullOrWhiteSpace(i.ProvinceCode)
                        && !string.IsNullOrWhiteSpace(i.DistrictName))
            .GroupBy(i => DistrictKey(i.ProvinceCode!, i.DistrictName!), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.OrderBy(i => i.Id).First().Id, StringComparer.OrdinalIgnoreCase);

        var schools = all
            .Where(i => i.NodeType == InstitutionNodeType.School)
            .OrderBy(i => i.Id)
            .ToList();

        // Yol, kimlikler çözüldükten SONRA kurulur; bu yüzden düğüm kimlikleri önce toplanır.
        var provincePaths = new Dictionary<string, (Guid Id, string Path)>(StringComparer.OrdinalIgnoreCase);
        var districtPaths = new Dictionary<string, (Guid Id, string Path)>(StringComparer.OrdinalIgnoreCase);

        foreach (var school in schools)
        {
            if (string.IsNullOrWhiteSpace(school.ProvinceCode))
            {
                skipped.Add(school.Id);
                continue;
            }

            var provinceCode = school.ProvinceCode.Trim();
            var province = EnsureProvince(provinceCode);

            var parent = province;

            if (!string.IsNullOrWhiteSpace(school.DistrictName))
                parent = EnsureDistrict(provinceCode, school.DistrictName.Trim(), province);

            assignments.Add(new HierarchyAssignment(
                school.Id,
                parent.Id,
                InstitutionNodeType.School.Name,
                InstitutionPath.Child(parent.Path, school.Id)));
        }

        return new HierarchyPlan(created, assignments, skipped);

        // ── yerel yardımcılar ──

        (Guid Id, string Path) EnsureProvince(string code)
        {
            if (provincePaths.TryGetValue(code, out var known))
                return known;

            var id = provinces.TryGetValue(code, out var existingId) ? existingId : newId();
            var path = InstitutionPath.Root(id);

            if (!provinces.ContainsKey(code))
            {
                created.Add(new HierarchyNodeToCreate(
                    id, null, InstitutionNodeType.Province.Name, path,
                    $"{TurkishProvinces.GetName(code) ?? code} İl Millî Eğitim Müdürlüğü",
                    code, null));
            }

            assignments.Add(new HierarchyAssignment(id, null, InstitutionNodeType.Province.Name, path));

            var node = (id, path);
            provincePaths[code] = node;
            return node;
        }

        (Guid Id, string Path) EnsureDistrict(string code, string name, (Guid Id, string Path) province)
        {
            var key = DistrictKey(code, name);

            if (districtPaths.TryGetValue(key, out var known))
                return known;

            var id = districts.TryGetValue(key, out var existingId) ? existingId : newId();
            var path = InstitutionPath.Child(province.Path, id);

            if (!districts.ContainsKey(key))
            {
                created.Add(new HierarchyNodeToCreate(
                    id, province.Id, InstitutionNodeType.District.Name, path,
                    $"{name} İlçe Millî Eğitim Müdürlüğü", code, name));
            }

            assignments.Add(new HierarchyAssignment(
                id, province.Id, InstitutionNodeType.District.Name, path));

            var node = (id, path);
            districtPaths[key] = node;
            return node;
        }

        // İlçe adı tek başına benzersiz DEĞİLDİR ("Merkez" 81 ilde var); anahtar daima
        // (il, ilçe) ikilisidir.
        static string DistrictKey(string provinceCode, string districtName) =>
            $"{provinceCode.Trim()}|{districtName.Trim()}";
    }
}
