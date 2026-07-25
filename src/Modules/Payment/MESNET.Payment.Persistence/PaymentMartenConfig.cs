using Marten;
using MESNET.Payment.Core.Entities;
using MESNET.Payment.Core.ReadModels;

namespace MESNET.Payment.Persistence;

public class PaymentMartenConfig : IConfigureMarten
{
    public void Configure(IServiceProvider services, StoreOptions options)
    {
        // SalaryCalculationConfig — institution başına parametreler
        options.Schema.For<SalaryCalculationConfig>().DatabaseSchemaName("payment");
        options.Schema.For<SalaryCalculationConfig>().Index(x => x.InstitutionId);
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

        // PlacementView — Enrollment event'lerinden; aylık maaş zamanlayıcısının çalışma listesi (#63)
        options.Schema.For<PlacementView>().DatabaseSchemaName("payment");
        options.Schema.For<PlacementView>().Index(x => x.StudentId);
        options.Schema.For<PlacementView>().Index(x => x.AcademicPeriodId);

        // StudentContractWageView — Contract event'lerinden; sözleşme ücreti yasal tabanın üstündeyse esas alınır (#84)
        options.Schema.For<StudentContractWageView>().DatabaseSchemaName("payment");

        // AcademicPeriodView — Institution event'lerinden (kapalı dönem kontrolü, #8)
        options.Schema.For<AcademicPeriodView>().DatabaseSchemaName("payment");
        options.Schema.For<AcademicPeriodView>().Index(x => x.InstitutionId);
    }
}
