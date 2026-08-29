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

    private static IEnumerable<Type> KomutAdAlanindakiTipler()
        => ModulAssemblyleri
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsPublic: true, IsAbstract: false }
                        && t.Namespace is { } ns
                        && ns.EndsWith(".Commands", StringComparison.Ordinal));

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
}
