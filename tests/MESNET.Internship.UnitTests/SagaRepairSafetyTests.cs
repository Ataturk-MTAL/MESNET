using System.Reflection;
using MESNET.Internship.Application.Commands;
using MESNET.Internship.Application.Sagas;
using MESNET.Internship.Core.Enums;
using Shouldly;
using Xunit;

namespace MESNET.Internship.UnitTests;

/// <summary>
/// Onarım uçlarının saga durumunu <b>bozmadığını</b> kilitler (#297).
///
/// <para>İkisi de #292 düzeltmesiyle ortaya çıktı: o uçlar fiilen ölüydü (platform aktörü kiracı
/// damgalı satırı görmüyordu, 200 + sıfır), canlandıklarında taşıdıkları kusurlar da
/// canlandı.</para>
/// </summary>
public class SagaRepairSafetyTests
{
    [Fact]
    public void Sozlesme_bagi_yurumus_fazi_GERI_ALMAZ()
    {
        // Arrange — fesih onay zinciri yürüyor.
        var saga = new InternshipSaga { Phase = InternshipPhase.TerminationInProgress };

        // Act — onarım ucu ContractActivated yeniden yayınlıyor.
        saga.Handle(new LinkInternshipContract(saga.Id, Guid.NewGuid()));

        // Assert — KİLİT NOKTA. Koşulsuz `Phase = Active` yürüyen fesih zincirini SESSİZCE
        // iptal ederdi: SagaCorrelationPolicy.IsOpen TerminationInProgress'i bilerek açık sayar,
        // yani o saga bu yolu görür.
        saga.Phase.ShouldBe(InternshipPhase.TerminationInProgress);
    }

    [Fact]
    public void Sozlesme_bagi_her_zaman_yazilir()
    {
        var contractId = Guid.NewGuid();
        var saga = new InternshipSaga { Phase = InternshipPhase.TerminationInProgress };

        saga.Handle(new LinkInternshipContract(saga.Id, contractId));

        // Onarımın amacı bağı kurmaktır; faz korunurken bağ atlanırsa uç işini yapmamış olur.
        saga.ContractId.ShouldBe(contractId);
    }

    [Fact]
    public void Sozlesme_bekleyen_saga_Active_e_ilerler()
    {
        var saga = new InternshipSaga { Phase = InternshipPhase.AwaitingContract };

        saga.Handle(new LinkInternshipContract(saga.Id, Guid.NewGuid()));

        // İleri yön korunmalı — aksi hâlde düzeltme, ucun asıl işini de kapatırdı.
        saga.Phase.ShouldBe(InternshipPhase.Active);
    }

    /// <summary>
    /// Kimlik yeniden yazılırken saga durumunun <b>tamamı</b> taşınmalıdır.
    ///
    /// <para><c>TerminationRequestedAt</c> D2 ile eklenmiş ve <c>WithId</c>'de unutulmuştu:
    /// onarım o alanı <c>null</c>'a çeviriyordu ve <c>StuckApprovalPolicy</c> null'u
    /// <b>tıkanmış</b> sayıyor — yani onarım, müdürlük panosunda olmayan tıkanmalar üretirdi.</para>
    ///
    /// <para>Alan listesini elle saymak yerine <b>yansıma</b> kullanılıyor: saga'ya yeni bir alan
    /// eklendiğinde bu test kendiliğinden onu da arar. Elle liste, unutulan alanın tam olarak
    /// tekrar unutulacağı yerdir.</para>
    /// </summary>
    [Fact]
    public void Kimlik_yeniden_yazilirken_HICBIR_alan_dusmez()
    {
        var method = typeof(MESNET.Internship.Application.Handlers.ResyncInternshipSagasHandler)
            .GetMethod("WithId", BindingFlags.NonPublic | BindingFlags.Static);

        method.ShouldNotBeNull("WithId taşınmış ya da yeniden adlandırılmış — kilit boşa düştü.");

        var source = new InternshipSaga
        {
            Id = Guid.NewGuid(),
            PlacementId = Guid.NewGuid(),
            StudentId = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            InstitutionId = Guid.NewGuid(),
            AcademicPeriodId = Guid.NewGuid(),
            ContractId = Guid.NewGuid(),
            Phase = InternshipPhase.TerminationInProgress,
            TerminationReason = "gerekçe",
            TerminationReasonType = "tür",
            RequiresParentApproval = true,
            TerminationRequestedAt = new DateTime(2026, 3, 4, 5, 6, 7, DateTimeKind.Utc),
        };

        var newId = Guid.NewGuid();
        var copy = (InternshipSaga)method!.Invoke(null, [source, newId])!;

        copy.Id.ShouldBe(newId);

        var dropped = typeof(InternshipSaga)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.Name != nameof(InternshipSaga.Id))
            .Where(p => !Equals(p.GetValue(copy), p.GetValue(source)))
            .Select(p => p.Name)
            .ToList();

        dropped.ShouldBeEmpty(
            "Saga kimliği yeniden yazılırken alan DÜŞTÜ. WithId gövdesine ekleyin; düşen alan "
            + $"sessizce varsayılan değerinde doğar. Düşenler: {string.Join(", ", dropped)}");
    }
}
