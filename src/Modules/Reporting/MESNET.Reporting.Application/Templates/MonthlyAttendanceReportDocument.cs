using MESNET.Reporting.Application.Templates.Components;
using MESNET.Reporting.Core.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MESNET.Reporting.Application.Templates;

/// <summary>
/// Form 7: Aylık Devam - Devamsızlık Bildirim Çizelgesi
/// MEB referans formu — öğretmen bazlı, her işletme = 1 sayfa (Portrait A4).
/// Tüm işletmeler tek PDF'te toplanır.
/// </summary>
public class MonthlyAttendanceReportDocument : IDocument
{
    private readonly IReadOnlyList<MonthlyAttendanceReportData> _pages;

    private static readonly string[] TurkishMonths =
        ["Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
         "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"];

    // Sütun genişlikleri — Landscape A4 ≈ 842pt, margin 1.0cm×2 = 56.7pt → 785.3pt kullanılabilir
    // 115+26+54+14 + 15×16 + 14 + 16×16 + 33×2 = 209+240+14+256+66 = 785pt ✅ (tam dolu)
    private const float ColName   = 115f;
    private const float ColNo     = 26f;
    private const float ColBranch = 54f;
    private const float ColLabel  = 14f; // S / Ö etiketi
    private const float ColDay    = 16f;
    private const float ColD      = 14f; // D ayraç sütunu (1-15 ile 16-31 arasında)
    private const float ColTotal  = 33f; // Özürlü / Özürsüz toplamı

    private const float FontTiny   = 5.5f;
    private const float FontSmall  = 6.5f;
    private const float FontBody   = 8f;
    private const float FontHeader = 10f;

    public MonthlyAttendanceReportDocument(IReadOnlyList<MonthlyAttendanceReportData> pages) => _pages = pages;

    /// <summary>Geriye dönük uyumluluk — tekil sayfa</summary>
    public MonthlyAttendanceReportDocument(MonthlyAttendanceReportData single) : this([single]) { }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        foreach (var data in _pages)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.MarginHorizontal(1.0f, Unit.Centimetre);
                page.MarginVertical(0.6f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(FontBody));

