using MESNET.Coordination.Core.Enums;

namespace MESNET.Coordination.Core.Services;

/// <summary>
/// Bir alanın saat dağıtımını <b>tek seferde</b> doğrulayan saf politika (#117).
///
/// <para><b>Neden var:</b> saatler işletme başına ayrı çağrıyla kaydedilirken her çağrı
/// <c>Σ AssignedHours ≤ P</c> kontrolünden geçiyordu. Havuz 40, mevcut A=20 B=10 iken
/// yeni dağıtım A=10 B=20 uygulanmak istenince, B önce kaydedilirse ara toplam
/// 20 (A henüz düşmemiş) + 20 = 40 çıkıyor ve bir saat fazlada işlem reddediliyordu.
/// Sonuç çağrı sırasına bağlıydı; kullanıcı kısmi başarı alıyordu.</para>
///
/// <para><b>Çözüm:</b> tüm set birlikte değerlendirilir — değişen satırların
/// <b>yeni</b> değerleri, değişmeyenlerin mevcut değerleri toplanır. Toplam sıradan
/// bağımsızdır. Doğrulama geçmezse çağıran hiçbir satırı yazmaz.</para>
///
/// <para>Dış bağımlılığı yoktur (Marten/Wolverine görmez); girdiyi değiştirmez.</para>
/// </summary>
public static class BranchHoursPolicy
{
    /// <summary>
    /// Seti doğrular. Kırılan ilk kısıtı döndürür, her şey geçerliyse <c>null</c>.
    /// Sıra bilinçlidir: önce satır bazlı kısıtlar (kullanıcı kendi girdiği satırı
    /// düzeltebilsin), sonra havuz, en sonda öğretmen kapasitesi.
    /// </summary>
    public static BranchHoursViolation? Validate(BranchHoursValidationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Changes);
        ArgumentNullException.ThrowIfNull(input.OtherTeacherBillableHours);

        foreach (var change in input.Changes)
        {
            var rowViolation = ValidateRow(change);
            if (rowViolation is not null) return rowViolation;
        }

        return ValidatePool(input) ?? ValidateTeachers(input);
    }

    /// <summary>
    /// Satır bazlı kısıtlar: <c>0 &lt; x_i ≤ max_i</c>.
    /// Fahri satır muaftır — saati 0'a sabitlenir, ücret doğurmaz (#115).
    /// </summary>
    private static BranchHoursViolation? ValidateRow(BranchHoursChange change)
    {
        if (change.IsHonoraryVisit) return null;

        if (change.RequestedHours <= 0)
        {
            return new BranchHoursViolation(
                HoursViolationKind.InvalidAssignedHours,
                Attempted: change.RequestedHours,
                Limit: change.MaxCoordinationHours,
                BusinessId: change.BusinessId,
                BusinessName: change.BusinessName);
        }

        if (change.RequestedHours > change.MaxCoordinationHours)
        {
            return new BranchHoursViolation(
                HoursViolationKind.AssignedHoursExceedMax,
                Attempted: change.RequestedHours,
                Limit: change.MaxCoordinationHours,
                BusinessId: change.BusinessId,
                BusinessName: change.BusinessName);
        }

        return null;
    }

    /// <summary>
    /// Havuz kısıtı: <c>Σ x_i ≤ P</c>. Fahri satırlar 0 katkı verir.
    /// Havuz yapılandırılmamışsa (<c>null</c>) kısıt uygulanmaz — tekil uç noktanın
    /// "config yoksa erken dön" davranışıyla aynı.
    /// </summary>
    private static BranchHoursViolation? ValidatePool(BranchHoursValidationInput input)
    {
        if (input.TotalWorkloadPool is not { } pool) return null;

        var total = input.OtherBillableHours + input.Changes.Sum(c => c.EffectiveBillableHours());
        if (total <= pool) return null;

        return new BranchHoursViolation(
            HoursViolationKind.WorkloadPoolExceeded,
            Attempted: total,
            Limit: pool,
            AffectedBusinessNames: [.. input.Changes.Select(c => c.BusinessName)]);
    }

    /// <summary>
    /// Öğretmen kısıtı: öğretmenin tüm satırlarının hedef saati ≤ MaxWeeklyExtraHours.
    /// Yalnız <b>ücretli</b> değişikliği olan öğretmenler denetlenir: tamamı fahriye
    /// çevrilen bir set toplamı yalnızca düşürür, denetlemek onu haksız yere reddederdi (#115).
    /// </summary>
    private static BranchHoursViolation? ValidateTeachers(BranchHoursValidationInput input)
    {
        if (input.MaxWeeklyExtraHours is not { } maxHours) return null;

        var teacherGroups = input.Changes
            .Where(c => c.AssignedTeacherId.HasValue)
            .GroupBy(c => c.AssignedTeacherId!.Value);

        foreach (var group in teacherGroups)
        {
            if (group.All(c => c.IsHonoraryVisit)) continue;

            input.OtherTeacherBillableHours.TryGetValue(group.Key, out var otherHours);
            var total = otherHours + group.Sum(c => c.EffectiveBillableTargetHours());

            if (total <= maxHours) continue;

            return new BranchHoursViolation(
                HoursViolationKind.TeacherHoursExceedMax,
                Attempted: total,
                Limit: maxHours,
                TeacherId: group.Key,
                TeacherName: group.Select(c => c.AssignedTeacherName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)),
                AffectedBusinessNames: [.. group.Select(c => c.BusinessName)]);
        }

        return null;
    }
}
