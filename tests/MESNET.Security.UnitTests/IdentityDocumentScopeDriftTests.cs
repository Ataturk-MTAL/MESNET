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
/// <see cref="ScopeDecisionMarkers"/> kontrolü tüm dosya metnini tarar: aynı dosyada TEK bir çağrı
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
/// <para><b>Karara bağlandı — <c>RoleIntegrityHandler.cs</c> artık çözücüyü çağırıyor (#283).</b>
/// <c>GET /api/security/role-integrity</c> uzun süre kapsamsızdı ve bu test onun yüzünden
/// bilerek KIRMIZI bırakılmıştı. Karar <b>kurum düzeyi</b> oldu: yerel iki bacak (davetler,
/// hesaplar) diğer üç uçla aynı kapıdan (<see cref="ScopeDecisionMarkers"/>) geçiyor. Belirleyici
/// gerekçe, ucun kendi tasarımıydı — raporu görmesi gereken kişi düzeltmeyi de yapacak olandır
/// ve düzeltme ucu (<c>POST /api/security/users/{id}/roles</c>) kurum kapsamlıdır; rapor
/// platforma çekilseydi gören ile düzeltebilen ayrılırdı. Realm bacağı (Keycloak'ta hiç rolü
/// olmayan hesaplar) daraltılamaz — orada kurum kavramı yok — ve ayrı bir izne
/// (<c>platform:tenant:manage</c>) bağlandı; yetkisi olmayanda boş döner ve boş olduğu
/// <c>RealmScanPermitted</c> ile SÖYLENİR.</para>
///
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

    /// <summary>
    /// Kapsam kararının <b>fiilen verildiğini</b> gösteren ÇAĞRI kalıpları (#284).
    ///
    /// <para><b>Neden tip adı yetmiyor:</b> ilk sürüm yalnız <c>UserScopeResolver</c> dizesini
    /// arıyordu. Ölçüldü — bu, kararın VERİLDİĞİNİ değil parametrenin BİLDİRİLDİĞİNİ kanıtlar:
    /// gövdeden kontrolü silip imzadaki <c>UserScopeResolver scopeResolver</c> parametresini
    /// bırakan bir mutasyon kilidi olduğu gibi geçti. Derleyici de susar — kullanılmayan metot
    /// parametresi C#'ta uyarı üretmez.</para>
    ///
    /// <para>İki ayrı meşru mekanizma vardır ve ikisi de kapsam kararıdır: çözücü
    /// <b>görünürlük</b> sorusunu cevaplar (bu kaydı okuyabilir miyim),
    /// <c>UserInstitutionScopePolicy</c> ise <b>atama</b> sorusunu (bu kuruma yazabilir miyim).
    /// Tek işaretle yetinmek, ikinci mekanizmayı doğru kullanan sınıfı ihlal gösterirdi.</para>
    /// </summary>
    private static readonly Regex[] ScopeDecisionMarkers =
    [
        new(@"[Ss]copeResolver\s*\.\s*ResolveAsync\s*\(", RegexOptions.Compiled),
        new(@"UserScopePolicy\s*\.\s*(?:IsVisible|VisibleInstitutionIds)\s*\(", RegexOptions.Compiled),
        new(@"UserInstitutionScopePolicy\s*\.\s*CanAssign\s*\(", RegexOptions.Compiled),
    ];

    /// <summary>
    /// Kapsam kararı vermeden kimlik belgesi çekebilecek <b>sınıflar</b> — dosya değil.
    ///
    /// <para><c>CompleteInvitationHandler</c>: davet kabul ucu <b>anonimdir</b>
    /// (<c>InvitationEndpoints.cs</c> → <c>.AllowAnonymous()</c>). Davetlinin henüz hesabı
    /// yoktur, dolayısıyla kapsam türetilecek bir aktör de yoktur; davet GUID'i orada kimlik
    /// bilgisinin <b>ta kendisidir</b>. Kapsam kontrolü eklemek ucu kullanılamaz hâle
    /// getirirdi.</para>
    ///
    /// <para><b>Bu liste küçük kalmalı.</b> Büyümesi, kilidin kural olmaktan çıkıp istisnalar
    /// tablosuna döndüğünün işaretidir.</para>
    /// </summary>
    private static readonly HashSet<string> AllowedClasses = new(StringComparer.Ordinal)
    {
        "src/Modules/Security/MESNET.Security.Application/Handlers/InvitationHandler.cs::CompleteInvitationHandler",
    };

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
    /// <c>InvitationHandler.cs</c>) artık <see cref="ScopeDecisionMarkers"/>'ı çağırıyor ve bu listede
    /// DEĞİL. Onbirincisi — <c>RoleIntegrityHandler.cs</c> — #283 kararından sonra o da çözücüyü
    /// çağırıyor ve listede DEĞİL. Sekizi aşağıda, gerekçesiyle.</para>
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
            var relative = Relative(file);
            if (AllowedFiles.Contains(relative, StringComparer.Ordinal))
                continue;

            var code = StripComments(File.ReadAllText(file));
            if (!IdentityDocumentQueryCall.IsMatch(code))
                continue;

            foreach (var (className, body) in ClassBlocks(code))
            {
                if (!IdentityDocumentQueryCall.IsMatch(body))
                    continue;

                if (ScopeDecisionMarkers.Any(marker => marker.IsMatch(body)))
                    continue;

                var key = $"{relative}::{className}";
                if (AllowedClasses.Contains(key))
                    continue;

                violations.Add(key);
            }
        }

        violations.ShouldBeEmpty(FailureMessage(violations));
    }

    /// <summary>
    /// İhlal mesajı kendini savunmalıdır: kuralın NEDENİ ve iki çıkış yolu test çıktısının
    /// İÇİNDE olur, koda gömülü bir doküman aranmaz.
    /// </summary>
    private static string FailureMessage(IReadOnlyList<string> violations) =>
        "UserAccount/UserInvitation sorgulayan dosya UserScopeResolver çağırmıyor ve izin "
        + "listesinde de değil. Bu ikisi Identity sınıfındadır — Marten kiracılığı onları "
        + "SÜZMEZ, kapsam kararı burada elle verilmek zorunda. UserScopeResolver.ResolveAsync "
        + "ile aktörün kurum kimliğinden kapsam türetin; gerçekten kiracı-üstü/kimlik-temelli "
        + "bir okumaysa gerekçesiyle izin listesine ekleyin (bu kilidin amacı mevcut dosyaları "
        + "yasaklamak değil, YENİ bir dosyayı gerekçelenmeye zorlamaktır). İhlaller: "
        + $"{string.Join(" | ", violations)}";

    /// <summary>Üst düzey sınıf bildirimleri.</summary>
    private static readonly Regex ClassDeclaration = new(
        @"(?m)^\s*(?:public|internal)\s+(?:static\s+|sealed\s+|abstract\s+|partial\s+)*class\s+(\w+)",
        RegexOptions.Compiled);

    /// <summary>
    /// Kaynağı sınıf bildirimlerinden böler — işaretleyici artık DOSYA değil SINIF düzeyinde
    /// değerlendirilsin diye (#284).
    ///
    /// <para><b>Sınıf bulunamazsa dosya TEK blok döner</b> — bölünemeyen bir dosyayı sessizce
    /// atlamak, tam da bu kilidin engellemeye çalıştığı şeyi üretirdi. Roslyn gerekmedi: bu
    /// depoda handler'lar üst düzey, tek sorumlu sınıflardır.</para>
    /// </summary>
    private static IEnumerable<(string ClassName, string Body)> ClassBlocks(string code)
    {
        var declarations = ClassDeclaration.Matches(code);

        if (declarations.Count == 0)
        {
            yield return ("(sınıfsız)", code);
            yield break;
        }

        // İlk sınıftan ÖNCEKİ kısım da taranır; atlanırsa orada duran bir çağrı görünmez olurdu.
        if (declarations[0].Index > 0)
            yield return ("(sınıf dışı)", code[..declarations[0].Index]);

        for (var i = 0; i < declarations.Count; i++)
        {
            var start = declarations[i].Index;
            var end = i + 1 < declarations.Count ? declarations[i + 1].Index : code.Length;
            yield return (declarations[i].Groups[1].Value, code[start..end]);
        }
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
