namespace MESNET.Common.Shared.Security;

/// <summary>
/// Bir role sahip kullanıcıya DIRECT (bireysel) olarak atanabilecek yetkilerin sınırı.
/// "yetkili olan herkes yapabilir" ilkesinin guardrail'i: yetki kime atanabilir KISITLI olmalı —
/// örn. işletme sahibine (CompanyManager) kurum-yönetimi (institution:*) yetkisi ATANAMAZ.
/// Rol-bazlı atama (changeRoles) zaten domain-sınırlıdır; bu kısıt direct-permission grant'ı içindir.
/// </summary>
public static class AssignablePermissionScope
{
    private const string All = "*";

    // Rol → o role sahip kullanıcıya atanabilecek yetki domain (prefix) kümesi.
    private static readonly Dictionary<string, string[]> ByRole = new()
    {
        // Müdür / müdür yardımcısı — tam yetkili (her şey atanabilir)
        [MesnetRoles.InstitutionManager] = [All],

        // Kurum personeli — kullanıcı/rol yönetimi hariç operasyonel domainler
        [MesnetRoles.InstitutionStaff] =
        [
            "institution:", "student:", "internship:", "attendance:", "salary:",
            "document:", "communication:", "coordinator:", "department:", "company:",
        ],

        // Koordinatör öğretmen — koordinasyon/staj odaklı; kurum-yönetimi/işletme-yönetimi YOK
        [MesnetRoles.Teacher] =
        [
            "coordinator:", "internship:", "attendance:", "document:", "communication:", "student:",
        ],

        // Alan şefi — öğretmen kapsamı + alan dağıtımı
        [MesnetRoles.DepartmentHead] =
        [
            "coordinator:", "internship:", "attendance:", "document:", "communication:", "student:", "department:",
        ],

        // İşletme yetkilisi — yalnız işletme-tarafı domainler; kurum/koordinasyon/öğrenci-yönetimi/maaş ATANAMAZ
        [MesnetRoles.CompanyManager] =
        [
            "company:", "attendance:", "communication:",
        ],

        // Öğrenci — yalnız kendi alanı + iletişim
        [MesnetRoles.Student] =
        [
            "student:", "communication:",
        ],
    };

    /// <summary>Verilen rollere sahip bir kullanıcıya bu yetki DIRECT olarak atanabilir mi?</summary>
    public static bool CanAssign(IEnumerable<string> userRoles, string permission)
    {
        var allowed = userRoles.SelectMany(r => ByRole.GetValueOrDefault(r, [])).ToHashSet();
        if (allowed.Contains(All)) return true;
        return allowed.Any(prefix => permission.StartsWith(prefix, StringComparison.Ordinal));
    }
}
