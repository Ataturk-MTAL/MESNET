using System.Collections.Concurrent;

namespace MESNET.Audit.Core.Services;

/// <summary>
/// Komut tipinden denetim satırının iki kimlik alanını çıkarır: kısa tip adı ve modül adı.
/// </summary>
/// <remarks>
/// Modül adı ad alanı konvansiyonundan okunur: <c>MESNET.&lt;Modül&gt;.Application.Commands</c>.
/// Konvansiyon depoda zaten klasör yapısıyla zorlanıyor; yeni bir kural icat edilmiyor.
/// Beklenmeyen bir ad alanında modül BOŞ kalır — satır yine yazılır, çünkü "kim, ne, ne zaman"
/// modül adı olmadan da anlamlıdır.
/// </remarks>
public static class AuditCommandDescriptor
{
    private const string RootNamespace = "MESNET.";

    private static readonly ConcurrentDictionary<Type, (string CommandType, string Module)> Cache = new();

    public static (string CommandType, string Module) Describe(Type type)
        => Cache.GetOrAdd(type, static t => (t.Name, ResolveModule(t.Namespace)));

    private static string ResolveModule(string? ns)
    {
        if (string.IsNullOrEmpty(ns) || !ns.StartsWith(RootNamespace, StringComparison.Ordinal))
            return string.Empty;

        var rest = ns[RootNamespace.Length..];
        var dot = rest.IndexOf('.');
        return dot < 0 ? rest : rest[..dot];
    }
}
