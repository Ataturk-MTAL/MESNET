using MESNET.Payment.Application.Commands;
using MESNET.Payment.Application.Validators;
using Shouldly;
using Xunit;

namespace MESNET.Payment.UnitTests;

/// <summary>
/// Asgari ücret girişinin sınır doğrulamaları — hatalı değer doğrudan yanlış para üretir.
/// </summary>
public class UpdateMinimumWageValidatorTests
{
    private static readonly UpdateMinimumWageValidator Validator = new();
    private static readonly DateTime NextYear = new(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static UpdateMinimumWage Command(
        decimal wage = 30_000m, decimal? under16 = null, DateTime? effectiveFrom = null) =>
        new(wage, under16, effectiveFrom ?? NextYear);

    [Fact]
    public void accepts_a_future_effective_date()
    {
        // İleri tarihli giriş asıl kullanım senaryosu: takvim yılı başlamadan zam kaydedilir.
        Validator.Validate(Command()).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void rejects_a_missing_effective_date()
    {
        // Alan gönderilmezse 0001-01-01 gelir ve yürürlük zinciri tarihin başından açılır.
        var result = Validator.Validate(Command(effectiveFrom: default(DateTime)));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateMinimumWage.EffectiveFrom));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void rejects_a_non_positive_wage(decimal wage)
    {
        Validator.Validate(Command(wage)).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void rejects_an_under16_wage_above_the_general_wage()
    {
        // 16 yaş altı tutar yaşa uygun (daha DÜŞÜK) asgari ücrettir; aşarsa taban hesabı ters döner.
        var result = Validator.Validate(Command(wage: 30_000m, under16: 31_000m));

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void accepts_an_under16_wage_equal_to_the_general_wage()
    {
        // Eşitlik meşru: bazı yıllarda yaşa göre ayrı tutar belirlenmez.
        Validator.Validate(Command(wage: 30_000m, under16: 30_000m)).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void rejects_a_non_positive_under16_wage()
    {
        Validator.Validate(Command(wage: 30_000m, under16: 0m)).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void accepts_a_null_under16_wage()
    {
        // null = yaş ayrımı yapılmaz, genel asgari ücret uygulanır (#85).
        Validator.Validate(Command(wage: 30_000m, under16: null)).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void command_carries_no_institution_scope()
    {
        // Asgari ücret ULUSAL parametredir (#147). Komuta kurum kimliği geri eklenirse yazma
        // ucu yine gövdeden kurum alır ve kurumlar arası yazma deliği açılır.
        typeof(UpdateMinimumWage).GetProperties()
            .Select(p => p.Name)
            .ShouldNotContain("InstitutionId");
    }
}
