namespace MESNET.Seeder;

public sealed class SeedContext
{
    private readonly Dictionary<string, Guid> _ids = new();

    public void Set(string key, Guid id) => _ids[key] = id;

    public Guid Get(string key) =>
        _ids.TryGetValue(key, out var id)
            ? id
            : throw new InvalidOperationException($"Seed ID '{key}' bulunamadı. Önceki adım başarısız olmuş olabilir.");

    public bool Has(string key) => _ids.ContainsKey(key);
}
