using MESNET.Audit.Core.Enums;
using MESNET.Audit.Core.Services;
using MESNET.Common.Shared;
using Shouldly;
using Xunit;

namespace MESNET.Audit.UnitTests;

public class AuditEntryFactoryTests
{
    private static readonly Guid AktorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AktorKurumu = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid BaskaKurum = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static AuditInput Girdi(
        object? command = null,
        Guid? actorInstitutionId = null,
        string? actorPath = null,
        string? subjectPathOverride = null)
        => new(
            Id: Guid.NewGuid(),
            OccurredAt: new DateTimeOffset(2026, 8, 28, 9, 0, 0, TimeSpan.Zero),
            ActorId: AktorId,
            ActorName: "Ayşe Öğretmen",
            CommandType: (command ?? new object()).GetType(),
            Command: command,
            TenantId: AktorKurumu.ToString(),
            ActorInstitutionId: actorInstitutionId ?? AktorKurumu,
            ActorInstitutionPath: actorPath,
            SubjectInstitutionPathOverride: subjectPathOverride,
            DurationMs: 42);

    private sealed record OrnekKomut(Guid StudentId, Guid InstitutionId);

    // ── Sonuç eşlemesi ────────────────────────────────────────────────────

    [Fact]
    public void Basarili_komut_Succeeded_yazar_ve_hata_kodu_tasimaz()
    {
        var entry = AuditEntryFactory.Succeeded(Girdi());

        entry.OutcomeName.ShouldBe(AuditOutcome.Succeeded.Name);
        entry.ErrorCode.ShouldBeNull();
    }

    [Fact]
    public void DomainException_Rejected_yazar_ve_Error_Code_saklar()
    {
        // "Sistem çalıştı, kural izin vermedi" — bir davranış kaydıdır, arıza değil.
        var ex = new DomainException(new Error("INSTITUTION_SCOPE_DENIED", "Kurum kapsamı dışında."));

        var entry = AuditEntryFactory.Failed(Girdi(), ex);

        entry.OutcomeName.ShouldBe(AuditOutcome.Rejected.Name);
        entry.ErrorCode.ShouldBe("INSTITUTION_SCOPE_DENIED");
    }

    [Fact]
    public void DomainException_hata_MESAJINI_saklamaz()
    {
        // Mesaj PII taşıyabilir (öğrenci adı, ilçe adı). Kod makine okunurdur ve sabittir.
        var ex = new DomainException(new Error("X", "Ahmet Yılmaz adlı öğrenci bulunamadı."));

        var entry = AuditEntryFactory.Failed(Girdi(), ex);

        entry.ErrorCode.ShouldBe("X");
        // Satırın hiçbir alanında mesaj geçmemeli.
        entry.ToString().ShouldNotContain("Ahmet");
    }

    [Fact]
    public void Diger_istisna_Failed_yazar_ve_istisna_tipinin_adini_saklar()
    {
        var entry = AuditEntryFactory.Failed(Girdi(), new InvalidOperationException("bağlantı düştü"));

        entry.OutcomeName.ShouldBe(AuditOutcome.Failed.Name);
        entry.ErrorCode.ShouldBe(nameof(InvalidOperationException));
    }

    // ── Kiracı sınırı ─────────────────────────────────────────────────────

    [Fact]
    public void Ayni_kurum_kiracı_sinirini_asmaz()
    {
        var komut = new OrnekKomut(Guid.NewGuid(), AktorKurumu);

        var entry = AuditEntryFactory.Succeeded(Girdi(komut, actorInstitutionId: AktorKurumu));

        entry.SubjectInstitutionId.ShouldBe(AktorKurumu);
        entry.CrossedTenantBoundary.ShouldBeFalse();
    }

    [Fact]
    public void Farkli_kurum_kiracı_sinirini_asar()
    {
        // B parçasının sorumluluk sorgusu tek bu alana iner.
        var komut = new OrnekKomut(Guid.NewGuid(), BaskaKurum);

        var entry = AuditEntryFactory.Succeeded(Girdi(komut, actorInstitutionId: AktorKurumu));

        entry.SubjectInstitutionId.ShouldBe(BaskaKurum);
        entry.CrossedTenantBoundary.ShouldBeTrue();
    }

