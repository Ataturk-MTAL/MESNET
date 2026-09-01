using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Kurum kimliğini <b>istekten</b> alan mesajlar için kapsam kilidi (ADR-0003 adım 6) —
/// ve o kilidin <b>nereye kadar uygulanabildiğinin</b> kaydı (#235).
///
/// <para><b>Neden kilit gerekiyor:</b> kiracılık bu yüzeyi korumaz — <c>Institution</c> belgesi
/// kiracının kendisidir ve kiracı damgası taşımaz. Koruma tek tek uçlara bırakılırsa unutulan
/// biri <b>başka okulun kaydını açar</b> ve sonuç sessizdir: istek 200 döner, log temiz kalır.
/// Ölçüldü — kontrol yokken bir okul müdürü diğer okulun personel listesini okudu, adını
/// değiştirdi ve personel ekledi.</para>
///
/// <para><b>Kural neden yalnız Institution modülünde ZORLANABİLİYOR:</b> <c>IInstitutionScoped</c>
/// <c>MESNET.Institution.Application</c> içinde yaşar ve csproj kuralı bir modülün başka modülün
/// <b>Application</b> katmanını referans etmesini yasaklar (yalnız <c>*.Shared</c> serbesttir).
/// Yani Institution dışındaki mesajlar bu arayüzü <b>uygulayamaz</b>. İlk sürümün taramayı tek
/// modüle sabitlemesi bir eksiklik değil, o görünürlük sınırının yansımasıydı — ama docstring
/// "her command/query" diyordu ve bu, olmayan bir güvence veriyordu.</para>
///
/// <para><b>Bugünkü gerçek:</b> Institution dışında kurum kimliğini istekten alan <b>49 mesaj</b>
/// var. Bunlar <see cref="KnownDebt"/> listesinde <b>donduruldu</b>: test liste <b>büyüyünce</b>
/// kırmızı olur. Amaç refleksi tersine çevirmek — yeni bir mesaj kurum kimliğini istekten
/// <b>alamaz</b>, kiracıdan türetir (#229/#230'un <c>business_id</c> ve <c>student_id</c> için
/// yaptığının aynısı).</para>
///
/// <para><b>Neden 49'una arayüz eklemek çözüm DEĞİL:</b> derlenmez (yukarıdaki csproj sınırı) ve
/// tek çıkış arayüzü <c>Common.Shared</c>'a taşımaktır — o da yanlış deseni, kurum kimliğini
/// istekten almayı, altyapı seviyesinde meşrulaştırır.</para>
///
/// <para><b>Bugünkü riskin sınıfı:</b> çoğu sızıntı değil. Bu mesajların dokunduğu belgeler
/// kiracı damgalıdır; satır <b>aktörün</b> kiracısına düşer (damga <c>IMessageBus.TenantId</c>'den
/// gelir, gövdeden değil) ve yalnız <c>InstitutionId</c> <i>alanı</i> gövdedeki değeri taşır —
/// etiket kayması, sayım/rapor sapması. Gerçek sızıntı ancak <b>Shared</b> sınıflı bir belgeyi
/// istekten gelen kimlikle süzen handler'da olurdu; Coordination dışındaki 15 mesaj denetlendi,
/// öyle bir handler yok. <b>Coordination'ın 34'ü tek tek denetlenmedi.</b></para>
/// </summary>
public sealed class InstitutionScopeDriftTests
{
    /// <summary>
    /// Kuralın <b>zorlanabildiği</b> modül: <c>IInstitutionScoped</c> burada tanımlıdır.
    /// </summary>
    private const string GuardableApplicationPath = "Modules/Institution/MESNET.Institution.Application";

    /// <summary><c>Guid InstitutionId</c> alanı taşıyan kayıt bildirimleri.</summary>
    private static readonly Regex RecordDeclaration = new(
        @"public sealed record (?<name>\w+)\((?<body>[^)]*)\)(?<bases>[^;{]*)", RegexOptions.Compiled | RegexOptions.Singleline);

