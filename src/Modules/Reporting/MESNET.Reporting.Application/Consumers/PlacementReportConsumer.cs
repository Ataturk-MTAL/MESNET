using Marten;
using MESNET.Enrollment.Shared.Events;
using MESNET.Reporting.Core.ReadModels;

namespace MESNET.Reporting.Application.Consumers;

/// <summary>
/// Enrollment modülü event'lerini dinleyerek StudentPlacementReportView read model'ini günceller.
/// Öğrenci-işletme-alan eşleşmesi bilgisi sağlar.
/// </summary>
public static class PlacementReportConsumer
{
    /// <summary>Öğrenci kaydı olayı — canlı yol.</summary>
    public static Task Consume(StudentRegistered @event, IDocumentSession session)
        => ApplyStudent(@event.ToSnapshot(), session);

    /// <summary>
    /// Onarım yolu (#290): <c>POST /api/students/resync-projections</c> bu olayı yayınlar.
    /// <c>StudentRegistered</c> yeniden yayınlanamaz — tüketicilerinden biri şube sayacını
    /// ARTIRIYOR ve her yeniden yayın sayacı şişirirdi.
    /// </summary>
    public static Task Consume(StudentSnapshotResynced @event, IDocumentSession session)
        => ApplyStudent(@event, session);

    /// <summary>
    /// Öğrenci alanlarını yazar — <b>yalnız kendi alanlarını</b> (#296).
    ///
    /// <para><b>Eski hâli satırı sıfırdan kuruyordu</b> ve <c>session.Store</c> ile üzerine
    /// yazıyordu. Sonuç: yerleştirme yolunun doldurduğu dört alan
    /// (<c>BusinessId</c>, <c>BusinessName</c>, <c>BranchName</c>, <c>TeacherId</c>) ve
    /// <c>BusinessReportConsumer</c>'ın doldurduğu üç alan (<c>BusinessPhone</c>,
    /// <c>BusinessEmail</c>, <c>BusinessContactName</c>) her öğrenci olayında varsayılana
    /// düşüyordu. <c>POST /api/students/resync-projections</c> her koşusunda TÜM satırlarda.
    /// Belirti sessizdi: uç 200 döner, aylık devamsızlık formu ve dönem not fişi işletme
    /// bilgisi olmadan basılırdı.</para>
    /// </summary>
    private static async Task ApplyStudent(StudentSnapshotResynced @event, IDocumentSession session)
    {
        var view = await LoadOrCreateAsync(session, @event.StudentId, @event.AcademicPeriodId, @event.InstitutionId);

        view.StudentName = @event.FullName;
        view.StudentNumber = @event.StudentNumber;
        view.ClassName = $"{@event.BranchCode} - {(@event.ClassYear > 0 ? @event.ClassYear.ToString() : "?")}";
        view.ClassYear = @event.ClassYear;
        view.BranchCode = @event.BranchCode;

        // BranchName BURADA YAZILMAZ: Enrollment olayı onu taşımıyor ve boş dizeyle yazmak,
        // yerleştirme yolunun doldurduğu değeri ezerdi — düzeltilen kusurun ta kendisi.

        session.Store(view);
    }

    /// <summary>
    /// Yerleştirme yaşam döngüsü olayı — canlı yol.
    /// </summary>
    public static Task Consume(StudentPlaced @event, IDocumentSession session)
        => Apply(@event.ToSnapshot(), session);

    /// <summary>
    /// Onarım yolu (#291): <c>POST /api/placements/resync-projections</c> bu olayı yayınlar.
    /// <c>StudentPlaced</c> yeniden yayınlanamaz — o, saga'nın başlatıcı olayıdır ve yeniden
    /// yayını tekil kısıt ihlaliyle ölü mektuba düşerdi (uç yine 200 dönerek).
    /// </summary>
    public static Task Consume(PlacementSnapshotResynced @event, IDocumentSession session)
        => Apply(@event, session);

    /// <summary>
    /// Yerleştirme alanlarını yazar — <b>yalnız kendi alanlarını</b>.
    ///
    /// <para>Eski hâli satırı sorguyla buluyordu ve bulamazsa <c>Id = PlacementId</c> ile
    /// kuruyordu; öğrenci yolu ise <c>Id = StudentId</c> kullanıyordu. Hangi kimliğin geçerli
    /// olduğu hangi olayın önce geldiğine bağlıydı (#296). Artık ikisi de aynı deterministik
    /// kimliği üretiyor.</para>
    /// </summary>
    private static async Task Apply(PlacementSnapshotResynced @event, IDocumentSession session)
    {
        var view = await LoadOrCreateAsync(session, @event.StudentId, @event.AcademicPeriodId, @event.InstitutionId);

        view.BusinessId = @event.BusinessId;
        view.BusinessName = @event.BusinessName;
        view.BranchCode = @event.BranchCode;
        view.BranchName = @event.BranchName;
        view.TeacherId = @event.TeacherId;

        session.Store(view);
    }

    /// <summary>
    /// Satırı deterministik kimliğiyle yükler, yoksa kurar — <b>iki yolun tek kapısı</b>.
    ///
    /// <para><b>Eski kimlikli satır aynı anda silinir (kendini onaran göç).</b> Mevcut satırlar
    /// <c>Id = StudentId</c> ile yazılmıştı (ölçüldü: dev'de 363/363). Silinmeseler okuyan
    /// sorgular aynı öğrenciyi <b>iki kez</b> görürdü — aylık devamsızlık formunda ve toplu
    /// belge üretiminde çift satır. Ayrı bir temizlik adımı yerine buraya konuldu: atlanabilecek
    /// bir dağıtım adımı, atlanmayacak bir koda tercih edilmez.</para>
    /// </summary>
    private static async Task<StudentPlacementReportView> LoadOrCreateAsync(
        IDocumentSession session, Guid studentId, Guid academicPeriodId, Guid institutionId)
    {
        var id = StudentPlacementReportView.CreateId(studentId, academicPeriodId);

        var view = await session.LoadAsync<StudentPlacementReportView>(id);

        if (view is null)
        {
            // Eski kimlikli satırdan DEVRAL. Devralmasaydık, dağıtım ile resync arasında canlı
            // gelen tek bir öğrenci olayı satırı yeni kimlikle sıfırdan kurar ve işletme
            // alanlarını kaybederdik — onarımı bekleyen bir veri kaybı penceresi.
            //
            // Nesne KOPYALANIR, kimliği değiştirilmez: yüklenen belgenin Id'sini yerinde
            // değiştirip Store etmek, oturumun kimlik haritası tutup tutmamasına göre farklı
            // davranır (lightweight session'da çalışır, izleyen session'da eski satırı
            // güncellerdi). Kopya bu belirsizliği tümden kaldırır.
            var legacy = await session.LoadAsync<StudentPlacementReportView>(studentId);

            view = legacy is null
                ? new StudentPlacementReportView { StudentId = studentId }
                : legacy.WithId(id);

            view.Id = id;
        }

        // Eski kimlikli satır her hâlükârda silinir; kimlik değiştiği için kopya kalırdı.
        if (studentId != id)
            session.Delete<StudentPlacementReportView>(studentId);

        view.StudentId = studentId;
        view.AcademicPeriodId = academicPeriodId;
        view.InstitutionId = institutionId;

        return view;
    }
}
