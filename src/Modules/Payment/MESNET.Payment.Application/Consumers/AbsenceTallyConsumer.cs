using Marten;
using MESNET.Attendance.Shared.Events;
using MESNET.Payment.Application.Commands;
using MESNET.Payment.Application.Services;
using MESNET.Payment.Core.ReadModels;
using MESNET.Payment.Core.Services;
using Microsoft.Extensions.Logging;

namespace MESNET.Payment.Application.Consumers;

/// <summary>
/// Attendance olaylarından Payment'ın yerel devamsızlık kaydını besler <b>ve</b> kesinti
/// kümesi değiştiğinde maaşı yeniden hesaplatır (#64, #255).
/// </summary>
/// <remarks>
/// <para><c>AttendanceVerified</c> / <c>AttendanceApproved</c> / <c>AttendanceCorrected</c> /
/// <c>AttendanceDeleted</c> olayları tarih ve işletme bilgisi taşımıyor, yalnız
/// <c>AttendanceId</c> var — bu yüzden kayıt <c>AttendanceMarked</c>'ta kurulup sonraki
/// olaylarda kimlikten yüklenerek güncelleniyor. Gün bilgisi de görünümün kendisinden
/// (<c>StudentAbsenceView.Date</c>, #154) okunur.</para>
///
/// <para><b>Yeniden hesap tetiği neden BURADA (#255):</b> tetikleyici, sayacı güncelleyen
/// tüketicinin <b>dönüşü</b> olmak zorundadır — gerekçe
/// <see cref="SalaryRecalculationTrigger"/>. Ayrı bir tüketici sıra garantisi olmadığı için
/// bayat görünüm okur ve kesinti sessizce kaybolur. Eski <c>SalaryTriggerConsumer</c> tam da bu
/// yüzden kaldırıldı; <c>AttendanceMarked</c> tetiği de buraya taşındı, yoksa altı olay düzelir
/// yedincisi yarışlı kalırdı.</para>
///
/// <para><b>Tetik yalnız sayılabilirlik DEĞİŞTİĞİNDE atılır</b>
/// (<see cref="AbsenceDeductionPolicy.CountsTowardDeduction"/>). Durum <c>Recorded</c>'dan
/// <c>Verified</c>'a geçmek tutarı etkilemez; her olayda komut yayınlamak saga'yı gereksiz
/// eşzamanlı çağrılarla döverdi.</para>
/// </remarks>
public static class AbsenceTallyConsumer
{
    public static async Task<RecalculateMonthlySalary?> Consume(
        AttendanceMarked @event, IDocumentSession session)
    {
        var view = new StudentAbsenceView
        {
            Id = @event.AttendanceId,
            InstitutionId = @event.InstitutionId,
            StudentId = @event.StudentId,
            BusinessId = @event.BusinessId,
            Month = @event.Date.ToString("yyyy-MM"),
            // Gün, kesintinin hangi sözleşmeye yazılacağını belirler (#154).
            Date = @event.Date.Date,
            AbsenceTypeName = @event.AbsenceType,
            StatusName = @event.InitialStatus
        };

        session.Store(view);

        // Yeni kayıt: öncesi yok, yani yalnız sayılabilir doğduysa tutar değişir.
        return AbsenceDeductionPolicy.CountsTowardDeduction(view.AbsenceTypeName, view.StatusName)
            ? await SalaryRecalculationTrigger.ForDayAsync(session, view.StudentId, view.Date)
            : null;
    }

    /// <summary>
    /// İşletmenin girdiği kayıt öğretmence onaylandı: <c>Pending → Recorded</c>. Kaydı kesinti
    /// kümesine <b>sokan</b> adım budur (#252) — tutar burada değişir.
    /// </summary>
    public static Task<RecalculateMonthlySalary?> Consume(
        AttendanceApproved @event, IDocumentSession session, ILogger logger)
        => UpdateAsync(session, logger, @event.AttendanceId, nameof(AttendanceApproved),
            view => view.StatusName = "Recorded");

    public static Task<RecalculateMonthlySalary?> Consume(
        AttendanceVerified @event, IDocumentSession session, ILogger logger)
        => UpdateAsync(session, logger, @event.AttendanceId, nameof(AttendanceVerified),
            view => view.StatusName = "Verified");

    public static Task<RecalculateMonthlySalary?> Consume(
        AttendanceCorrected @event, IDocumentSession session, ILogger logger)
        => UpdateAsync(session, logger, @event.AttendanceId, nameof(AttendanceCorrected), view =>
        {
            view.AbsenceTypeName = @event.NewAbsenceType;
            view.StatusName = "Corrected";
        });

    /// <summary>
    /// Sağlık raporu ONAYLANDI (#172) — devamsızlık türü <c>HealthReport</c>'a döner ve o tür
    /// ücret kesintisine tabi değildir (business-rules.md §6.2). Kesinti bu anda kalkar.
    /// </summary>
    public static Task<RecalculateMonthlySalary?> Consume(
        HealthReportApproved @event, IDocumentSession session, ILogger logger)
        => UpdateAsync(session, logger, @event.AttendanceId, nameof(HealthReportApproved),
            view => view.AbsenceTypeName = "HealthReport");

    /// <summary>
    /// Rapor yüklendi (#172). Kesinti YALNIZ okul tarafının doğrudan girdiği raporda kalkar
    /// (<c>RequiresApproval = false</c>) — onay zaten o rollerde biter.
    ///
    /// <para>İşletme, usta öğretici, işletme İK ya da öğrenci yüklediğinde
    /// <c>RequiresApproval = true</c>'dur ve burada HİÇBİR ŞEY yapılmaz: ödemeyi yapan taraf
    /// kendi kesintisini tek taraflı kaldıramaz. Tür ancak koordinatör öğretmenin onayıyla,
    /// <c>HealthReportApproved</c> üzerinden değişir.</para>
    /// </summary>
    public static Task<RecalculateMonthlySalary?> Consume(
        HealthReportAttached @event, IDocumentSession session, ILogger logger)
        => @event.RequiresApproval
            ? Task.FromResult<RecalculateMonthlySalary?>(null)
            : UpdateAsync(session, logger, @event.AttendanceId, nameof(HealthReportAttached),
                view => view.AbsenceTypeName = "HealthReport");

