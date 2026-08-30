namespace MESNET.Audit.Application.Auditing;

/// <summary>
/// Bir mesaj tipinin denetim izine yazılıp yazılmayacağına karar veren SAF yüklem.
/// </summary>
/// <remarks>
/// <para><b>Neden ad alanı konvansiyonu:</b> denetim middleware'i modülleri tanımaz ve
/// tanımamalıdır (<c>MESNET.Audit.*</c> hiçbir modülü referans etmez). Kayıt listesi
/// tutulsaydı 201 komutluk elle bakımlı bir tablo doğardı ve o tablo sessizce eskirdi.
/// Konvansiyon depoda zaten klasör yapısıyla zorlanıyor.</para>
///
/// <para><b>İkinci kural neden var:</b> <c>Commands/</c> klasörüne YANLIŞ yerleştirilmiş
/// sorgular var (<c>GetUserAccounts</c>, <c>GetDocumentById</c>, <c>GetInvitations</c>,
/// <c>GetPermissionScopes</c>, <c>GetRoleIntegrityReport</c>, <c>GetStudentsWithoutGuardian</c>,
/// <c>GetUserAccount</c>). Yalnız ad alanına bakılsaydı bütün liste trafiği ize düşerdi.
/// Doğru çözüm o tipleri <c>Queries/</c>'e taşımaktır; bu plan onu kapsam DIŞI bırakır ve
/// bedeli burada, tek satırda görünür tutar.</para>
/// </remarks>
public static class AuditCommandFilter
{
    private const string CommandsNamespaceSuffix = ".Commands";
    private const string QueryNamePrefix = "Get";

    public static bool ShouldAudit(Type messageType)
    {
        var ns = messageType.Namespace;
        if (string.IsNullOrEmpty(ns)) return false;
        if (!ns.EndsWith(CommandsNamespaceSuffix, StringComparison.Ordinal)) return false;

        return !messageType.Name.StartsWith(QueryNamePrefix, StringComparison.Ordinal);
    }
}
