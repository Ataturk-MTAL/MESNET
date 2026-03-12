using MESNET.Reporting.Application.Templates.Components;
using MESNET.Reporting.Core.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MESNET.Reporting.Application.Templates;

/// <summary>
/// Form 7: Aylık Devam - Devamsızlık Bildirim Çizelgesi
/// MEB standart formu — her işletme için ayrı bir form üretilir.
/// Landscape A4, her öğrenci için İş Günleri (S) ve Ö.S Günleri (Ö) olmak üzere 2 satır.
/// </summary>
public class MonthlyAttendanceReportDocument : IDocument
{
    private readonly MonthlyAttendanceReportData _data;

    private static readonly string[] TurkishMonths =
        ["Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
         "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"];

    // Sabit sütun genişlikleri
    private const float ColClass = 55f;
    private const float ColNo = 30f;
    private const float ColName = 95f;
    private const float ColBranch = 90f;
    private const float ColRowLabel = 14f; // İş/Ö.S label
    private const float ColDay = 16f;
    private const float ColSep = 12f; // D ayraç sütunu
    private const float ColTotal = 32f;

    private const float FontTiny = 5.5f;
    private const float FontSmall = 6.5f;
    private const float FontBody = 7f;
    private const float FontHeader = 9f;

    public MonthlyAttendanceReportDocument(MonthlyAttendanceReportData data) => _data = data;

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.MarginHorizontal(1f, Unit.Centimetre);
            page.MarginVertical(0.8f, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(FontBody));

