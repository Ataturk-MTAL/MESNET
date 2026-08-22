namespace MESNET.Enrollment.Application.Commands;

/// <summary>
/// Öğrenciyi staja yerleştirir.
/// </summary>
/// <param name="BusinessId">
/// İşletme. <b>null = okulda staj</b> (#159): staj yeri bulunamayan öğrenci stajını okulda
/// yapar; ücret ve devlet katkısı doğmaz, dekont beklenmez.
/// </param>
/// <param name="TeacherId">
/// İşletmede stajda koordinatör öğretmen; okulda stajda gözetmen (alan/atölye şefi).
/// Gözetmenlik ataması ücret üretmez.
/// </param>
public sealed record PlaceStudent(
    Guid StudentId,
    Guid? BusinessId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    Guid? TeacherId);
