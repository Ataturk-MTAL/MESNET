using System.Text.Json;
using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Rol modelinin <b>tek doğruluk kaynağı</b> kilidi (#129).
///
/// <para>#129'un kök nedeni sapmaydı: arayüz elle yazılmış bir rol listesi tutuyordu ve o liste
/// gerçek rollerle eşleşmiyordu (<c>deputy_director</c>, <c>coordinator_teacher</c>,
/// <c>master_trainer</c> — hiçbirinin karşılığı yoktu; <c>InstitutionStaff</c> ve <c>Teacher</c>
/// hiç yoktu). Karşılığı olmayan rol adı Keycloak'ta çözülemediği için kullanıcı sıfır realm
/// rolüyle açılıyor, hiçbir izin almıyor ve hata da görmüyordu.</para>
///
/// <para>Aşağıdaki testler dört listeyi birbirine kilitler: <see cref="MesnetRoles.All"/>,
/// <see cref="RolePermissionMap"/> anahtarları, <see cref="AssignablePermissionScope.Defaults"/>
/// anahtarları ve Keycloak realm tanımı (<c>mesnet-realm.json</c>). Biri değişip diğeri
/// unutulursa <b>kırmızı test</b> çıkar — sessiz yetki kaybı değil.</para>
/// </summary>
public sealed class RoleModelDriftTests
{
    /// <summary>Realm rol adları — test derlemesinin yanına kopyalanan gerçek realm dosyasından.</summary>
    private static IReadOnlyList<string> ReadRealmRoleNames()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "mesnet-realm.json");
        File.Exists(path).ShouldBeTrue(
            $"Realm tanımı test çıktısına kopyalanmadı: {path}. csproj'daki Content girdisine bakın.");

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return [.. doc.RootElement
            .GetProperty("roles")
            .GetProperty("realm")
            .EnumerateArray()
            .Select(r => r.GetProperty("name").GetString()!)];
    }

    [Fact]
    public void Rol_listesi_ile_izin_haritasi_birebir_ayni()
    {
        foreach (var role in MesnetRoles.All)
            RolePermissionMap.GetRawPermissionsForRole(role)
                .ShouldNotBeEmpty($"{role} rolünün izin demeti tanımlanmamış (RolePermissionMap).");
    }

    [Fact]
    public void Izin_haritasinda_bilinmeyen_rol_yok()
    {
        // Ters yön: haritada olup MesnetRoles.All'da olmayan bir anahtar, hiçbir kullanıcıya
        // ulaşmayan ölü bir demettir — ya rol silinmiş ya adı değişmiştir.
        foreach (var role in MesnetRoles.All)
            MesnetRoles.IsValid(role).ShouldBeTrue();

        RolePermissionMap.GetRawPermissionsForRole("OlmayanRol").ShouldBeEmpty();
    }

    [Fact]
    public void Rol_listesi_ile_atanabilir_kapsam_varsayilanlari_birebir_ayni()
    {
        AssignablePermissionScope.Defaults.Keys.OrderBy(k => k)
            .ShouldBe(MesnetRoles.All.OrderBy(k => k));
    }

    /// <summary>
    /// <b>Asıl kilit:</b> Keycloak realm rol adları ile <see cref="MesnetRoles.All"/> birebir aynı.
    /// Realm'de olmayan bir rol adı çalışma zamanında çözülemez; kodda olmayan bir realm rolü de
    /// hiçbir izne bağlanmaz. İkisi de sessiz yetki kaybıdır.
    /// </summary>
    [Fact]
    public void Realm_tanimindaki_rol_adlari_kod_ile_birebir_ayni()
    {
        ReadRealmRoleNames().OrderBy(r => r)
            .ShouldBe(MesnetRoles.All.OrderBy(r => r));
    }

    [Fact]
    public void Rol_katalogunda_her_rolun_turkce_etiketi_ve_aciklamasi_var()
    {
        foreach (var role in MesnetRoles.Catalog)
        {
            role.Label.ShouldNotBeNullOrWhiteSpace();
            role.Description.ShouldNotBeNullOrWhiteSpace();
            // Etiket Türkçedir, ad İngilizce — arayüz ham rol adı basmasın diye ayrılar.
            role.Label.ShouldNotBe(role.Name);
        }
    }

    [Fact]
    public void Rol_adlari_tekrar_etmez()
    {
        MesnetRoles.All.Distinct(StringComparer.OrdinalIgnoreCase).Count()
            .ShouldBe(MesnetRoles.All.Count);
    }

    [Theory]
    [InlineData("deputy_director")]
    [InlineData("coordinator_teacher")]
    [InlineData("master_trainer")]
    [InlineData("institution_manager")]
    [InlineData("")]
    [InlineData(null)]
    public void Eski_arayuz_listesindeki_uydurma_adlar_gecersizdir(string? role)
    {
        MesnetRoles.IsValid(role).ShouldBeFalse();
    }

    [Fact]
    public void Rol_adi_buyuk_kucuk_harfe_duyarsiz_dogrulanir()
    {
        // Keycloak rol eşlemesi de büyük/küçük harfe duyarsız çalışır; doğrulama daha katı olmamalı.
        MesnetRoles.IsValid("institutionmanager").ShouldBeTrue();
        MesnetRoles.Find("DEPUTYDIRECTOR")!.Name.ShouldBe(MesnetRoles.DeputyDirector);
    }
}
