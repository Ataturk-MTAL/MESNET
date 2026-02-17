using MESNET.Reporting.Application.Templates.Components;
using MESNET.Reporting.Core.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MESNET.Reporting.Application.Templates;

/// <summary>
/// Form 2: Aylık Eğitim Faaliyeti Formu
/// Günlük faaliyet kaydı tablosu (31 gün) + usta öğretici ve koordinatör değerlendirmesi
/// </summary>
public class MonthlyActivityFormDocument : IDocument
{
    private readonly MonthlyActivityFormData _data;
    private static readonly string[] TurkishMonths =
        ["Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
         "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"];

    public MonthlyActivityFormDocument(MonthlyActivityFormData data) => _data = data;

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
                col.Item().MebHeaderWithQr("AYLIK EĞİTİM FAALİYETİ FORMU", _data.InstitutionName, _data.DocumentId);

                // Bilgi satırları
                col.Item().PaddingTop(8).Row(row =>
                {
                    row.RelativeItem().Column(info =>
                    {
                        info.Item().LabelValue("Öğrenci Adı Soyadı", _data.StudentFullName);
                        info.Item().LabelValue("Okul No", _data.StudentNumber);
                        info.Item().LabelValue("Alan/Dal", _data.BranchName);
                    });
                    row.RelativeItem().Column(info =>
                    {
                        info.Item().LabelValue("İşletme Adı", _data.BusinessName);
                        info.Item().LabelValue("Sınıf", _data.ClassYear.ToString());
                        info.Item().LabelValue("Ay / Yıl", $"{TurkishMonths[_data.Month - 1]} {_data.Year}");
                    });
                });

                // Faaliyet tablosu
                col.Item().PaddingTop(8).Element(ComposeActivityTable);

                // Değerlendirme alanları
                col.Item().PaddingTop(8).Column(evalCol =>
                {
                    evalCol.Item().Border(0.5f).BorderColor(MebFormStyles.BorderColor).Padding(5).Column(inner =>
                    {
                        inner.Item().Text(text => text.Span("Usta Öğretici Değerlendirmesi:").FormBody().Bold());
                        inner.Item().PaddingTop(3).MinHeight(40)
                            .Text(text => text.Span(_data.InstructorComment ?? "").FormBody());
                    });
                    evalCol.Item().PaddingTop(5).Border(0.5f).BorderColor(MebFormStyles.BorderColor).Padding(5).Column(inner =>
                    {
                        inner.Item().Text(text => text.Span("Koordinatör Öğretmen Değerlendirmesi:").FormBody().Bold());
                        inner.Item().PaddingTop(3).MinHeight(40)
                            .Text(text => text.Span(_data.TeacherComment ?? "").FormBody());
                    });
                });

                // İmza
                col.Item().PaddingTop(10).Element(c =>
                    c.SignatureBlock(
                        "Usta Öğretici\n" + _data.MasterInstructorName,
                        "Koordinatör Öğretmen\n" + _data.CoordinatorTeacherName));
            });
        });
    }

    private void ComposeActivityTable(IContainer container)
    {
        var daysInMonth = DateTime.DaysInMonth(_data.Year, _data.Month);
        var activityMap = _data.Activities.ToDictionary(a => a.DayNumber, a => a.Description);

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(35); // Gün
                columns.RelativeColumn();    // Yapılan Faaliyet
            });

            table.Header(header =>
            {
                header.Cell().HeaderCellStyle().AlignCenter().Text(text => text.Span("Gün").FormSmall().Bold());
                header.Cell().HeaderCellStyle().Text(text => text.Span("Yapılan İş / Faaliyet").FormSmall().Bold());
            });

            for (var day = 1; day <= daysInMonth; day++)
            {
                activityMap.TryGetValue(day, out var description);
                table.Cell().CellStyle().AlignCenter().Text(text => text.Span(day.ToString()).FormSmall());
                table.Cell().CellStyle().Text(text => text.Span(description ?? "").FormSmall());
            }
        });
    }
}
