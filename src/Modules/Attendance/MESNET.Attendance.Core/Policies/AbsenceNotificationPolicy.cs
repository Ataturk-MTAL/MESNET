namespace MESNET.Attendance.Core.Policies;

/// <summary>Bildirim ayağı — hangi sayaçtan doğdu.</summary>
public static class AbsenceNotificationLeg
{
    /// <summary>Mazeretsiz (özürsüz) devamsızlık ayağı.</summary>
    public const string Unexcused = "Unexcused";

    /// <summary>Toplam devamsızlık ayağı — mazeretli, raporlu ve izinli dâhil.</summary>
    public const string Total = "Total";
}

/// <summary>Bir ayak için verilen bildirim kararı.</summary>
/// <param name="Leg"><see cref="AbsenceNotificationLeg"/> değeri.</param>
/// <param name="Step">Ulaşılan kademe — 5, 15 ya da 25.</param>
/// <param name="Days">Kademeyi dolduran gün sayısı.</param>
/// <param name="SkippedSteps">
/// Atlanan kademeler. Toplu giriş ya da özelliğin sonradan açılması yüzünden sayaç bir sıçramada
/// birden çok kademeyi geçmiş olabilir; bu alan hangi bildirimlerin <b>zamanında
/// yapılamadığını</b> kayda geçirir.
/// </param>
public sealed record AbsenceNotificationDecision(
    string Leg, int Step, int Days, IReadOnlyList<int> SkippedSteps);

/// <summary>
/// Kademeli devamsızlık bildirimi — <b>5., 15. ve 25.</b> gün (#247).
///
/// <para><b>Yasal dayanak:</b> MEB Ortaöğretim Kurumları Yönetmeliği <b>md. 36 (4)</b>: işletmede
/// mesleki eğitimde devamsızlığın <b>5., 15. ve 25.</b> gününde <b>veliye ve işletmeye</b>
/// yazılı bildirim yapılır; 18 yaşından büyük öğrencide <b>öğrencinin kendisine de</b>. Fıkranın
/// amacı ailenin, md. 36 (5) feshi gelmeden önce durumu öğrenmesidir.</para>
///
/// <para><b>İKİ AYAK AYRI DEĞERLENDİRİLİR.</b> Md. 36 (5) fesih için "özürsüz 10 / toplam 30"
/// diye ayırıyor; bildirim fıkrası yalnız "devamsızlık" diyor. Sahibin kararı: her iki ayak da
/// ayrı takip edilir ve ayrı bildirim üretir. Yalnız mazeretsiz sayılsaydı, raporlu ve mazeretli
/// günlerle 25 günü geçen öğrencinin ailesi hiç uyarılmazdı — oysa toplam ayak 30'da fesih
/// getiriyor.</para>
///
/// <para><b><see cref="AttendanceLimitPolicy.Evaluate"/> YENİDEN KULLANILMAZ:</b> o metot tek bir
/// karar döndürür ve mazeretsiz ayağı önceler (fesih gerekçesinde hangi ayağın dolduğu tek
/// olmalıdır). Bildirimde ikisi de bağımsızdır ve aynı gün ikisi birden dolabilir.</para>
///
/// <para><b>Kademeler neden sabit:</b> 5/15/25 doğrudan md. 36 (4)'ün lafzıdır ve okul başına
/// değişmez. #183'ün parametrik yaptığı sayılar <i>sınırlardı</i> (mevzuat değişirse kayıt
/// değişsin); burada sayı hükmün kendisidir. Mevzuat değişirse bu sabitler de değişir.</para>
/// </summary>
public static class AbsenceNotificationPolicy
{
    /// <summary>Md. 36 (4): işletmede mesleki eğitim için bildirim kademeleri.</summary>
    public static readonly int[] Steps = [5, 15, 25];

    /// <summary>18 yaşını dolduran öğrenciye bildirim <b>ayrıca</b> yapılır.</summary>
    public const int StudentNotificationAge = 18;

