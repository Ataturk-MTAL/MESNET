using MESNET.Common.Infrastructure.Database;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// API'nin bağlandığı veritabanı rolü RLS'i atlayamamalı (#316).
///
/// <para><b>Neden açılışta:</b> süper kullanıcı ve BYPASSRLS rolü row-level security'yi HER
/// ZAMAN atlar — FORCE bile işlemez. Bu bağlantıyla kurulan kiracı politikaları yerinde durur
/// ama hiçbir şeyi süzmez ve bunu hiçbir test göstermez. Ayarı depoya yazmak yetmez; çalışan
/// ortamın gerçekten hangi rolle bağlandığı ölçülmelidir (#195 ile aynı gerekçe).</para>
/// </summary>
public sealed class DatabaseRolePolicyTests
{
    [Fact]
    public void Siradan_rol_kabul_edilir()
    {
        DatabaseRolePolicy.Violation(new DatabaseRole("mesnet_app", IsSuperuser: false, BypassesRls: false))
            .ShouldBeNull();
    }

    [Fact]
    public void Super_kullanici_reddedilir()
    {
        var violation = DatabaseRolePolicy.Violation(new DatabaseRole("postgres", IsSuperuser: true, BypassesRls: false));

        violation.ShouldNotBeNull();
        violation.ShouldContain("postgres");
        violation.ShouldContain("süper kullanıcı");
    }

    /// <summary>BYPASSRLS süper kullanıcı olmadan da RLS'i atlar — ayrıca yakalanmalı.</summary>
    [Fact]
    public void Bypassrls_rolu_reddedilir()
    {
        var violation = DatabaseRolePolicy.Violation(new DatabaseRole("mesnet_system", IsSuperuser: false, BypassesRls: true));

        violation.ShouldNotBeNull();
        violation.ShouldContain("BYPASSRLS");
    }

    [Fact]
    public void Ikisi_birden_varsa_super_kullanici_bildirilir()
    {
        DatabaseRolePolicy.Violation(new DatabaseRole("postgres", IsSuperuser: true, BypassesRls: true))
            .ShouldNotBeNull()
            .ShouldContain("süper kullanıcı");
    }
}