    [Fact]
    public void Kurumsuz_aktor_siniri_asmis_sayilmaz()
    {
        // Platform aktörünün kurumu yoktur; "ayrıştı" demek yanlış olurdu — karşılaştıracak
        // bir taraf yok. Sınır aşımı bir İDDİADIR, veri eksikliği onu doğurmaz.
        // NOT: Girdi(...) yardımcısı "actorInstitutionId ?? AktorKurumu" ile varsayılan
        // atar; bu yüzden `actorInstitutionId: null` GEÇMEK, argümanı hiç vermemekle
        // AYNI sonucu (AktorKurumu) üretir — brief'teki hâliyle bu test hep AktorKurumu
        // görür ve amacını (kurumsuz aktör) hiç sınamaz. `with` ile üretilen girdi
        // üzerinde ActorInstitutionId'i açıkça null'a zorluyoruz (bkz. task-5-report.md).
        var komut = new OrnekKomut(Guid.NewGuid(), BaskaKurum);

        var entry = AuditEntryFactory.Succeeded(Girdi(komut) with { ActorInstitutionId = null });

        entry.CrossedTenantBoundary.ShouldBeFalse();
    }

    [Fact]
    public void Kurum_hedefi_olmayan_komutta_konu_kurum_aktorun_kurumudur()
    {
        var entry = AuditEntryFactory.Succeeded(Girdi(new { X = 1 }));

        entry.SubjectInstitutionId.ShouldBe(AktorKurumu);
        entry.CrossedTenantBoundary.ShouldBeFalse();
    }

    // ── Yol ───────────────────────────────────────────────────────────────

    [Fact]
    public void Konu_aktorun_kurumuysa_yol_aktorun_claim_yolundan_gelir()
    {
        // Sıcak yolda EK OKUMA YOK: okul kullanıcısının kendi kurumuna yazması bu daldadır.
        var komut = new OrnekKomut(Guid.NewGuid(), AktorKurumu);

        var entry = AuditEntryFactory.Succeeded(
            Girdi(komut, actorInstitutionId: AktorKurumu, actorPath: "/il/ilce/okul/"));

        entry.SubjectInstitutionPath.ShouldBe("/il/ilce/okul/");
    }

    [Fact]
    public void Konu_baska_kurumsa_yol_disaridan_verilen_degerden_gelir()
    {
        var komut = new OrnekKomut(Guid.NewGuid(), BaskaKurum);

        var entry = AuditEntryFactory.Succeeded(Girdi(
            komut,
            actorInstitutionId: AktorKurumu,
            actorPath: "/il/",
            subjectPathOverride: "/il/ilce/baska-okul/"));

        entry.SubjectInstitutionPath.ShouldBe("/il/ilce/baska-okul/");
    }

    [Fact]
    public void Yol_cozulemezse_satir_yine_yazilir_yol_null_kalir()
    {
        // Sessiz kayıp yok: satır durur, yalnız yol önekiyle okuyana görünmez.
        var komut = new OrnekKomut(Guid.NewGuid(), BaskaKurum);

        var entry = AuditEntryFactory.Succeeded(
            Girdi(komut, actorInstitutionId: AktorKurumu, actorPath: "/il/", subjectPathOverride: null));

        entry.SubjectInstitutionPath.ShouldBeNull();
        entry.ActorId.ShouldBe(AktorId);
    }

    [Fact]
    public void ResolveSubject_yazicinin_sordugu_soruya_ayni_cevabi_verir()
    {
        // Yazıcı, yolu aramaya gerek olup olmadığını satırı kurmadan önce bu yardımcıdan
        // öğrenir. İki yerde iki ayrı "konu kurum" tanımı doğmasın diye tek kaynak.
        var komut = new OrnekKomut(Guid.NewGuid(), BaskaKurum);

        var (subjectId, crossed) = AuditEntryFactory.ResolveSubject(komut, AktorKurumu);
        var entry = AuditEntryFactory.Succeeded(Girdi(komut, actorInstitutionId: AktorKurumu));

        subjectId.ShouldBe(entry.SubjectInstitutionId);
        crossed.ShouldBe(entry.CrossedTenantBoundary);
    }

    // ── Kimlik alanları ───────────────────────────────────────────────────

    [Fact]
    public void Komut_tipi_kisa_adi_modul_ve_Turkce_etiket_yazilir()
    {
        var entry = AuditEntryFactory.Succeeded(Girdi(
            new MESNET.AuditFixtures.Sample.Application.Commands.MarkAttendanceSample(
                Guid.NewGuid(), Guid.NewGuid())));

        entry.CommandType.ShouldBe("MarkAttendanceSample");
        entry.Module.ShouldBe("AuditFixtures");
        // Sözlükte yok → ham ad. Boş DÖNMEZ.
        entry.CommandLabel.ShouldBe("MarkAttendanceSample");
    }

    [Fact]
    public void Hedef_kimlikleri_satira_yazilir()
    {
        var studentId = Guid.NewGuid();
        var komut = new OrnekKomut(studentId, AktorKurumu);

        var entry = AuditEntryFactory.Succeeded(Girdi(komut));

        entry.TargetIds["StudentId"].ShouldBe(studentId);
    }
}
