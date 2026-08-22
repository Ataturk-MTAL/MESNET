using MESNET.Reporting.Application.Templates.Components;
using MESNET.Reporting.Core.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MESNET.Reporting.Application.Templates;

/// <summary>
/// Form 8: Dönem Not Fişi — "İşletmelerde Meslek Eğitimi Gören Öğrencilere Ait Dönem Not Fişi".
/// MEB Mesleki ve Teknik Eğitim Yönetmeliği md. 82. Yatay (landscape) yerleşim.
/// </summary>
public class TermGradeSlipDocument : IDocument
{
    private readonly TermGradeSlipFormData _data;

    public TermGradeSlipDocument(TermGradeSlipFormData data) => _data = data;

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());   // yatık yerleşim — geniş not ızgarası
            page.Margin(MebFormStyles.PageMarginCm, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(MebFormStyles.BodyFontSize));

            page.Content().Column(col =>
            {
                col.Item().MebHeaderWithQr(
                    "İŞLETMELERDE MESLEK EĞİTİMİ GÖREN ÖĞRENCİLERE AİT DÖNEM NOT FİŞİ",
                    _data.InstitutionName, _data.DocumentId);

                col.Item().PaddingTop(10).Element(ComposeInfo);
                col.Item().PaddingTop(6).Element(ComposeGradeTable);
                col.Item().PaddingTop(22).Element(ComposeSignatures);
                col.Item().PaddingTop(14).Element(ComposeNotes);
            });
        });
    }

    // Okul / dönem / işletme bilgi bloğu
    private void ComposeInfo(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(120); c.RelativeColumn(); // Okul/Kurum Adı
                c.ConstantColumn(80); c.RelativeColumn();  // orta
                c.ConstantColumn(70); c.RelativeColumn();  // sağ
            });

            table.Cell().HeaderCellStyle().Text(t => t.Span("Okul/Kurumun Adı").FormSmall().Bold());
            table.Cell().ColumnSpan(5).CellStyle().Text(t => t.Span(_data.InstitutionName).FormSmall());

            table.Cell().HeaderCellStyle().Text(t => t.Span("Öğretim Yılı").FormSmall().Bold());
            table.Cell().CellStyle().Text(t => t.Span(_data.AcademicYear).FormSmall());
            table.Cell().HeaderCellStyle().Text(t => t.Span("Dönemi").FormSmall().Bold());
            table.Cell().CellStyle().Text(t => t.Span(SemesterLabel(_data.Semester)).FormSmall());
            table.Cell().HeaderCellStyle().Text(t => t.Span("Ders").FormSmall().Bold());
            table.Cell().CellStyle().Text(t => t.Span(_data.CourseName).FormSmall());

            table.Cell().HeaderCellStyle().Text(t => t.Span("İşletmenin Adı").FormSmall().Bold());
            table.Cell().CellStyle().Text(t => t.Span(_data.BusinessName).FormSmall());
            table.Cell().HeaderCellStyle().Text(t => t.Span("Tel").FormSmall().Bold());
            table.Cell().CellStyle().Text(t => t.Span(_data.BusinessPhone ?? "").FormSmall());
            table.Cell().HeaderCellStyle().Text(t => t.Span("E-Posta").FormSmall().Bold());
            table.Cell().CellStyle().Text(t => t.Span(_data.BusinessEmail ?? "").FormSmall());
        });
    }

    // Not ızgarası — gruplu başlık + tek öğrenci satırı
    private void ComposeGradeTable(IContainer container)
    {
        // Alt başlıklar (UI metni — Türkçe)
        var subHeaders = new[]
        {
            "Numarası", "Adı Soyadı", "Meslek Alan/Dalı",
            "Temrin", "İş-Hizmet", "Proje", "Deney",
            "Telafi Eğitim Puanı (*)", "Beceri Yarışması (*)", "Ort. (Rakam ile)", "Ort. (Yazı ile)"
        };

        var values = new[]
        {
            _data.StudentNumber,
            _data.StudentFullName,
            _data.BranchName,
            JoinGrades(_data.PracticeGrades),
            JoinGrades(_data.ServiceGrades),
            JoinGrades(_data.ProjectGrades),
            JoinGrades(_data.ExperimentGrades),
            _data.MakeupTrainingScore?.ToString() ?? "",
            _data.SkillCompetitionScore?.ToString() ?? "",
            _data.TermAverage?.ToString("0.##") ?? "",
            _data.TermAverageInWords ?? ""
        };

        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(50);    // Numarası
                c.RelativeColumn(2.4f);  // Adı Soyadı
                c.RelativeColumn(2.4f);  // Meslek Alan/Dalı
                c.RelativeColumn(1.2f);  // Temrin
                c.RelativeColumn(1.2f);  // İş-Hizmet
                c.RelativeColumn(1.1f);  // Proje
                c.RelativeColumn(1.1f);  // Deney
                c.ConstantColumn(60);    // Telafi (*)
                c.ConstantColumn(60);    // Beceri Yarışması (*)
                c.ConstantColumn(55);    // Ort. (Rakam)
                c.RelativeColumn(1.6f);  // Ort. (Yazı)
            });

            table.Header(header =>
            {
                // Grup satırı
                header.Cell().ColumnSpan(3).HeaderCellStyle().AlignCenter()
                    .Text(t => t.Span("Öğrencinin").FormSmall().Bold());
                header.Cell().ColumnSpan(4).HeaderCellStyle().AlignCenter()
                    .Text(t => t.Span("İşletmelerde Verilen Puanlar").FormSmall().Bold());
                header.Cell().ColumnSpan(2).HeaderCellStyle().AlignCenter()
                    .Text(t => t.Span("Okulda Verilen Puanlar").FormSmall().Bold());
                header.Cell().ColumnSpan(2).HeaderCellStyle().AlignCenter()
                    .Text(t => t.Span("Dönem Başarısı").FormSmall().Bold());

                // Alt başlık satırı
                foreach (var h in subHeaders)
                    header.Cell().HeaderCellStyle().AlignCenter().Text(t => t.Span(h).FormSmall().Bold());
            });

            // Veri satırı
            foreach (var v in values)
                table.Cell().CellStyle().AlignCenter().Text(t => t.Span(v).FormSmall());
        });
    }

    // Dört imza bloğu: Usta Öğretici, İşletme Yetkilisi, Koor. Md. Yrd., Müdür
    private void ComposeSignatures(IContainer container)
    {
        container.Row(row =>
        {
            SignatureColumn(row, "Usta Öğretici / Eğitici Personel", _data.MasterInstructorName);
            SignatureColumn(row, "İşletme Yetkilisi", _data.BusinessOfficialName);
            SignatureColumn(row, "Okul/Kurum Koor. Müdür Yardımcısı", _data.VicePrincipalName);
            SignatureColumn(row, "Okul/Kurum Müdürü", _data.PrincipalName);
        });
    }

    private static void SignatureColumn(RowDescriptor row, string role, string? name)
    {
        row.RelativeItem().Column(col =>
        {
            col.Item().Text(t => { t.AlignCenter(); t.Span(role).FormSmall().Bold(); });
            col.Item().PaddingTop(22).Text(t => { t.AlignCenter(); t.Span(name ?? "").FormBody().Bold(); });
            col.Item().Text(t => { t.AlignCenter(); t.Span("İmza").FormSmall(); });
        });
    }

    private void ComposeNotes(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Text(t => t.Span("AÇIKLAMALAR:").FormSmall().Bold());
            col.Item().PaddingTop(2).Text(t => t.Span(
                "1) Bu çizelge, Mesleki ve Teknik Eğitim Yönetmeliğinin 82'nci maddesine göre, işletme yetkilisi " +
                "tarafından doldurulacak ve dönem sona ermeden beş (5) gün önceden kapalı zarf içinde okul/kurum " +
                "müdürlüğüne teslim edilecektir.").FormSmall());
            col.Item().Text(t => t.Span(
                "2) (*) işaretli bölümler okul/kurum müdürlüğümüzce doldurulacak ve puan ortalaması alınarak dönem " +
                "notu belirlenecektir.").FormSmall());
        });
    }

    /// <summary>
    /// Dönem adını MEB terminolojisiyle Türkçeleştirir. Backend AcademicSemester SmartEnum'ında
    /// Name = İngilizce (Fall/Spring); resmî basılı belgede İngilizce görünmemeli — çağıran ne
    /// gönderirse göndersin burada normalize edilir (#60). Tanınmayan değer olduğu gibi basılır.
    /// </summary>
    private static string SemesterLabel(string semester) => semester?.Trim() switch
    {
        "Fall" => "1. Dönem",
        "Spring" => "2. Dönem",
        _ => semester ?? string.Empty
    };

    private static string JoinGrades(List<int> grades) =>
        grades.Count == 0 ? "" : string.Join("   ", grades);
}
