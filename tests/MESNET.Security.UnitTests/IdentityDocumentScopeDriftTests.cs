using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Kimlik katmanı belgelerinde (<c>UserAccount</c>, <c>UserInvitation</c>) kapsam kilidi.
///
/// <para><b>Neden gerekli:</b> <c>UserAccount</c> ve <c>UserInvitation</c>
/// <c>DocumentTenancyMap</c>'te <c>Identity</c> sınıfındadır — kiracıya <i>ait</i> değil, onu ya
/// da kullanıcıyı tanımlarlar. Marten'ın conjoined kiracılığı (<c>AllDocumentsAreMultiTenanted</c>)
/// bu belgeleri SÜZMEZ; satırlarında kiracı damgası yoktur. Yani her kapsam kararı burada
/// <b>elle</b> verilmek zorundadır — kiracılık kapısı bu yüzeyde hiçbir işi yapmaz.</para>
///
/// <para><b>Neden derleyici yakalayamaz:</b> <c>session.Query&lt;UserAccount&gt;()</c> geçerli bir
/// çağrıdır ve institution/branş/business kapsamına hiç bakmadan da derlenir, hiç bakmadan da
/// çalışır. Yeni bir handler kapsamı unutup sorguyu doğrudan yazarsa hiçbir davranış testi
/// kırılmaz — okullar arası sızıntı <b>sessizce</b> açılır. Tek savunma, çağrının kaynakta ya
/// kapsamı çözen tek kapıdan (<c>UserScopeResolver</c>) geçmesi ya da gerekçesi yazılı bir izin
/// listesinde olmasıdır.</para>
///
/// <para><b>Doğrusu:</b> okuma tarafında kapsam <c>UserScopeResolver.ResolveAsync</c> ile
/// aktörün claim'lerinden (kurum kimliği + alt ağaç) türetilir; sorgu handler'ı dönen kimlik
/// listesiyle süzer (bkz. <c>UserQueryHandler</c>, <c>InvitationHandler</c> — Task 3/4 ile bu
/// çözücüyü çağırdıkları için bu listede DEĞİLLER).</para>
///
/// <para><b>Belgelenmiş sınırlama — işaretleyici DOSYA düzeyindedir, ÇAĞRI düzeyinde değil.</b>
/// <see cref="ResolverMarker"/> kontrolü tüm dosya metnini tarar: aynı dosyada TEK bir çağrı
/// <c>UserScopeResolver</c> kullanıyorsa, o dosyadaki DİĞER tüm kimlik-belgesi çağrıları da
/// (kapsamsız olsalar bile) geçmiş sayılır. <c>InvitationHandler.cs</c> bunun canlı örneğidir
/// (#284): <c>ApproveInvitationHandler</c>, <c>RejectInvitationHandler</c> ve
/// <c>ResendInvitationHandler</c> davetini <c>session.LoadAsync&lt;UserInvitation&gt;(...)</c>
/// ile hiçbir kurum kontrolü olmadan çeker — başka okulun davetini onaylayabilir/reddedebilir/
/// yeniden gönderebilir. Ama aynı dosyadaki <c>GetInvitationsHandler</c>, listelemede
/// <c>UserScopeResolver</c> çağırdığı için dosya bütünüyle "işaretli" sayılır ve bu üç handler
/// kilide GÖRÜNMEZ kalır. Bu, kilidin bilinen bir açığıdır — çözümü bu testin kapsamı DIŞINDADIR
/// (çağrı-bazlı işaretleme AST/Roslyn analizi gerektirir, basit metin taramasıyla güvenilir
/// yapılamaz); kayıt burada tutulur ki gelecekte biri "test yeşil, öyleyse güvenli" sanmasın.</para>
///
/// <para><b><c>RoleIntegrityHandler.cs</c> (role-integrity) BİLEREK izin listesinde
/// DEĞİL — test bu dosya yüzünden KIRMIZI kalır ve bu bir hata değil, henüz verilmemiş açık
/// bir ürün kararının kaydıdır (#283).</b> Ucun <c>GET /api/security/role-integrity</c> hiçbir kapsam
/// taşımıyor: rapor tüm okulların <c>UserAccount</c>/<c>UserInvitation</c> kayıtlarını tarıyor.
/// Karar verilmesi gereken şey şu: bu tanılama <b>kurum düzeyinde</b> mi olmalı (yalnız kendi
/// okulunun bozuk kayıtlarını gösteren bir uç) yoksa <b>platform düzeyinde</b> mi (bilerek
/// kurum-üstü, <c>platform:</c> önekli bir izinle korunan bir sistem-sağlığı ucu)? İkisi de
/// meşru bir tasarımdır ama ikisi ayrı bir izin/kapsam sözleşmesi gerektirir ve o seçim henüz
/// yapılmadı. <b>Bu dosyayı izin listesine eklemek o kararı VERMEK anlamına gelir</b> — testi
/// susturmak değil. Ekleyen kişi aynı zamanda ucun kapsamını (ya <c>UserScopeResolver</c>'a
/// bağlayarak ya da <c>platform:</c> önekli bir izinle kilitleyerek) de karara bağlamış
/// olmalıdır; yalnız satırı eklemek bu kilidin amacını boşa çıkarır.</para>
/// </summary>
public sealed class IdentityDocumentScopeDriftTests
{
    /// <summary>
    /// Kimlik katmanı belgesini sorgulayan YA DA kimlikle tek kayıt çeken çağrı. <c>Query</c>
    /// koleksiyon tarar; <c>LoadAsync</c>/<c>LoadManyAsync</c> id ile doğrudan çeker — ikisi de
    /// aynı kapsam kararına muhtaçtır, id ile çekmek "zaten kapsamlı" anlamına gelmez (bkz.
    /// #284: üç davet yazma handler'ı tam olarak bunu, kontrolsüz, yapıyordu). Üç metot adı ve
    /// iki tip tek regex'te birleştirilmiştir — alternation aradaki "&gt;" ile "(" karakterlerini
    /// bitiştirmediği için bu dosyanın kendi kaynağında (alan bildirimi, aşağıda) yanlışlıkla
    /// kendi kendine eşleşmez.
    /// </summary>
    private static readonly Regex IdentityDocumentQueryCall = new(
        @"\b(?:Query|LoadAsync|LoadManyAsync)<(?:UserAccount|UserInvitation)>\s*\(", RegexOptions.Compiled);

