using MESNET.Reporting.Application.Templates.Components;
using MESNET.Reporting.Core.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MESNET.Reporting.Application.Templates;

/// <summary>
/// Form 3: Günlük Rehberlik Görev Formu
/// Bir öğretmenin tüm ziyaretleri tek PDF'te toplanır.
/// Her yatay A4 sayfasında iki A5 form yan yana — aralarında kesikli kesim çizgisi.
/// Tek sayıda form varsa son sayfanın sağ tarafı boş kalır.
/// </summary>
public class GuidanceVisitFormDocument : IDocument
{
    private readonly IReadOnlyList<GuidanceVisitFormData> _forms;

    public GuidanceVisitFormDocument(IReadOnlyList<GuidanceVisitFormData> forms) => _forms = forms;

    /// <summary>
    /// Tek form için geriye dönük uyumluluk
    /// </summary>
    public GuidanceVisitFormDocument(GuidanceVisitFormData singleForm) : this([singleForm]) { }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        // Formları ikişerli grupla — son grup tek eleman olabilir
        for (var i = 0; i < _forms.Count; i += 2)
        {
            var left = _forms[i];
            var right = i + 1 < _forms.Count ? _forms[i + 1] : null;

            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.MarginVertical(0.7f, Unit.Centimetre);
                page.MarginHorizontal(0.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(7f));

                page.Content().Row(row =>
                {
                    // Sol form
                    row.RelativeItem().PaddingRight(8).Element(c => RenderSingleForm(c, left));

                    // Kesikli kesim çizgisi (✂)
                    row.ConstantItem(12).Element(RenderCutLine);

                    // Sağ form veya boş alan
                    row.RelativeItem().PaddingLeft(8).Element(c =>
                    {
                        if (right is not null)
                            RenderSingleForm(c, right);
                        // Sağ taraf boş — yarım A4 başka işte kullanılabilir
                    });
                });
            });
        }
    }

    /// <summary>
    /// Dikey kesikli kesim çizgisi — kağıt ortasından kesilecek yer
    /// </summary>
    private static void RenderCutLine(IContainer container)
    {
        container.Column(col =>
        {
            // Makas ikonu
            col.Item().AlignCenter().Text(text => text.Span("✂").FontSize(7f).FontColor("#999999"));

            // Kesikli çizgi — küçük dikdörtgenler dikey tekrar
            for (var i = 0; i < 60; i++)
            {
                col.Item().AlignCenter().Width(0.5f).Height(4).Background("#BBBBBB");
                col.Item().Height(3); // boşluk
            }
        });
    }

    private static void RenderSingleForm(IContainer container, GuidanceVisitFormData data)
    {
        container.Border(0.5f).BorderColor(MebFormStyles.BorderColor).Padding(8).Column(col =>
        {
            // ── Başlık ──
            col.Item().Column(header =>
            {
                header.Item().Text(text =>
                {
                    text.AlignCenter();
                    text.Span("İŞLETMELERDE MESLEK EĞİTİMİ").FontSize(9f).Bold();
                });
                header.Item().Text(text =>
                {
                    text.AlignCenter();
                    text.Span("GÜNLÜK REHBERLİK GÖREV FORMU").FontSize(9f).Bold();
                });
            });

            // ── QR kod (sağ üst) ──
            col.Item().AlignRight().Element(c => c.QrCode(data.DocumentId, 1.5f));

            // ── Bilgi alanları ──
            col.Item().PaddingTop(4).Element(c => InfoField(c, "İşletmenin Adı", data.BusinessName));
            col.Item().PaddingTop(2).Element(c => InfoField(c, "İzlemede Olduğu Öğrenci Sayısı",
                data.StudentCount > 0 ? data.StudentCount.ToString() : "..."));
            col.Item().PaddingTop(2).Element(c => InfoField(c, "Meslek Alan/Dalı", data.BranchName));
            col.Item().PaddingTop(2).Element(c => InfoField(c, "Görev Tarihi",
                data.VisitDate.ToString("dd.MM.yyyy")));
            col.Item().PaddingTop(2).Element(c => InfoField(c, "Form ID",
                data.DocumentId.ToString()[..8]));

            // ── "Aylık Rehberlik Formuna Göre ;" başlığı ──
            col.Item().PaddingTop(10).Text(text =>
            {
                text.Span("Aylık Rehberlik Formuna Göre ;").FontSize(8f).Bold().Underline();
            });

            // ── 3 serbest metin kutusu ──
            col.Item().PaddingTop(6).Element(c =>
                TextArea(c, "İşletmede öğrenim gören öğrencilerin eğitimini olumsuz yönde etkileyen hususlar: (varsa yazınız.)",
                    data.NegativeFactors));

            col.Item().PaddingTop(4).Element(c =>
                TextArea(c, "Belirlenen aksaklıklarla ilgili yapılan rehberlik ve alınan önlemler:",
                    data.GuidanceActions));

            col.Item().PaddingTop(4).Element(c =>
                TextArea(c, "Aylık Rehberlik formunda belirtilmesinde yarar görülen hususlar:",
                    data.ReportNotes));

            // ── İmza bloğu ──
            col.Item().Extend().AlignBottom().Column(sig =>
            {
                sig.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Column(s =>
                    {
                        s.Item().Text(text => text.Span("İşletme Eğitim Yetkilisi").FontSize(7f));
                        s.Item().Text(text => text.Span(data.BusinessContactName ?? "").FontSize(7f).Bold());
                        s.Item().Text(text => text.Span("Kaşe ve İmza").FontSize(6f));
                    });
                    row.RelativeItem().Column(s =>
                    {
                        s.Item().Text(text => text.Span("Koor. Öğretmen").FontSize(7f));
                        s.Item().Text(text => text.Span(data.TeacherName).FontSize(7f).Bold());
                        s.Item().Text(text => text.Span("İmza").FontSize(6f));
                    });
                    row.RelativeItem().Column(s =>
                    {
                        s.Item().Text(text => text.Span("Koor. Md. Yrd.").FontSize(7f));
                        s.Item().Text(text => text.Span(data.VicePrincipalName ?? "").FontSize(7f).Bold());
                        s.Item().Text(text => text.Span("İmza").FontSize(6f));
                    });
                });

                // ── Açıklamalar ──
                sig.Item().PaddingTop(8).Column(notes =>
                {
                    notes.Item().Text(text => text.Span("Açıklamalar:").FontSize(6f).Bold());
                    notes.Item().PaddingTop(2).Text(text =>
                        text.Span("Bu form koordinatör öğretmen tarafından her görev için görev haftası başında koordinatör Müdür Yrd.'ndan alınır. Görev sonrasında okula geldiği gün içinde imzaları tamamlanmış olarak Koordinatör Md. Yrd.'na teslim edilir.")
                            .FontSize(5.5f));
                    notes.Item().PaddingTop(1).Text(text =>
                        text.Span("Bu form \"Aylık Rehberlik Formu\"nun doldurulmasında esas alınır ve rapora eklenir.")
                            .FontSize(5.5f));
                });
            });
        });
    }

    private static void InfoField(IContainer container, string label, string? value)
    {
        container.Row(row =>
        {
            row.ConstantItem(160).Text(text => text.Span(label).FontSize(7f).Bold());
            row.ConstantItem(10).Text(text => text.Span(":").FontSize(7f));
            row.RelativeItem().Text(text => text.Span(value ?? "").FontSize(7f));
        });
    }

    private static void TextArea(IContainer container, string label, string? content)
    {
        container.Column(col =>
        {
            col.Item().Text(text => text.Span(label).FontSize(6.5f));
            col.Item().PaddingTop(2).MinHeight(40)
                .Text(text => text.Span(content ?? "").FontSize(7f));
        });
    }
}
