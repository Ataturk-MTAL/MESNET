using Marten;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Core.Enums;
using MESNET.Institution.Core.Services;
using Microsoft.Extensions.Logging;
using InstitutionRecord = MESNET.Institution.Core.Entities.Institution;

namespace MESNET.Institution.Application.Handlers;

/// <summary>
/// Kurum ağacını kurar (dağıtım ön koşulu).
///
/// <para><b>Atlanırsa sessizdir.</b> Yollar boş kalır, <c>StartsWith</c> hiçbir şeyle
/// eşleşmez ve il yetkilisi hata değil <b>boş liste</b> görür. Bu yüzden
/// <c>src/Docs/docs/infrastructure/dagitim-on-kosullari.md</c> içinde zorunlu adım olarak
/// yazılıdır.</para>
///
/// <para><b>Olay yayınlamaz.</b> Ağaç alanları hiçbir modülün görünümünü beslemiyor; yayın
/// yalnız bütün tüketicileri boşuna uyandırırdı. B parçasında düğüm taşıma geldiğinde bu
/// karar yeniden değerlendirilir.</para>
///
/// <para><b>Kurum belgesini filtresiz dolaşır</b> — bu bilinçlidir ve
/// <c>InstitutionScopeDriftTests.MayEnumerateAll</c> listesinde gerekçesiyle yazılıdır:
/// ağacı kurmak tanımı gereği bütün düğümleri görmeyi gerektirir ve uç kurum üstü izinle
/// korunur.</para>
/// </summary>
public static class RebuildInstitutionHierarchyHandler
{
    public static async Task<RebuildInstitutionHierarchyResult> Handle(
        RebuildInstitutionHierarchy command,
        IDocumentSession session,
        ILogger<RebuildInstitutionHierarchy> logger,
        CancellationToken cancellationToken)
    {
        var all = await session.Query<InstitutionRecord>().ToListAsync(cancellationToken);
        var plan = InstitutionHierarchyPlanner.Plan(all, Guid.NewGuid);

        var byId = all.ToDictionary(i => i.Id);

        foreach (var node in plan.Created)
        {
            var record = new InstitutionRecord
            {
                Id = node.Id,
                InstitutionCode = InstitutionHierarchyPlanner.UnknownInstitutionCode,
                FullName = node.FullName,
                ProvinceCode = node.ProvinceCode,
                DistrictName = node.DistrictName
            };

            byId[node.Id] = record;
            session.Store(record);
        }

        foreach (var assignment in plan.Assignments)
        {
            if (!byId.TryGetValue(assignment.Id, out var record))
                continue;

            record.ParentId = assignment.ParentId;
            record.NodeTypeName = assignment.NodeTypeName;
            record.Path = assignment.Path;

            session.Store(record);
        }

        await session.SaveChangesAsync(cancellationToken);

        if (plan.SkippedNoProvince.Count > 0)
        {
            // Sessiz kalmaz: bu okullar hiçbir il yetkilisinin listesinde görünmez ve bunu
            // fark ettirecek başka bir sinyal yok (hata değil, BOŞ SONUÇ üretirler).
            logger.LogWarning(
                "Kurum ağacı kuruldu ama {Count} okulun il kodu yok; kapsamsız kaldılar ve "
                + "hiçbir il/ilçe yetkilisinin listesinde görünmezler. Kimlikler: {Ids}",
                plan.SkippedNoProvince.Count, string.Join(", ", plan.SkippedNoProvince));
        }

        return new RebuildInstitutionHierarchyResult(
            plan.Created.Count(c => c.NodeTypeName == InstitutionNodeType.Province.Name),
            plan.Created.Count(c => c.NodeTypeName == InstitutionNodeType.District.Name),
            plan.Assignments.Count,
            plan.SkippedNoProvince.Count);
    }
}
