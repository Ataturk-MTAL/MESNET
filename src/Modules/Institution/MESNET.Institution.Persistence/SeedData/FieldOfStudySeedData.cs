using System.Reflection;
using System.Text.Json;
using Marten;
using Marten.Schema;
using MESNET.Common.Shared.Enums;
using MESNET.Institution.Core.Entities;
using MESNET.Institution.Core.ValueObjects;

namespace MESNET.Institution.Persistence.SeedData;

public class FieldOfStudySeedData : IInitialData
{
    public async Task Populate(IDocumentStore store, CancellationToken cancellation)
    {
        await using var session = store.LightweightSession();

        var existing = await session.Query<FieldOfStudy>().CountAsync(cancellation);
        if (existing > 0)
            return;

        var mtalFields = LoadFromResource("mtal_fields.json", EducationType.Formal);
        var mesemFields = LoadFromResource("mesem_fields.json", EducationType.Mesem);

        session.Store(mtalFields.ToArray());
        session.Store(mesemFields.ToArray());

        await session.SaveChangesAsync(cancellation);
    }

    private static List<FieldOfStudy> LoadFromResource(string fileName, EducationType educationType)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(fileName))
            ?? throw new InvalidOperationException($"Embedded resource {fileName} not found.");

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        var jsonFields = JsonSerializer.Deserialize<List<SeedFieldDto>>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? [];

        return jsonFields.Select(f => new FieldOfStudy
        {
            Id = Guid.NewGuid(),
            Code = f.Code,
            Name = f.Name,
            Type = educationType,
            IsProtocolBased = f.IsProtocolBased,
            IsActive = true,
            Specializations = f.Specializations.Select(s => new Specialization(s.Code, s.Name)).ToList()
        }).ToList();
    }

    private sealed record SeedFieldDto
    {
        public string Code { get; init; } = "";
        public string Name { get; init; } = "";
        public bool IsProtocolBased { get; init; }
        public List<SeedSpecializationDto> Specializations { get; init; } = [];
    }

    private sealed record SeedSpecializationDto
    {
        public string Code { get; init; } = "";
        public string Name { get; init; } = "";
    }
}