    /// <summary>
    /// Kayıt silindi. <b>Sıra önemlidir:</b> gün ve sayılabilirlik SİLMEDEN ÖNCE okunur; sonrası
    /// yok sayılır. Silinmiş devamsızlıktan para kesilmeye devam etmemeli.
    /// </summary>
    public static async Task<RecalculateMonthlySalary?> Consume(
        AttendanceDeleted @event, IDocumentSession session, ILogger logger)
    {
        var view = await session.LoadAsync<StudentAbsenceView>(@event.AttendanceId);
        if (view is null)
        {
            WarnMissing(logger, nameof(AttendanceDeleted), @event.AttendanceId);
            return null;
        }

        var wasCountable = AbsenceDeductionPolicy.CountsTowardDeduction(
            view.AbsenceTypeName, view.StatusName);

        var studentId = view.StudentId;
        var day = view.Date;

        session.Delete<StudentAbsenceView>(@event.AttendanceId);

        return wasCountable
            ? await SalaryRecalculationTrigger.ForDayAsync(session, studentId, day)
            : null;
    }

    /// <summary>
    /// Geçmişe dönük onarım (#256): kaydın <b>bugünkü tam hâli</b> gelir, yerel satır ona
    /// eşitlenir.
    ///
    /// <para><b>Neden gerekli:</b> <c>AttendanceApproved</c> ve kardeşleri uzun süre hiç teslim
    /// edilmedi (#253), dolayısıyla bu satırlar <c>AttendanceMarked</c> anındaki hâlde donmuş
    /// olabilir — agregada <c>Recorded</c> olan kayıt burada <c>Pending</c>, onaylanmış raporlu
    /// gün hâlâ <c>Unexcused</c>, silinen kayıt hiç silinmemiş.</para>
    ///
    /// <para><b>Gün anlık görüntüden alınır</b>, yerel satırdan değil: yerel satır #154 öncesi
    /// yazılmışsa tarihsizdir ve onarımın amacı tam da onu düzeltmektir.</para>
    /// </summary>
    public static async Task<RecalculateMonthlySalary?> Consume(
        AttendanceSnapshotResynced @event, IDocumentSession session)
    {
        var view = await session.LoadAsync<StudentAbsenceView>(@event.AttendanceId);

        var before = view is not null
            && AbsenceDeductionPolicy.CountsTowardDeduction(view.AbsenceTypeName, view.StatusName);

        bool after;

        if (@event.IsDeleted)
        {
            if (view is not null) session.Delete<StudentAbsenceView>(@event.AttendanceId);
            after = false;
        }
        else
        {
            session.Store(new StudentAbsenceView
            {
                Id = @event.AttendanceId,
                InstitutionId = @event.InstitutionId,
                StudentId = @event.StudentId,
                BusinessId = @event.BusinessId,
                Month = @event.Date.ToString("yyyy-MM"),
                Date = @event.Date.Date,
                AbsenceTypeName = @event.AbsenceType,
                StatusName = @event.Status
            });

            after = AbsenceDeductionPolicy.CountsTowardDeduction(@event.AbsenceType, @event.Status);
        }

        return before == after
            ? null
            : await SalaryRecalculationTrigger.ForDayAsync(session, @event.StudentId, @event.Date);
    }

    /// <summary>
    /// Görünümü günceller ve <b>yalnız sayılabilirlik değiştiyse</b> yeniden hesap ister.
    /// </summary>
    private static async Task<RecalculateMonthlySalary?> UpdateAsync(
        IDocumentSession session, ILogger logger, Guid attendanceId, string olay,
        Action<StudentAbsenceView> apply)
    {
        var view = await session.LoadAsync<StudentAbsenceView>(attendanceId);
        if (view is null)
        {
            WarnMissing(logger, olay, attendanceId);
            return null;
        }

        var before = AbsenceDeductionPolicy.CountsTowardDeduction(
            view.AbsenceTypeName, view.StatusName);

        apply(view);
        session.Store(view);

        var after = AbsenceDeductionPolicy.CountsTowardDeduction(
            view.AbsenceTypeName, view.StatusName);

        return before == after
            ? null
            : await SalaryRecalculationTrigger.ForDayAsync(session, view.StudentId, view.Date);
    }

    /// <summary>
    /// Görünüm bulunamadı — <b>sessizce yutulmaz</b>. Bu tüketicinin yerel kuyruğu varsayılan
    /// olarak paralel ve sırasızdır; <c>AttendanceApproved</c>, kaydı kuran
    /// <c>AttendanceMarked</c>'ı geçerse satır hiç bulunamaz, durum kalıcı olarak <c>Pending</c>
    /// kalır ve o gün kesintiye <b>hiç</b> girmez. Sıralama sorunu bu düzeltmenin kapsamı
    /// dışındadır (ayrı iş) — ama sessiz kalması kabul edilemez.
    /// </summary>
    private static void WarnMissing(ILogger logger, string olay, Guid attendanceId)
        => logger.LogWarning(
            "{Olay} işlendi ama Payment'ın devamsızlık görünümü bulunamadı: {AttendanceId}. "
            + "Kayıt kesinti hesabında güncellenmedi; olay sırası ters gelmiş olabilir.",
            olay, attendanceId);
}
