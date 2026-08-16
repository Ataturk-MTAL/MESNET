using MESNET.Internship.Core.Enums;
using MESNET.Internship.Core.Policies;
using Shouldly;
using Xunit;

namespace MESNET.Internship.UnitTests;

/// <summary>
/// Başka modülün olayı hangi staj saga'sına gider (#248).
///
/// <para>Aktarıcı yanlış saga'yı bulursa sonuç <b>yanlış öğrencinin feshi</b>dir; hiç bulamazsa
/// sonuç sessizliktir. İkisi de kabul edilemez, o yüzden karar politikada ve testli.</para>
/// </summary>
public sealed class SagaCorrelationPolicyTests
{
    private static readonly Guid Student = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Business = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Period = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Other = Guid.Parse("99999999-9999-9999-9999-999999999999");

    // ─── Sözleşme olayları: öğrenci + işletme ────────────────────────────────────────

    [Fact]
    public void Sozlesme_ayni_ogrenci_ve_isletmede_eslesir()
    {
        SagaCorrelationPolicy.MatchesContract(
            Student, Business, InternshipPhase.AwaitingContract, Student, Business)
            .ShouldBeTrue();
    }

    [Fact]
    public void Sozlesme_baska_ogrenciye_gitmez()
    {
        SagaCorrelationPolicy.MatchesContract(
            Other, Business, InternshipPhase.AwaitingContract, Student, Business)
            .ShouldBeFalse();
    }

    /// <summary>
    /// Öğrencinin birden çok saga'sı olabilir (fesih → yeni yerleştirme). İşletme ayrımı
    /// olmasaydı sözleşme olayı eski stajı güncellerdi.
    /// </summary>
    [Fact]
    public void Sozlesme_baska_isletmenin_stajina_gitmez()
    {
        SagaCorrelationPolicy.MatchesContract(
            Student, Other, InternshipPhase.AwaitingContract, Student, Business)
            .ShouldBeFalse();
    }

    /// <summary>
    /// Okulda stajda <c>BusinessId</c> <c>null</c>'dır (#159). Sözleşme kurulmadığı için o
    /// saga'ya sözleşme olayı da gelmemeli.
    /// </summary>
    [Fact]
    public void Sozlesme_isletmesiz_okulda_staja_gitmez()
    {
        SagaCorrelationPolicy.MatchesContract(
            Student, null, InternshipPhase.Active, Student, Business)
            .ShouldBeFalse();
    }

    // ─── Devamsızlık olayları: öğrenci + akademik dönem ──────────────────────────────

    [Fact]
    public void Devamsizlik_ayni_ogrenci_ve_donemde_eslesir()
    {
        SagaCorrelationPolicy.MatchesAttendance(
            Student, Period, InternshipPhase.Active, Student, Period)
            .ShouldBeTrue();
    }

    /// <summary>
    /// Sayaç dönem başına sıfırlanır (#242); geçen yılın saga'sı bu yılın sınırından
    /// etkilenmemeli.
    /// </summary>
    [Fact]
    public void Devamsizlik_baska_donemin_stajina_gitmez()
    {
        SagaCorrelationPolicy.MatchesAttendance(
            Student, Other, InternshipPhase.Active, Student, Period)
            .ShouldBeFalse();
    }

    /// <summary>
    /// <b>İşletme eşleşmesi ARANMAZ.</b> Devamsızlık öğrencinin eğitim yılına aittir, işletmeye
    /// değil (#242) — öğrenci yıl içinde işletme değiştirmiş olabilir ve staj aynı stajdır.
    /// Aksi hâlde işletme değiştiren öğrencide sınır aşımı hiçbir saga'yı bulamazdı.
    /// </summary>
    [Fact]
    public void Devamsizlik_isletmeye_bakmaz()
    {
        SagaCorrelationPolicy.MatchesAttendance(
            Student, Period, InternshipPhase.Active, Student, Period)
            .ShouldBeTrue();
    }

    // ─── Faz: kapanmış staja olay taşınmaz ───────────────────────────────────────────

    [Theory]
    [InlineData(nameof(InternshipPhase.Placed), true)]
    [InlineData(nameof(InternshipPhase.AwaitingContract), true)]
    [InlineData(nameof(InternshipPhase.Active), true)]
    [InlineData(nameof(InternshipPhase.TerminationInProgress), true)]
    [InlineData(nameof(InternshipPhase.Terminated), false)]
    [InlineData(nameof(InternshipPhase.Completed), false)]
    public void Kapanmis_staj_aday_degildir(string phaseName, bool expected)
    {
        SagaCorrelationPolicy.IsOpen(InternshipPhase.FromName(phaseName)).ShouldBe(expected);
    }

    /// <summary>
    /// <c>TerminationInProgress</c> bilerek <b>açık</b> sayılır: fesih süreci sürerken gelen
    /// sözleşme feshi olayı zincirin devamıdır. Kapalı sayılsaydı fesih onay zinciri
    /// tamamlanır ama saga hiç <c>Terminated</c>'a geçmezdi.
    /// </summary>
    [Fact]
    public void Fesih_surecindeki_staj_hala_acik_sayilir()
    {
        SagaCorrelationPolicy.IsOpen(InternshipPhase.TerminationInProgress).ShouldBeTrue();
    }
}

/// <summary>
/// Saga kimliği yerleştirmeden <b>deterministik</b> türer (#251).
///
/// <para><b>Yaşanan:</b> <c>Guid.NewGuid()</c> yüzünden tekrar yayınlanan <c>StudentPlaced</c>
/// her seferinde yeni saga doğuruyordu — 2248 saga, yalnız 95 yerleştirme. Fesih zinciri
/// düzeltildikten sonra bile bir saga fesihe girip 23 kardeşi geride kalıyordu.</para>
/// </summary>
public sealed class InternshipSagaIdTests
{
    private static readonly Guid Placement = Guid.Parse("44444444-4444-4444-4444-444444444444");

    /// <summary><b>Asıl kural:</b> aynı yerleştirme her zaman aynı satıra düşer.</summary>
    [Fact]
    public void Ayni_yerlestirme_ayni_kimligi_uretir()
    {
        MESNET.Internship.Core.Services.InternshipSagaId.For(Placement)
            .ShouldBe(MESNET.Internship.Core.Services.InternshipSagaId.For(Placement));
    }

    /// <summary>
    /// Farklı yerleştirme farklı saga demektir: öğrenci fesih sonrası yeni işletmeye
    /// yerleşirse ikinci staj birincinin üzerine yazmamalı.
    /// </summary>
    [Fact]
    public void Farkli_yerlestirme_farkli_kimlik_uretir()
    {
        MESNET.Internship.Core.Services.InternshipSagaId.For(Placement)
            .ShouldNotBe(MESNET.Internship.Core.Services.InternshipSagaId.For(Guid.NewGuid()));
    }

    /// <summary>Boş kimlik sessizce çakışan bir saga üretmemeli — yine de deterministik olmalı.</summary>
    [Fact]
    public void Bos_yerlestirme_de_deterministiktir()
    {
        MESNET.Internship.Core.Services.InternshipSagaId.For(Guid.Empty)
            .ShouldNotBe(MESNET.Internship.Core.Services.InternshipSagaId.For(Placement));
    }
}