    /// <summary>Kapsamı çözen TEK kapı — bu ad geçen dosya kararı çözücüye devretmiş sayılır.</summary>
    private const string ResolverMarker = "UserScopeResolver";

    /// <summary>
    /// Çözücüyü çağırmadan kimlik belgesi sorgulayabilecek üretim dosyaları — depo köküne göre
    /// TAM YOL (bkz. <see cref="Relative"/>). Yalnız dosya adını karşılaştırmak, başka bir
    /// modülde aynı adı taşıyan bir dosyanın sessizce izinli sayılmasına yol açardı.
    ///
    /// <para><b>Ölçüldü (2026-08-31):</b> <c>src/Modules/Security/MESNET.Security.Application/</c>
    /// altında <c>Query&lt;UserAccount&gt;()</c>/<c>Query&lt;UserInvitation&gt;()</c> ÇAĞRISI
    /// çağıran TÜM dosyalar tarandı; tarama sonradan <c>LoadAsync</c>/<c>LoadManyAsync</c>'i de
    /// kapsayacak şekilde genişletildi (id ile tek kayıt çekmek de aynı kapsam kararına muhtaçtır
    /// — yukarıdaki "Belgelenmiş sınırlama" paragrafına bakınız) ve tarama TEKRARLANDI: sonuç
    /// KÜMESİ değişmedi, yine aynı on bir dosya eşleşti. On birinden ikisi (<c>UserQueryHandler.cs</c>,
    /// <c>InvitationHandler.cs</c>) artık <see cref="ResolverMarker"/>'ı çağırıyor ve bu listede
    /// DEĞİL. Sekizi aşağıda, gerekçesiyle. Onbirincisi — <c>RoleIntegrityHandler.cs</c> —
    /// BİLEREK burada değil (yukarıdaki sınıf açıklamasına, #283'e bakınız).</para>
    ///
    /// <para><b>Her gerekçe dosya okunarak doğrulandı, tarama sonucundan kopyalanmadı:</b></para>
    ///
    /// <para><c>GuardianLinkGapHandler.cs</c>: sonuç kiracı damgalı <c>GuardianLinkView</c> ile
    /// sınırlıdır (yalnız istek yapan okulun öğrencileri). <c>UserAccount</c> yalnız ÜYELİK
    /// kontrolü için okunur — "bu öğrenci herhangi bir hesaba bağlı mı" — ve hiçbir
    /// <c>UserAccount</c> alanı yanıtta dönmez; okullar arası sızıntı doğurmaz.</para>
    ///
    /// <para><c>ReplayUserAccountsHandler.cs</c>: <c>POST /api/security/users/replay</c> dağıtım
    /// ön koşuludur ve <c>platform:tenant:manage</c> ile korunur — kurum-üstü bir bakım ucudur,
    /// kurum kapsamı tanımı gereği yoktur.</para>
    ///
    /// <para><c>UserManagementHandler.cs</c>: yazma komutlarının çoğu hedefi <c>userAccountId</c>
    /// kimliğiyle (route parametresi) <c>LoadAsync</c> ile alır, dolaşmaz. Kalan
    /// <c>Query&lt;UserAccount&gt;()</c> çağrıları (senkronizasyon, ad-olayı yeniden yayını,
    /// öznitelik temizliği, yönetici-var-mı ölçümü) Keycloak senkronizasyonunun ya da idari bir
    /// bakım işleminin parçasıdır; bunlar doğaları gereği realm geneli çalışır ve BİLEREK kurum
    /// bağı KURMAZ (<c>SyncUsersFromKeycloak</c> yeni hesabı <c>InstitutionId = null</c> ile
    /// yaratır — ADR-0003 adım 2, ilgili yorum satır 602-611'de).</para>
    ///
    /// <para><c>SetActiveInstitutionHandler.cs</c>: hesabı aktörün KENDİ <c>KeycloakUserId</c>'siyle
    /// (token'ın <c>sub</c>'ı) bulur — başka kullanıcının kaydına asla dokunmaz.</para>
    ///
    /// <para><c>UserPermissionProvider.cs</c>: claim dönüşümü — yetkilendirme kurulurken, yani
    /// istek kiracısı daha çözülmeden çalışır (bkz. <c>TenantlessSessionDriftTests</c>'in
    /// <c>IUserPermissionProvider</c>'ı istek-dışı sınıf sayan listesi). Sorgu, dönüşüme verilen
    /// <c>keycloakUserId</c> parametresiyle yine aktörün KENDİ kaydını arar.</para>
    ///
    /// <para><c>AbsenceNotificationEmailConsumer.cs</c>: olay tüketicisi — alıcıları olayın
    /// taşıdığı kimliklerden (<c>StudentId</c>, <c>BusinessId</c>) çözer; sonuç hiçbir HTTP
    /// yanıtına dönmez, yalnız e-posta göndermek için kullanılır.</para>
    ///
    /// <para><c>StaffBranchSyncConsumer.cs</c>: olay tüketicisi — <c>StaffAuthorized</c>
    /// olayının <c>KeycloakId</c>'siyle TEK hesabı bulur, kurum/alan kapsamını o olaydan yazar.</para>
    ///
    /// <para><c>StudentAccountSyncConsumer.cs</c>: olay tüketicisi — <c>StudentRegistered</c>
    /// olayının <c>KeycloakUserId</c>'siyle TEK hesabı bulur, <c>StudentId</c> bağını o olaydan
    /// kurar.</para>
    /// </summary>
    private static readonly string[] AllowedFiles =
    [
        "src/Modules/Security/MESNET.Security.Application/Handlers/GuardianLinkGapHandler.cs",
        "src/Modules/Security/MESNET.Security.Application/Handlers/ReplayUserAccountsHandler.cs",
        "src/Modules/Security/MESNET.Security.Application/Handlers/UserManagementHandler.cs",
        "src/Modules/Security/MESNET.Security.Application/Handlers/SetActiveInstitutionHandler.cs",
        "src/Modules/Security/MESNET.Security.Application/Services/UserPermissionProvider.cs",
        "src/Modules/Security/MESNET.Security.Application/Consumers/AbsenceNotificationEmailConsumer.cs",
        "src/Modules/Security/MESNET.Security.Application/Consumers/StaffBranchSyncConsumer.cs",
        "src/Modules/Security/MESNET.Security.Application/Consumers/StudentAccountSyncConsumer.cs",
    ];

