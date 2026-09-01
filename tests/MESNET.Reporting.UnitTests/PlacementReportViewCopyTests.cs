using System.Reflection;
using MESNET.Reporting.Core.ReadModels;
using Shouldly;
using Xunit;

namespace MESNET.Reporting.UnitTests;

/// <summary>
/// Rapor görünümünün kimliği ve göç kopyası (#296).
/// </summary>
public class PlacementReportViewCopyTests
{
    [Fact]
    public void Kimlik_ogrenci_ve_donem_ikilisinden_DETERMINISTIK_turer()
    {
        var ogrenci = Guid.NewGuid();
        var donem = Guid.NewGuid();

        StudentPlacementReportView.CreateId(ogrenci, donem)
            .ShouldBe(StudentPlacementReportView.CreateId(ogrenci, donem));
    }

    [Fact]
    public void Ayni_ogrencinin_IKI_donemi_AYRI_satirdir()
    {
        var ogrenci = Guid.NewGuid();

        // KİLİT NOKTA. Eski hâlde kimlik `StudentId`'ydi; öğrencinin ikinci akademik dönemi
        // birincisini EZERDİ. Ölçüldü (dev): 363 satırın 363'ünde Id == StudentId ve henüz tek
        // dönem olduğu için kayıp görünmüyordu — yani hata gelecek öğretim yılında doğacaktı.
        StudentPlacementReportView.CreateId(ogrenci, Guid.NewGuid())
            .ShouldNotBe(StudentPlacementReportView.CreateId(ogrenci, Guid.NewGuid()));
    }

    [Fact]
    public void Kimlik_ESKI_kimlikle_asla_cakismaz()
    {
        var ogrenci = Guid.NewGuid();

        // Göç, eski kimlikli satırı siliyor. Yeni kimlik eskisiyle çakışsaydı, silme az önce
        // yazılan satırı düşürürdü.
        StudentPlacementReportView.CreateId(ogrenci, Guid.NewGuid()).ShouldNotBe(ogrenci);
    }

    /// <summary>
    /// Göç kopyası <b>hiçbir alanı düşürmemeli</b>.
    ///
    /// <para>Alan listesini elle saymak yerine yansıma kullanılıyor: görünüme yeni bir alan
    /// eklendiğinde bu test kendiliğinden onu da arar. #297'de <c>WithId</c> tam olarak bir
    /// alanı düşürmüştü ve elle yazılmış liste bunu göremiyordu.</para>
    /// </summary>
    [Fact]
    public void Goc_kopyasi_HICBIR_alani_dusurmez()
    {
        var source = new StudentPlacementReportView
        {
            Id = Guid.NewGuid(),
            StudentId = Guid.NewGuid(),
            StudentName = "Test Öğrenci",
            StudentNumber = "1234",
            ClassName = "BIL - 11",
            ClassYear = 11,
            BusinessId = Guid.NewGuid(),
            BusinessName = "Test İşletme",
            BusinessPhone = "0212",
            BusinessEmail = "a@b.c",
            BusinessContactName = "Yetkili",
            BranchCode = "BIL",
            BranchName = "Bilişim Teknolojileri",
            TeacherId = Guid.NewGuid(),
            TeacherName = "Öğretmen",
            InstitutionId = Guid.NewGuid(),
            AcademicPeriodId = Guid.NewGuid(),
        };

        var yeniKimlik = Guid.NewGuid();
        var copy = source.WithId(yeniKimlik);

        copy.Id.ShouldBe(yeniKimlik);

        var dusen = typeof(StudentPlacementReportView)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.Name != nameof(StudentPlacementReportView.Id))
            .Where(p => !Equals(p.GetValue(copy), p.GetValue(source)))
            .Select(p => p.Name)
            .ToList();

        dusen.ShouldBeEmpty(
            "Göç kopyası alan DÜŞÜRDÜ. CopyWithId gövdesine ekleyin; düşen alan sessizce "
            + $"varsayılan değerinde doğar ve raporda boş basılır. Düşenler: {string.Join(", ", dusen)}");
    }
}
