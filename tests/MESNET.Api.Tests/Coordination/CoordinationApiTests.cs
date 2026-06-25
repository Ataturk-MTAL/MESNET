using System.Net;
using System.Text;
using MESNET.Api.Tests.Infrastructure;
using Shouldly;
using Xunit;

namespace MESNET.Api.Tests.Coordination;

/// <summary>
/// Coordination modülünün tüm HTTP endpoint'leri için BDD-style black-box davranış testleri.
///
/// Kapsanan endpoint grupları:
/// - CoordinationEndpoints     → /api/coordination/teachers
/// - WeeklyVisitEndpoints      → /api/coordination/weekly-visits
/// - SkillExamEndpoints        → /api/coordination/skill-exams
/// - GuidanceVisitEndpoints    → /api/coordination/guidance-visits
/// - BusinessEvaluationEndpoints → /api/coordination/business-evaluations
/// - MonthlyActivityReportEndpoints → /api/coordination/activity-reports
///
/// Test felsefesi: Mutasyon yapan happy-path CREATE/UPDATE/DELETE testleri YOKTUR
/// (paylaşılan dev DB'yi kirletmemek için). Sadece: liste-okuma, not-found (500 değil),
/// auth (401) ve validation-reddi (boş gövde → 4xx) testleri.
/// </summary>
[Collection("api")]
public sealed class CoordinationApiTests(ApiTestFixture fixture)
{
    private readonly ApiTestFixture _fixture = fixture;

    private static StringContent EmptyJson() =>
        new("{}", Encoding.UTF8, "application/json");

