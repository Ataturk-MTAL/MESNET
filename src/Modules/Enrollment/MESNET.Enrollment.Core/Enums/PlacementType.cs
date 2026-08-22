using Ardalis.SmartEnum;

namespace MESNET.Enrollment.Core.Enums;

/// <summary>
/// Yerleştirmenin türü — öğrenci stajını bir işletmede mi, okulda mı yapıyor (#159).
/// </summary>
/// <remarks>
/// <para><b>Neden ayrı bir kavram:</b> 3308 Madde 25 (ücret tabanı) ve Geçici Madde 12 (devlet
/// katkısı) <b>ayrı ayrı</b> aynı istisnayı yazıyor: <i>"Staj yapacak işletme bulunamaması
/// nedeniyle stajını okulda yapan ortaöğretim öğrencileri ... bu fıkra hükmü kapsamı
/// dışındadır."</i> Yani biri diğerinin sonucu değil; ücret de katkı da doğmaz.</para>
///
/// <para><b>Neden #157'deki kamu işareti kullanılamaz:</b> <c>IsPublicInstitution</c> yalnız
/// devlet katkısını sıfırlar, <b>ücret yükümlülüğünü bırakır</b> — dekont beklenir, ayın 8'inde
/// gecikme uyarısı gider. Benzediği için mevcut bir bayrağı yeniden kullanmak, o issue'da
/// düzeltilen hatanın aynısı olurdu.</para>
///
/// <para><b>Neden okul adına sahte işletme kaydı değil:</b> sözleşme kurulabilir hale gelir ve
/// sistem ücret + katkı hesaplar; ikisi de kanuna aykırıdır.</para>
/// </remarks>
public sealed class PlacementType : SmartEnum<PlacementType>
{
    /// <summary>İşletmede staj — olağan hâl. İşletme zorunludur.</summary>
    public static readonly PlacementType Business = new(nameof(Business), 1, "İşletmede", requiresBusiness: true);

    /// <summary>
    /// Okulda staj — staj yeri bulunamadığı için. İşletme YOKTUR: ücret ve devlet katkısı
    /// doğmaz, dekont beklenmez. Staj sürer; devamsızlık, dönem notu ve mezuniyet akışı işler.
    /// </summary>
    public static readonly PlacementType School = new(nameof(School), 2, "Okulda", requiresBusiness: false);

    /// <summary>Türkçe UI karşılığı.</summary>
    public string Slug { get; }

    /// <summary>Bu tür bir işletme kimliği taşımak zorunda mı.</summary>
    public bool RequiresBusiness { get; }

    private PlacementType(string name, int value, string slug, bool requiresBusiness) : base(name, value)
    {
        Slug = slug;
        RequiresBusiness = requiresBusiness;
    }
}
