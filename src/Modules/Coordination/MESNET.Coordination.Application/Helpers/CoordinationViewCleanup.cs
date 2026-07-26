using Marten;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Helpers;

/// <summary>
/// İşletme düzeyi olaylarda (kapatma/pasife alma/askıya alma) o işletmenin
/// <b>tüm</b> coordination satırlarını silmek için ortak yardımcı.
/// </summary>
public static class CoordinationViewCleanup
{
    /// <summary>
    /// İşletmenin temel satırı + tüm alan satırlarını siler.
    /// <c>Id == businessId</c> koşulu, çok-alanlı modele geçmeden önce yazılmış
    /// eski tek-satır kayıtlarını da temizler.
    /// </summary>
    public static void DeleteAllRows(IDocumentSession session, Guid businessId)
    {
        session.DeleteWhere<BusinessCoordinationView>(
            v => v.BusinessId == businessId || v.Id == businessId);
    }
}