    /// <summary>
    /// Kapsam guard'ından <b>bilinçli olarak</b> muaf tutulanlar.
    ///
    /// <para><c>CreateInstitution</c>: henüz var olmayan bir kurumu yaratır, karşılaştırılacak
    /// mevcut kapsam yoktur. Kontrol yerine <b>ucun izni</b> kurum üstüdür
    /// (<c>platform:tenant:manage</c>) — yeni okul açmak kurum içi bir iş değildir.</para>
    /// </summary>
    private static readonly HashSet<string> Exempt = new(StringComparer.Ordinal)
    {
        "CreateInstitution",
    };

    /// <summary>
    /// <b>Dondurulmuş borç (#235).</b> Institution dışında kurum kimliğini istekten alan mesajlar.
    /// Bunlar <c>IInstitutionScoped</c>'u <b>uygulayamaz</b> — arayüz Institution.Application'da
    /// ve modüller arası Application referansı yasak.
    ///
    /// <para><b>Bu liste yalnız KÜÇÜLÜR.</b> Yeni satır eklemek, kurum kimliğini istekten alan
    /// yeni bir mesaj yazmak demektir ve kural odur ki yazılmaz — kiracıdan türetin. Borcu
    /// yakarken (alanı istek yüzeyinden kaldırıp <c>IMessageBus.TenantId</c>/claim'den türeterek)
    /// satırı buradan da silin; artık var olmayan bir ad testi kırar.</para>
    /// </summary>
    private static readonly HashSet<string> KnownDebt = new(StringComparer.Ordinal)
    {
        // Attendance
        "Attendance.GetWorkCalendar",
        "Attendance.MarkAttendance",
        "Attendance.NotifyAttendancePendingApproval",
        "Attendance.UpdateWorkCalendar",

        // Contract
        "Contract.CreateContract",

        // Coordination
        "Coordination.AddWeeklyVisitAssignment",
        "Coordination.AssignBusinessToTeacher",
        "Coordination.CreateBusinessEvaluation",
        "Coordination.CreateGuidanceVisit",
        "Coordination.CreateMonthlyActivityReport",
        "Coordination.CreateSkillExam",
        "Coordination.DeleteWeeklyVisitAssignment",
        "Coordination.DeleteWeeklyVisitPlan",
        "Coordination.GenerateWeeklyVisits",
        "Coordination.GetAllTeachersOverview",
        "Coordination.GetAssignmentHistory",
        "Coordination.GetBranchWorkloadConfig",
        "Coordination.GetBusinessClusters",
        "Coordination.GetCoordinationConfig",
        "Coordination.GetCoordinationSummary",
        "Coordination.GetSchoolStudentsForGrading",
        "Coordination.GetSubmittedTermGrades",
        "Coordination.GetTeacherOverview",
        "Coordination.GetTeacherWorkload",
        "Coordination.ListBusinessesForAssignment",
        "Coordination.ListWeeklyVisitAssignments",
        "Coordination.ListWeeklyVisitPlans",
        "Coordination.RecalculateDistances",
        "Coordination.ResyncCoordinationViews",
        "Coordination.ResyncWeeklyVisitEvents",
        "Coordination.SetBusinessManualDistance",
        "Coordination.SuggestAssignedHours",
        "Coordination.UnassignBusinessFromTeacher",
        "Coordination.UnassignBusinessSlot",
        "Coordination.UpdateBranchAssignedHours",
        "Coordination.UpdateBusinessAssignedHours",
        "Coordination.UpsertBranchWorkloadConfig",
        "Coordination.UpsertCoordinationConfig",
        "Coordination.UpsertTeacherSchedule",

        // Enrollment
        "Enrollment.MarkAsFailedToComplete",
        "Enrollment.PlaceStudent",
        "Enrollment.RegisterStudent",
        "Enrollment.RegisterTeacher",
        "Enrollment.RequestStudent",
        "Enrollment.SyncStudentCounts",

        // Payment
        "Payment.CalculateMonthlySalary",

        // Reporting
        "Reporting.GenerateBatchDocuments",
        "Reporting.GenerateMonthlyAttendanceBatchPreview",
        "Reporting.GenerateMonthlyAttendancePreview",
    };