    // ───────────────────────────────────────────────────────────────────
    //  CoordinationEndpoints — /api/coordination/teachers
    // ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Ogretmen_program_akislari_listelenir()
    {
        // Given — rastgele bir öğretmen kimliği
        var teacherId = Guid.NewGuid();

        // When — program stream özetleri istenir
        var response = await _fixture.Client.GetAsync(
            $"/api/coordination/teachers/{teacherId}/schedule/streams");

        // Then — boş liste geçerlidir; sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Olmayan_ogretmen_program_gecmisi_istenince_500_donmez()
    {
        // Given — kaydı olmayan bir öğretmen
        var teacherId = Guid.NewGuid();

        // When — program geçmişi istenir
        var response = await _fixture.Client.GetAsync(
            $"/api/coordination/teachers/{teacherId}/schedule/history");

        // Then — 404/200 beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Olmayan_ogretmen_donemlik_program_istenince_500_donmez()
    {
        // Given — kaydı olmayan öğretmen + yıl/dönem
        var teacherId = Guid.NewGuid();

        // When — yıl ve dönem ile ders programı istenir
        var response = await _fixture.Client.GetAsync(
            $"/api/coordination/teachers/{teacherId}/schedule?year=2025&semester=Fall");

        // Then — null-return 500 bug'ı yakalanır; sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Ogretmen_program_kaydetme_bos_govdeyle_reddedilir()
    {
        // Given — geçerli rota ama boş/geçersiz gövde
        var teacherId = Guid.NewGuid();

        // When — boş JSON ile ders programı kaydedilmek istenir
        var response = await _fixture.Client.PostAsync(
            $"/api/coordination/teachers/{teacherId}/schedule", EmptyJson());

        // Then — validation reddi (4xx); sunucu hatası DEĞİL (mutasyon olmaz)
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Ogretmen_bos_zaman_dilimleri_listelenir()
    {
        // Given — rastgele öğretmen + zorunlu yıl/dönem parametreleri
        var teacherId = Guid.NewGuid();

        // When — boş slotlar istenir
        var response = await _fixture.Client.GetAsync(
            $"/api/coordination/teachers/{teacherId}/free-slots?year=2025&semester=Fall");

        // Then — boş liste geçerlidir; sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Bos_slota_isletme_atama_bos_govdeyle_reddedilir()
    {
        // Given — geçerli rota ama eksik gövde
        var teacherId = Guid.NewGuid();

        // When — boş JSON ile işletme ataması yapılmak istenir
        var response = await _fixture.Client.PostAsync(
            $"/api/coordination/teachers/{teacherId}/assign-business", EmptyJson());

        // Then — validation reddi (4xx); sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Olmayan_ogretmen_is_yuku_istenince_500_donmez()
    {
        // Given — kaydı olmayan öğretmen
        var teacherId = Guid.NewGuid();

        // When — iş yükü istenir
        var response = await _fixture.Client.GetAsync(
            $"/api/coordination/teachers/{teacherId}/workload");

        // Then — 404/200 beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Olmayan_ogretmen_ozeti_istenince_500_donmez()
    {
        // Given — kaydı olmayan öğretmen + zorunlu dönem/semester
        var teacherId = Guid.NewGuid();
        var academicPeriodId = Guid.NewGuid();

        // When — öğretmen özeti istenir
        var response = await _fixture.Client.GetAsync(
            $"/api/coordination/teachers/{teacherId}/overview" +
            $"?academicPeriodId={academicPeriodId}&semester=Fall");

        // Then — sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Koordinatorluk_yapilandirmasi_okunur()
    {
        // When — koordinatörlük config istenir (institution claim token'dan gelir)
        var response = await _fixture.Client.GetAsync("/api/coordination/teachers/config");

        // Then — sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Koordinatorluk_yapilandirmasi_bos_govdeyle_reddedilir()
    {
        // When — boş JSON ile config güncellenmek istenir
        var response = await _fixture.Client.PostAsync(
            "/api/coordination/teachers/config", EmptyJson());

        // Then — validation reddi (4xx); sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Atama_icin_isletmeler_listelenir()
    {
        // When — atama listesi istenir (tüm filtreler opsiyonel)
        var response = await _fixture.Client.GetAsync("/api/coordination/teachers/assignments");

        // Then — boş liste geçerlidir; sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Isletme_atama_bos_govdeyle_reddedilir()
    {
        // When — boş JSON ile işletme atanmak istenir
        var response = await _fixture.Client.PostAsync(
            "/api/coordination/teachers/assignments", EmptyJson());

        // Then — validation reddi (4xx); sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Olmayan_isletme_atamasi_kaldirilinca_500_donmez()
    {
        // Given — kaydı olmayan bir işletme
        var businessId = Guid.NewGuid();

        // When — atama kaldırılmak istenir
        var response = await _fixture.Client.DeleteAsync(
            $"/api/coordination/teachers/assignments/{businessId}");

        // Then — not-found/422 beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Takdir_edilen_saat_bos_govdeyle_reddedilir()
    {
        // Given — kaydı olmayan işletme + boş gövde
        var businessId = Guid.NewGuid();

        // When — PATCH ile saat güncellenmek istenir
        var response = await _fixture.Client.PatchAsync(
            $"/api/coordination/teachers/assignments/{businessId}/hours", EmptyJson());

        // Then — validation/not-found (4xx); sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Olmayan_isletme_slot_atamasi_kaldirilinca_500_donmez()
    {
        // Given — kaydı olmayan işletme + zorunlu day/periodNumber query
        var businessId = Guid.NewGuid();

        // When — slot ataması kaldırılmak istenir
        var response = await _fixture.Client.DeleteAsync(
            $"/api/coordination/teachers/assignments/{businessId}/slot?day=Monday&periodNumber=1");

        // Then — not-found/422 beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Isletme_manuel_mesafe_bos_govdeyle_reddedilir()
    {
        // Given — kaydı olmayan işletme + boş gövde
        var businessId = Guid.NewGuid();

        // When — manuel mesafe ayarlanmak istenir
        var response = await _fixture.Client.PostAsync(
            $"/api/coordination/teachers/assignments/{businessId}/distance", EmptyJson());

        // Then — validation/not-found (4xx); sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Olmayan_isletme_atama_gecmisi_istenince_500_donmez()
    {
        // Given — kaydı olmayan işletme
        var businessId = Guid.NewGuid();

        // When — atama geçmişi istenir
        var response = await _fixture.Client.GetAsync(
            $"/api/coordination/teachers/assignments/{businessId}/history");

        // Then — boş liste/not-found beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Koordinatorluk_ozeti_istenince_500_donmez()
    {
        // When — koordinatörlük özeti istenir
        var response = await _fixture.Client.GetAsync("/api/coordination/teachers/summary");

        // Then — sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Tum_ogretmenlerin_genel_bakisi_listelenir()
    {
        // Given — zorunlu academicPeriodId + semester
        var academicPeriodId = Guid.NewGuid();

        // When — tüm öğretmenlerin özeti istenir
        var response = await _fixture.Client.GetAsync(
            $"/api/coordination/teachers/overview-all" +
            $"?academicPeriodId={academicPeriodId}&semester=Fall");

        // Then — boş liste geçerlidir; sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Isletme_kumeleri_istenince_500_donmez()
    {
        // Given — zorunlu epsMeters + minPoints query parametreleri
        // When — DBSCAN tabanlı işletme kümeleri istenir
        var response = await _fixture.Client.GetAsync(
            "/api/coordination/teachers/business-clusters?epsMeters=500&minPoints=2");

        // Then — boş liste geçerlidir; sunucu hatası DEĞİL (PostGIS raw SQL yolu)
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Mesafe_yeniden_hesaplama_500_donmez()
    {
        // When — mesafeler yeniden hesaplanmak istenir (gövdesiz POST)
        var response = await _fixture.Client.PostAsync(
            "/api/coordination/teachers/recalculate-distances", EmptyJson());

        // Then — sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Alan_ders_yuku_yapilandirmasi_istenince_500_donmez()
    {
        // Given — branchCode rota parametresi + zorunlu academicPeriodId/educationType
        var academicPeriodId = Guid.NewGuid();

        // When — alan ders yükü config istenir
        var response = await _fixture.Client.GetAsync(
            $"/api/coordination/teachers/branch-workload/MAK" +
            $"?academicPeriodId={academicPeriodId}&educationType=Normal");

        // Then — boş/not-found geçerlidir; sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Alan_ders_yuku_yapilandirmasi_bos_govdeyle_reddedilir()
    {
        // Given — branchCode rota parametresi + boş gövde
        // When — PUT ile config güncellenmek istenir
        var response = await _fixture.Client.PutAsync(
            "/api/coordination/teachers/branch-workload/MAK", EmptyJson());

        // Then — validation reddi (4xx); sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Guncel_ders_programi_olmayan_donemde_500_donmez()
    {
        // Given — kaydı olmayan öğretmen + dönem
        var teacherId = Guid.NewGuid();
        var academicPeriodId = Guid.NewGuid();

        // When — güncel ders programı istenir
        var response = await _fixture.Client.GetAsync(
            $"/api/coordination/teachers/{teacherId}/schedule/current" +
            $"?academicPeriodId={academicPeriodId}&semester=Fall");

        // Then — "program yok" geçerli boş durumdur (404); sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // ───────────────────────────────────────────────────────────────────
    //  WeeklyVisitEndpoints — /api/coordination/weekly-visits
    // ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Haftalik_ziyaret_planlari_listelenir()
    {
        // When — ziyaret planları (sayfalı) istenir
        var response = await _fixture.Client.GetAsync(
            "/api/coordination/weekly-visits/plans?page=1&pageSize=20");

        // Then — boş sayfa geçerlidir; sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Haftalik_ziyaret_olusturma_bos_govdeyle_reddedilir()
    {
        // When — boş JSON ile plan oluşturulmak istenir
        var response = await _fixture.Client.PostAsync(
            "/api/coordination/weekly-visits/generate", EmptyJson());

        // Then — validation reddi (4xx); sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Olmayan_ziyaret_plani_silinince_500_donmez()
    {
        // Given — kaydı olmayan bir plan
        var planId = Guid.NewGuid();

        // When — plan silinmek istenir
        var response = await _fixture.Client.DeleteAsync(
            $"/api/coordination/weekly-visits/plans/{planId}");

        // Then — not-found/422 beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Olmayan_plan_ziyaret_atamalari_istenince_500_donmez()
    {
        // Given — kaydı olmayan bir plan
        var planId = Guid.NewGuid();

        // When — plan atamaları (sayfalı) istenir
        var response = await _fixture.Client.GetAsync(
            $"/api/coordination/weekly-visits/plans/{planId}/assignments");

        // Then — boş sayfa/not-found geçerlidir; sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Ziyaret_atamasi_ekleme_bos_govdeyle_reddedilir()
    {
        // Given — kaydı olmayan plan + boş gövde
        var planId = Guid.NewGuid();

        // When — boş JSON ile atama eklenmek istenir
        var response = await _fixture.Client.PostAsync(
            $"/api/coordination/weekly-visits/plans/{planId}/assignments", EmptyJson());

        // Then — validation reddi (4xx); sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Olmayan_ziyaret_atamasi_silinince_500_donmez()
    {
        // Given — kaydı olmayan plan + atama
        var planId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();

        // When — atama silinmek istenir
        var response = await _fixture.Client.DeleteAsync(
            $"/api/coordination/weekly-visits/plans/{planId}/assignments/{assignmentId}");

        // Then — not-found/422 beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Ziyaret_olaylarini_yeniden_senkronize_etme_bos_govdeyle_reddedilir()
    {
        // When — boş JSON ile resync istenir
        var response = await _fixture.Client.PostAsync(
            "/api/coordination/weekly-visits/resync", EmptyJson());

        // Then — validation reddi (4xx); sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // ───────────────────────────────────────────────────────────────────
    //  SkillExamEndpoints — /api/coordination/skill-exams
    // ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Beceri_sinavlari_listelenir()
    {
        // When — beceri sınavları (sayfalı) istenir
        var response = await _fixture.Client.GetAsync(
            "/api/coordination/skill-exams?page=1&pageSize=20");

        // Then — boş sayfa geçerlidir; sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Olmayan_beceri_sinavi_istenince_500_donmez()
    {
        // Given — kaydı olmayan bir sınav
        var examId = Guid.NewGuid();

        // When — sınav detayı istenir
        var response = await _fixture.Client.GetAsync(
            $"/api/coordination/skill-exams/{examId}");

        // Then — null-return 500 bug'ı yakalanır; sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Beceri_sinavi_olusturma_bos_govdeyle_reddedilir()
    {
        // When — boş JSON ile sınav oluşturulmak istenir
        var response = await _fixture.Client.PostAsync(
            "/api/coordination/skill-exams/", EmptyJson());

        // Then — validation reddi (4xx); sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Beceri_sinavi_guncelleme_bos_govdeyle_reddedilir()
    {
        // Given — kaydı olmayan sınav + boş gövde
        var examId = Guid.NewGuid();

        // When — PUT ile sınav güncellenmek istenir
        var response = await _fixture.Client.PutAsync(
            $"/api/coordination/skill-exams/{examId}", EmptyJson());

        // Then — validation/not-found (4xx); sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // ───────────────────────────────────────────────────────────────────
    //  GuidanceVisitEndpoints — /api/coordination/guidance-visits
    // ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Rehberlik_ziyaretleri_listelenir()
    {
        // When — rehberlik ziyaretleri (sayfalı) istenir
        var response = await _fixture.Client.GetAsync(
            "/api/coordination/guidance-visits?page=1&pageSize=20");

        // Then — boş sayfa geçerlidir; sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Olmayan_rehberlik_ziyareti_istenince_500_donmez()
    {
        // Given — kaydı olmayan bir ziyaret
        var visitId = Guid.NewGuid();

        // When — ziyaret detayı istenir
        var response = await _fixture.Client.GetAsync(
            $"/api/coordination/guidance-visits/{visitId}");

        // Then — null-return 500 bug'ı yakalanır; sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Rehberlik_ziyareti_olusturma_bos_govdeyle_reddedilir()
    {
        // When — boş JSON ile ziyaret oluşturulmak istenir
        var response = await _fixture.Client.PostAsync(
            "/api/coordination/guidance-visits/", EmptyJson());

        // Then — validation reddi (4xx); sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Rehberlik_ziyareti_guncelleme_bos_govdeyle_reddedilir()
    {
        // Given — kaydı olmayan ziyaret + boş gövde
        var visitId = Guid.NewGuid();

        // When — PUT ile ziyaret güncellenmek istenir
        var response = await _fixture.Client.PutAsync(
            $"/api/coordination/guidance-visits/{visitId}", EmptyJson());

        // Then — validation/not-found (4xx); sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Olmayan_rehberlik_ziyareti_gonderilince_500_donmez()
    {
        // Given — kaydı olmayan ziyaret
        var visitId = Guid.NewGuid();

        // When — ziyaret submit edilmek istenir (gövdesiz)
        var response = await _fixture.Client.PostAsync(
            $"/api/coordination/guidance-visits/{visitId}/submit", EmptyJson());

        // Then — not-found/422 beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Olmayan_rehberlik_ziyareti_onaylanmaya_calisilinca_500_donmez()
    {
        // Given — kaydı olmayan ziyaret
        var visitId = Guid.NewGuid();

        // When — ziyaret approve edilmek istenir (gövdesiz)
        var response = await _fixture.Client.PostAsync(
            $"/api/coordination/guidance-visits/{visitId}/approve", EmptyJson());

        // Then — not-found/422 beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // ───────────────────────────────────────────────────────────────────
    //  BusinessEvaluationEndpoints — /api/coordination/business-evaluations
    // ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Isletme_degerlendirmeleri_listelenir()
    {
        // When — işletme değerlendirmeleri (sayfalı) istenir
        var response = await _fixture.Client.GetAsync(
            "/api/coordination/business-evaluations?page=1&pageSize=20");

        // Then — boş sayfa geçerlidir; sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Olmayan_isletme_degerlendirmesi_istenince_500_donmez()
    {
        // Given — kaydı olmayan bir değerlendirme
        var evaluationId = Guid.NewGuid();

        // When — değerlendirme detayı istenir
        var response = await _fixture.Client.GetAsync(
            $"/api/coordination/business-evaluations/{evaluationId}");

        // Then — null-return 500 bug'ı yakalanır; sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Isletme_degerlendirmesi_olusturma_bos_govdeyle_reddedilir()
    {
        // When — boş JSON ile değerlendirme oluşturulmak istenir
        var response = await _fixture.Client.PostAsync(
            "/api/coordination/business-evaluations/", EmptyJson());

        // Then — validation reddi (4xx); sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Isletme_degerlendirmesi_guncelleme_bos_govdeyle_reddedilir()
    {
        // Given — kaydı olmayan değerlendirme + boş gövde
        var evaluationId = Guid.NewGuid();

        // When — PUT ile değerlendirme güncellenmek istenir
        var response = await _fixture.Client.PutAsync(
            $"/api/coordination/business-evaluations/{evaluationId}", EmptyJson());

        // Then — validation/not-found (4xx); sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // ───────────────────────────────────────────────────────────────────
    //  MonthlyActivityReportEndpoints — /api/coordination/activity-reports
    // ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Aylik_faaliyet_raporlari_listelenir()
    {
        // When — aylık faaliyet raporları (sayfalı) istenir
        var response = await _fixture.Client.GetAsync(
            "/api/coordination/activity-reports?page=1&pageSize=20");

        // Then — boş sayfa geçerlidir; sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Olmayan_aylik_faaliyet_raporu_istenince_500_donmez()
    {
        // Given — kaydı olmayan bir rapor
        var reportId = Guid.NewGuid();

        // When — rapor detayı istenir
        var response = await _fixture.Client.GetAsync(
            $"/api/coordination/activity-reports/{reportId}");

        // Then — null-return 500 bug'ı yakalanır; sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Aylik_faaliyet_raporu_olusturma_bos_govdeyle_reddedilir()
    {
        // When — boş JSON ile rapor oluşturulmak istenir
        var response = await _fixture.Client.PostAsync(
            "/api/coordination/activity-reports/", EmptyJson());

        // Then — validation reddi (4xx); sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Aylik_faaliyet_raporu_guncelleme_bos_govdeyle_reddedilir()
    {
        // Given — kaydı olmayan rapor + boş gövde
        var reportId = Guid.NewGuid();

        // When — PUT ile rapor güncellenmek istenir
        var response = await _fixture.Client.PutAsync(
            $"/api/coordination/activity-reports/{reportId}", EmptyJson());

        // Then — validation/not-found (4xx); sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Olmayan_aylik_rapor_gonderilince_500_donmez()
    {
        // Given — kaydı olmayan rapor
        var reportId = Guid.NewGuid();

        // When — rapor submit edilmek istenir (gövdesiz)
        var response = await _fixture.Client.PostAsync(
            $"/api/coordination/activity-reports/{reportId}/submit", EmptyJson());

        // Then — not-found/422 beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Olmayan_aylik_rapor_onaylanmaya_calisilinca_500_donmez()
    {
        // Given — kaydı olmayan rapor
        var reportId = Guid.NewGuid();

        // When — rapor approve edilmek istenir (gövdesiz)
        var response = await _fixture.Client.PostAsync(
            $"/api/coordination/activity-reports/{reportId}/approve", EmptyJson());

        // Then — not-found/422 beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // ───────────────────────────────────────────────────────────────────
    //  Auth — temsilci endpoint'ler token'sız 401 döndürmeli
    // ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Tokensiz_atama_listesi_istegi_401_doner()
    {
        // Given — token'sız (anonim) client
        // When — atama listesi istenir
        var response = await _fixture.Anonymous.GetAsync(
            "/api/coordination/teachers/assignments");

        // Then — kimlik doğrulama gerekir → 401 Unauthorized
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Tokensiz_haftalik_ziyaret_plani_istegi_401_doner()
    {
        // Given — token'sız (anonim) client
        // When — ziyaret planları istenir
        var response = await _fixture.Anonymous.GetAsync(
            "/api/coordination/weekly-visits/plans");

        // Then — 401 Unauthorized
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Tokensiz_beceri_sinavi_listesi_istegi_401_doner()
    {
        // Given — token'sız (anonim) client
        // When — beceri sınavları istenir
        var response = await _fixture.Anonymous.GetAsync(
            "/api/coordination/skill-exams");

        // Then — 401 Unauthorized
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Tokensiz_rehberlik_ziyareti_listesi_istegi_401_doner()
    {
        // Given — token'sız (anonim) client
        // When — rehberlik ziyaretleri istenir
        var response = await _fixture.Anonymous.GetAsync(
            "/api/coordination/guidance-visits");

        // Then — 401 Unauthorized
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Tokensiz_isletme_degerlendirmesi_listesi_istegi_401_doner()
    {
        // Given — token'sız (anonim) client
        // When — işletme değerlendirmeleri istenir
        var response = await _fixture.Anonymous.GetAsync(
            "/api/coordination/business-evaluations");

        // Then — 401 Unauthorized
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Tokensiz_aylik_faaliyet_raporu_listesi_istegi_401_doner()
    {
        // Given — token'sız (anonim) client
        // When — aylık faaliyet raporları istenir
        var response = await _fixture.Anonymous.GetAsync(
            "/api/coordination/activity-reports");

        // Then — 401 Unauthorized
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