    /// <summary>role-integrity ucunun handler'ı — BİLEREK <see cref="AllowedFiles"/>'ta değil.</summary>
    private const string RoleIntegrityHandlerPath =
        "src/Modules/Security/MESNET.Security.Application/Handlers/RoleIntegrityHandler.cs";

    /// <summary>
    /// Bu kilidin KENDİ dosyası. Tarama <c>tests/</c> ağacını da kapsıyor ve bu dosyanın XML
    /// doc'u yasak çağrının adını (yorum olarak) taşıyor — <see cref="StripComments"/> onu siler,
    /// ama kendi kendine tetiklenmeyi önlemek için yine de gerçek üretim/test koduna ait
    /// olmayan bu tek dosya taramadan hariç tutulur (bkz. <c>CrossTenantQueryDriftTests</c> ile
    /// aynı gerekçe).
    /// </summary>
    private const string SelfPath = "tests/MESNET.Security.UnitTests/IdentityDocumentScopeDriftTests.cs";

    [Fact]
    public void Kimlik_belgesi_sorgusu_cozucuyu_kullanir_ya_da_izinlidir()
    {
        var violations = new List<string>();

        foreach (var file in SourceFiles())
        {
            var code = StripComments(File.ReadAllText(file));
            if (!IdentityDocumentQueryCall.IsMatch(code))
                continue;

            if (code.Contains(ResolverMarker, StringComparison.Ordinal))
                continue;

            var relative = Relative(file);
            if (AllowedFiles.Contains(relative, StringComparer.Ordinal))
                continue;

            violations.Add(relative);
        }

        violations.ShouldBeEmpty(BuildFailureMessage(violations));
    }

