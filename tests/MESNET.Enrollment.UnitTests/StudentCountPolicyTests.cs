using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Core.Enums;
using MESNET.Enrollment.Core.Policies;
using Shouldly;
using Xunit;

namespace MESNET.Enrollment.UnitTests;

/// <summary>
/// Şube öğrenci sayacının mutlak hesabı (#290).
///
/// <para>Bu sayı doğrudan para kararına giriyor: <c>BranchStudentCountView</c> →
/// <c>UpsertBranchWorkloadConfig</c> → <c>GroupCalculator</c> (Norm Kadro Yön. Md.22) →
/// <c>TotalWorkloadPool</c>, ve o havuz koordinasyon saati dağıtımının <b>sert tavanıdır</b>.</para>
/// </summary>
public class StudentCountPolicyTests
{
    [Fact]
    public void Aktif_ogrenciler_sinif_bazinda_sayilir()
    {
        // Arrange
        var students = new[]
        {
            Student("BIL", 9, StudentStatus.Registered),
            Student("BIL", 9, StudentStatus.ActiveInternship),
            Student("BIL", 10, StudentStatus.Placed),
        };

        // Act
        var result = StudentCountPolicy.ActiveCountsByBranch(students);

        // Assert
        var bil = result.Single(c => c.BranchCode == "BIL");
        bil.Counts[9].ShouldBe(2);
        bil.Counts[10].ShouldBe(1);
    }

    [Fact]
    public void Kaydi_silinmis_ve_tamamlamis_ogrenci_sayilmaz()
    {
        var students = new[]
        {
            Student("BIL", 9, StudentStatus.Registered),
            Student("BIL", 9, StudentStatus.Deregistered),
            Student("BIL", 9, StudentStatus.Completed),
        };

        StudentCountPolicy.ActiveCountsByBranch(students).Single().Counts[9].ShouldBe(1);
    }

    [Fact]
    public void Aktifi_kalmamis_sube_BOS_sozlukle_YAYINLANIR_atlanmaz()
    {
        // Arrange — şubede kayıt var ama hiçbiri aktif değil.
        var students = new[]
        {
            Student("MAK", 12, StudentStatus.Completed),
            Student("MAK", 12, StudentStatus.Deregistered),
        };

        // Act
        var result = StudentCountPolicy.ActiveCountsByBranch(students);

        // Assert — KİLİT NOKTA. Önce süzüp sonra gruplayan eski sürüm bu şube için HİÇ sonuç
        // üretmiyordu; tüketici sözlüğü replace ettiği için "yayınlanmayan" satır eski
        // (sıfır olmayan) değerinde donuyordu. "Dokunma" ile "sıfırla" farklı şeylerdir ve
        // burada doğru olan sıfırlamaktır: aksi hâlde öğrencisi kalmamış bir alan için
        // koordinasyon saati tavanı düşmez.
        var mak = result.ShouldHaveSingleItem();
        mak.BranchCode.ShouldBe("MAK");
        mak.Counts.ShouldBeEmpty();
    }

    [Fact]
    public void Ogretim_turu_subeyi_ayirir()
    {
        var students = new[]
        {
            Student("BIL", 9, StudentStatus.Registered, "Formal"),
            Student("BIL", 9, StudentStatus.Registered, "Mesem"),
        };

        var result = StudentCountPolicy.ActiveCountsByBranch(students);

        // Aynı şube kodu iki ayrı satırdır: görünüm kimliği öğretim türünü de içerir
        // (BranchStudentCountView.CreateId). Birleştirmek iki türün sayısını tek havuza katardı.
        result.Count.ShouldBe(2);
        result.ShouldAllBe(c => c.Counts[9] == 1);
    }

    private static StudentProfile Student(
        string branchCode, int classYear, StudentStatus status, string educationTypeName = "Formal") =>
        new()
        {
            Id = Guid.NewGuid(),
            FullName = "Test Öğrenci",
            BranchCode = branchCode,
            BranchName = branchCode,
            ClassYear = classYear,
            EducationTypeName = educationTypeName,
            Status = status,
        };
}
