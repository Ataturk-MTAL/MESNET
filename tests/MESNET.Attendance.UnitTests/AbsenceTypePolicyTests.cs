using MESNET.Attendance.Core.Enums;
using MESNET.Attendance.Core.Services;
using Shouldly;
using Xunit;

namespace MESNET.Attendance.UnitTests;

/// <summary>
/// Devamsızlık türü giriş kuralları (#175).
///
/// <para>Sahibin iki kuralı: <i>"İşletme resmî izin veremez, devamsızlığı bildirir"</i> ve
/// <i>"MESEM'lerde ücretli izin var ama örgün eğitimde ücretli izin hakkı yok — rapor ya da
/// veli izni şart."</i></para>
///
/// <para>İkisi de para kuralıdır: mazeretsiz ve ücretsiz izin ücret kesintisi doğurur, diğer
/// türler doğurmaz (<c>AbsenceType.AffectsSalary</c>). Türü seçen taraf, dolaylı olarak
/// kesintiyi seçer — #172'nin sağlık raporunda kapattığı açığın aynısı.</para>
/// </summary>
public sealed class AbsenceTypePolicyTests
{
    /// <summary>İşletmenin bildiremeyeceği türler — hepsi birer sınıflandırma kararıdır.</summary>
    public static TheoryData<AbsenceType> ClassificationTypes =>
    [
        AbsenceType.Excused,
        AbsenceType.PaidLeave,
        AbsenceType.UnpaidLeave,
        AbsenceType.HealthReport
    ];

    [Theory]
    [MemberData(nameof(ClassificationTypes))]
    public void Isletme_yalniz_mazeretsiz_bildirebilir(AbsenceType type)
    {
        AbsenceTypePolicy.CanReport(type, hasDirectEntry: false).ShouldBeFalse();
    }

    [Fact]
    public void Isletme_mazeretsiz_devamsizligi_bildirebilir()
    {
        AbsenceTypePolicy.CanReport(AbsenceType.Unexcused, hasDirectEntry: false).ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(ClassificationTypes))]
    public void Okul_tarafi_her_turu_girebilir(AbsenceType type)
    {
        AbsenceTypePolicy.CanReport(type, hasDirectEntry: true).ShouldBeTrue();
    }

    [Fact]
    public void Ucretli_izin_yalniz_MESEM_ogrencisinde_gecerlidir()
    {
        AbsenceTypePolicy.IsValidForEducationType(AbsenceType.PaidLeave, "Mesem").ShouldBeTrue();
        AbsenceTypePolicy.IsValidForEducationType(AbsenceType.PaidLeave, "mesem").ShouldBeTrue();
        AbsenceTypePolicy.IsValidForEducationType(AbsenceType.PaidLeave, "Formal").ShouldBeFalse();
    }

    /// <summary>
    /// Eğitim türü bilinmiyorsa ücretli izin REDDEDİLİR. İzin verilseydi eksik veri sessizce
    /// para sonucu doğururdu (ücretli izin kesinti doğurmaz); reddetmek görünür hata üretir.
    /// </summary>
    [Fact]
    public void Egitim_turu_bilinmiyorsa_ucretli_izin_reddedilir()
    {
        AbsenceTypePolicy.IsValidForEducationType(AbsenceType.PaidLeave, null).ShouldBeFalse();
        AbsenceTypePolicy.IsValidForEducationType(AbsenceType.PaidLeave, "").ShouldBeFalse();
    }

    /// <summary>Kısıt yalnız ücretli izne özeldir; diğer türler eğitim türünden bağımsızdır.</summary>
    [Theory]
    [InlineData(nameof(AbsenceType.Unexcused))]
    [InlineData(nameof(AbsenceType.Excused))]
    [InlineData(nameof(AbsenceType.UnpaidLeave))]
    [InlineData(nameof(AbsenceType.HealthReport))]
    public void Diger_turler_egitim_turunden_bagimsizdir(string typeName)
    {
        var type = AbsenceType.FromName(typeName);

        AbsenceTypePolicy.IsValidForEducationType(type, "Formal").ShouldBeTrue();
        AbsenceTypePolicy.IsValidForEducationType(type, null).ShouldBeTrue();
    }

    /// <summary>
    /// Ücretli izin kesinti doğurmaz — kısıtın nedeni budur. Örgün öğrencide kullanılsaydı
    /// öğrenci hak etmediği bir gün için tam ücret alırdı.
    /// </summary>
    [Fact]
    public void Ucretli_izin_kesinti_dogurmaz()
    {
        AbsenceType.PaidLeave.AffectsSalary.ShouldBeFalse();
        AbsenceType.Unexcused.AffectsSalary.ShouldBeTrue();
    }

    /// <summary>
    /// Ücretli izin DOĞRUDAN GİRİLEMEZ (#177) — okul tarafı da giremez. Yalnız öğrenci
    /// başvurusunun işletme ve okul onayından geçmesiyle doğar.
    ///
    /// <para>Kısıt okul tarafına da uygulanır çünkü türü seçmek doğrudan para kararıdır: doğrudan
    /// giriş açık kalsaydı iki taraflı onay zinciri tek komutla atlanabilirdi.</para>
    /// </summary>
    [Fact]
    public void Ucretli_izin_dogrudan_girilemez()
    {
        AbsenceTypePolicy.RequiresApprovedRequest(AbsenceType.PaidLeave).ShouldBeTrue();
    }

    /// <summary>Diğer türler onay zinciri istemez — doğrudan girilebilir kalır.</summary>
    [Theory]
    [InlineData(nameof(AbsenceType.Unexcused))]
    [InlineData(nameof(AbsenceType.Excused))]
    [InlineData(nameof(AbsenceType.UnpaidLeave))]
    [InlineData(nameof(AbsenceType.HealthReport))]
    public void Diger_turler_basvuru_zorunlulugu_tasimaz(string typeName)
    {
        AbsenceTypePolicy.RequiresApprovedRequest(AbsenceType.FromName(typeName)).ShouldBeFalse();
    }
}
