using MESNET.Reporting.Application.Templates.Components;
using MESNET.Reporting.Core.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MESNET.Reporting.Application.Templates;

/// <summary>
/// Form 6: Beceri Eğitimi İşletme Değerlendirme Formu
/// İşletmenin eğitim ortamı olarak değerlendirilmesi (checklist)
/// </summary>
public class BusinessEvaluationFormDocument : IDocument
{
    private readonly BusinessEvaluationFormData _data;

    public BusinessEvaluationFormDocument(BusinessEvaluationFormData data) => _data = data;

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
                col.Item().MebHeaderWithQr("BECERİ EĞİTİMİ İŞLETME DEĞERLENDİRME FORMU", _data.InstitutionName, _data.DocumentId);

                // İşletme bilgileri
                col.Item().PaddingTop(8).Column(info =>
                {
                    info.Item().LabelValue("İşletme Adı", _data.BusinessName);
                    info.Item().LabelValue("Adresi", _data.BusinessAddress);
                    info.Item().LabelValue("Telefon", _data.BusinessPhone);
                    info.Item().LabelValue("Faaliyet Alanı", _data.ActivityField);
                    info.Item().LabelValue("Değerlendirme Tarihi", _data.EvaluationDate.ToString("dd.MM.yyyy"));
                });

                // Değerlendirme tablosu — Kategoriler ve sorular
                col.Item().PaddingTop(8).Element(ComposeEvaluationTable);

                // Sonuç
                col.Item().PaddingTop(8).Border(0.5f).BorderColor(MebFormStyles.BorderColor).Padding(8).Column(resCol =>
                {
                    resCol.Item().Text(text => text.Span($"Değerlendirme Sonucu: {_data.Result}").FormBody().Bold());
                    if (!string.IsNullOrEmpty(_data.Notes))
                    {
                        resCol.Item().PaddingTop(3).Text(text =>
                        {
                            text.Span("Açıklama: ").FormBody().Bold();
                            text.Span(_data.Notes).FormBody();
                        });
                    }
                });

                // İmza
                col.Item().PaddingTop(15).Element(c =>
                    c.SignatureBlock(
                        "Değerlendirici\n" + _data.EvaluatorName,
                        "Koordinatör Öğretmen",
                        "Okul Müdürü"));
            });
        });
    }

    private void ComposeEvaluationTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(30);   // Sıra
                columns.RelativeColumn(5);    // Soru
                columns.ConstantColumn(50);   // Uygun
                columns.ConstantColumn(50);   // Uygun Değil
                columns.RelativeColumn(2);    // Açıklama
            });

            table.Header(header =>
            {
                header.Cell().HeaderCellStyle().AlignCenter().Text(text => text.Span("S.N.").FormSmall().Bold());
                header.Cell().HeaderCellStyle().Text(text => text.Span("Değerlendirme Kriteri").FormSmall().Bold());
                header.Cell().HeaderCellStyle().AlignCenter().Text(text => text.Span("Uygun").FormSmall().Bold());
                header.Cell().HeaderCellStyle().AlignCenter().Text(text => text.Span("Uygun\nDeğil").FormSmall().Bold());
                header.Cell().HeaderCellStyle().Text(text => text.Span("Açıklama").FormSmall().Bold());
            });

            var counter = 1;
            foreach (var category in _data.Categories)
            {
                // Kategori başlığı
                table.Cell().ColumnSpan(5).Border(0.5f).BorderColor(MebFormStyles.BorderColor)
                    .Background(Colors.Grey.Lighten4).Padding(MebFormStyles.CellPadding)
                    .Text(text => text.Span(category.Name).FormSmall().Bold());

                foreach (var item in category.Items)
                {
                    var num = counter++;
                    table.Cell().CellStyle().AlignCenter().Text(text => text.Span(num.ToString()).FormSmall());
                    table.Cell().CellStyle().Text(text => text.Span(item.Question).FormSmall());
                    table.Cell().CellStyle().AlignCenter().Text(text =>
                        text.Span(item.IsCompliant ? "X" : "").FormSmall());
                    table.Cell().CellStyle().AlignCenter().Text(text =>
                        text.Span(!item.IsCompliant ? "X" : "").FormSmall());
                    table.Cell().CellStyle().Text(text => text.Span(item.Note ?? "").FormSmall());
                }
            }
        });
    }
}
