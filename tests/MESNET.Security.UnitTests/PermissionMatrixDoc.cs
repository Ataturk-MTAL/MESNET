using System.Text;
using MESNET.Common.Shared.Security;

namespace MESNET.Security.UnitTests;

/// <summary>
/// ADR-0002'nin <b>üretilen</b> bölümünü koddan kurar.
///
/// <para><b>Neden üretiliyor:</b> elle yazılan bir izin matrisi ilk yeni izinde çürür ve
/// çürüdüğü fark edilmez — doküman "referans kaynak" olduğu için yanlış hâli doğru sanılır.
/// <see cref="PermissionMatrixDocTests"/> üretilen metni ADR'deki blokla karşılaştırır; sapma
/// kırmızı testtir.</para>
///
/// <para>Biçim burada tek yerde tanımlıdır: hem üretici hem doğrulayıcı aynı metni kurar.</para>
/// </summary>
public static class PermissionMatrixDoc
{
    public const string BeginMarker = "<!-- BEGIN generated: permission-matrix -->";
    public const string EndMarker = "<!-- END generated: permission-matrix -->";

    /// <summary>
    /// Matris sütun sırası — okuldan işletmeye, geniş yetkiden dara.
    ///
    /// <para><b>Elle tutulur, <see cref="MesnetRoles.All"/>'dan üretilmez.</b> Bu, bilinçli bir
    /// kaynak-tarama kilidinin tersidir: burada eksik kalan rol matriste hiç görünmez, ADR de
    /// üretilen metinle eşleştiği için <c>PermissionMatrixDocTests.ADR_izin_matrisi_kodla_ayni</c>
    /// yeşil kalır — iki taraf da aynı rolü atladığı için sapma görünmez. Bu kör noktayı
    /// <see cref="PermissionMatrixDocTests.RoleOrder_ve_ShortLabels_MesnetRoles_All_ile_birebir_ayni"/>
    /// kapatır: bu liste <see cref="MesnetRoles.All"/> ile birebir aynı değilse kırmızı test verir.</para>
    /// </summary>
    internal static readonly string[] RoleOrder =
    [
        MesnetRoles.InstitutionManager,
        MesnetRoles.DeputyDirector,
        MesnetRoles.InstitutionStaff,
        MesnetRoles.DepartmentHead,
        MesnetRoles.Teacher,
        MesnetRoles.CompanyManager,
        MesnetRoles.MasterTrainer,
        MesnetRoles.CompanyHR,
        MesnetRoles.Student,
        MesnetRoles.Parent,
        MesnetRoles.ProvincialAdmin,
        MesnetRoles.DistrictAdmin,
        MesnetRoles.SystemAdmin
    ];

    /// <summary>
    /// Tablo başlığı için kısaltmalar — tam ad matrisi okunmaz genişliğe çıkarır.
    /// <see cref="RoleOrder"/> ile aynı kör nokta ve aynı kilit geçerlidir.
    /// </summary>
    internal static readonly Dictionary<string, string> ShortLabels = new()
    {
        [MesnetRoles.InstitutionManager] = "MÜD",
        [MesnetRoles.DeputyDirector] = "MYRD",
        [MesnetRoles.InstitutionStaff] = "PERS",
        [MesnetRoles.DepartmentHead] = "AŞEF",
        [MesnetRoles.Teacher] = "ÖĞRT",
        [MesnetRoles.CompanyManager] = "İŞYT",
        [MesnetRoles.MasterTrainer] = "USTA",
        [MesnetRoles.CompanyHR] = "İİK",
        [MesnetRoles.Student] = "ÖĞRC",
        [MesnetRoles.Parent] = "VELİ",
        // "İL" ve "İLÇE" tek başına belirsiz (il/ilçe müdürlüğü mü, il/ilçe okulu mu?) —
        // diğer kısaltmalar gibi 3-4 harfte rolü tek anlama indiriyor, MEM (millî eğitim
        // müdürlüğü) ekleniyor.
        [MesnetRoles.ProvincialAdmin] = "İLMEM",
        [MesnetRoles.DistrictAdmin] = "İLÇMEM",
        [MesnetRoles.SystemAdmin] = "SİSY"
    };

    /// <summary>Domain öneki → o önekteki izinler. Sıra <c>Permissions.GetAll()</c> sırasıdır.</summary>
    private static IReadOnlyList<(string Prefix, List<string> Permissions)> Domains()
    {
        var byPrefix = new List<(string, List<string>)>();

        foreach (var permission in Permissions.GetAll())
        {
            var prefix = permission[..(permission.IndexOf(':') + 1)];
            var bucket = byPrefix.FirstOrDefault(b => b.Item1 == prefix);
            if (bucket.Item2 is null)
            {
                bucket = (prefix, []);
                byPrefix.Add(bucket);
            }

            bucket.Item2.Add(permission);
        }

        return byPrefix;
    }

