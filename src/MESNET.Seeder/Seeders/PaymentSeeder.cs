namespace MESNET.Seeder.Seeders;

public static class PaymentSeeder
{
    public static async Task SeedAsync(MesnetApiClient api, SeedContext ctx)
    {
        Console.WriteLine();
        Console.WriteLine("── Ödeme Ayarları ─────────────────");

        if (!ctx.Has("Institution")) return;

        var result = await api.PutAsync("/api/payments/config/minimum-wage", new
        {
            institutionId = ctx.Get("Institution"),
            newMinimumWage = 22104.00m,
            effectiveFrom = new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc)
            // updatedBy gönderilmez — aktör artık token'dan damgalanır (#137).
        });

        if (result is not null)
            Console.WriteLine("  ✓ Asgari ücret: 22.104 TL (01.07.2025'ten itibaren)");
    }
}
