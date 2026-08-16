namespace MESNET.Attendance.Application.Commands;

/// <summary>
/// Devamsızlık sınırlarını günceller — <b>ulusal parametre</b> (#183).
///
/// <para>Kurum kimliği <b>taşımaz</b>: sınır md. 36'dan türer ve okul başına değişemez. Yazma
/// izni <c>platform:parameter:manage</c>'dir; hiçbir okul rolünde yoktur.</para>
///
/// <para><b>MESEM'in özürsüz alanı yoktur</b> ve eklenmemelidir: yönetmelik işletme eğitimini
/// yalnız 3308 izin hakkı toplamıyla karşılaştırır. Simetrik görünsün diye alan açmak,
/// mevzuatta karşılığı olmayan bir fesih eşiği yaratırdı.</para>
/// </summary>
public sealed record UpdateAbsenceLimits(
    int FormalUnexcusedDayLimit, int FormalTotalDayLimit, int MesemTotalDayLimit);
