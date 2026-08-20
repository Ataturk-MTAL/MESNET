using MESNET.Internship.Application.Sagas;
using MESNET.Internship.Core.Enums;
using MESNET.Internship.Core.Policies;
using MESNET.Internship.Core.ValueObjects;
using MESNET.Internship.Shared.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace MESNET.Internship.UnitTests;

/// <summary>
/// Yürüyen fesih onay zinciri <b>yeniden başlatılamaz</b> (#252).
///
/// <para><b>Bulunan açık:</b> <c>InternshipSaga.Handle(InternshipTerminationRequested)</c>
/// zinciri koşulsuz kuruyordu (<c>ApprovalChain = new(...)</c>). İkinci bir fesih talebi,
/// toplanmış koordinatör öğretmen / müdür yardımcısı / müdür onaylarını <b>sessizce
/// siliyordu</b> — zincirde kimin onayladığı saklanmadığı için (yalnız bool'lar) sıfırlama
/// denetim izine de düşmüyordu.</para>
///
/// <para><b>Neden tekrar eden talep gerçek:</b> aktarıcı her <c>AttendanceLimitExceeded</c>'de
/// bir talep üretir; devamsızlık sayacı dönem içinde <b>sıfırlanmadığı</b> için sınır dolduktan
/// sonraki her yeni ya da <b>onaylanan</b> kayıt yeniden tetikler. #252 onayı da bir tetikleyici
/// yaptığı için pencere genişledi. Manuel uç da ikinci kez çağrılabilir.</para>
///
/// <para><b>Çözüm neden burada değil de saga'da:</b> aktarıcıdaki faz süzgeci yalnız bir okuma
/// olurdu; iki olay eşzamanlı işlendiğinde ikisi de saga'yı <c>Active</c> görebilir. Karar
/// durumun sahibinde verilir.</para>
/// </summary>
public sealed class TerminationChainRestartTests
{
    private static readonly Guid SagaId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Student = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static InternshipSaga AktifStaj() => new()
    {
        Id = SagaId,
        StudentId = Student,
        BusinessId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
        InstitutionId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
        AcademicPeriodId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
        Phase = InternshipPhase.Active
    };

    private static InternshipTerminationRequested Talep(string gerekce, string tur) =>
        new(SagaId, gerekce, tur, "Sistem");

    // ─── Politika: karar saf ve testli ───────────────────────────────────────────────

    [Fact]
    public void Zincir_hic_baslamamissa_baslatilabilir()
    {
        TerminationChainPolicy.CanStart(null).ShouldBeTrue();
    }

    [Fact]
    public void Yuruyen_zincir_yeniden_baslatilamaz()
    {
        TerminationChainPolicy.CanStart(new TerminationApprovalChain()).ShouldBeFalse();
    }

    [Fact]
    public void Kapanmis_zincir_de_yeniden_baslatilamaz()
    {
        var kapali = new TerminationApprovalChain
        {
            TeacherApproved = true,
            DeputyApproved = true,
            DirectorApproved = true,
            CompletedAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        TerminationChainPolicy.CanStart(kapali).ShouldBeFalse();
    }

    // ─── Saga: uygulaması ────────────────────────────────────────────────────────────

    [Fact]
    public void Ilk_talep_zinciri_baslatir()
    {
        var saga = AktifStaj();

        var sonuc = saga.Handle(Talep("Devamsızlık limiti aşıldı: 10/10 gün",
            "AttendanceLimitExceeded"), NullLogger.Instance);

        sonuc.ShouldNotBeNull();
        saga.Phase.ShouldBe(InternshipPhase.TerminationInProgress);
        saga.ApprovalChain.ShouldNotBeNull();
    }

    /// <summary>
    /// <b>Asıl regresyon.</b> Öğretmen ve müdür yardımcısı onaylamışken gelen ikinci talep
    /// onayları silmez ve ikinci bir zincir-başladı bildirimi yayınlamaz.
    /// </summary>
    [Fact]
    public void Ikinci_talep_toplanmis_onaylari_silmez()
    {
        var saga = AktifStaj();
        saga.Handle(Talep("Devamsızlık limiti aşıldı: 10/10 gün", "AttendanceLimitExceeded"),
            NullLogger.Instance);

        saga.ApprovalChain = saga.ApprovalChain! with { TeacherApproved = true, DeputyApproved = true };

        var ikinci = saga.Handle(Talep("Devamsızlık limiti aşıldı: 11/10 gün",
            "AttendanceLimitExceeded"), NullLogger.Instance);

        ikinci.ShouldBeNull("Yürüyen zincir ikinci bir başlangıç bildirimi yayınlamamalı.");
        saga.ApprovalChain!.TeacherApproved.ShouldBeTrue("Öğretmen onayı silinemez.");
        saga.ApprovalChain!.DeputyApproved.ShouldBeTrue("Müdür yardımcısı onayı silinemez.");
    }

    /// <summary>
    /// Sıradaki adım korunur: sıfırlansaydı zincir başa dönerdi ve müdür yardımcısı, öğretmen
    /// yeniden onaylayana kadar onaylayamazdı (#218 sıra dayatması).
    /// </summary>
    [Fact]
    public void Ikinci_talep_siradaki_adimi_geri_almaz()
    {
        var saga = AktifStaj();
        saga.Handle(Talep("ilk gerekçe", "Manual"), NullLogger.Instance);
        saga.ApprovalChain = saga.ApprovalChain! with { TeacherApproved = true };

        saga.Handle(Talep("ikinci gerekçe", "AttendanceLimitExceeded"), NullLogger.Instance);

        TerminationChainPolicy.NextStep(saga.ApprovalChain).ShouldBe(TerminationStep.Deputy);
    }

    /// <summary>İlk talebin gerekçesi kalır — fesih dosyasındaki sebep sonradan değişmez.</summary>
    [Fact]
    public void Ikinci_talep_ilk_gerekceyi_ezmez()
    {
        var saga = AktifStaj();
        saga.Handle(Talep("Veli talebi", "ParentRequest"), NullLogger.Instance);

        saga.Handle(Talep("Devamsızlık limiti aşıldı: 10/10 gün", "AttendanceLimitExceeded"),
            NullLogger.Instance);

        saga.TerminationReason.ShouldBe("Veli talebi");
        saga.TerminationReasonType.ShouldBe("ParentRequest");
    }
}