    /// <summary>
    /// Kuralın zorlanabildiği yer: Institution modülünde kurum kimliği taşıyan her mesaj
    /// <c>IInstitutionScoped</c> uygulamalıdır.
    /// </summary>
    [Fact]
    public void Institution_modulunde_kurum_kimligi_tasiyan_mesajlar_guard_arayuzunu_tasir()
    {
        var missing = ScopedMessages(GuardableApplicationPath)
            .Where(m => !m.IsGuarded)
            .Select(m => $"{m.Name} ({m.File})")
            .ToList();

        missing.ShouldBeEmpty(
            "Kurum kimliğini istekten alan mesaj IInstitutionScoped taşımıyor; kapsam guard'ı bu "
            + "mesaj için HİÇ çalışmaz ve aktör başka okulun verisine dokunabilir. Arayüzü ekleyin "
            + "ya da bilinçli bir istisnaysa testteki muafiyet listesine gerekçesiyle yazın. "
            + $"Eksikler: {string.Join(", ", missing)}");
    }

    /// <summary>
    /// <b>Borç büyümez (#235).</b> Institution dışında kurum kimliğini istekten alan YENİ bir
    /// mesaj eklenemez.
    ///
    /// <para>Guard bu mesajları koruyamaz (arayüz görünmez), kiracılık da yalnız satırın
    /// düşeceği kiracıyı doğru seçer — gövdedeki kimlik yine de <b>alan</b> olarak yanlış
    /// yazılabilir ve ondan türeyen sayımlar kayar. Yeni yüzey bu borca eklenmemeli: kurum
    /// kimliğini istekten almayın, <c>IMessageBus.TenantId</c>/claim'den türetin.</para>
    /// </summary>
    [Fact]
    public void Institution_disinda_yeni_kurum_kimlikli_mesaj_eklenemez()
    {
        var newDebt = OtherModuleScopedMessages()
            .Where(m => !KnownDebt.Contains(m.Key))
            .Select(m => $"{m.Key} ({m.File})")
            .ToList();

        newDebt.ShouldBeEmpty(
            "Kurum kimliğini İSTEKTEN alan yeni mesaj eklenmiş. Bu mesaj kapsam guard'ından "
            + "geçemez (IInstitutionScoped Institution.Application'da ve modüller arası "
            + "Application referansı yasak) ve kimlik gövdeden geldiği için aktörün kiracısıyla "
            + "ayrışabilir. Kurum kimliğini istekten almayın — IMessageBus.TenantId ya da "
            + "institution_id claim'inden türetin. Gerçekten kaçınılmazsa KnownDebt listesine "
            + $"gerekçesiyle ekleyin (#235). Yeni borç: {string.Join(", ", newDebt)}");
    }

    /// <summary>
    /// <b>Liste bayatlamaz.</b> Borç ödendiğinde (alan istek yüzeyinden kaldırıldığında) satır
    /// listeden de silinmelidir; yoksa liste zamanla gerçekle ilgisini kaybeder ve "49 borç var"
    /// cümlesi ölçülemez hâle gelir.
    /// </summary>
    [Fact]
    public void Dondurulmus_borc_listesinde_olu_satir_kalmaz()
    {
        var actual = OtherModuleScopedMessages().Select(m => m.Key).ToHashSet(StringComparer.Ordinal);
        var stale = KnownDebt.Where(d => !actual.Contains(d)).ToList();

        stale.ShouldBeEmpty(
            "KnownDebt listesinde artık var olmayan mesaj var. Borç ödendiyse satırı listeden "
            + $"silin (#235). Ölü satırlar: {string.Join(", ", stale)}");
    }

    /// <summary>
    /// Bütün kurumları dolaşan kod. Guard bunu <b>göremez</b>: mesaj hiç kurum kimliği
    /// taşımadığı için <c>IInstitutionScoped</c> kuralına takılmaz.
    /// </summary>
    private static readonly Regex EnumeratesAllInstitutions = new(
        @"Query<(Core\.Entities\.)?Institution(Record)?>\(\)", RegexOptions.Compiled);