                page.Content().Column(col =>
                {
                    ComposeHeader(col, data);
                    col.Item().PaddingTop(3).Element(c => ComposeInfoRow(c, data));
                    col.Item().PaddingTop(2).Element(c => ComposeStudentTable(c, data));
                    col.Item().PaddingTop(4).Element(c => ComposeFooter(c, data));
                    col.Item().PaddingTop(3).Element(c => ComposeNote(c));
                });
            });
        }
    }

    // ─── Başlık ───────────────────────────────────────────────────────────────────

    private static void ComposeHeader(ColumnDescriptor col, MonthlyAttendanceReportData data)
    {
        col.Item().Row(row =>
        {
            row.RelativeItem().Column(inner =>
            {
                inner.Item().AlignCenter()
                    .Text(t => t.Span("T.C.").FontSize(FontBody));
                inner.Item().AlignCenter()
                    .Text(t => t.Span("MİLLÎ EĞİTİM BAKANLIĞI").FontSize(FontHeader).Bold());
                inner.Item().AlignCenter()
                    .Text(t => t.Span(data.InstitutionName).FontSize(FontBody).Bold());
                inner.Item().AlignCenter()
                    .Text(t => t.Span($"{data.AcademicYear} EĞİTİM ÖĞRETİM YILI").FontSize(FontBody));
                inner.Item().PaddingTop(4).AlignCenter()
                    .Text(t => t.Span("ÖĞRENCİLERİNİN İŞLETMELERDE MESLEKİ EĞİTİMİ AYLIK DEVAM - DEVAMSIZLIK BİLDİRİM ÇİZELGESİ")
                        .FontSize(FontBody).Bold());
            });

            row.ConstantItem(60).Column(qrCol =>
            {
                qrCol.Item().Element(c => c.QrCode(data.DocumentId, 2f));
                qrCol.Item().AlignCenter()
                    .Text(t => t.Span(data.DocumentId.ToString()[..8]).FontSize(4f));
            });
        });
    }

    // ─── Üst bilgi tablosu ────────────────────────────────────────────────────────

    private static void ComposeInfoRow(IContainer container, MonthlyAttendanceReportData data)
    {
        var monthName = TurkishMonths[data.Month - 1];
        var dateStr = data.DocumentDate == DateOnly.MinValue
            ? $".../.../{ data.Year}"
            : data.DocumentDate.ToString("dd/MM/yyyy");

        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(2.5f); // Okul/Kurumun Adı
                cols.RelativeColumn(2.5f); // İşletme Adı
                cols.RelativeColumn(1.5f); // Tel
                cols.RelativeColumn(1.5f); // E-posta
                cols.RelativeColumn(1.2f); // Ay
                cols.RelativeColumn(1.8f); // Tarih
            });

            InfoHeaderCell(table, "OKUL / KURUMUN ADI");
            InfoHeaderCell(table, "İŞLETMENİN ADI VE ADRESİ");
            InfoHeaderCell(table, "TEL. ve FAKSI");
            InfoHeaderCell(table, "e-POSTASI");
            InfoHeaderCell(table, "Ait Olduğu Ay");
            InfoHeaderCell(table, "Belgenin Düzenlendiği Tarih:");

            InfoDataCell(table, data.InstitutionName);
            InfoDataCell(table, data.BusinessName);
            InfoDataCell(table, data.BusinessPhone ?? "");
            InfoDataCell(table, data.BusinessEmail ?? "");
            InfoDataCell(table, $"{monthName} {data.Year}");
            InfoDataCell(table, dateStr);
        });
    }

    private static void InfoHeaderCell(TableDescriptor table, string text)
        => table.Cell().Border(0.5f).Padding(2)
            .Text(t => t.Span(text).FontSize(FontSmall).Bold());

    private static void InfoDataCell(TableDescriptor table, string text)
        => table.Cell().Border(0.5f).MinHeight(14).Padding(2)
            .Text(t => t.Span(text).FontSize(FontSmall));

    // ─── Öğrenci tablosu ─────────────────────────────────────────────────────────

    private static void ComposeStudentTable(IContainer container, MonthlyAttendanceReportData data)
    {
        var daysInMonth = DateTime.DaysInMonth(data.Year, data.Month);
        var nonWorkDays = new HashSet<int>(data.WeekendDays.Concat(data.HolidayDays));

        container.Table(table =>
        {
            // Sütun tanımları: Ad|No|Alan|S/Ö|1-15|D|16-31|Özürlü|Özürsüz
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(ColName);
                cols.ConstantColumn(ColNo);
                cols.ConstantColumn(ColBranch);
                cols.ConstantColumn(ColLabel);

                for (var d = 1; d <= 15; d++)
                    cols.ConstantColumn(ColDay);

                cols.ConstantColumn(ColD); // D ayraç

                for (var d = 16; d <= 31; d++)
                    cols.ConstantColumn(ColDay);

                cols.ConstantColumn(ColTotal); // Özürlü
                cols.ConstantColumn(ColTotal); // Özürsüz
            });

            // ── Başlık satırı 1 ──
            table.Cell().ColumnSpan(3).Border(0.5f).Padding(1).AlignCenter()
                .Text(t => t.Span("ÖĞRENCİNİN").FontSize(FontSmall).Bold());

            // S/Ö + 15 gün + D + 16 gün = 33 sütun
            table.Cell().ColumnSpan(33).Border(0.5f).Padding(1).AlignCenter()
                .Text(t => t.Span("GÜNLER").FontSize(FontSmall).Bold());

            table.Cell().ColumnSpan(2).Border(0.5f).Padding(1).AlignCenter()
                .Text(t => t.Span("TOPLAM\nDEVAMSIZLIĞI").FontSize(4f).Bold());

            // ── Başlık satırı 2 ──
            table.Cell().Border(0.5f).Padding(1)
                .Text(t => t.Span("ADI SOYADI").FontSize(FontTiny).Bold());
            table.Cell().Border(0.5f).Padding(1).AlignCenter()
                .Text(t => t.Span("NUMARASI").FontSize(FontTiny).Bold());
            table.Cell().Border(0.5f).Padding(1).AlignCenter()
                .Text(t => t.Span("ALAN / DAL").FontSize(FontTiny).Bold());
            table.Cell().Border(0.5f).Padding(0).AlignCenter().AlignMiddle()
                .Text(t => t.Span("S\nÖ").FontSize(3.5f));

            for (var d = 1; d <= 15; d++)
                table.Cell().Border(0.5f).Padding(0).AlignCenter().AlignMiddle()
                    .Text(t => t.Span(d.ToString()).FontSize(FontTiny).Bold());

            table.Cell().Border(0.5f).Padding(0).AlignCenter().AlignMiddle()
                .Background(Colors.Grey.Lighten3)
                .Text(t => t.Span("D").FontSize(FontTiny).Bold());

            for (var d = 16; d <= 31; d++)
                table.Cell().Border(0.5f).Padding(0).AlignCenter().AlignMiddle()
                    .Text(t => t.Span(d.ToString()).FontSize(FontTiny).Bold());

            table.Cell().Border(0.5f).Padding(1).AlignCenter()
                .Text(t => t.Span("ÖZÜRLÜ").FontSize(FontTiny).Bold());
            table.Cell().Border(0.5f).Padding(1).AlignCenter()
                .Text(t => t.Span("ÖZÜRSÜZ").FontSize(FontTiny).Bold());

            // ── Öğrenci satırları ──
            if (data.Students.Count == 0)
            {
                for (var i = 0; i < 5; i++)
                    RenderEmptyStudentRows(table, daysInMonth);
            }
            else
            {
                foreach (var student in data.Students)
                    RenderStudentRows(table, student, daysInMonth, nonWorkDays);

                var emptyRows = Math.Max(0, 5 - data.Students.Count);
                for (var i = 0; i < emptyRows; i++)
                    RenderEmptyStudentRows(table, daysInMonth);
            }
        });
    }

    private static void RenderStudentRows(
        TableDescriptor table, StudentAttendanceRow student,
        int daysInMonth, HashSet<int> nonWorkDays)
    {
        // Ad, No, Alan — 2 satırı kapsıyor
        table.Cell().RowSpan(2).Border(0.5f).Padding(1).AlignMiddle()
            .Text(t => t.Span(student.StudentName).FontSize(FontTiny));
        table.Cell().RowSpan(2).Border(0.5f).Padding(1).AlignCenter().AlignMiddle()
            .Text(t => t.Span(student.StudentNumber).FontSize(FontTiny));
        table.Cell().RowSpan(2).Border(0.5f).Padding(1).AlignMiddle()
            .Text(t => t.Span(student.BranchName).FontSize(FontTiny));

        // Sabah (S) satırı
        table.Cell().Border(0.5f).Padding(0).AlignCenter().AlignMiddle()
            .Text(t => t.Span("S").FontSize(3.5f).Bold());
        RenderDayCells(table, student.MorningMarks, daysInMonth, nonWorkDays, 1, 15);

        // D ayraç — 2 satırı kapsıyor
        table.Cell().RowSpan(2).Border(0.5f).Background(Colors.Grey.Lighten3).Text("");

        RenderDayCells(table, student.MorningMarks, daysInMonth, nonWorkDays, 16, 31);

        // Totaller — 2 satırı kapsıyor
        table.Cell().RowSpan(2).Border(0.5f).Padding(1).AlignCenter().AlignMiddle()
            .Text(t => t.Span(student.ExcusedAbsences > 0 ? student.ExcusedAbsences.ToString() : "")
                .FontSize(FontTiny));
        table.Cell().RowSpan(2).Border(0.5f).Padding(1).AlignCenter().AlignMiddle()
            .Text(t => t.Span(student.UnexcusedAbsences > 0 ? student.UnexcusedAbsences.ToString() : "")
                .FontSize(FontTiny));

        // Öğleden sonra (Ö) satırı
        table.Cell().Border(0.5f).Padding(0).AlignCenter().AlignMiddle()
            .Text(t => t.Span("Ö").FontSize(3.5f).Bold());
        RenderDayCells(table, student.AfternoonMarks, daysInMonth, nonWorkDays, 1, 15);
        // D ayraç RowSpan(2) tarafından kapatıldı
        RenderDayCells(table, student.AfternoonMarks, daysInMonth, nonWorkDays, 16, 31);
        // Totaller RowSpan(2) tarafından kapatıldı
    }

    private static void RenderEmptyStudentRows(TableDescriptor table, int daysInMonth)
    {
        table.Cell().RowSpan(2).Border(0.5f).MinHeight(10).Padding(1).Text("");
        table.Cell().RowSpan(2).Border(0.5f).Padding(1).Text("");
        table.Cell().RowSpan(2).Border(0.5f).Padding(1).Text("");

        // Sabah
        table.Cell().Border(0.5f).Padding(0).AlignCenter()
            .Text(t => t.Span("S").FontSize(3.5f));
        for (var d = 1; d <= 15; d++)
            table.Cell().Border(0.5f).MinHeight(8).Text("");
        table.Cell().RowSpan(2).Border(0.5f).Background(Colors.Grey.Lighten3).Text(""); // D ayraç
        for (var d = 16; d <= 31; d++)
            table.Cell().Border(0.5f).MinHeight(8).Text("");
        table.Cell().RowSpan(2).Border(0.5f).Text(""); // Özürlü
        table.Cell().RowSpan(2).Border(0.5f).Text(""); // Özürsüz

        // Öğleden sonra
        table.Cell().Border(0.5f).Padding(0).AlignCenter()
            .Text(t => t.Span("Ö").FontSize(3.5f));
        for (var d = 1; d <= 15; d++)
            table.Cell().Border(0.5f).MinHeight(8).Text("");
        // D ayraç RowSpan(2) tarafından kapatıldı
        for (var d = 16; d <= 31; d++)
            table.Cell().Border(0.5f).MinHeight(8).Text("");
        // Totaller RowSpan(2) tarafından kapatıldı
    }

    private static void RenderDayCells(
        TableDescriptor table, Dictionary<int, string?> marks,
        int daysInMonth, HashSet<int> nonWorkDays,
        int fromDay, int toDay)
    {
        for (var d = fromDay; d <= toDay; d++)
        {
            var cell = table.Cell().Border(0.5f).Padding(0).AlignCenter().AlignMiddle();
            if (d > daysInMonth)
            {
                cell.Background(Colors.Grey.Lighten3).Text("");
                continue;
            }

            var isNonWork = nonWorkDays.Contains(d);
            var mark = marks.GetValueOrDefault(d);

            if (mark is not null)
                cell.Text(t => t.Span(mark).FontSize(4f).Bold());
            else if (isNonWork)
                cell.Background(Colors.Grey.Lighten3)
                    .Text(t => t.Span("X").FontSize(3.5f));
            else
                cell.Text("");
        }
    }

    // ─── Footer — 4 sütun ────────────────────────────────────────────────────────

    private static void ComposeFooter(IContainer container, MonthlyAttendanceReportData data)
    {
        container.Row(row =>
        {
            // 1 — İşletme Yetkilisi
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(t => t.Span("İŞLETME YETKİLİSİ").FontSize(FontSmall).Bold());
                col.Item().PaddingTop(10)
                    .Text(t => t.Span("........../.........../..........").FontSize(FontSmall));
                col.Item().PaddingTop(3)
                    .Text(t => t.Span(data.BusinessContactName).FontSize(FontSmall));
                col.Item().Text(t => t.Span("Adı Soyadı / Kaşe - İmza").FontSize(4f));
            });

            // 2 — Koordinatör Öğretmen
            row.RelativeItem().Column(col =>
            {
                col.Item().AlignCenter()
                    .Text(t => t.Span("KOORDİNATÖR ÖĞRETMEN").FontSize(FontSmall).Bold());
                col.Item().PaddingTop(10).AlignCenter()
                    .Text(t => t.Span("........../.........../..........").FontSize(FontSmall));
                col.Item().PaddingTop(3).AlignCenter()
                    .Text(t => t.Span(data.CoordinatorTeacherName).FontSize(FontSmall));
                col.Item().AlignCenter()
                    .Text(t => t.Span("Adı Soyadı / İmza").FontSize(4f));
            });

            // 3 — İncelendi / Koordinatör Müdür Yardımcısı
            row.RelativeItem().Column(col =>
            {
                col.Item().AlignCenter()
                    .Text(t => t.Span("İNCELENDİ").FontSize(FontSmall).Bold());
                col.Item().PaddingTop(10).AlignCenter()
                    .Text(t => t.Span("........../.........../..........").FontSize(FontSmall));
                col.Item().PaddingTop(3).AlignCenter()
                    .Text(t => t.Span(data.VicePrincipalName).FontSize(FontSmall));
                col.Item().AlignCenter()
                    .Text(t => t.Span("Koordinatör Müdür Yardımcısı").FontSize(4f));
            });

            // 4 — Semboller
            row.RelativeItem(1.2f).Column(col =>
            {
                col.Item().Border(0.5f).Padding(3).Column(leg =>
                {
                    leg.Item().Text(t => t.Span("Devamın Göstereceği Semboller").FontSize(4f).Bold());
                    leg.Item().PaddingTop(1).Row(r =>
                    {
                        r.RelativeItem().Text(t => t.Span("X: İşletmede").FontSize(4f));
                        r.RelativeItem().Text(t => t.Span("O: Okulda").FontSize(4f));
                    });
                    leg.Item().PaddingTop(2).Text(t => t.Span("Devamsızlığın Göstereceği Semboller").FontSize(4f).Bold());
                    leg.Item().PaddingTop(1).Row(r =>
                    {
                        r.RelativeItem().Text(t => t.Span("İ: İzinli").FontSize(4f));
                        r.RelativeItem().Text(t => t.Span("D: Özürsüz Devamsız").FontSize(4f));
                    });
                    leg.Item().Row(r =>
                    {
                        r.RelativeItem().Text(t => t.Span("H: Hasta Sevki").FontSize(4f));
                        r.RelativeItem().Text(t => t.Span("S: Sabah").FontSize(4f));
                    });
                    leg.Item().Row(r =>
                    {
                        r.RelativeItem().Text(t => t.Span("R: Raporlu").FontSize(4f));
                        r.RelativeItem().Text(t => t.Span("Ö: Öğle").FontSize(4f));
                    });
                    leg.Item().Row(r =>
                    {
                        r.RelativeItem().Text(t => t.Span("T: Resmi Tatil").FontSize(4f));
                    });
                });
            });
        });
    }

    private static void ComposeNote(IContainer container)
    {
        container.Border(0.5f).Padding(3).Column(col =>
        {
            col.Item().Text(t =>
                t.Span("Bu çizelge, işletme tarafından tutulacak, öğrencinin işletmede bulunması gereken günlere ait devamsızlık durumları ilgili sütunda, yanda gösterilen uygun sembolle belirlenecektir.")
                    .FontSize(FontTiny));
            col.Item().PaddingTop(1).Text(t =>
                t.Span("(İ), (H), (R) sembolleri ile gösterilen devamsızlıklar toplamı özürlü devamsızlık sütununa yazılacaktır")
                    .FontSize(FontTiny));
        });
    }
}
