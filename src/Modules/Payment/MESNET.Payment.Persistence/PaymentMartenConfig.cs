using Marten;
using MESNET.Payment.Core.Entities;
using MESNET.Payment.Core.ReadModels;

namespace MESNET.Payment.Persistence;

public class PaymentMartenConfig : IConfigureMarten
{
    public void Configure(IServiceProvider services, StoreOptions options)
    {
        // SalaryCalculationConfig — ULUSAL parametreler (#147), kurum başına değil.
        options.Schema.For<SalaryCalculationConfig>().DatabaseSchemaName("payment");
        options.Schema.For<SalaryCalculationConfig>().Index(x => x.EffectiveFrom);
        options.Schema.For<SalaryCalculationConfig>().Index(x => x.EffectiveTo);

        // PaymentSummary — read model projection (consumer'lar günceller)
        options.Schema.For<PaymentSummary>().DatabaseSchemaName("payment");
        options.Schema.For<PaymentSummary>().Index(x => x.StudentId);
        options.Schema.For<PaymentSummary>().Index(x => x.BusinessId);
        options.Schema.For<PaymentSummary>().Index(x => x.InstitutionId);
        options.Schema.For<PaymentSummary>().Index(x => x.AcademicPeriodId);
        options.Schema.For<PaymentSummary>().Index(x => x.Month);
        options.Schema.For<PaymentSummary>().Index(x => x.Phase);
        options.Schema.For<PaymentSummary>().Index(x => x.ReceiptDueDate);
        options.Schema.For<PaymentSummary>().Index(x => x.BranchCode);

        // StudentPaymentProfile — enrollment event'inden beslenen yerel öğrenci profili
        options.Schema.For<StudentPaymentProfile>().DatabaseSchemaName("payment");
        options.Schema.For<StudentPaymentProfile>().Index(x => x.BranchCode);

        // BusinessPaymentProfile — Business event'lerinden; taban ücret oranı personel sayısına bağlı (#64)
        options.Schema.For<BusinessPaymentProfile>().DatabaseSchemaName("payment");

        // StudentAbsenceView — Attendance event'lerinden; devamsızlık kesintisi buradan sayılır (#64)
        options.Schema.For<StudentAbsenceView>().DatabaseSchemaName("payment");
        options.Schema.For<StudentAbsenceView>().Index(x => new { x.StudentId, x.Month },
            x => x.Name = "idx_absence_student_month");
        // Kesinti sözleşmenin istihdam penceresine göre sayılıyor (#154) — gün alanı sorguda.
        options.Schema.For<StudentAbsenceView>().Index(x => x.Date);

        // PlacementView — Enrollment event'lerinden; aylık maaş zamanlayıcısının çalışma listesi (#63)
        options.Schema.For<PlacementView>().DatabaseSchemaName("payment");
        options.Schema.For<PlacementView>().Index(x => x.StudentId);
        options.Schema.For<PlacementView>().Index(x => x.AcademicPeriodId);

        // ContractEmploymentView — Contract event'lerinden; maaş dönemlerinin çalışma listesi,
        // gün oranlamasının ve sözleşme ücretinin kaynağı (#84, #154).
        // StudentContractWageView'ın yerine geçti: o kayıt öğrenci başına tekti ve ay içinde
        // işletme değiştiren öğrencide eski sözleşmenin ücretini kaybediyordu.
        options.Schema.For<ContractEmploymentView>().DatabaseSchemaName("payment");
        options.Schema.For<ContractEmploymentView>().Index(x => x.StudentId);
        options.Schema.For<ContractEmploymentView>().Index(x => x.AcademicPeriodId);
        // Ay kesişimi sorgusu (StartDate <= ay sonu && (EndDate == null || EndDate >= ay başı))
        options.Schema.For<ContractEmploymentView>().Index(x => x.StartDate);

        // AcademicPeriodView — Institution event'lerinden (kapalı dönem kontrolü, #8)
        options.Schema.For<AcademicPeriodView>().DatabaseSchemaName("payment");
        options.Schema.For<AcademicPeriodView>().Index(x => x.InstitutionId);

        // UserNameView — Security.UserDisplayNameUpserted ile beslenir; denetim alanları
        // yalnız kullanıcı kimliğini saklar, ad sorgu tarafında buradan çözülür (#137)
        options.Schema.For<UserNameView>().DatabaseSchemaName("payment");
    }
}