    /// <summary>
    /// Kurum belgesini <b>filtresiz</b> dolaşmasına izin verilen yerler.
    ///
    /// <para><c>GetInstitutionsHandler</c>: listeleme sorgusu; kapsam guard'la değil
    /// <c>InstitutionScopePolicy.VisibleScope</c> ile uygulanır (hedef istekte geçmez).
    /// <c>InstitutionTenantDirectory</c>: arka plan işlerinin kiracı listesi — kiracıları
    /// saymak bu servisin tanımıdır, kullanıcı isteğine bağlı değildir.
    /// <c>InstitutionSubtreeDirectory</c>: <c>SubtreeTenantScope</c> için okul kiracılarını
    /// listeler — girdisi yalnız <c>InstitutionVisibility</c>'den türer, istekten asla gelmez.
    /// <c>RebuildInstitutionHierarchyHandler</c>: ağacı kurmak tanımı gereği bütün düğümleri
    /// görmeyi gerektirir; uç kurum üstü izinle korunur (<c>platform:tenant:manage</c>) ve
    /// komut hiçbir kurum kimliği taşımaz.</para>
    /// </summary>
    private static readonly HashSet<string> MayEnumerateAll = new(StringComparer.Ordinal)
    {
        "GetInstitutionsHandler.cs",
        // GetInstitutionsHandler ile AYNI desen: listeleme sorgusu, kapsam guard'la değil
        // InstitutionScopePolicy.VisibleScope ile uygulanır (hedef istekte geçmez). Negatif
        // yön (yönetilenlerin DIŞI) ikinci bir Query<InstitutionManagerLink>() gerektirir ama
        // o ayrı bir belge tipi, bu taramanın konusu değil.
        "GetUnmanagedInstitutionsHandler.cs",
        "InstitutionTenantDirectory.cs",
        // SubtreeTenantScope için okul kiracılarını listeler — girdisi yalnız
        // InstitutionVisibility'den türer, istekten asla gelmez.
        "InstitutionSubtreeDirectory.cs",
        // Ağacı kurmak tanımı gereği bütün düğümleri görmeyi gerektirir; uç kurum üstü izinle
        // korunur (platform:tenant:manage) ve komut hiçbir kurum kimliği taşımaz.
        "RebuildInstitutionHierarchyHandler.cs",
        // Dağıtım ön koşulu sondaları: yalnız SAYARLAR — satır içeriği okunmaz, hiçbir şey
        // yazılmaz ve hiçbir kullanıcı isteğine bağlı değildirler (açılışta koşarlar). Ölçtükleri
        // şey zaten "kaç okulun ağaç yolu boş" ve "görünüm tümden boş mu"; kuruma daraltılmış bir
        // sayı bu iki soruyu cevaplayamaz.
        "InstitutionHierarchyProbe.cs",
        "InstitutionManagerLinkProbe.cs",
    };

    /// <summary>
    /// <b>Guard'ın kör noktası.</b> Kimliği istekten alan mesajları guard kapatır; ama hiç
    /// kimlik almadan <b>bütün kurumları</b> dolaşan bir handler kuralın dışında kalır ve
    /// sessizce her okulun verisine dokunur.
    ///
    /// <para>Gerçekten yaşandı: <c>ResyncStaffBranchCodesHandler</c> tüm kurumları tarıyordu ve
    /// kodda "Faz 1 tek kurumlu olduğu için pratik etkisi yok" diyen bir TODO vardı. İkinci okul
    /// açılınca ölçüldü — kendi okulunda <b>1</b> personeli olan müdür ucu çağırdığında
    /// <b>9</b> personel işlendi ve olaylar başka okulların kullanıcılarına <b>kapsam yazdı</b>.</para>
    /// </summary>
    [Fact]
    public void Butun_kurumlari_dolasan_yeni_kod_eklenemez()
    {
        var offenders = new List<string>();

        foreach (var file in SourceFilesUnder(Path.Combine(RepoRoot(), "src", GuardableApplicationPath)))
        {
            var name = Path.GetFileName(file);
            if (MayEnumerateAll.Contains(name)) continue;

            if (EnumeratesAllInstitutions.IsMatch(File.ReadAllText(file)))
                offenders.Add(name);
        }

        offenders.ShouldBeEmpty(
            "Kurum belgesi filtresiz dolaşılıyor. Bu kod hiçbir kurum kimliği almadığı için "
            + "kapsam guard'ı ONU HİÇ GÖRMEZ ve her okulun verisine dokunur. Komuta InstitutionId "
            + "ekleyip IInstitutionScoped yapın; gerçekten kiracı-üstü bir işse muafiyet listesine "
            + $"gerekçesiyle yazın. İhlaller: {string.Join(", ", offenders)}");
    }

