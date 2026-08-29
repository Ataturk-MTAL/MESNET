using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// İl/ilçe yetkilisi rollerinin izin demeti.
///
/// <para><b>Neden yeni izin tanımlanmadı (okuma tarafı):</b> <c>InstitutionManager</c>
/// <c>institution:*</c> taşır. <c>institution:</c> önekli her yeni izin — adı ne olursa
/// olsun — o wildcard üzerinden <b>her okul müdürüne</b> geçer ve bunu kimse fark etmez
/// (ADR-0002 önek tuzağı, #126'da alan muafiyeti izninde bire bir yaşandı). İl yetkilisinin
/// farkı izinde değil ağaçtaki YERİNDEDİR.</para>
///
/// <para><b>Yazma (B parçası):</b> A parçası müdahale yetkisini bilerek ertelemişti — "denetim
/// izi (C) yazılmadan yazma verilmez, sıra bağlayıcıdır". C (Audit) yazıldı; B bu sırayı
/// tamamlar ve <see cref="Permissions.Institution.Manage"/>'i AÇIKÇA verir — wildcard'la değil,
/// <see cref="RolePermissionMap"/>'te tek tek satırla (bkz. <c>DirectoratePermissionMappingTests</c>).
/// Kurum üstü sınır hâlâ geçerlidir: bu roller hâlâ yeni okul açamaz, hâlâ wildcard almaz.</para>
/// </summary>
public sealed class UpperNodeRoleMappingTests
{
    [Fact]
    public void Il_ve_ilce_yetkilisi_kurum_okuma_iznine_sahiptir()
    {
        foreach (var role in new[] { MesnetRoles.ProvincialAdmin, MesnetRoles.DistrictAdmin })
        {
            RolePermissionMap.GetPermissionsForRoles([role])
                .ShouldContain(Permissions.Institution.View, $"{role} kurum listesini görmeli.");
        }
    }

    /// <summary>
    /// <b>B parçasıyla yazma AÇILDI.</b> A parçası "denetim izi (C) yazılmadan yazma
    /// verilmez" diyordu; C (Audit) yazıldı, sıra tamamlandı. Kapsam yine izinden değil
    /// ağaçtaki YERDEN gelir — <see cref="Permissions.Institution.Manage"/> yalnız aktif
    /// bağlamdaki (kendi ili/ilçesi) kurumlara uygulanır.
    /// </summary>
    [Fact]
    public void Il_ve_ilce_yetkilisinin_artik_kurum_yazma_izni_vardir()
    {
        foreach (var role in new[] { MesnetRoles.ProvincialAdmin, MesnetRoles.DistrictAdmin })
        {
            RolePermissionMap.GetPermissionsForRoles([role])
                .ShouldContain(Permissions.Institution.Manage,
                    $"{role} müdahale yetkisi (B parçası) ile kurum künyesini değiştirebilmeli.");
        }
    }

    /// <summary>
    /// Wildcard verilmez. <c>institution:*</c> verilseydi bu roller kurum yazma, personel
    /// yönetimi ve alan kapsamı muafiyeti dahil <b>her</b> institution iznini alırdı.
    /// </summary>
    [Fact]
    public void Il_ve_ilce_yetkilisine_wildcard_verilmez()
    {
        foreach (var role in new[] { MesnetRoles.ProvincialAdmin, MesnetRoles.DistrictAdmin })
        {
            RolePermissionMap.GetPermissionsForRoles([role])
                .ShouldNotContain(Permissions.Institution.AllBranches,
                    $"{role} kapsam muafiyeti izni almamalı.");
        }
    }

    /// <summary>
    /// Kurum üstü izin de verilmez: bu roller yeni okul AÇAMAZ ve başka bir ağaca yazamaz.
    /// Kapsamları ağaçtaki yerleriyle sınırlıdır, izinle genişletilmez.
    /// </summary>
    [Fact]
    public void Il_ve_ilce_yetkilisi_kurum_ustu_degildir()
    {
        foreach (var role in new[] { MesnetRoles.ProvincialAdmin, MesnetRoles.DistrictAdmin })
        {
            RolePermissionMap.GetPermissionsForRoles([role])
                .ShouldNotContain(Permissions.Platform.TenantManage,
                    $"{role} kurum sınırının üstünde çalışamaz — kapsamı ağaçtan gelir.");
        }
    }
}
