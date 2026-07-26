using MESNET.Coordination.Core.ReadModels;
using Shouldly;
using Xunit;

namespace MESNET.Coordination.UnitTests;

/// <summary>
/// <see cref="BusinessCoordinationView.ResolveBusinessId"/> — çok-alanlı modele geçişte
/// eski tek-satır kayıtlarının işletme kimliği (#114).
/// </summary>
public sealed class BusinessCoordinationViewTests
{
    private static readonly Guid Business = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Period = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public void Yeni_satirda_isletme_kimligi_BusinessId_alanindan_gelir()
    {
        // Given — çok-alanlı modelde yazılmış satır
        var row = new BusinessCoordinationView
        {
            Id = CoordinationViewId.For(Business, "EET", Period),
            BusinessId = Business,
            BranchCode = "EET",
            AcademicPeriodId = Period,
        };

        // Then
        row.ResolveBusinessId().ShouldBe(Business);
    }

    [Fact]
    public void Eski_tek_satir_kaydinda_isletme_kimligi_Id_alanindan_gelir()
    {
        // Given — BusinessId alanı olmayan eski kayıt: Id = BusinessId
        var legacy = new BusinessCoordinationView { Id = Business };

        // Then — geriye dönük çözümleme
        legacy.ResolveBusinessId().ShouldBe(Business);
    }
}
