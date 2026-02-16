using Ardalis.SmartEnum;

namespace MESNET.Payment.Core.Enums;

public sealed class PaymentPhase : SmartEnum<PaymentPhase>
{
    public static readonly PaymentPhase Calculated = new(nameof(Calculated), 1, "Hesaplandı");
    public static readonly PaymentPhase AwaitingReceipt = new(nameof(AwaitingReceipt), 2, "Dekont Bekleniyor");
    public static readonly PaymentPhase ReceiptUploaded = new(nameof(ReceiptUploaded), 3, "Dekont Yüklendi");
    public static readonly PaymentPhase StudentConfirmed = new(nameof(StudentConfirmed), 4, "Öğrenci Onayladı");
    public static readonly PaymentPhase TeacherApproved = new(nameof(TeacherApproved), 5, "Öğretmen Onayladı");
    public static readonly PaymentPhase DeputyApproved = new(nameof(DeputyApproved), 6, "Müdür Yardımcısı Onayladı");
    public static readonly PaymentPhase Completed = new(nameof(Completed), 7, "Tamamlandı");
    public static readonly PaymentPhase Rejected = new(nameof(Rejected), 8, "Reddedildi");

    public string Slug { get; }

    private PaymentPhase(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }

    public bool IsFinal => this == Completed || this == Rejected;
}
