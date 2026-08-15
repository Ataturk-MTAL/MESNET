using Marten;
using Marten.Schema.Indexing.Unique;
using MESNET.Enrollment.Core.ReadModels;
using MESNET.Enrollment.Core.Entities;

namespace MESNET.Enrollment.Persistence;

public class EnrollmentMartenConfig : IConfigureMarten
{
    public void Configure(IServiceProvider services, StoreOptions options)
    {
        options.Schema.For<StudentProfile>().DatabaseSchemaName("enrollment");
        options.Schema.For<StudentProfile>().Index(x => x.InstitutionId);
        options.Schema.For<StudentProfile>().Index(x => x.AcademicPeriodId);
        options.Schema.For<StudentProfile>().Index(x => x.BranchCode);
        options.Schema.For<StudentProfile>().Index(x => x.KeycloakUserId);

        // Doğal anahtar (#237): aynı dönemde aynı öğrenci numarası iki kez kaydedilemez.
        // Handler zaten kontrol edip anlaşılır hata döndürüyor; bu index yarış durumlarına
        // karşı emniyet kemeri — kontrol ile yazma arasında ikinci bir istek geçebilir.
        //
        // KISMİ: numara zorunlu değil (string?) ve zorunlu kılmak mevcut kayıtları kırardı.
        // Predicate olmasaydı numarasız bütün öğrenciler birbiriyle çakışırdı.
        //
        // PerTenant: öğrenci numarası okul içinde benzersizdir, iki okulun aynı numarayı
        // kullanması NORMALDİR. Marten'da varsayılan kapsam Global'dir — açıkça yazılmazsa
        // kısıt okullar arası uygulanır ve ikinci okul kendi 1101'ini kaydedemez.
        //
        // Kısa isim zorunlu: PostgreSQL identifier sınırı 64 karakter, Marten'ın ürettiği
        // otomatik ad kolayca aşar (CLAUDE.md'de yazılı tuzak).
        options.Schema.For<StudentProfile>()
            .Index(x => new { x.AcademicPeriodId, x.StudentNumber }, x =>
            {
                x.IsUnique = true;
                x.TenancyScope = TenancyScope.PerTenant;
                x.Name = "idx_studentprofile_period_number";
                x.Predicate = "data ->> 'studentNumber' is not null";
            });

        options.Schema.For<TeacherProfile>().DatabaseSchemaName("enrollment");
        options.Schema.For<TeacherProfile>().Index(x => x.InstitutionId);

        options.Schema.For<InternshipPlacement>().DatabaseSchemaName("enrollment");
        options.Schema.For<InternshipPlacement>().Index(x => x.StudentId);
        options.Schema.For<InternshipPlacement>().Index(x => x.BusinessId);
        options.Schema.For<InternshipPlacement>().Index(x => x.AcademicPeriodId);
        // Okulda staj / işletmede staj ayrımıyla filtreleme (#159)
        options.Schema.For<InternshipPlacement>().Index(x => x.TypeName);

        options.Schema.For<BusinessProfileView>().DatabaseSchemaName("enrollment");
        options.Schema.For<BusinessProfileView>().Index(x => x.IsActive);

        options.Schema.For<BusinessBranchAuthorizationView>().DatabaseSchemaName("enrollment");

        options.Schema.For<AcademicPeriodView>().DatabaseSchemaName("enrollment");
        options.Schema.For<AcademicPeriodView>().Index(x => x.InstitutionId);
    }
}
