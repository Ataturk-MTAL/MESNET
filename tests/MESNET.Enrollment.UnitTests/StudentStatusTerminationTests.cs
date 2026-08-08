using MESNET.Enrollment.Core.Enums;
using Shouldly;
using Xunit;

namespace MESNET.Enrollment.UnitTests;

/// <summary>
/// Fesih sonrası öğrenci yeniden yerleştirilebilmelidir (#220).
///
/// <para><b>Engel neydi:</b> <c>ActiveInternship</c> durumundan yalnız <c>Completed</c>'a geçiş
/// vardı. Stajı feshedilen öğrenci hiçbir yere yerleştirilemiyordu — durum makinesi otomatik
/// okula atamayı sessizce engellerdi ve öğrenci hiçbir yere bağlı kalmazdı.</para>
///
/// <para>Kural: fesih anında öğrenci okula (alan şefinin takibine) atanır, yani doğrudan
/// yeniden <c>Placed</c> olur.</para>
/// </summary>
public sealed class StudentStatusTerminationTests
{
    [Fact]
    public void Aktif_stajdan_yeniden_yerlestirmeye_gecilebilir()
    {
        StudentStatus.ActiveInternship.CanTransitionTo(StudentStatus.Placed).ShouldBeTrue();
    }

    /// <summary>
    /// Sözleşme aktifleşmeden (öğrenci hâlâ <c>Placed</c>) feshedilen staj da aynı yola girer.
    /// Kendi kendine geçiş burada anlamlıdır: yerleştirme değişir, durum aynı kalır.
    /// </summary>
    [Fact]
    public void Yerlestirilmis_ogrenci_yeniden_yerlestirilebilir()
    {
        StudentStatus.Placed.CanTransitionTo(StudentStatus.Placed).ShouldBeTrue();
    }

    /// <summary>
    /// <b>Nihai durumlar açılmadı.</b> Fesih yolu için geçiş eklemek, mezun olmuş ya da kaydı
    /// silinmiş öğrenciyi yeniden yerleştirilebilir yapmamalı.
    /// </summary>
    [Theory]
    [InlineData(nameof(StudentStatus.Completed))]
    [InlineData(nameof(StudentStatus.Deregistered))]
    public void Nihai_durumdan_yerlestirmeye_gecilemez(string statusName)
    {
        StudentStatus.FromName(statusName)
            .CanTransitionTo(StudentStatus.Placed).ShouldBeFalse();
    }

    [Fact]
    public void Aktif_staj_tamamlanabilmeye_devam_eder()
    {
        StudentStatus.ActiveInternship.CanTransitionTo(StudentStatus.Completed).ShouldBeTrue();
    }
}
