using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using ZXing;
using ZXing.QrCode;
using ZXing.Rendering;

namespace MESNET.Reporting.Application.Templates.Components;

public static class QrCodeComponent
{
    /// <summary>
    /// Doküman ID'sini içeren QR kod ekler.
    /// Müdür yardımcısı haftalık oluşturduğu dökümanları bu QR ile doğrulayabilir.
    /// </summary>
    public static void QrCode(this IContainer container, Guid documentId, float sizeCm = 2.5f)
    {
        var qrContent = documentId.ToString();

        container
            .Width(sizeCm, Unit.Centimetre)
            .Height(sizeCm, Unit.Centimetre)
            .Svg(size =>
            {
                var writer = new QRCodeWriter();
                var matrix = writer.encode(qrContent, BarcodeFormat.QR_CODE, (int)size.Width, (int)size.Height);
                var renderer = new SvgRenderer();
                return renderer.Render(matrix, BarcodeFormat.QR_CODE, null).Content;
            });
    }

    /// <summary>
    /// Header'a entegre edilmiş QR kod — sağ üst köşede doküman ID ile birlikte
    /// </summary>
    public static void MebHeaderWithQr(this IContainer container, string formTitle, string institutionName, Guid documentId)
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

            row.ConstantItem(70).Column(qrCol =>
            {
                qrCol.Item().Element(c => c.QrCode(documentId, 2f));
                qrCol.Item().Text(text =>
                {
                    text.AlignCenter();
                    text.Span(documentId.ToString()[..8]).FormSmall();
                });
            });
        });
    }
}
