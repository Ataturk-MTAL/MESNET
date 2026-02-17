using MESNET.Reporting.Application.Templates.Components;
using MESNET.Reporting.Core.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MESNET.Reporting.Application.Templates;

/// <summary>
/// Form 5: Dönem Sonu Beceri Sınavı Not Fişi
/// Beceri sınavı kriter bazlı puanlama tablosu
/// </summary>
public class SkillExamFormDocument : IDocument
{
    private readonly SkillExamFormData _data;

    public SkillExamFormDocument(SkillExamFormData data) => _data = data;

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
                col.Item().MebHeaderWithQr("DÖNEM SONU BECERİ SINAVI NOT FİŞİ", _data.InstitutionName, _data.DocumentId);

                // Öğrenci bilgileri
                col.Item().PaddingTop(8).Row(row =>
                {
                    row.RelativeItem().Column(info =>
                    {
                        info.Item().LabelValue("Öğrenci Adı Soyadı", _data.StudentFullName);
                        info.Item().LabelValue("Okul No", _data.StudentNumber);
                        info.Item().LabelValue("Alan/Dal", _data.BranchName);
                        info.Item().LabelValue("Sınıf", _data.ClassYear.ToString());
                    });
                    row.RelativeItem().Column(info =>
                    {
                        info.Item().LabelValue("İşletme Adı", _data.BusinessName);
                        info.Item().LabelValue("Öğretim Yılı", _data.AcademicYear);
                        info.Item().LabelValue("Dönem", _data.Semester);
                        info.Item().LabelValue("Sınav Tarihi", _data.ExamDate.ToString("dd.MM.yyyy"));
                    });
                });

                // Kriter tablosu
                col.Item().PaddingTop(10).Element(ComposeCriteriaTable);

                // Toplam ve sonuç
                col.Item().PaddingTop(5).Border(0.5f).BorderColor(MebFormStyles.BorderColor).Padding(5).Row(row =>
                {
                    row.RelativeItem().Text(text =>
                        text.Span($"Toplam Puan: {_data.Score} / {_data.Criteria.Sum(c => c.MaxScore)}").FormBody().Bold());
                    row.RelativeItem().Text(text =>
                    {
                        text.AlignRight();
                        text.Span($"Sonuç: {_data.Result}").FormBody().Bold();
                    });
                });

                // Sınav komisyonu
                col.Item().PaddingTop(10).Element(ComposeCommittee);

                // İmza
                col.Item().PaddingTop(15).Element(c =>
                    c.SignatureBlock("Okul Müdürü", "Koordinatör Öğretmen", "Usta Öğretici"));
            });
        });
    }

    private void ComposeCriteriaTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(30);  // Sıra
                columns.RelativeColumn(4);   // Kriter Adı
                columns.ConstantColumn(70);  // Puan Değeri
                columns.ConstantColumn(70);  // Verilen Puan
            });

            table.Header(header =>
            {
                header.Cell().HeaderCellStyle().AlignCenter().Text(text => text.Span("S.N.").FormSmall().Bold());
                header.Cell().HeaderCellStyle().Text(text => text.Span("Değerlendirme Kriterleri").FormSmall().Bold());
                header.Cell().HeaderCellStyle().AlignCenter().Text(text => text.Span("Puan Değeri").FormSmall().Bold());
                header.Cell().HeaderCellStyle().AlignCenter().Text(text => text.Span("Verilen Puan").FormSmall().Bold());
            });

            for (var i = 0; i < _data.Criteria.Count; i++)
            {
                var criterion = _data.Criteria[i];
                table.Cell().CellStyle().AlignCenter().Text(text => text.Span((i + 1).ToString()).FormSmall());
                table.Cell().CellStyle().Text(text => text.Span(criterion.Name).FormSmall());
                table.Cell().CellStyle().AlignCenter().Text(text => text.Span(criterion.MaxScore.ToString()).FormSmall());
                table.Cell().CellStyle().AlignCenter().Text(text => text.Span(criterion.Score.ToString()).FormSmall());
            }
        });
    }

    private void ComposeCommittee(IContainer container)
    {
        if (_data.CommitteeMembers.Count == 0) return;

        container.Column(col =>
        {
            col.Item().Text(text => text.Span("Sınav Komisyonu:").FormBody().Bold());
            col.Item().PaddingTop(3).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);  // Sıra
                    columns.RelativeColumn();     // Ad Soyad
                    columns.RelativeColumn();     // Unvan
                    columns.ConstantColumn(80);   // İmza
                });

                table.Header(header =>
                {
                    header.Cell().HeaderCellStyle().AlignCenter().Text(text => text.Span("S.N.").FormSmall().Bold());
                    header.Cell().HeaderCellStyle().Text(text => text.Span("Adı Soyadı").FormSmall().Bold());
                    header.Cell().HeaderCellStyle().Text(text => text.Span("Unvanı").FormSmall().Bold());
                    header.Cell().HeaderCellStyle().AlignCenter().Text(text => text.Span("İmza").FormSmall().Bold());
                });

                for (var i = 0; i < _data.CommitteeMembers.Count; i++)
                {
                    var member = _data.CommitteeMembers[i];
                    table.Cell().CellStyle().AlignCenter().Text(text => text.Span((i + 1).ToString()).FormSmall());
                    table.Cell().CellStyle().Text(text => text.Span(member.FullName).FormSmall());
                    table.Cell().CellStyle().Text(text => text.Span(member.Title).FormSmall());
                    table.Cell().CellStyle().Text(""); // İmza alanı boş
                }
            });
        });
    }
}