    /// <summary>
    /// Muafiyet listesi <b>küçük kalmalı</b>. Büyümesi, guard'ın kural olmaktan çıkıp
    /// istisnalar tablosuna dönüştüğünün işaretidir.
    /// </summary>
    [Fact]
    public void Muafiyet_listesi_kucuk_kalir()
    {
        Exempt.Count.ShouldBeLessThanOrEqualTo(2);
    }

    /// <summary>
    /// Aktörün kurum kapsamını taşıyan ifade — bitişik, nokta (<c>?.</c>) ya da null-forgiving
    /// (<c>!.</c>) erişimli. İkincisi eskiden kaçırılıyordu (#kapsam-kilidi-null-forgiving):
    /// <c>currentUser.GetCurrentUser()!.InstitutionId == ...</c> deseni <c>\??\.</c> ile
    /// eşleşmiyordu, yani <c>!.</c> idiomuyla yazılmış elle kopya bu kilidin görünmez noktasıydı.
    /// </summary>
    private const string ActorInstitutionId =
        @"(?:GetCurrentUser\(\)\s*[?!]?\.\s*InstitutionId|\b(?:actor|current|user)\w*(?:\s*[?!]?\.\s*)?InstitutionId)";

    /// <summary>
    /// Aktörün kurum kapsamını <b>elle</b> bir hedefle karşılaştıran kod.
    ///
    /// <para><b>Desen aktör tarafına demirlenmiştir</b>, iki tarafta da aranır ve hem bitişik
    /// (<c>actorInstitutionId</c>) hem nokta erişimli (<c>actor?.InstitutionId</c>) yazımı
    /// kapsar. İkincisi bu deponun kendi idiomudur: doğru uygulama olan
    /// <c>InstitutionScopeGuardMiddleware</c> bile <c>actor?.InstitutionId</c> yazar, yani elle
    /// kopyanın en olası biçimi odur.</para>
    ///
    /// <para><b>Neden yalnız "iki InstitutionId karşılaştırılıyor" denmiyor:</b> meşru veri
    /// süzgeçleri (<c>.Where(p =&gt; p.InstitutionId == query.InstitutionId)</c>) ihlal sayılırdı
    /// ve gürültülü bir kilit kapatılır.</para>
    ///
    /// <para><b>Bilinen sınır:</b> aktörün kurumu nötr bir ada atanırsa
    /// (<c>var mine = currentUser.GetCurrentUser()?.InstitutionId;</c> ardından
    /// <c>mine == ...</c>) desen onu göremez. Regex tabanlı her tarayıcının doğasında olan bir
    /// sınırdır; kilit refleksi kurmak içindir, kanıt değildir.</para>
    /// </summary>
    private static readonly Regex HandRolledScopeComparison = new(
        ActorInstitutionId + @"\s*(?:==|!=)|(?:==|!=)\s*" + ActorInstitutionId,
        RegexOptions.Compiled);

    /// <summary>
    /// Kapsam kararını <b>kopyalayan</b> uç kalmamalı; karar politikadan geçmeli.
    ///
    /// <para><b>Neden kilitleniyor:</b> karar artık iki aşamalıdır — kimlik eşitliği, sonra
    /// gerekiyorsa ağaçtaki yol. Kararı kopyalayan bir yer <b>yalnız birinci aşamayı</b>
    /// yapar: kimlikler eşit değilse reddeder. Sonuç, il yetkilisinin kendi ilindeki hiçbir
    /// okulun kaydını açamaması — ve bu sessizdir: kod derlenir, mevcut testler yeşil kalır,
    /// istek 422 döner ve mesajı "yalnız kendi kurumunuz" der (doğru görünür).</para>
    ///
    /// <para><b>Muafiyet yok.</b> Karar kaynağının kendisi (<see cref="InstitutionScopePolicy"/>,
    /// <c>UserInstitutionScopePolicy</c>) <c>src/MESNET.Common.Shared/Security/</c> altında
    /// yaşar — bu tarama yalnız <see cref="GuardableApplicationPath"/>'i (Institution.Application)
    /// dolaşır ve oraya hiç girmez, dolayısıyla bir muafiyet listesine gerek yoktur.</para>
    /// </summary>
    [Fact]
    public void Kapsam_karari_kopyalanmaz()
    {
        var offenders = new List<string>();

        foreach (var file in SourceFilesUnder(Path.Combine(RepoRoot(), "src", GuardableApplicationPath)))
        {
            var name = Path.GetFileName(file);

            if (HandRolledScopeComparison.IsMatch(File.ReadAllText(file)))
                offenders.Add(name);
        }

        offenders.ShouldBeEmpty(
            "Kapsam kararı elle kopyalanmış. Karar artık iki aşamalı: kimlik eşitliği, sonra "
            + "gerekiyorsa ağaçtaki yol. Kopya YALNIZ birinci aşamayı yapar ve il/ilçe "
            + "yetkilisi alt ağacındaki hiçbir kaydı açamaz — sessizce: kod derlenir, testler "
            + "yeşil kalır, istek 422 döner ve mesajı doğru görünür. Kararı "
            + $"InstitutionScopePolicy'ye devredin. İhlaller: {string.Join(", ", offenders)}");
    }