    /// <summary>
    /// İhlal mesajı kendini savunmalıdır: genel kural + (varsa) role-integrity için ayrı,
    /// kendi kendine yeten bir paragraf. Böylece "neden RoleIntegrityHandler.cs kırmızı"
    /// sorusunun cevabı test çıktısının İÇİNDE olur, koda gömülü bir doküman aranmaz.
    /// </summary>
    private static string BuildFailureMessage(IReadOnlyList<string> violations)
    {
        var general =
            "UserAccount/UserInvitation sorgulayan dosya UserScopeResolver çağırmıyor ve izin "
            + "listesinde de değil. Bu ikisi Identity sınıfındadır — Marten kiracılığı onları "
            + "SÜZMEZ, kapsam kararı burada elle verilmek zorunda. UserScopeResolver.ResolveAsync "
            + "ile aktörün kurum kimliğinden kapsam türetin; gerçekten kiracı-üstü/kimlik-temelli "
            + "bir okumaysa gerekçesiyle izin listesine ekleyin (bu kilidin amacı mevcut dosyaları "
            + $"yasaklamak değil, YENİ bir dosyayı gerekçelenmeye zorlamaktır). İhlaller: "
            + $"{string.Join(" | ", violations)}";

        if (!violations.Contains(RoleIntegrityHandlerPath, StringComparer.Ordinal))
            return general;

        var roleIntegrityNote =
            "\n\nRoleIntegrityHandler.cs İÇİN: bu bir gözden kaçma DEĞİL, verilmemiş bir ürün "
            + "kararının kaydı. GET /api/security/role-integrity hiçbir kurum kapsamı taşımıyor "
            + "ve karar şudur: bu tanılama uç KURUM DÜZEYİNDE mi olmalı (yalnız kendi okulunun "
            + "bozuk kayıtlarını gösterir) yoksa PLATFORM DÜZEYİNDE mi (bilerek kurum-üstü bir "
            + "sistem-sağlığı ucu, platform: önekli izinle korunur)? Takip: #283. Bu dosyayı izin "
            + "listesine eklemek o kararı VERMEK anlamına gelir, testi susturmak değil — "
            + "ekleyen kişi ucun kapsamını da (UserScopeResolver'a bağlayarak ya da platform: "
            + "önekli bir izinle) karara bağlamalıdır.";

        return general + roleIntegrityNote;
    }

    /// <summary>
    /// Satır ve blok yorumlarını atar: bu kuralın NEDENİNİ anlatan XML doc'lar yasak çağrının
    /// adını (UserAccount, UserInvitation, UserScopeResolver) geçirir. Yorumu koda saymak doğru
    /// yazılmış dosyayı ihlal gösterirdi.
    /// </summary>
    private static string StripComments(string source)
    {
        var withoutBlocks = Regex.Replace(source, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        return Regex.Replace(withoutBlocks, @"//.*$", string.Empty, RegexOptions.Multiline);
    }

    /// <summary>
    /// Tarama <c>src/</c> ile sınırlı DEĞİLDİR — <c>tests/</c> ağacı da dahildir. <see cref="SelfPath"/>
    /// kilidin kendi dosyasıdır ve ayrı bir gerekçeyle hariç tutulur (yukarıya bakınız).
    /// </summary>
    private static IEnumerable<string> SourceFiles()
    {
        var repoRoot = RepoRoot();
        var roots = new[] { Path.Combine(repoRoot, "src"), Path.Combine(repoRoot, "tests") };
        var obj = $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}";
        var bin = $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}";

        return roots
            .SelectMany(root => Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            .Where(f => !f.Contains(obj, StringComparison.Ordinal)
                     && !f.Contains(bin, StringComparison.Ordinal)
                     && !string.Equals(Relative(f), SelfPath, StringComparison.Ordinal));
    }

    /// <summary>
    /// Depo köküne göre göreli yol, her zaman <c>/</c> ile ayrılmış. <see cref="AllowedFiles"/>
    /// karşılaştırması bu normalizasyona dayanır — Windows'ta <c>Path.DirectorySeparatorChar</c>
    /// <c>\</c> olduğundan normalize edilmezse aynı dosya platforma göre farklı dizgeye çevrilir.
    /// </summary>
    private static string Relative(string file) =>
        Path.GetRelativePath(RepoRoot(), file).Replace(Path.DirectorySeparatorChar, '/');

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MESNET.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Depo kökü bulunamadı (MESNET.slnx aranıyordu).");
    }
}