    /// <summary>
    /// Bu ayakta yeni bir bildirim gerekiyor mu — gerekmiyorsa <c>null</c>.
    /// </summary>
    /// <param name="days">Ayaktaki güncel gün sayısı.</param>
    /// <param name="lastNotifiedStep">
    /// Bu ayak için en son bildirilen kademe (0 = hiç bildirilmedi).
    /// </param>
    /// <remarks>
    /// <para><b>Yalnız EN YÜKSEK ulaşılan kademe bildirilir.</b> Sayaç bir sıçramada birden çok
    /// kademeyi geçmiş olabilir (haftalık toplu giriş, biriken kayıtların toplu onayı, ya da bu
    /// özelliğin sonradan açılması). 27 günü olan öğrenci için 5/15/25'in üçünü birden
    /// göndermek gürültüdür ve 5. gün uyarısı o noktada zaten anlamsızdır. Atlananlar
    /// <see cref="AbsenceNotificationDecision.SkippedSteps"/> ile kayda geçer, böylece
    /// tebligatta "5. ve 15. gün bildirimleri zamanında yapılamadı" bilgisi görünebilir.</para>
    ///
    /// <para><b>Sayaç geri gidebilir</b> (düzeltme, silme, sağlık raporu onayı). Kademe kaydı
    /// GERİ ALINMAZ: yapılmış bir tebligat yapılmamış sayılamaz. Sayaç düşüp yeniden aynı
    /// kademeye çıkarsa ikinci bildirim gitmez — istenen budur.</para>
    /// </remarks>
    public static AbsenceNotificationDecision? Evaluate(string leg, int days, int lastNotifiedStep)
    {
        var reached = Steps.Where(s => days >= s).ToArray();
        if (reached.Length == 0) return null;

        var highest = reached[^1];
        if (highest <= lastNotifiedStep) return null;

        var skipped = reached
            .Where(s => s > lastNotifiedStep && s < highest)
            .ToArray();

        return new AbsenceNotificationDecision(leg, highest, days, skipped);
    }

    /// <summary>
    /// Öğrencinin kendisine de bildirim yapılır mı — md. 36 (4) bunu 18 yaşını doldurmuş
    /// öğrenciler için istiyor.
    /// </summary>
    /// <remarks>
    /// <para><b>Doğum tarihi BİLİNMİYORSA GÖNDERİLİR.</b> <c>StudentProfile.BirthDate</c>
    /// nullable ve kayıt sırasında zorunlu değil — yani bilinmeyen doğum tarihi kenar durum
    /// değil, yaygın bir hâl.</para>
    ///
    /// <para>Gönderme yönü güvenlidir, göndermeme yönü değil: veli ve işletme ayakları
    /// koşulsuzdur, öğrenci ayağı yalnız bir alıcı <b>ekler</b>, hiçbir zorunlu alıcıyı
    /// kaldırmaz. Göndermezsek 18 yaşını doldurmuş öğrenci hakkı olan tebligatı hiç almaz ve bu
    /// sessizdir — tam da #247'nin kapatmak istediği boşluk. Gönderirsek 18 altı öğrenciye
    /// fazladan bir bildirim gider; o bildirim zaten kendi devamsızlığıyla ilgilidir.</para>
    /// </remarks>
    public static bool ShouldNotifyStudent(DateTime? birthDate, DateTime referenceDate)
    {
        if (birthDate is not { } birth) return true;

        var age = referenceDate.Year - birth.Year;
        if (referenceDate.Month < birth.Month
            || (referenceDate.Month == birth.Month && referenceDate.Day < birth.Day))
            age--;

        // Bozuk/gelecek tarih: yine gönderilir — eksik veri alıcıyı düşürmemeli.
        if (age < 0) return true;

        return age >= StudentNotificationAge;
    }
}
