using MESNET.Reporting.Application.Templates.Components;
using MESNET.Reporting.Core.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MESNET.Reporting.Application.Templates;

/// <summary>
/// Form 3: Günlük Rehberlik Formu
/// Koordinatör öğretmenin işletme ziyaret raporu
/// </summary>
public class GuidanceVisitFormDocument : IDocument
{
    private readonly GuidanceVisitFormData _data;

    public GuidanceVisitFormDocument(GuidanceVisitFormData data) => _data = data;

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(MebFormStyles.PageMarginCm, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(MebFormStyles.BodyFontSize));

            page.Content().Column(col =>
            {
                col.Item().MebHeaderWithQr("İŞLETMELERDE BECERİ EĞİTİMİ GÜNLÜK REHBERLİK FORMU", _data.InstitutionName, _data.DocumentId);

                // Ziyaret bilgileri
                col.Item().PaddingTop(8).Row(row =>
                {
                    row.RelativeItem().Column(info =>
                    {
                        info.Item().LabelValue("Koordinatör Öğretmen", _data.TeacherName);
                        info.Item().LabelValue("Ziyaret Tarihi", _data.VisitDate.ToString("dd.MM.yyyy"));
                    });
                    row.RelativeItem().Column(info =>
                    {
                        info.Item().LabelValue("İşletme Adı", _data.BusinessName);
                        info.Item().LabelValue("İşletme Adresi", _data.BusinessAddress);
                    });
                });

                // Öğrenci değerlendirme tablosu
                col.Item().PaddingTop(10).Text(text =>
                    text.Span("Öğrenci Değerlendirmeleri").FormSubTitle());
                col.Item().PaddingTop(3).Element(ComposeStudentTable);

                // Usta öğretici ile görüşme
                col.Item().PaddingTop(8).Element(c =>
                    ComposeSection(c, "Usta Öğretici ile Görüşme Notları", _data.InstructorMeetingNotes));

                // Tespit edilen sorunlar
                col.Item().PaddingTop(5).Element(c =>
                    ComposeSection(c, "Tespit Edilen Sorunlar", _data.IssuesIdentified));

                // Yapılan işlemler
                col.Item().PaddingTop(5).Element(c =>
                    ComposeSection(c, "Yapılan İşlemler / Alınan Önlemler", _data.ActionsTaken));

                // Genel değerlendirme
                col.Item().PaddingTop(5).Element(c =>
                    ComposeSection(c, "Genel Değerlendirme", _data.GeneralAssessment));

                // İmza
                col.Item().PaddingTop(15).Element(c =>
                    c.SignatureBlock(
                        "Koordinatör Öğretmen\n" + _data.TeacherName,
                        "Usta Öğretici",
                        "İşletme Yetkilisi"));
            });
        });
    }

    private void ComposeStudentTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(30);   // Sıra
                columns.RelativeColumn(2);    // Öğrenci Adı
                columns.RelativeColumn(4);    // Performans Notu
            });

            table.Header(header =>
            {
                header.Cell().HeaderCellStyle().AlignCenter().Text(text => text.Span("S.N.").FormSmall().Bold());
                header.Cell().HeaderCellStyle().Text(text => text.Span("Öğrenci Adı Soyadı").FormSmall().Bold());
                header.Cell().HeaderCellStyle().Text(text => text.Span("Performans Değerlendirmesi").FormSmall().Bold());
            });

            for (var i = 0; i < _data.StudentNotes.Count; i++)
            {
                var note = _data.StudentNotes[i];
                table.Cell().CellStyle().AlignCenter().Text(text => text.Span((i + 1).ToString()).FormSmall());
                table.Cell().CellStyle().Text(text => text.Span(note.StudentName).FormSmall());
                table.Cell().CellStyle().Text(text => text.Span(note.PerformanceNote).FormSmall());
            }
        });
    }

    private static void ComposeSection(IContainer container, string title, string? content)
    {
        container.Border(0.5f).BorderColor(MebFormStyles.BorderColor).Padding(5).Column(col =>
        {
            col.Item().Text(text => text.Span(title).FormBody().Bold());
            col.Item().PaddingTop(3).MinHeight(35)
                .Text(text => text.Span(content ?? "").FormBody());
        });
    }
}
