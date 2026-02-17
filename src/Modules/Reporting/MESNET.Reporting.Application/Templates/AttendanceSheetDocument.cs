using MESNET.Reporting.Application.Templates.Components;
using MESNET.Reporting.Core.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MESNET.Reporting.Application.Templates;

/// <summary>
/// Form 4: İşletme Devamsızlık Çizelgesi
/// Aylık yoklama tablosu — her gün için P (geldi) veya devamsızlık işareti
/// </summary>
public class AttendanceSheetDocument : IDocument
{
    private readonly AttendanceSheetData _data;
    private static readonly string[] TurkishMonths =
        ["Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
         "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"];

    public AttendanceSheetDocument(AttendanceSheetData data) => _data = data;

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(MebFormStyles.PageMarginCm, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(MebFormStyles.BodyFontSize));

            page.Content().Column(col =>
            {
                // Başlık + QR
                col.Item().MebHeaderWithQr("İŞLETMELERDE BECERİ EĞİTİMİ DEVAMSIZLIK ÇİZELGESİ", _data.InstitutionName, _data.DocumentId);

                col.Item().PaddingTop(10).Row(row =>
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
                        info.Item().LabelValue("Öğretim Yılı", _data.AcademicYear);
                    });
                });

                // Ay ve tablo
                col.Item().PaddingTop(8).Text(text =>
                {
                    text.AlignCenter();
                    text.Span($"{TurkishMonths[_data.Month - 1]} {_data.Year}").FormSubTitle();
                });

                col.Item().PaddingTop(5).Element(ComposeAttendanceTable);

                // Alt açıklama
                col.Item().PaddingTop(5).Text(text =>
                {
                    text.Span("P: Geldi / D: Devamsız / T: Tatil / -: İş günü değil").FormSmall();
                });

                // Toplam
                var totalWorkDays = _data.WorkingDays.Count;
                var totalAbsent = _data.AbsentDays.Count;
                col.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Text(text =>
                        text.Span($"Toplam İş Günü: {totalWorkDays} | Devamsız Gün: {totalAbsent} | Geldi: {totalWorkDays - totalAbsent}").FormBody().Bold());
                });

                // İmza alanı
                col.Item().PaddingTop(15).Element(c =>
                    c.SignatureBlock("Usta Öğretici\n" + _data.MasterInstructorName,
                        "Koordinatör Öğretmen\n" + _data.CoordinatorTeacherName,
                        "Müdür Yardımcısı"));
            });
        });
    }

    private void ComposeAttendanceTable(IContainer container)
    {
        var daysInMonth = DateTime.DaysInMonth(_data.Year, _data.Month);

        container.Table(table =>
        {
            // Sütun tanımları: Gün numaraları (1-31)
            table.ColumnsDefinition(columns =>
            {
                for (var i = 0; i <= daysInMonth; i++)
                {
                    if (i == 0)
                        columns.ConstantColumn(40); // "Gün" başlığı
                    else
                        columns.RelativeColumn();
                }
            });

            // Header: Gün numaraları
            table.Header(header =>
            {
                header.Cell().HeaderCellStyle().Text(text => text.Span("Gün").FormSmall().Bold());
                for (var day = 1; day <= daysInMonth; day++)
                {
                    header.Cell().HeaderCellStyle().AlignCenter()
                        .Text(text => text.Span(day.ToString()).FormSmall().Bold());
                }
            });

            // Tek satır: yoklama durumu
            table.Cell().CellStyle().Text(text => text.Span("Durum").FormSmall().Bold());
            for (var day = 1; day <= daysInMonth; day++)
            {
                var isWorkDay = _data.WorkingDays.Contains(day);
                var isAbsent = _data.AbsentDays.Contains(day);

                var mark = !isWorkDay ? "-" : isAbsent ? "D" : "P";
                var cellContainer = table.Cell().CellStyle().AlignCenter();

                cellContainer.Text(text =>
                {
                    var span = text.Span(mark).FormSmall();
                    if (isAbsent) span.Bold().FontColor(Colors.Red.Medium);
                });
            }
        });
    }
}
