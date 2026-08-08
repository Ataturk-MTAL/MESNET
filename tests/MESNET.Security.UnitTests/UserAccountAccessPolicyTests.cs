using System.Text.RegularExpressions;
using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Silinmiş kullanıcı erişim ÜRETMEZ (#210).
///
/// <para><b>Yaşanan açık:</b> <c>DeleteUser</c> hem Keycloak kullanıcısını hem
/// <c>UserAccount</c> kaydını siliyordu. Kullanıcının elindeki erişim token'ı ise imzalıydı ve
/// API onu yalnız imza + <c>exp</c> üzerinden doğruluyor — introspection ya da iptal kontrolü
/// yok. Sonuç iki aşamalıydı:</para>
///
/// <list type="number">
///   <item>İlk 5 dakika: izin önbelleğindeki girdi yaşamaya devam ediyor.</item>
///   <item>Sonrası: kayıt artık YOK → <c>PermissionClaimsTransformation</c> "bu kullanıcının
///   kaydı henüz oluşturulmamış" sanıp <b>token yedeğine</b> düşüyor ve izinleri
///   <c>realm_access</c> rollerinden <b>yeniden türetiyor</b>.</item>
/// </list>
///
/// <para>Pencere token ömrüyle sınırlıydı — realm'de <c>accessTokenLifespan: 1800</c>, yani
/// <b>30 dakika tam yetki</b>.</para>
///
/// <para><b>Asimetri:</b> pasife alma korunuyordu (<c>IsEnabled</c> kontrolü + önbellek
/// temizliği), silme korunmuyordu — çünkü kontrol kaydın <i>var olmasına</i> dayanıyor ve kayıt
/// siliniyordu. "Erişimi kes" için iki yol vardı ve <b>daha kesin görüneni zayıf olanıydı.</b></para>
///
/// <para>Çözüm: kayıt silinmez, <b>mezar taşı</b> bırakılır. Kayıt bulunmaya devam eder, karar
/// aşağıdaki politikadan geçer ve token yedeğine hiç düşülmez.</para>
/// </summary>
public sealed class UserAccountAccessPolicyTests
{
    private static readonly DateTime SilinmeAni = new(2026, 8, 6, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Aktif_ve_silinmemis_hesap_erisim_uretir()
    {
        UserAccountAccessPolicy.GrantsAccess(isEnabled: true, deletedAt: null).ShouldBeTrue();
    }

    [Fact]
    public void Pasife_alinmis_hesap_erisim_uretmez()
    {
        UserAccountAccessPolicy.GrantsAccess(isEnabled: false, deletedAt: null).ShouldBeFalse();
    }

    /// <summary>#210'un tam kendisi.</summary>
    [Fact]
    public void Silinmis_hesap_erisim_uretmez()
    {
        UserAccountAccessPolicy.GrantsAccess(isEnabled: true, deletedAt: SilinmeAni).ShouldBeFalse();
    }

    /// <summary>
    /// <c>IsEnabled</c> ile <c>DeletedAt</c> birbirinin yedeği DEĞİLDİR — ikisi ayrı ayrı
    /// kapatır. Silme sırasında <c>IsEnabled</c>'ı düşürmek unutulsa bile erişim doğmamalı.
    /// </summary>
    [Fact]
    public void Silinmis_ama_aktif_isaretli_hesap_yine_erisim_uretmez()
    {
        UserAccountAccessPolicy.GrantsAccess(isEnabled: true, deletedAt: SilinmeAni).ShouldBeFalse();
    }

    /// <summary>
    /// <b>Sert silme geri gelmemeli (#210).</b> Kayıt gerçekten silinirse
    /// <c>PermissionClaimsTransformation</c> onu bulamaz, "kaydı henüz yok" sanar ve token
    /// yedeğine düşer — açık aynen geri gelir. Politika bunu göremez, çünkü çağrılmaz bile.
    ///
    /// <para>Bu yüzden karar testine ek olarak <b>kaynak taraması</b> gerekiyor: ileride biri
    /// "mezar taşları birikiyor" diye temizlik yaparken sert silmeye dönerse test kırılsın.</para>
    /// </summary>
    [Fact]
    public void UserAccount_sert_silinmiyor()
    {
        var kok = Path.Combine(RepoRoot(), "src", "Modules", "Security");
        var desen = new Regex(@"session\.Delete\s*\(\s*account\s*\)|Delete<UserAccount>", RegexOptions.Compiled);
        var violations = new List<string>();

        foreach (var file in Directory.EnumerateFiles(kok, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                continue;

            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
                if (desen.IsMatch(lines[i]))
                    violations.Add($"{Path.GetFileName(file)}:{i + 1}  {lines[i].Trim()}");
        }

        violations.ShouldBeEmpty(
            "UserAccount SERT SİLİNEMEZ (#210). Kayıt silinirse izin dönüşümü onu bulamaz, "
            + "\"kaydı henüz yok\" sanar ve token'daki rollerden izin türetir — silinen kullanıcı "
            + "token'ı sona erene kadar (realm'de 1800 sn) tam yetkiyle çalışır. Silme yerine "
            + "DeletedAt damgalayın:\n  " + string.Join("\n  ", violations));
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MESNET.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Depo kökü bulunamadı (MESNET.slnx aranıyordu): {AppContext.BaseDirectory}");
    }
}