    /// <summary>Rolün ham (genişletilmemiş) demetindeki wildcard önekleri.</summary>
    private static IReadOnlyList<string> WildcardPrefixesOf(string role) =>
        [.. RolePermissionMap.GetRawPermissionsForRole(role)
            .Where(p => p.EndsWith('*'))
            .Select(p => p[..^1])
            .Order(StringComparer.Ordinal)];

    /// <summary>ADR'ye gömülecek üretilmiş markdown bloğu.</summary>
    public static string Build()
    {
        var sb = new StringBuilder();
        sb.AppendLine(BeginMarker);
        sb.AppendLine();
        sb.AppendLine("> Bu bölüm **koddan üretilir** — elle düzenlenmez. Değişiklik için");
        sb.AppendLine("> `Permissions.cs` / `RolePermissionMap.cs` düzenlenir ve");
        sb.AppendLine("> `PermissionMatrixDocTests` çalıştırılır (sapma kırmızı testtir).");
        sb.AppendLine();

        AppendRoleTotals(sb);
        AppendDomainSummary(sb);
        AppendMatrix(sb);
        AppendNeverAssignable(sb);

        sb.Append(EndMarker);
        return sb.ToString();
    }

    private static void AppendRoleTotals(StringBuilder sb)
    {
        sb.AppendLine("### Rol başına toplam izin");
        sb.AppendLine();
        sb.AppendLine("Wildcard'lar genişletilmiş hâliyle.");
        sb.AppendLine();
        sb.AppendLine("| Rol | İzin | Wildcard önekleri |");
        sb.AppendLine("| --- | ---: | --- |");

        foreach (var role in RoleOrder)
        {
            var total = RolePermissionMap.GetPermissionsForRoles([role]).Count;
            var wildcards = WildcardPrefixesOf(role);
            var wildcardText = wildcards.Count == 0
                ? "—"
                : string.Join(" ", wildcards.Select(w => $"`{w}*`"));

            sb.AppendLine($"| {MesnetRoles.Find(role)!.Label} (`{role}`) | {total} | {wildcardText} |");
        }

        sb.AppendLine();
    }

    private static void AppendDomainSummary(StringBuilder sb)
    {
        sb.AppendLine("### Domainler");
        sb.AppendLine();
        sb.AppendLine("| Önek | İzin | `önek:*` wildcard'ını taşıyan roller |");
        sb.AppendLine("| --- | ---: | --- |");

        foreach (var (prefix, permissions) in Domains())
        {
            var holders = RoleOrder
                .Where(r => WildcardPrefixesOf(r).Contains(prefix))
                .Select(r => MesnetRoles.Find(r)!.Label)
                .ToList();

            var holderText = holders.Count == 0 ? "— (her rol tek tek alır)" : string.Join(", ", holders);
            sb.AppendLine($"| `{prefix}` | {permissions.Count} | {holderText} |");
        }

        sb.AppendLine();
    }

    private static void AppendMatrix(StringBuilder sb)
    {
        sb.AppendLine("### Tam matris");
        sb.AppendLine();
        sb.AppendLine("`●` rol haritasında **açık satır** · `○` **wildcard'dan** geliyor · `·` yok");
        sb.AppendLine();
        sb.AppendLine("Ayrım önemlidir: açık satır silinirse izin kaybolur, wildcard'dan gelen kaybolmaz.");
        sb.AppendLine();

        sb.AppendLine("| İzin | " + string.Join(" | ", RoleOrder.Select(r => ShortLabels[r])) + " |");
        sb.AppendLine("| --- | " + string.Join(" | ", RoleOrder.Select(_ => ":-:")) + " |");

        foreach (var (prefix, permissions) in Domains())
        {
            sb.AppendLine($"| **`{prefix}`** | " + string.Join(" | ", RoleOrder.Select(_ => "")) + " |");

            foreach (var permission in permissions)
            {
                var cells = RoleOrder.Select(role => Mark(role, permission));
                sb.AppendLine($"| `{permission}` | " + string.Join(" | ", cells) + " |");
            }
        }

        sb.AppendLine();
    }

    private static string Mark(string role, string permission)
    {
        var raw = RolePermissionMap.GetRawPermissionsForRole(role);

        if (raw.Contains(permission, StringComparer.OrdinalIgnoreCase))
            return "●";

        return RolePermissionMap.GetPermissionsForRoles([role])
            .Contains(permission, StringComparer.OrdinalIgnoreCase)
            ? "○"
            : "·";
    }

    private static void AppendNeverAssignable(StringBuilder sb)
    {
        sb.AppendLine("### Bireysel (direct) atanamayan izinler");
        sb.AppendLine();
        sb.AppendLine("Hiçbir yapılandırmayla tek bir kullanıcıya verilemez —");
        sb.AppendLine("`AssignablePermissionScope.NeverDirectlyAssignable` sabit listesi yapılandırmayı ezer.");
        sb.AppendLine();

        foreach (var permission in AssignablePermissionScope.NeverDirectlyAssignable.Order(StringComparer.Ordinal))
            sb.AppendLine($"- `{permission}`");

        sb.AppendLine();
    }
}