    private sealed record ScopedMessage(string Key, string Name, string File, bool IsGuarded);

    /// <summary>Verilen Application yolundaki, kurum kimliğini istekten alan mesajlar.</summary>
    private static IEnumerable<ScopedMessage> ScopedMessages(string applicationPath)
    {
        var moduleName = applicationPath.Split('/')[1];

        foreach (var file in RequestFiles(Path.Combine(RepoRoot(), "src", applicationPath)))
        foreach (Match match in RecordDeclaration.Matches(File.ReadAllText(file)))
        {
            var name = match.Groups["name"].Value;
            if (Exempt.Contains(name)) continue;

            // Yalnız kurum kimliğini İSTEKTEN alanlar; sonuç/DTO kayıtları elenir.
            if (!match.Groups["body"].Value.Contains("Guid InstitutionId", StringComparison.Ordinal))
                continue;

            yield return new ScopedMessage(
                $"{moduleName}.{name}",
                name,
                Path.GetFileName(file),
                match.Groups["bases"].Value.Contains("IInstitutionScoped", StringComparison.Ordinal));
        }
    }

    /// <summary>
    /// Institution <b>dışındaki</b> modüllerin kurum kimliği taşıyan mesajları — guard'ın
    /// ulaşamadığı yüzey.
    /// </summary>
    private static IEnumerable<ScopedMessage> OtherModuleScopedMessages()
    {
        var modulesRoot = Path.Combine(RepoRoot(), "src", "Modules");

        foreach (var moduleDir in Directory.EnumerateDirectories(modulesRoot).OrderBy(d => d, StringComparer.Ordinal))
        {
            var moduleName = Path.GetFileName(moduleDir);
            if (moduleName == "Institution") continue;

            var applicationDir = Path.Combine(moduleDir, $"MESNET.{moduleName}.Application");
            if (!Directory.Exists(applicationDir)) continue;

            foreach (var message in ScopedMessages($"Modules/{moduleName}/MESNET.{moduleName}.Application"))
                yield return message;
        }
    }

    /// <summary>
    /// Yalnız <b>istek</b> tipleri taranır: <c>Commands/</c> ve <c>Queries/</c>. DTO'lar da
    /// <c>Guid InstitutionId</c> taşır ama onlar isteğin girdisi değil ÇIKTISIDIR; kapsam
    /// kararına konu olmazlar. (İlk sürüm bütün klasörü tarıyordu ve <c>AcademicPeriodDto</c>'yu
    /// ihlal sayıyordu.)
    /// </summary>
    private static IEnumerable<string> RequestFiles(string root)
    {
        var requestFolders = new[]
        {
            $"{Path.DirectorySeparatorChar}Commands{Path.DirectorySeparatorChar}",
            $"{Path.DirectorySeparatorChar}Queries{Path.DirectorySeparatorChar}",
        };

        return SourceFilesUnder(root)
            .Where(f => requestFolders.Any(folder => f.Contains(folder, StringComparison.Ordinal)));
    }

    private static IEnumerable<string> SourceFilesUnder(string root)
    {
        var obj = $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}";
        var bin = $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}";

        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(obj, StringComparison.Ordinal)
                     && !f.Contains(bin, StringComparison.Ordinal));
    }

    /// <summary>
    /// Test derlemesi depo içinde değil <c>bin/</c> altında koşar; göreli yol doğrudan
    /// kullanılamaz — çözüm dosyası (<c>MESNET.slnx</c>) işaretçi olarak aranır.
    /// </summary>
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
