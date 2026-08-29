using MESNET.Audit.Core.Services;
using Shouldly;
using Xunit;

namespace MESNET.Audit.UnitTests;

/// <summary>
/// Hedef kimliği çıkarımı KONVANSİYONA dayalıdır: middleware komutları tanımaz, tanısaydı
/// 201 komutluk elle bakımlı bir kayıt listesi tutmak gerekirdi. Bedeli burada ölçülür:
/// kümede olmayan bir ad kullanan komut HEDEFSİZ kaydolur — satır yine oluşur (kim, ne,
/// ne zaman durur), yalnız hangi kayda dokunulduğu yazılmaz.
/// </summary>
public class AuditTargetExtractorTests
{
    private sealed record MarkAttendance(Guid StudentId, Guid ContractId, DateTime Date);
    private sealed record UnknownShape(Guid WidgetId, string Name);
    private sealed record NullableTarget(Guid? StudentId);
    private sealed record EmptyTarget(Guid StudentId);

    [Fact]
    public void Bilinen_adlardaki_kimlikleri_cikarir()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var contractId = Guid.NewGuid();
        var command = new MarkAttendance(studentId, contractId, DateTime.UtcNow);

        // Act
        var targets = AuditTargetExtractor.Extract(command);

        // Assert
        targets.Count.ShouldBe(2);
        targets["StudentId"].ShouldBe(studentId);
        targets["ContractId"].ShouldBe(contractId);
    }

    [Fact]
    public void Bilinmeyen_ad_kullanan_komut_hedefsiz_kaydolur()
    {
        // Bu SESSİZ bir eksikliktir ve bilinçlidir. Satırın kendisi kaybolmaz.
        var targets = AuditTargetExtractor.Extract(new UnknownShape(Guid.NewGuid(), "x"));

        targets.ShouldBeEmpty();
    }

    [Fact]
    public void Dolu_nullable_kimlik_cikarilir()
    {
        var studentId = Guid.NewGuid();

        var targets = AuditTargetExtractor.Extract(new NullableTarget(studentId));

        targets["StudentId"].ShouldBe(studentId);
    }

    [Fact]
    public void Bos_nullable_kimlik_cikarilmaz()
    {
        var targets = AuditTargetExtractor.Extract(new NullableTarget(null));

        targets.ShouldBeEmpty();
    }

    [Fact]
    public void Guid_Empty_hedef_sayilmaz()
    {
        // Guid.Empty "kimlik verilmedi" demektir; iz onu gerçek bir kayıtmış gibi göstermemeli.
        var targets = AuditTargetExtractor.Extract(new EmptyTarget(Guid.Empty));

        targets.ShouldBeEmpty();
    }

    [Fact]
    public void Null_komut_bos_sozluk_dondurur()
    {
        AuditTargetExtractor.Extract(null).ShouldBeEmpty();
    }

    [Fact]
    public void Bilinen_ad_kumesi_beklenen_dokuz_adi_icerir()
    {
        // Küme SABİTTİR ve testle kilitlidir — sessizce daralması hedeflerin kaybolması demek.
        AuditTargetExtractor.KnownTargetNames.ShouldBe(
            new[]
            {
                "AcademicPeriodId", "AttendanceId", "BusinessId", "ContractId", "InstitutionId",
                "PaymentId", "StudentId", "TeacherId", "UserAccountId",
            },
            ignoreOrder: true);
    }
}