            page.Content().Column(col =>
            {
                ComposeHeader(col);
                col.Item().PaddingTop(3).Element(ComposeInfoSection);
                col.Item().PaddingTop(3).Element(ComposeTable);
                col.Item().PaddingTop(4).Element(ComposeFooter);
            });
        });
    }

    private void ComposeHeader(ColumnDescriptor col)
    {
        col.Item().Row(row =>
        {
            row.RelativeItem().Column(header =>
            {
                var monthName = TurkishMonths[_data.Month - 1];
                header.Item().Text(text =>
                {
                    text.AlignCenter();
                    text.Span($"{_data.InstitutionName} {_data.AcademicYear} EĞİTİM ÖĞRETİM YILI")
                        .FontSize(FontHeader).Bold();
                });
                header.Item().Text(text =>
                {
                    text.AlignCenter();
                    text.Span("İŞLETMELERDE MESLEK EĞİTİMİ").FontSize(FontBody).Bold();
                });
                header.Item().Text(text =>
                {
                    text.AlignCenter();
                    text.Span("AYLIK DEVAM - DEVAMSIZLIK BİLDİRİM ÇİZELGESİ").FontSize(FontHeader).Bold();
                });
            });

            row.ConstantItem(65).Column(qrCol =>
            {
                qrCol.Item().Element(c => c.QrCode(_data.DocumentId, 1.8f));
                qrCol.Item().Text(text =>
                {
                    text.AlignCenter();
                    text.Span(_data.DocumentId.ToString()[..8]).FontSize(FontTiny);
                });
            });
        });

        // FORM ID
        col.Item().Text(text =>
        {
            text.Span("FORM ID:  ").FontSize(FontSmall).Bold();
            text.Span(_data.DocumentId.ToString()[..8]).FontSize(FontSmall);
        });
    }

    private void ComposeInfoSection(IContainer container)
    {
        var monthName = TurkishMonths[_data.Month - 1];

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(4); // Okul/Kurumun
                columns.RelativeColumn(3); // İşletmenin Adı
                columns.RelativeColumn(2); // Telefonu ve Faksı
                columns.RelativeColumn(2); // e-posta adresi
                columns.RelativeColumn(1.5f); // Ait olduğu Ay
                columns.RelativeColumn(1.5f); // Belgenin düzenlendiği Tarih
            });

            // Başlık satırı
            table.Cell().ColumnSpan(2).Border(0.5f).Padding(2)
                .Text(text => text.Span("Okul/Kurumun").FontSize(FontSmall).Bold());
            table.Cell().ColumnSpan(4).Border(0.5f).Padding(2)
                .Text(text => text.Span("İşletmenin").FontSize(FontSmall).Bold());

            // Veri satırı
            table.Cell().RowSpan(2).Border(0.5f).Padding(3).AlignMiddle()
                .Text(text => text.Span(_data.InstitutionName).FontSize(FontSmall).Bold());

            // İşletme bilgileri başlık
            table.Cell().Border(0.5f).Padding(2)
                .Text(text => text.Span("Adı").FontSize(FontTiny).Bold());
            table.Cell().Border(0.5f).Padding(2)
                .Text(text => text.Span("Telefonu ve Faksı:").FontSize(FontTiny).Bold());
            table.Cell().Border(0.5f).Padding(2)
                .Text(text => text.Span("e-posta adresi:").FontSize(FontTiny).Bold());
            table.Cell().Border(0.5f).Padding(2)
                .Text(text => text.Span("Ait olduğu Ay").FontSize(FontTiny).Bold());
            table.Cell().Border(0.5f).Padding(2)
                .Text(text => text.Span("Belgenin düzenlendiği\nTarih").FontSize(FontTiny).Bold());

            // İşletme bilgileri veri
            table.Cell().Border(0.5f).Padding(2)
                .Text(text => text.Span(_data.BusinessName).FontSize(FontSmall));
            table.Cell().Border(0.5f).Padding(2)
                .Text(text => text.Span(_data.BusinessPhone ?? "").FontSize(FontSmall));
            table.Cell().Border(0.5f).Padding(2)
                .Text(text => text.Span(_data.BusinessEmail ?? "").FontSize(FontSmall));
            table.Cell().Border(0.5f).Padding(2)
                .Text(text => text.Span($"{monthName} {_data.Year}").FontSize(FontSmall));
            table.Cell().Border(0.5f).Padding(2)
                .Text(text => text.Span($"..../...../{_data.Year}").FontSize(FontSmall));
        });
    }

    private void ComposeTable(IContainer container)
    {
        var daysInMonth = DateTime.DaysInMonth(_data.Year, _data.Month);
        var nonWorkDays = new HashSet<int>(_data.WeekendDays.Concat(_data.HolidayDays));

        container.Table(table =>
        {
            // Sütun tanımları
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(ColClass);   // SINIFI
                columns.ConstantColumn(ColNo);       // NO
                columns.ConstantColumn(ColName);     // ADI VE SOYADI
                columns.ConstantColumn(ColBranch);   // ALAN/DAL
                columns.ConstantColumn(ColRowLabel); // İş Günleri / Ö.S Günleri

                // Gün 1-15
                for (var i = 1; i <= 15; i++)
                    columns.ConstantColumn(ColDay);

                columns.ConstantColumn(ColSep); // D ayraç sütunu

                // Gün 17-31
                for (var i = 17; i <= daysInMonth; i++)
                    columns.ConstantColumn(ColDay);

                // Boş günler (ayda 31 günden az ise)
                for (var i = daysInMonth + 1; i <= 31; i++)
                    columns.ConstantColumn(ColDay);

                columns.ConstantColumn(ColTotal); // Özürlü
                columns.ConstantColumn(ColTotal); // Özürsüz
            });

            var totalDayColumns = 15 + 1 + 15; // 1-15 + D + 17-31 = 31 sütun
            var totalColumns = 5 + totalDayColumns + 2; // info cols + day cols + totals

            // ─── Header Satır 1: "Öğrencinin" + Gün numaraları + "Toplam Devamsızlığı" ───
            table.Cell().ColumnSpan(4).Border(0.5f).Padding(1).AlignCenter()
                .Text(text => text.Span("Öğrencinin").FontSize(FontSmall).Bold());

            // İş Günleri / Ö.S Günleri label header
            table.Cell().RowSpan(2).Border(0.5f).Padding(0).AlignCenter().AlignMiddle()
                .Text(text =>
                {
                    text.AlignCenter();
                    text.Span("İş\nG.\nÖ.\nS.").FontSize(4f);
                });

            // Gün 1-15
            for (var day = 1; day <= 15; day++)
            {
                table.Cell().RowSpan(2).Border(0.5f).Padding(0).AlignCenter().AlignMiddle()
                    .Text(text => text.Span(day.ToString()).FontSize(FontTiny).Bold());
            }

            // D ayraç
            table.Cell().RowSpan(2).Border(0.5f).Padding(0).AlignCenter().AlignMiddle()
                .Background(Colors.Grey.Lighten3)
                .Text(text => text.Span("D").FontSize(FontTiny).Bold());

            // Gün 17-31 (veya ayın son gününe kadar)
            for (var day = 17; day <= 31; day++)
            {
                if (day <= daysInMonth)
                {
                    table.Cell().RowSpan(2).Border(0.5f).Padding(0).AlignCenter().AlignMiddle()
                        .Text(text => text.Span(day.ToString()).FontSize(FontTiny).Bold());
                }
                else
                {
                    table.Cell().RowSpan(2).Border(0.5f).Padding(0).AlignCenter().AlignMiddle()
                        .Background(Colors.Grey.Lighten4)
                        .Text(text => text.Span("").FontSize(FontTiny));
                }
            }

            // Toplam Devamsızlığı header
            table.Cell().ColumnSpan(2).Border(0.5f).Padding(1).AlignCenter()
                .Text(text => text.Span("Toplam\nDevamsızlığı").FontSize(FontTiny).Bold());

            // ─── Header Satır 2: SINIFI | NO | ADI VE SOYADI | ALAN/DAL + Özürlü | Özürsüz ───
            table.Cell().Border(0.5f).Padding(1)
                .Text(text => text.Span("SINIFI").FontSize(FontTiny).Bold());
            table.Cell().Border(0.5f).Padding(1).AlignCenter()
                .Text(text => text.Span("NO").FontSize(FontTiny).Bold());
            table.Cell().Border(0.5f).Padding(1)
                .Text(text => text.Span("ADI VE SOYADI").FontSize(FontTiny).Bold());
            table.Cell().Border(0.5f).Padding(1)
                .Text(text => text.Span("ALAN /DAL").FontSize(FontTiny).Bold());

            // İş Günleri / Ö.S Günleri ve gün header'ları zaten RowSpan(2) ile kaplanmış

            table.Cell().Border(0.5f).Padding(1).AlignCenter()
                .Text(text => text.Span("Özürlü").FontSize(FontTiny).Bold());
            table.Cell().Border(0.5f).Padding(1).AlignCenter()
                .Text(text => text.Span("Özürsüz").FontSize(FontTiny).Bold());

            // ─── Öğrenci Satırları ───
            foreach (var student in _data.Students)
            {
                ComposeStudentRows(table, student, daysInMonth, nonWorkDays);
            }
        });
    }

    private void ComposeStudentRows(
        TableDescriptor table, StudentAttendanceRow student, int daysInMonth, HashSet<int> nonWorkDays)
    {
        // SINIFI — 2 satırı kapsar
        table.Cell().RowSpan(2).Border(0.5f).Padding(1).AlignMiddle()
            .Text(text => text.Span(student.ClassName).FontSize(FontTiny));

        // NO — 2 satırı kapsar
        table.Cell().RowSpan(2).Border(0.5f).Padding(1).AlignCenter().AlignMiddle()
            .Text(text => text.Span(student.StudentNumber).FontSize(FontTiny));

        // ADI VE SOYADI — 2 satırı kapsar
        table.Cell().RowSpan(2).Border(0.5f).Padding(1).AlignMiddle()
            .Text(text => text.Span(student.StudentName).FontSize(FontTiny));

        // ALAN/DAL — 2 satırı kapsar
        table.Cell().RowSpan(2).Border(0.5f).Padding(1).AlignMiddle()
            .Text(text => text.Span(student.BranchName).FontSize(FontTiny));

        // ─── Satır 1: İş Günleri (Sabah) ───
        table.Cell().Border(0.5f).Padding(0).AlignCenter().AlignMiddle()
            .Text(text => text.Span("S").FontSize(4f).Bold());

        ComposeDayCells(table, student.MorningMarks, daysInMonth, nonWorkDays);

        // Özürlü (toplam)
        table.Cell().RowSpan(2).Border(0.5f).Padding(1).AlignCenter().AlignMiddle()
            .Text(text => text.Span(student.ExcusedAbsences > 0 ? student.ExcusedAbsences.ToString() : "").FontSize(FontTiny));

        // Özürsüz (toplam)
        table.Cell().RowSpan(2).Border(0.5f).Padding(1).AlignCenter().AlignMiddle()
            .Text(text => text.Span(student.UnexcusedAbsences > 0 ? student.UnexcusedAbsences.ToString() : "").FontSize(FontTiny));

        // ─── Satır 2: Ö.S Günleri (Öğleden sonra) ───
        table.Cell().Border(0.5f).Padding(0).AlignCenter().AlignMiddle()
            .Text(text => text.Span("Ö").FontSize(4f).Bold());

        ComposeDayCells(table, student.AfternoonMarks, daysInMonth, nonWorkDays);

        // Özürlü ve Özürsüz zaten RowSpan(2) ile kaplandı
    }

    private void ComposeDayCells(
        TableDescriptor table, Dictionary<int, string?> marks, int daysInMonth, HashSet<int> nonWorkDays)
    {
        // Gün 1-15
        for (var day = 1; day <= 15; day++)
        {
            ComposeSingleDayCell(table, marks, day, nonWorkDays);
        }

        // D ayraç
        table.Cell().Border(0.5f).Padding(0).AlignCenter().AlignMiddle()
            .Background(Colors.Grey.Lighten3)
            .Text(text => text.Span("").FontSize(FontTiny));

        // Gün 17-31
        for (var day = 17; day <= 31; day++)
        {
            if (day <= daysInMonth)
            {
                ComposeSingleDayCell(table, marks, day, nonWorkDays);
            }
            else
            {
                // Ayda olmayan gün — boş
                table.Cell().Border(0.5f).Padding(0)
                    .Background(Colors.Grey.Lighten4)
                    .Text(text => text.Span("").FontSize(FontTiny));
            }
        }
    }

    private static void ComposeSingleDayCell(
        TableDescriptor table, Dictionary<int, string?> marks, int day, HashSet<int> nonWorkDays)
    {
        var isNonWork = nonWorkDays.Contains(day);
        var mark = marks.GetValueOrDefault(day);

        var cellContainer = table.Cell().Border(0.5f).Padding(0).AlignCenter().AlignMiddle();

        if (isNonWork && mark is null)
        {
            // Hafta tatili / resmi tatil → X
            cellContainer.Text(text => text.Span("X").FontSize(4.5f));
        }
        else if (mark is not null)
        {
            // Devamsızlık sembolü
            cellContainer.Text(text => text.Span(mark).FontSize(4.5f).Bold());
        }
        else
        {
            // Boş — devam etti
            cellContainer.Text(text => text.Span("").FontSize(4.5f));
        }
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            // Sembol açıklamaları
            col.Item().Border(0.5f).Padding(3).Column(legend =>
            {
                legend.Item().Text(text =>
                    text.Span("Devamsızlığın gösterileceği semboller ( X: Hafta Tatili, Resmi Tatil )")
                        .FontSize(FontTiny).Bold());

                legend.Item().PaddingTop(2).Row(row =>
                {
                    row.RelativeItem().Text(text =>
                    {
                        text.Span("1) D  :Devamsız\n").FontSize(FontTiny);
                        text.Span("2) S  :Raporlu\n").FontSize(FontTiny);
                        text.Span("3) SV :Sevkli\n").FontSize(FontTiny);
                        text.Span("4) İ  :İzinli").FontSize(FontTiny);
                    });
                    row.RelativeItem().Text(text =>
                    {
                        text.Span("5) SH :Sağlık Kurulu\n").FontSize(FontTiny);
                        text.Span("6) TU :Tutuklanma\n").FontSize(FontTiny);
                        text.Span("7) C  :Uzaklaştırma\n").FontSize(FontTiny);
                        text.Span("8) T  :Tedbir Kararı").FontSize(FontTiny);
                    });
                    row.RelativeItem().Text(text =>
                    {
                        text.Span("9) GÖ :Görevli\n").FontSize(FontTiny);
                        text.Span("10) F :Faaliyet\n").FontSize(FontTiny);
                        text.Span("11) N :Nöbetçi\n").FontSize(FontTiny);
                        text.Span("12) G :Geç").FontSize(FontTiny);
                    });
                    row.RelativeItem().Text(text =>
                    {
                        text.Span("13) DA :Doğal Afet\n").FontSize(FontTiny);
                        text.Span("14) YA :Yangın\n").FontSize(FontTiny);
                        text.Span("15) SY :Sabahtan Yarım\n").FontSize(FontTiny);
                        text.Span("16) ÖY :Öğleden Yarım").FontSize(FontTiny);
                    });
                });
            });

            // İmza alanları
            col.Item().PaddingTop(8).Row(row =>
            {
                row.RelativeItem().Column(sig =>
                {
                    sig.Item().Text(text =>
                    {
                        text.AlignCenter();
                        text.Span("İşletme Yetkilisi").FontSize(FontSmall);
                    });
                    sig.Item().PaddingTop(3).Text(text =>
                    {
                        text.AlignCenter();
                        text.Span("___/___/____").FontSize(FontSmall);
                    });
                    sig.Item().PaddingTop(3).Text(text =>
                    {
                        text.AlignCenter();
                        text.Span(_data.BusinessContactName).FontSize(FontSmall).Bold();
                    });
                    sig.Item().Text(text =>
                    {
                        text.AlignCenter();
                        text.Span("Kaşe - İmza").FontSize(FontTiny);
                    });
                });

                row.RelativeItem().Column(sig =>
                {
                    sig.Item().Text(text =>
                    {
                        text.AlignCenter();
                        text.Span("Koordinatör Öğretmen").FontSize(FontSmall);
                    });
                    sig.Item().PaddingTop(3).Text(text =>
                    {
                        text.AlignCenter();
                        text.Span("___/___/____").FontSize(FontSmall);
                    });
                    sig.Item().PaddingTop(3).Text(text =>
                    {
                        text.AlignCenter();
                        text.Span(_data.CoordinatorTeacherName).FontSize(FontSmall).Bold();
                    });
                    sig.Item().Text(text =>
                    {
                        text.AlignCenter();
                        text.Span("İmza").FontSize(FontTiny);
                    });
                });

                row.RelativeItem().Column(sig =>
                {
                    sig.Item().Text(text =>
                    {
                        text.AlignCenter();
                        text.Span("incelendi").FontSize(FontSmall);
                    });
                    sig.Item().PaddingTop(3).Text(text =>
                    {
                        text.AlignCenter();
                        text.Span("___/___/____").FontSize(FontSmall);
                    });
                    sig.Item().PaddingTop(3).Text(text =>
                    {
                        text.AlignCenter();
                        text.Span(_data.VicePrincipalName).FontSize(FontSmall).Bold();
                    });
                    sig.Item().Text(text =>
                    {
                        text.AlignCenter();
                        text.Span("Koordinatör Müdür Yardımcısı").FontSize(FontTiny);
                    });
                });
            });
        });
    }
}
