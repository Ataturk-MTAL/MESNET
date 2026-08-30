using MESNET.Common.Infrastructure.Tenancy;
using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Institution.UnitTests;

/// <summary>
/// Kiracılar arası okumanın kapsamı. <b>Kimlikler istekten HİÇ gelmez</b>; bu testin varlık
/// nedeni o kuralı kod düzeyinde kilitlemektir.
/// </summary>
public sealed class SubtreeTenantScopeTests
{
    /// <summary>Dizini taklit eder — gerçek Marten gerekmez, karar saf.</summary>
    private sealed class FakeDirectory : IInstitutionSubtreeDirectory
    {
        public string? RequestedPrefix { get; private set; }
        public bool AllRequested { get; private set; }

        public Task<IReadOnlyList<string>> GetSchoolTenantsAsync(
            string pathPrefix, CancellationToken cancellationToken = default)
        {
            RequestedPrefix = pathPrefix;
            return Task.FromResult<IReadOnlyList<string>>(["okul-a", "okul-b"]);
        }

        public Task<IReadOnlyList<string>> GetAllSchoolTenantsAsync(
            CancellationToken cancellationToken = default)
        {
            AllRequested = true;
            return Task.FromResult<IReadOnlyList<string>>(["okul-a", "okul-b", "okul-c"]);
        }
    }

    [Fact]
    public async Task Yol_oneki_olan_aktor_alt_agacini_gorur()
    {
        // Arrange
        var directory = new FakeDirectory();
        var scope = new SubtreeTenantScope(directory);
        var visibility = new InstitutionVisibility(
            Unrestricted: false, PathPrefix: "/il-35/ilce-konak", InstitutionId: null);

        // Act
        var tenants = await scope.ResolveAsync(visibility);

        // Assert
        tenants.ShouldBe(["okul-a", "okul-b"]);
        directory.RequestedPrefix.ShouldBe("/il-35/ilce-konak");
        directory.AllRequested.ShouldBeFalse();
    }

    [Fact]
    public async Task Kapsamsiz_platform_aktoru_butun_okullari_gorur()
    {
        var directory = new FakeDirectory();
        var scope = new SubtreeTenantScope(directory);
        var visibility = new InstitutionVisibility(
            Unrestricted: true, PathPrefix: null, InstitutionId: null);

        var tenants = await scope.ResolveAsync(visibility);

        tenants.Count.ShouldBe(3);
        directory.AllRequested.ShouldBeTrue();
    }

    [Fact]
    public async Task Okul_aktoru_yalniz_kendi_kiracisini_gorur()
    {
        var directory = new FakeDirectory();
        var scope = new SubtreeTenantScope(directory);
        var institutionId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var visibility = new InstitutionVisibility(
            Unrestricted: false, PathPrefix: null, InstitutionId: institutionId);

        var tenants = await scope.ResolveAsync(visibility);

        // Dizine HİÇ gitmez: kendi kiracısını bilmek için sorguya gerek yok.
        tenants.ShouldBe([institutionId.ToString()]);
        directory.RequestedPrefix.ShouldBeNull();
        directory.AllRequested.ShouldBeFalse();
    }

    /// <summary>
    /// Kapsamsız aktör HER ŞEYİ değil HİÇBİR ŞEYİ görür. Boş liste dönmesi çağıranın sorguyu
    /// hiç kurmaması içindir — parametresiz <c>TenantIsOneOf()</c>'un SQL'de ne ürettiğine
    /// güvenilmez.
    /// </summary>
    [Fact]
    public async Task Kapsamsiz_aktor_bos_liste_alir()
    {
        var directory = new FakeDirectory();
        var scope = new SubtreeTenantScope(directory);
        var visibility = new InstitutionVisibility(
            Unrestricted: false, PathPrefix: null, InstitutionId: Guid.Empty);

        var tenants = await scope.ResolveAsync(visibility);

        tenants.ShouldBeEmpty();
        directory.AllRequested.ShouldBeFalse();
    }
}
