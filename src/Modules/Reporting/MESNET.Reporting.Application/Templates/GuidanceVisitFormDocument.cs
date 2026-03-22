using MESNET.Reporting.Application.Templates.Components;
using MESNET.Reporting.Core.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MESNET.Reporting.Application.Templates;

/// <summary>
/// Form 3: Günlük Rehberlik Görev Formu
/// A4 Landscape (842 × 595 pt) — iki A5 form yan yana, aralarında kesim çizgisi.
///
/// Düzen hesabı:
///   Sayfa kenar boşlukları: tüm yönler 28pt (≈1cm)
///   Kullanılabilir genişlik: 842 - 56 = 786 pt
///   Kesim çizgisi: 14 pt
///   Form arası simetrik boşluk: sol PaddingRight(8) + sağ PaddingLeft(8) = 16 pt
///   Her form genişliği: (786 - 14 - 16) / 2 = 378 pt
///   Form iç içerik: 378 - border(2×0.7) - padding(2×8) ≈ 360 pt
/// </summary>
public class GuidanceVisitFormDocument : IDocument
{
    private readonly IReadOnlyList<GuidanceVisitFormData> _forms;

    public GuidanceVisitFormDocument(IReadOnlyList<GuidanceVisitFormData> forms) => _forms = forms;

