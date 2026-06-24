using Marten;
using MESNET.Business.Application.Dtos;
using MESNET.Business.Application.Extensions;
using MESNET.Business.Application.Queries;
using MESNET.Business.Core.Enums;

namespace MESNET.Business.Application.Handlers;

public static class SearchNearbyBusinessesHandler
{
    // PostGIS ST_DWithin ile yarıçap filtresi DB tarafında yapılır (eski bellek-içi haversine yerine).
    // Proje konvansiyonu (bkz. Coordination/GetBusinessClustersHandler): Marten store'dan raw ADO.NET
    // connection, SQL'de alias'sız `data`, named @param. SmartEnum (Status) düz string serialize
    // edildiğinden `statusName` düz string alanı kullanılır. Önce eşleşen id'ler çekilir, sonra
    // dokümanlar Marten ile yüklenip mevcut ToDto() ile maplenir.
    private const string NearbyIdsSql = """
        SELECT id FROM business.mt_doc_business
        WHERE data->>'statusName' = @status
          AND data->'location' IS NOT NULL
          AND ST_DWithin(
                ST_MakePoint((data->'location'->>'longitude')::float8, (data->'location'->>'latitude')::float8)::geography,
                ST_MakePoint(@lng, @lat)::geography,
                @radiusMeters)
        """;

    public static async Task<IReadOnlyList<BusinessDto>> Handle(
        SearchNearbyBusinesses query,
        IDocumentStore store,
        IQuerySession session,
        CancellationToken cancellationToken)
    {
        var ids = new List<Guid>();

        var conn = store.Storage.Database.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        await using (conn)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = NearbyIdsSql;
            cmd.Parameters.AddWithValue("status", BusinessStatus.Active.Name);
            cmd.Parameters.AddWithValue("lng", query.Longitude);
            cmd.Parameters.AddWithValue("lat", query.Latitude);
            cmd.Parameters.AddWithValue("radiusMeters", query.RadiusKm * 1000.0);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                ids.Add(reader.GetGuid(0));
        }

        if (ids.Count == 0)
            return [];

        var businesses = await session.LoadManyAsync<Core.Entities.Business>(ids);
        return businesses.Select(b => b.ToDto()).ToList();
    }
}
