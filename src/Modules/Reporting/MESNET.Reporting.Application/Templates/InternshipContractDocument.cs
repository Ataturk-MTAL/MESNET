using MESNET.Reporting.Application.Templates.Components;
using MESNET.Reporting.Core.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MESNET.Reporting.Application.Templates;

/// <summary>
/// Form 1: İşletmelerde Beceri Eğitimi Sözleşmesi (5 sayfa)
/// MEB standart sözleşme formu — madde madde + imza sayfası
/// </summary>
public class InternshipContractDocument : IDocument
{
    private readonly InternshipContractFormData _data;

    public InternshipContractDocument(InternshipContractFormData data) => _data = data;

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(MebFormStyles.PageMarginCm, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(MebFormStyles.BodyFontSize));

            page.Header().Element(ComposePageHeader);
            page.Footer().AlignCenter().Text(text =>
            {
                text.Span("Sayfa ").FormSmall();
                text.CurrentPageNumber().FormSmall();
                text.Span(" / ").FormSmall();
                text.TotalPages().FormSmall();
            });

            page.Content().Column(col =>
            {
                // Taraflar
                col.Item().PaddingTop(5).Element(ComposeParties);

                // Sözleşme maddeleri
                col.Item().PaddingTop(8).Element(ComposeArticles);

                // İmza sayfası
                col.Item().PageBreak();
                col.Item().Element(ComposeSignaturePage);
            });
        });
    }

    private void ComposePageHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().MebHeaderWithQr("İŞLETMELERDE BECERİ EĞİTİMİ SÖZLEŞMESİ", _data.InstitutionName, _data.DocumentId);
            col.Item().PaddingBottom(5).LineHorizontal(0.5f).LineColor(MebFormStyles.BorderColor);
        });
    }

    private void ComposeParties(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Text(text => text.Span("TARAFLAR").FormSubTitle());

            // Kurum
            col.Item().PaddingTop(5).Border(0.5f).BorderColor(MebFormStyles.BorderColor).Padding(5).Column(inner =>
            {
                inner.Item().Text(text => text.Span("A) EĞİTİM KURUMU BİLGİLERİ").FormBody().Bold());
                inner.Item().PaddingTop(3).LabelValue("Kurum Adı", _data.InstitutionName);
                inner.Item().LabelValue("Adres", _data.InstitutionAddress);
                inner.Item().LabelValue("İl / İlçe", $"{_data.InstitutionCity} / {_data.InstitutionDistrict}");
                inner.Item().LabelValue("Telefon", _data.InstitutionPhone);
                inner.Item().LabelValue("Müdür", _data.PrincipalName);
            });

            // İşletme
            col.Item().PaddingTop(5).Border(0.5f).BorderColor(MebFormStyles.BorderColor).Padding(5).Column(inner =>
            {
                inner.Item().Text(text => text.Span("B) İŞLETME BİLGİLERİ").FormBody().Bold());
                inner.Item().PaddingTop(3).LabelValue("İşletme Adı", _data.BusinessName);
                inner.Item().LabelValue("Adres", _data.BusinessAddress);
                inner.Item().LabelValue("Telefon", _data.BusinessPhone);
                inner.Item().LabelValue("E-posta", _data.BusinessEmail);
                inner.Item().LabelValue("Vergi No", _data.TaxNumber);
                inner.Item().LabelValue("SGK No", _data.SgkNumber);
                inner.Item().LabelValue("Faaliyet Alanı", _data.ActivityField);
            });

            // Usta öğretici
            col.Item().PaddingTop(5).Border(0.5f).BorderColor(MebFormStyles.BorderColor).Padding(5).Column(inner =>
            {
                inner.Item().Text(text => text.Span("C) USTA ÖĞRETİCİ BİLGİLERİ").FormBody().Bold());
                inner.Item().PaddingTop(3).LabelValue("Adı Soyadı", _data.MasterInstructorName);
                inner.Item().LabelValue("T.C. Kimlik No", _data.MasterInstructorTcKimlik);
                inner.Item().LabelValue("Belge No", _data.MasterInstructorCertificateNo);
                inner.Item().LabelValue("Belge Türü", _data.MasterInstructorCertificateType);
            });

            // Öğrenci
            col.Item().PaddingTop(5).Border(0.5f).BorderColor(MebFormStyles.BorderColor).Padding(5).Column(inner =>
            {
                inner.Item().Text(text => text.Span("D) ÖĞRENCİ BİLGİLERİ").FormBody().Bold());
                inner.Item().PaddingTop(3).LabelValue("Adı Soyadı", _data.StudentFullName);
                inner.Item().LabelValue("T.C. Kimlik No", _data.StudentTcKimlik);
                inner.Item().LabelValue("Okul No", _data.StudentNumber);
                inner.Item().LabelValue("Doğum Tarihi", _data.StudentBirthDate.ToString("dd.MM.yyyy"));
                inner.Item().LabelValue("Doğum Yeri", _data.StudentBirthPlace);
                inner.Item().LabelValue("Baba Adı", _data.FatherName);
                inner.Item().LabelValue("Ana Adı", _data.MotherName);
                inner.Item().LabelValue("Alan/Dal", _data.BranchName);
                inner.Item().LabelValue("Sınıf", _data.ClassYear.ToString());
            });

            // Veli
            col.Item().PaddingTop(5).Border(0.5f).BorderColor(MebFormStyles.BorderColor).Padding(5).Column(inner =>
            {
                inner.Item().Text(text => text.Span("E) VELİ BİLGİLERİ").FormBody().Bold());
                inner.Item().PaddingTop(3).LabelValue("Adı Soyadı", _data.GuardianName);
                inner.Item().LabelValue("T.C. Kimlik No", _data.GuardianTcKimlik);
                inner.Item().LabelValue("Telefon", _data.GuardianPhone);
                inner.Item().LabelValue("Adres", _data.GuardianAddress);
                inner.Item().LabelValue("Yakınlık Derecesi", _data.GuardianRelationship);
            });
        });
    }

    private void ComposeArticles(IContainer container)
    {
        var articles = GetContractArticles();

        container.Column(col =>
        {
            col.Item().Text(text => text.Span("SÖZLEŞME MADDELERİ").FormSubTitle());

            for (var i = 0; i < articles.Count; i++)
            {
                col.Item().PaddingTop(4).Row(row =>
                {
                    row.ConstantItem(25).Text(text => text.Span($"{i + 1}.").FormBody().Bold());
                    row.RelativeItem().Text(text => text.Span(articles[i]).FormBody());
                });
            }
        });
    }

    private void ComposeSignaturePage(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Text(text =>
            {
                text.AlignCenter();
                text.Span("İMZA SAYFASI").FormSubTitle();
            });

            col.Item().PaddingTop(5).Text(text =>
            {
                text.Span($"Sözleşme Dönemi: {_data.ContractStartDate:dd.MM.yyyy} — {_data.ContractEndDate:dd.MM.yyyy}").FormBody();
            });
            col.Item().Text(text =>
            {
                text.Span($"Öğretim Yılı: {_data.AcademicYear}").FormBody();
            });
            col.Item().Text(text =>
            {
                text.Span($"Koordinatör Öğretmen: {_data.CoordinatorTeacherName}").FormBody();
            });

            col.Item().PaddingTop(15).Text(text =>
            {
                text.Span("İşbu sözleşme taraflarca okunmuş ve kabul edilmiştir.").FormBody();
            });

            // 4 imza bloğu — 2x2 grid
            col.Item().PaddingTop(20).Row(row =>
            {
                row.RelativeItem().Column(signCol =>
                {
                    ComposeSignatureBox(signCol, "EĞİTİM KURUMU MÜDÜRÜ", _data.PrincipalName, _data.InstitutionSignedAt);
                });
                row.RelativeItem().Column(signCol =>
                {
                    ComposeSignatureBox(signCol, "İŞLETME YETKİLİSİ", _data.BusinessName, _data.BusinessSignedAt);
                });
            });

            col.Item().PaddingTop(20).Row(row =>
            {
                row.RelativeItem().Column(signCol =>
                {
                    ComposeSignatureBox(signCol, "ÖĞRENCİ", _data.StudentFullName, _data.StudentSignedAt);
                });
                row.RelativeItem().Column(signCol =>
                {
                    ComposeSignatureBox(signCol, "VELİ", _data.GuardianName, _data.ParentSignedAt);
                });
            });
        });
    }

    private static void ComposeSignatureBox(ColumnDescriptor col, string title, string name, DateTime? signedAt)
    {
        col.Item().Border(0.5f).BorderColor(MebFormStyles.BorderColor).Padding(8).MinHeight(80).Column(inner =>
        {
            inner.Item().Text(text =>
            {
                text.AlignCenter();
                text.Span(title).FormBody().Bold();
            });
            inner.Item().PaddingTop(3).Text(text =>
            {
                text.AlignCenter();
                text.Span(name).FormSmall();
            });
            inner.Item().PaddingTop(20).Text(text =>
            {
                text.AlignCenter();
                text.Span(signedAt.HasValue ? $"Tarih: {signedAt.Value:dd.MM.yyyy}" : "Tarih: __ / __ / ____").FormSmall();
            });
            inner.Item().PaddingTop(5).Text(text =>
            {
                text.AlignCenter();
                text.Span("İmza").FormSmall();
            });
        });
    }

    private List<string> GetContractArticles() =>
    [
        "İşletme, öğrencinin beceri eğitimini 3308 sayılı Mesleki Eğitim Kanunu ve ilgili mevzuat hükümlerine göre yürütür.",
        "İşletme, öğrenciye beceri eğitimi süresince asgari ücretin %30'undan az olmamak üzere ücret öder.",
        "İşletme, öğrenciyi iş kazası ve meslek hastalıklarına karşı sigorta ettirir.",
        "İşletme, öğrencinin devamını izler ve devamsızlık durumunu okula bildirir.",
        "Öğrenci, işletmede belirlenen çalışma saatlerine ve kurallarına uyar.",
        "Öğrenci, iş sağlığı ve güvenliği kurallarına uyar, koruyucu ekipmanları kullanır.",
        "Öğrenci, işletmenin ticari sırlarını ve gizli bilgilerini korur.",
        "Okul, koordinatör öğretmen aracılığıyla öğrencinin beceri eğitimini takip eder.",
        "Koordinatör öğretmen, ayda en az bir kez işletmeyi ziyaret eder ve rehberlik yapar.",
        "Sözleşme, taraflardan birinin haklı nedene dayanarak fesih talebinde bulunması halinde, diğer tarafın onayı ile feshedilebilir.",
        "Öğrencinin devamsızlığı yönetmelikte belirlenen süreyi aşarsa sözleşme feshedilebilir.",
        "Fesih halinde öğrenci, eğitim kurumunca başka bir işletmeye yerleştirilir.",
        "Beceri sınavları, her dönem sonunda okul ve işletme temsilcilerinden oluşan komisyon tarafından yapılır.",
        "İşbu sözleşme, taraflarca imzalandığı tarihte yürürlüğe girer ve eğitim dönemi sonuna kadar geçerlidir."
    ];
}
