using System.Reflection;
using MESNET.Audit.Application.Auditing;
using Shouldly;
using Xunit;

namespace MESNET.Audit.UnitTests;

/// <summary>
/// Süzgecin GERÇEK komutları kapsadığını kanıtlar.
/// </summary>
/// <remarks>
/// <para><b>Neden gerekli:</b> süzgeç bir konvansiyondur ve konvansiyonlar sessizce kırılır.
/// Bir modül komutlarını <c>Commands/</c> dışında bir ad alanına taşısa, denetim izi o modül
/// için sessizce boşalırdı — derleme geçer, testler geçer, dead letter boş kalır. Tam olarak
/// bu depoda ölçülmüş sessiz-boşluk kalıbıdır.</para>
///
/// <para><b>Muafiyet listesi bilinçli olarak DAR:</b> yalnız <c>Commands/</c> klasörüne yanlış
/// yerleştirilmiş sorgular. Liste büyürse test kırılır ve büyümenin sebebini sormak zorunda
/// kalırsınız.</para>
/// </remarks>
public class AuditCommandCoverageDriftTests
{
    /// <summary>
    /// <c>Commands/</c> klasöründe duran ama SORGU olan tipler. Doğru çözüm bunları
    /// <c>Queries/</c>'e taşımaktır; bu plan onu kapsam dışı bıraktı.
    /// </summary>
    private static readonly string[] BilinenYanlisYerlesimler =
    [
        "GetDocumentById",
        "GetDocumentPdf",
        "GetDocumentsByStudent",
        "GetInvitations",
        "GetPendingDocuments",
        "GetPermissionScopes",
        "GetRoleIntegrityReport",
        "GetStudentsWithoutGuardian",
        "GetUserAccount",
        "GetUserAccounts",
    ];

    private static readonly Assembly[] ModulAssemblyleri =
    [
        typeof(MESNET.Institution.Application.Commands.CreateInstitution).Assembly,
        typeof(MESNET.Business.Application.Commands.RegisterBusiness).Assembly,
        typeof(MESNET.Enrollment.Application.Commands.RegisterStudent).Assembly,
        typeof(MESNET.Contract.Application.Commands.CreateContract).Assembly,
        typeof(MESNET.Attendance.Application.Commands.MarkAttendance).Assembly,
        typeof(MESNET.Payment.Application.Commands.UploadReceiptByStudent).Assembly,
        typeof(MESNET.Coordination.Application.Commands.CreateBusinessEvaluation).Assembly,
        typeof(MESNET.Internship.Application.Commands.RequestTermination).Assembly,
        typeof(MESNET.Reporting.Application.Commands.GenerateInternshipContractDocument).Assembly,
        typeof(MESNET.Security.Application.Commands.CreateUser).Assembly,
    ];

    /// <summary>
    /// Bilinen yazma fiilleriyle başlayan tip adları — "komut şekli" sezgisinin ilk bacağı.
    /// Depodaki gerçek komut adlarından (ör. <c>CreateInstitution</c>, <c>MarkAttendance</c>,
    /// <c>ApproveHealthReport</c>) türetildi.
    /// </summary>
    private static readonly string[] YazmaFiilOnekleri =
    [
        "Create", "Update", "Delete", "Approve", "Reject", "Mark", "Assign", "Upload",
        "Register", "Terminate", "Suspend", "Activate", "Deactivate", "Change", "Set",
        "Enter", "Submit", "Confirm", "Calculate", "Generate", "Resync", "Backfill", "Sync",
        "Place", "Request", "Sign", "Complete", "Close", "Open", "Authorize", "Invalidate",
        "Retract", "Resume", "Override", "Purge", "Toggle", "Upsert", "Add", "Link",
        "Notify", "Verify", "Correct", "Attach", "Download", "Recalculate", "Unassign",
    ];

    /// <summary>
    /// Bu soneklerle biten tipler komut DEĞİLDİR — sonuç/DTO/doğrulayıcı/config tipleri de
    /// fiil önekiyle başlayabilir (ör. <c>CreateInstitutionValidator</c>,
    /// <c>ApproveAttendanceResult</c>) ama denetim süzgecinin ilgi alanı dışındadır.
    /// </summary>
    private static readonly string[] KomutOlmayanSonekler =
    [
        "Result", "Dto", "Input", "Item", "Request", "View", "Config", "Validator",
        "Handler", "Options", "Settings", "Response", "Event",
    ];

    private static bool KomutSeklindeMi(Type t) =>
        YazmaFiilOnekleri.Any(v => t.Name.StartsWith(v, StringComparison.Ordinal))
        && !KomutOlmayanSonekler.Any(s => t.Name.EndsWith(s, StringComparison.Ordinal));

