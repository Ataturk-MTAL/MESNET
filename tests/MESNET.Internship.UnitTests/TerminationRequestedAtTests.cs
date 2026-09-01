using MESNET.Enrollment.Shared.Events;
using MESNET.Internship.Application.Sagas;
using MESNET.Internship.Shared.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace MESNET.Internship.UnitTests;

/// <summary>
/// Fesih talebinin açılma zamanı kaydedilir. Bu alan olmadan "kaç gündür bekliyor"
/// hesaplanamaz — müdürlük panosunun tıkanmışlık kartı bütünüyle buna dayanır.
/// </summary>
public sealed class TerminationRequestedAtTests
{
    private static InternshipSaga StartedSaga()
    {
        var placed = new StudentPlaced(
            PlacementId: Guid.NewGuid(),
            StudentId: Guid.NewGuid(),
            BusinessId: Guid.NewGuid(),
            InstitutionId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            TeacherId: null,
            PlacedAt: DateTime.UtcNow,
            StudentName: "Test Öğrenci",
            BusinessName: "Test İşletme");

        var (saga, _) = InternshipSaga.Start(placed);
        return saga;
    }

    [Fact]
    public void Yeni_saga_talep_zamani_tasimaz()
    {
        StartedSaga().TerminationRequestedAt.ShouldBeNull();
    }

    [Fact]
    public void Fesih_talebi_acildiginda_zaman_kaydedilir()
    {
        // Arrange
        var saga = StartedSaga();
        var before = DateTime.UtcNow;

        // Act
        saga.Handle(
            new InternshipTerminationRequested(saga.Id, "Gerekçe", "BusinessRequest", "Sistem"),
            NullLogger.Instance);

        // Assert
        saga.TerminationRequestedAt.ShouldNotBeNull();
        saga.TerminationRequestedAt!.Value.ShouldBeGreaterThanOrEqualTo(before);
    }

    /// <summary>
    /// İkinci talep yok sayılır (zincir zaten yürüyor) — zaman damgası da EZİLMEZ, yoksa her
    /// yinelenen talep sayacı sıfırlar ve tıkanmış zincir sonsuza kadar taze görünür.
    /// </summary>
    [Fact]
    public void Ikinci_talep_zamani_ezmez()
    {
        var saga = StartedSaga();
        saga.Handle(
            new InternshipTerminationRequested(saga.Id, "İlk", "BusinessRequest", "Sistem"),
            NullLogger.Instance);
        var first = saga.TerminationRequestedAt;

        saga.Handle(
            new InternshipTerminationRequested(saga.Id, "İkinci", "BusinessRequest", "Sistem"),
            NullLogger.Instance);

        saga.TerminationRequestedAt.ShouldBe(first);
    }
}
