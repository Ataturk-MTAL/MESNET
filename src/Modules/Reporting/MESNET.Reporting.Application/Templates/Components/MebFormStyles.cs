using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MESNET.Reporting.Application.Templates.Components;

public static class MebFormStyles
{
    public const float PageMarginCm = 1.5f;
    public const float CellPadding = 3f;
    public const float HeaderFontSize = 12f;
    public const float SubHeaderFontSize = 10f;
    public const float BodyFontSize = 8f;
    public const float SmallFontSize = 7f;
    public const string BorderColor = "#000000";

    public static IContainer CellStyle(this IContainer container) =>
        container.Border(0.5f).BorderColor(BorderColor).Padding(CellPadding);

    public static IContainer HeaderCellStyle(this IContainer container) =>
        container.Border(0.5f).BorderColor(BorderColor).Padding(CellPadding)
            .Background(Colors.Grey.Lighten3);

    public static TextSpanDescriptor FormTitle(this TextSpanDescriptor text) =>
        text.FontSize(HeaderFontSize).Bold();

    public static TextSpanDescriptor FormSubTitle(this TextSpanDescriptor text) =>
        text.FontSize(SubHeaderFontSize).Bold();

    public static TextSpanDescriptor FormBody(this TextSpanDescriptor text) =>
        text.FontSize(BodyFontSize);

    public static TextSpanDescriptor FormSmall(this TextSpanDescriptor text) =>
        text.FontSize(SmallFontSize);

    public static void MebHeader(this IContainer container, string formTitle, string institutionName)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(text =>
                {
                    text.AlignCenter();
                    text.Span("T.C.").FormBody();
                });
                col.Item().Text(text =>
                {
                    text.AlignCenter();
                    text.Span("MİLLÎ EĞİTİM BAKANLIĞI").FormSubTitle();
                });
                col.Item().Text(text =>
                {
                    text.AlignCenter();
                    text.Span(institutionName).FormBody();
                });
                col.Item().PaddingTop(5).Text(text =>
                {
                    text.AlignCenter();
                    text.Span(formTitle).FormTitle();
                });
            });
        });
    }

    public static void SignatureBlock(this IContainer container, params string[] signerLabels)
    {
        container.Row(row =>
        {
            foreach (var label in signerLabels)
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().PaddingTop(30).Text(text =>
                    {
                        text.AlignCenter();
                        text.Span(label).FormBody();
                    });
                    col.Item().PaddingTop(5).Text(text =>
                    {
                        text.AlignCenter();
                        text.Span("İmza").FormSmall();
                    });
                });
            }
        });
    }

    public static void LabelValue(this IContainer container, string label, string? value)
    {
        container.Row(row =>
        {
            row.ConstantItem(140).Text(text => text.Span($"{label}:").FormBody().Bold());
            row.RelativeItem().Text(text => text.Span(value ?? "").FormBody());
        });
    }
}