    /// <summary>
    /// Yalnız madde-2 taraması (aşağıdaki <see cref="KomutSekilliAmaCommandsDisindakiTipler"/>)
    /// için: on gerçek modül + bu test projesinin KENDİSİ (yalnız <c>Fixtures.cs</c>
    /// örnekleri barındırır — <c>Fixtures.cs</c> içindeki not: "MESNET.AuditFixtures.Sample.*"
    /// ad alanları gerçek modüllerle ÇAKIŞMAZ). Test assembly'sinin dahil edilmesinin TEK
    /// nedeni: kanıt adımında gerçek modül koduna dokunmadan (bkz. sınıf dokümantasyonundaki
    /// "Kanıt" notu) senaryoyu uçtan uca doğrulayabilmek. <see cref="KomutAdAlanindakiTipler"/>
    /// (test 1 ve 2) bu listeyi KULLANMAZ — <c>Fixtures.cs</c>'teki <c>GetUserAccountsSample</c>
    /// o taramaya girseydi <see cref="BilinenYanlisYerlesimler"/> listesinde olmadığı için
    /// yanlışlıkla kırmızı üretirdi.
    /// </summary>
    private static readonly Assembly[] KomutSekliTaramaAssemblyleri =
        [.. ModulAssemblyleri, typeof(AuditCommandCoverageDriftTests).Assembly];

    private static IEnumerable<Type> ModulTipleri()
        => ModulAssemblyleri
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsPublic: true, IsAbstract: false });

    private static IEnumerable<Type> KomutAdAlanindakiTipler()
        => ModulTipleri()
            .Where(t => t.Namespace is { } ns && ns.EndsWith(".Commands", StringComparison.Ordinal));

    /// <summary>
    /// Ad alanı konvansiyonundan BAĞIMSIZ tarama: "komut şeklinde" ama <c>Commands/</c>
    /// klasöründe OLMAYAN tipler. Bu, <see cref="Commands_ad_alanindaki_her_komut_denetim_suzgecine_takilir"/>
    /// testinin kaçırdığı sınıfı yakalar — o test zaten <c>.Commands</c> soneğiyle süzdüğü
    /// için bir komut ad alanı DIŞINA taşınırsa hiç göremez (ölçüldü: MESNET.Attendance
    /// .Application.Cmds ad alanına eklenen komutla 2/2 yeşil kalmıştı).
    /// </summary>
    private static IEnumerable<Type> KomutSekilliAmaCommandsDisindakiTipler()
        => KomutSekliTaramaAssemblyleri
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsPublic: true, IsAbstract: false })
            .Where(t => t.Namespace is { } ns && !ns.EndsWith(".Commands", StringComparison.Ordinal))
            .Where(KomutSeklindeMi);

    [Fact]
    public void Commands_ad_alanindaki_her_komut_denetim_suzgecine_takilir()
    {
        // Arrange
        var kacanlar = KomutAdAlanindakiTipler()
            .Where(t => !BilinenYanlisYerlesimler.Contains(t.Name))
            .Where(t => !AuditCommandFilter.ShouldAudit(t))
            .Select(t => t.FullName)
            .OrderBy(x => x)
            .ToList();

        // Assert
        kacanlar.ShouldBeEmpty(
            $"Bu tipler Commands ad alanında ama denetim süzgecine takılmıyor — izleri sessizce eksik kalır:{Environment.NewLine}"
            + string.Join(Environment.NewLine, kacanlar));
    }

    [Fact]
    public void Bilinen_yanlis_yerlesim_listesi_gerceklikle_ortusur()
    {
        // Listede olup artık var olmayan bir ad, listenin ölü büyümesi demektir.
        var gercekAdlar = KomutAdAlanindakiTipler().Select(t => t.Name).ToHashSet(StringComparer.Ordinal);

        var oluGirisler = BilinenYanlisYerlesimler.Where(ad => !gercekAdlar.Contains(ad)).ToList();

        oluGirisler.ShouldBeEmpty(
            "Bu adlar muafiyet listesinde ama artık Commands ad alanında yoklar: "
            + string.Join(", ", oluGirisler));
    }

    /// <summary>
    /// Madde 2 (ÖNEMLİ) düzeltmesi: yukarıdaki iki test <c>.Commands</c> soneğiyle süzdüğü
    /// için döngü kendi içine kapanıyordu — bir komut o ad alanının DIŞINA taşınsa hiçbiri
    /// bunu göremezdi. Bu test ad alanı sonekinden BAĞIMSIZ tarar: on modülün Application
    /// assembly'lerindeki her public/somut tipi gezer, "komut şeklinde" (bilinen bir yazma
    /// fiiliyle başlıyor VE Result/Dto/Input/Item/Request/View/Config/Validator/Handler/
    /// Options/Settings/Response/Event ile bitmiyor) olanları bulur ve bunların
    /// <c>Commands/</c> ad alanında OLMASINI zorunlu kılar.
    /// </summary>
    [Fact]
    public void Komut_sekilli_tipler_Commands_ad_alaninin_disinda_kalmaz()
    {
        // Arrange
        var kacanlar = KomutSekilliAmaCommandsDisindakiTipler()
            .Select(t => t.FullName)
            .OrderBy(x => x)
            .ToList();

        // Assert
        kacanlar.ShouldBeEmpty(
            $"Bu tipler yazma komutu gibi adlandırılmış (bilinen fiil öneki + komut-dışı sonek yok) ama "
            + $"Commands ad alanında DEĞİL — denetim süzgeci yalnız ad alanına bakar, bunları KAÇIRIR:"
            + $"{Environment.NewLine}" + string.Join(Environment.NewLine, kacanlar));
    }
}
