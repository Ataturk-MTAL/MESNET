namespace MESNET.Common.Infrastructure.Database;

/// <summary>API'nin bağlandığı rolün RLS açısından önemli özellikleri (<c>pg_roles</c>).</summary>
public sealed record DatabaseRole(string Name, bool IsSuperuser, bool BypassesRls);

/// <summary>
/// API'nin bağlandığı rol row-level security'yi atlayabilir mi (#316). Saf fonksiyon.
///
/// <para>Süper kullanıcı ve <c>BYPASSRLS</c> rolü RLS'i <b>her zaman</b> atlar —
/// <c>FORCE ROW LEVEL SECURITY</c> bile işlemez. Bu bağlantıyla kurulan kiracı politikaları
/// yerinde durur ama hiçbir şeyi süzmez; hata da vermez.</para>
/// </summary>
public static class DatabaseRolePolicy
{
    /// <returns>İhlalin açıklaması; rol uygunsa <c>null</c>.</returns>
    public static string? Violation(DatabaseRole role)
    {
        if (role.IsSuperuser)
        {
            return $"API veritabanına süper kullanıcı rolüyle bağlanıyor: \"{role.Name}\". "
                + "Süper kullanıcı row-level security'yi her zaman atlar; kiracı yalıtımı veritabanında "
                + "hiç çalışmaz. Bağlantıyı mesnet_app (üretim/CI) ya da mesnet_owner (dev) rolüne "
                + "çevirin: src/Docs/docs/infrastructure/sql/316-veritabani-rolleri.sql (#316).";
        }

        if (role.BypassesRls)
        {
            return $"API veritabanına BYPASSRLS rolüyle bağlanıyor: \"{role.Name}\". Bu rol "
                + "row-level security'yi atlar; kiracı yalıtımı veritabanında çalışmaz. API rolünde "
                + "BYPASSRLS olmamalı (#316).";
        }

        return null;
    }
}