    public GuidanceVisitFormDocument(GuidanceVisitFormData singleForm) : this([singleForm]) { }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        for (var i = 0; i < _forms.Count; i += 2)
        {
            var left = _forms[i];
            var right = i + 1 < _forms.Count ? _forms[i + 1] : null;

            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.MarginTop(28); // 28pt ≈ 1cm — tüm kenarlar eşit
                page.MarginLeft(28);
                page.MarginBottom(28);
                page.MarginRight(0);
                page.DefaultTextStyle(x => x.FontSize(11f));

                page.Content().Row(row =>
                {
                    // Sol form — kesim çizgisinden 8pt boşluk
                    row.ConstantItem(378).PaddingRight(8).Element(c => RenderSingleForm(c, left));

                    // Kesim çizgisi — 14pt sabit
                    row.ConstantItem(14).Element(RenderCutLine);
                    row.ConstantItem(28);

                    // Sağ form — sol: kesim çizgisinden 8pt, sağ: 20pt delgeç/dosya payı
                    row.ConstantItem(378).PaddingRight(8).Element(c =>
                    {
                        if (right is not null)
                            RenderSingleForm(c, right);
                    });
                });
            });
        }
    }

    /// <summary>Dikey kesikli kesim çizgisi — makas ikonu + noktalı çizgi</summary>
    private static void RenderCutLine(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().AlignCenter().Text(text =>
                text.Span("✂").FontSize(8f).FontColor("#AAAAAA"));
            for (var i = 0; i < 70; i++)
            {
                col.Item().AlignCenter().Width(0.5f).Height(4).Background("#CCCCCC");
                col.Item().Height(3);
            }
        });
    }

    private static void RenderSingleForm(IContainer container, GuidanceVisitFormData data)
    {
        container.Border(0.7f).BorderColor(MebFormStyles.BorderColor).Padding(8).Column(col =>
        {
            // ── Başlık satırı: okul adı + MEB başlığı (sol) | QR (sağ) ──
            col.Item().Row(titleRow =>
            {
                titleRow.RelativeItem()
                .ZIndex(1)
                .Padding(15)
                .Column(t =>
                {
                    if (!string.IsNullOrWhiteSpace(data.InstitutionName))
                    {
                        t.Item().Text(text =>
                        {
                            text.AlignCenter();
                            text.Span(data.InstitutionName.ToUpperInvariant()).FontSize(9f).Bold();
                        });
                    }

                    t.Item().Text(text =>
                    {
                        text.AlignCenter();
                        text.Span("İŞLETMELERDE MESLEK EĞİTİMİ").FontSize(9f).Bold();
                    });
                    t.Item().Text(text =>
                    {
                        text.AlignCenter();
                        text.Span("GÜNLÜK REHBERLİK GÖREV FORMU").FontSize(9f).Bold();
                    });
                });

                // QR — 36pt sabit (1.2cm) — başlıkla hizalı
                titleRow.ConstantItem(36).AlignBottom().Element(c => c.QrCode(data.DocumentId, 1.2f));
            });

            // ── Bilgi alanları ──
            col.Item().PaddingTop(5).Element(c => InfoField(c, "İşletmenin Adı", data.BusinessName));
            col.Item().PaddingTop(2).Element(c => InfoField(c, "İzlemede Olduğu Öğrenci Sayısı",
                data.StudentCount > 0 ? data.StudentCount.ToString() : ""));
            col.Item().PaddingTop(2).Element(c => InfoField(c, "Meslek Alan/Dalı", data.BranchName));
            col.Item().PaddingTop(2).Element(c => InfoField(c, "Görev Tarihi",
                data.VisitDate.ToString("dd.MM.yyyy")));
            col.Item().PaddingTop(2).Element(c => InfoField(c, "Form ID",
                data.DocumentId.ToString()[..8]));

            // ── Aylık Rehberlik başlığı ──
            col.Item().PaddingTop(7).Text(text =>
                text.Span("Aylık Rehberlik Formuna Göre ;").FontSize(7.5f).Bold().Underline());

            // ── 3 metin alanı ──
            col.Item().PaddingTop(4).Element(c =>
                TextArea(c, "İşletmede öğrenim gören öğrencilerin eğitimini olumsuz yönde etkileyen hususlar: (varsa yazınız.)",
                    data.NegativeFactors));

            col.Item().PaddingTop(3).Element(c =>
                TextArea(c, "Belirlenen aksaklıklarla ilgili yapılan rehberlik ve alınan önlemler:",
                    data.GuidanceActions));

            col.Item().PaddingTop(3).Element(c =>
                TextArea(c, "Aylık Rehberlik formunda belirtilmesinde yarar görülen hususlar:",
                    data.ReportNotes));

            // ── İmza tablosu — 3 eşit sütun, ortalı, bordersız ──
            col.Item().PaddingTop(8).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn();
                    cols.RelativeColumn();
                    cols.RelativeColumn();
                });

                // Satır 1: ünvan
                table.Cell().AlignCenter().Text(text =>
                    text.Span("İşletme Eğitim Yetkilisi").FontSize(6.5f));
                table.Cell().AlignCenter().Text(text =>
                    text.Span("Koor. Öğretmen").FontSize(6.5f));
                table.Cell().AlignCenter().Text(text =>
                    text.Span("Koor. Md. Yrd.").FontSize(6.5f));

                // Satır 2: ad soyad (bold)
                table.Cell().AlignCenter().PaddingTop(2).Text(text =>
                    text.Span(data.BusinessContactName ?? "").FontSize(6.5f).Bold());
                table.Cell().AlignCenter().PaddingTop(2).Text(text =>
                    text.Span(data.TeacherName).FontSize(6.5f).Bold());
                table.Cell().AlignCenter().PaddingTop(2).Text(text =>
                    text.Span(data.VicePrincipalName ?? "").FontSize(6.5f).Bold());

                // Satır 3: imza notu
                table.Cell().AlignCenter().Text(text =>
                    text.Span("Kaşe ve İmza").FontSize(5.5f).FontColor("#666666"));
                table.Cell().AlignCenter().Text(text =>
                    text.Span("İmza").FontSize(5.5f).FontColor("#666666"));
                table.Cell().AlignCenter().Text(text =>
                    text.Span("İmza").FontSize(5.5f).FontColor("#666666"));
            });

            // ── Açıklamalar ──
            col.Item().PaddingTop(6).Column(notes =>
            {
                notes.Item().Text(text => text.Span("Açıklamalar:").FontSize(5.5f).Bold());
                notes.Item().PaddingTop(1).Text(text =>
                    text.Span("Bu form koordinatör öğretmen tarafından her görev için görev haftası başında koordinatör Müdür Yrd.'ndan alınır. Görev sonrasında okula geldiği gün içinde imzaları tamamlanmış olarak Koordinatör Md. Yrd.'na teslim edilir.")
                        .FontSize(5f));
                notes.Item().PaddingTop(1).Text(text =>
                    text.Span("Bu form \"Aylık Rehberlik Formu\"nun doldurulmasında esas alınır ve rapora eklenir.")
                        .FontSize(5f));
            });
        });
    }

    private static void InfoField(IContainer container, string label, string? value)
    {
        container.Row(row =>
        {
            row.ConstantItem(145).Text(text => text.Span(label).FontSize(6.5f).Bold());
            row.ConstantItem(8).Text(text => text.Span(":").FontSize(6.5f));
            row.RelativeItem().Text(text => text.Span(value ?? "").FontSize(6.5f));
        });
    }

    private static void TextArea(IContainer container, string label, string? content)
    {
        container.Column(col =>
        {
            col.Item().Text(text => text.Span(label).FontSize(6f));
            col.Item().PaddingTop(1).MinHeight(80)
                .Text(text => text.Span(content ?? "").FontSize(6.5f));
        });
    }
}
