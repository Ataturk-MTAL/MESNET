using System.Collections.Concurrent;
using System.Reflection;

namespace MESNET.Audit.Core.Services;

/// <summary>
/// Bir komuttan denetim izine yazılacak <b>hedef kayıt kimliklerini</b> çıkarır.
/// </summary>
/// <remarks>
/// <para><b>Neden konvansiyon:</b> komutlar heterojendir ve denetim middleware'i onları
/// tanımaz — tanısaydı 201 komut için elle bakımlı bir kayıt listesi tutmak gerekirdi ve o
/// liste sessizce eskirdi. Bunun yerine <see cref="KnownTargetNames"/> kümesindeki adları
/// taşıyan <c>Guid</c> özellikleri okunur.</para>
///
/// <para><b>Bedeli açıktır:</b> kümede olmayan bir ad kullanan komut HEDEFSİZ kaydolur.
/// Satır yine oluşur — kim, ne, ne zaman durur; yalnız hangi kayda dokunulduğu yazılmaz.</para>
///
/// <para><b>Yansıma maliyeti:</b> tip başına özellik listesi bir kez çözülür ve
/// <see cref="Cache"/>'te tutulur. İstek başına yansıma YAPILMAZ.</para>
/// </remarks>
public static class AuditTargetExtractor
{
    /// <summary>
    /// Hedef sayılan özellik adları. <b>Sabittir ve testle kilitlidir</b>; sessizce daralması
    /// hedeflerin izden kaybolması demektir.
    /// </summary>
    public static readonly IReadOnlySet<string> KnownTargetNames =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "AcademicPeriodId",
            "AttendanceId",
            "BusinessId",
            "ContractId",
            "InstitutionId",
            "PaymentId",
            "StudentId",
            "TeacherId",
            "UserAccountId",
        };

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> Cache = new();

    public static Dictionary<string, Guid> Extract(object? command)
    {
        var targets = new Dictionary<string, Guid>(StringComparer.Ordinal);
        if (command is null) return targets;

        foreach (var property in ResolveTargetProperties(command.GetType()))
        {
            // Guid.Empty "kimlik verilmedi" demektir; izde gerçek bir kayıtmış gibi
            // görünmesi, olmayan bir kaydı aramaya yollardı.
            if (property.GetValue(command) is Guid id && id != Guid.Empty)
                targets[property.Name] = id;
        }

        return targets;
    }

    private static PropertyInfo[] ResolveTargetProperties(Type type)
        => Cache.GetOrAdd(type, static t => t
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead
                        && KnownTargetNames.Contains(p.Name)
                        && (p.PropertyType == typeof(Guid) || p.PropertyType == typeof(Guid?)))
            .ToArray());
}
