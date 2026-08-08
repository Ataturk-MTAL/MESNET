using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Core.Enums;
using Shouldly;
using Xunit;

namespace MESNET.Enrollment.UnitTests;

/// <summary>
/// <c>Status</c>/<c>Type</c> yazıldığında LINQ kopyaları da güncellenir (#220).
///
/// <para><b>Yaşanan:</b> ikisi de auto-property'ydi ve düz string kopyaları (<c>StatusName</c>,
/// <c>TypeName</c>) elle yazılmak zorundaydı. Fesih sonrası okula atama tüketicisi yalnız
/// <c>Status</c>'ü yazdı; belge <c>status=Cancelled, statusName=Matched</c> hâline geldi.</para>
///
/// <para><b>Neden sessiz:</b> Marten LINQ SmartEnum'u sorgulayamaz, bu yüzden tüm sorgular
/// <c>StatusName</c>'e bakar. Kayıt iptal edilmiş olduğu hâlde sorgularda <b>hâlâ açık</b>
/// görünüyordu — hata veren bir şey yok, yalnız yanlış sonuç.</para>
/// </summary>
public sealed class PlacementStatusSyncTests
{
    [Fact]
    public void Status_yazilinca_StatusName_senkronlanir()
    {
        var placement = new InternshipPlacement { Status = PlacementStatus.Cancelled };

        placement.StatusName.ShouldBe(PlacementStatus.Cancelled.Name);
    }

    [Fact]
    public void Type_yazilinca_TypeName_senkronlanir()
    {
        var placement = new InternshipPlacement { Type = PlacementType.School };

        placement.TypeName.ShouldBe(PlacementType.School.Name);
    }

    /// <summary>Varsayılanlar da tutarlı başlamalı.</summary>
    [Fact]
    public void Varsayilan_degerler_tutarlidir()
    {
        var placement = new InternshipPlacement();

        placement.StatusName.ShouldBe(placement.Status.Name);
        placement.TypeName.ShouldBe(placement.Type.Name);
    }

    /// <summary>Art arda yazımda son değer geçerlidir — eski ad takılı kalmamalı.</summary>
    [Fact]
    public void Ard_arda_yazimda_son_deger_gecerlidir()
    {
        var placement = new InternshipPlacement { Status = PlacementStatus.Active };
        placement.Status = PlacementStatus.Completed;

        placement.StatusName.ShouldBe(PlacementStatus.Completed.Name);
    }
}
