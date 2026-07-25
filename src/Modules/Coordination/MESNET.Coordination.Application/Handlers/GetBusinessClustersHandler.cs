using Marten;
using MESNET.Coordination.Application.Dtos;
using MESNET.Coordination.Application.Queries;

namespace MESNET.Coordination.Application.Handlers;

public static class GetBusinessClustersHandler
{
    /// <summary>
    /// DBSCAN kümeleme sorgusu.
    /// ST_ClusterDBSCAN geometry(SRID 4326) ile derece biriminde çalışır.
    /// eps parametresi metre → derece dönüşümü SQL içinde yapılır:
    ///   eps_degrees = eps_meters / (111320 * cos(radians(avg_lat)))
    /// avg_lat: tüm işletmelerin ortalama enlemi (CTE ile hesaplanır).
    /// BranchCode filtresi opsiyonel — NULL geçilirse tüm alanlar döner.
    /// </summary>
    private const string ClusterSql = """
        WITH avg_lat AS (
            SELECT AVG((data->'location'->>'latitude')::float8) AS lat
            FROM coordination.mt_doc_businesscoordinationview
            WHERE (data->>'institutionId')::uuid = @institutionId
              AND data->'location' IS NOT NULL
              AND data->'location'->>'latitude' IS NOT NULL
              AND (@branchCode::text IS NULL OR data->>'branchCode' = @branchCode::text)
        )
        SELECT
            (data->>'id')::uuid                                        AS business_id,
            data->>'name'                                              AS business_name,
            (data->'location'->>'latitude')::float8                   AS latitude,
            (data->'location'->>'longitude')::float8                  AS longitude,
            data->>'district'                                          AS district,
            data->>'branchCode'                                        AS branch_code,
            data->>'branchName'                                        AS branch_name,
            data->>'assignedTeacherName'                               AS assigned_teacher_name,
            CASE WHEN data->>'assignedTeacherId' IS NOT NULL
                 THEN true ELSE false END                              AS is_assigned,
            COALESCE((data->>'activeStudentCount')::int, 0)            AS active_student_count,
            (data->>'distanceToSchoolKm')::float8                     AS distance_km,
            COALESCE((data->>'maxCoordinationHours')::int, 0)          AS max_coordination_hours,
            ST_ClusterDBSCAN(
                ST_SetSRID(
                    ST_MakePoint(
                        (data->'location'->>'longitude')::float8,
                        (data->'location'->>'latitude')::float8
                    ), 4326
                ),
                eps := (@eps::float8 / (111320.0 * cos(radians((SELECT lat FROM avg_lat))))),
                minpoints := @minPoints::int
            ) OVER ()                                                  AS cluster_id
        FROM coordination.mt_doc_businesscoordinationview
        WHERE (data->>'institutionId')::uuid = @institutionId
          AND data->'location' IS NOT NULL
          AND data->'location'->>'latitude' IS NOT NULL
          AND data->'location'->>'longitude' IS NOT NULL
          AND (@branchCode IS NULL OR data->>'branchCode' = @branchCode)
        ORDER BY cluster_id NULLS LAST, business_name
        """;

    public static async Task<List<BusinessClusterDto>> Handle(
        GetBusinessClusters query,
        IDocumentStore store,
        CancellationToken cancellationToken)
    {
        var result = new List<BusinessClusterDto>();

        // Raw ADO.NET, Marten'ın tembel tablo oluşturmasını atlar — yeni veritabanında tablo
        // henüz yoksa sorgu 42P01 ile düşüp HTTP 500 üretir. Şemayı açıkça garanti et (#67).
        await store.Storage.Database.EnsureStorageExistsAsync(
            typeof(Core.ReadModels.BusinessCoordinationView), cancellationToken);

        var conn = store.Storage.Database.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        await using (conn)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = ClusterSql;
            cmd.Parameters.AddWithValue("institutionId", query.InstitutionId);
            cmd.Parameters.AddWithValue("eps", query.EpsMeters);
            cmd.Parameters.AddWithValue("minPoints", query.MinPoints);
            cmd.Parameters.AddWithValue("branchCode", (object?)query.BranchCode ?? DBNull.Value);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var branchCodeOrd = reader.GetOrdinal("branch_code");
                var branchNameOrd = reader.GetOrdinal("branch_name");

                result.Add(new BusinessClusterDto(
                    BusinessId: reader.GetGuid(reader.GetOrdinal("business_id")),
                    BusinessName: reader.GetString(reader.GetOrdinal("business_name")),
                    Latitude: reader.GetDouble(reader.GetOrdinal("latitude")),
                    Longitude: reader.GetDouble(reader.GetOrdinal("longitude")),
                    District: reader.IsDBNull(reader.GetOrdinal("district"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("district")),
                    BranchCode: reader.IsDBNull(branchCodeOrd)
                        ? string.Empty
                        : reader.GetString(branchCodeOrd),
                    BranchName: reader.IsDBNull(branchNameOrd)
                        ? string.Empty
                        : reader.GetString(branchNameOrd),
                    ClusterId: reader.IsDBNull(reader.GetOrdinal("cluster_id"))
                        ? null
                        : reader.GetInt32(reader.GetOrdinal("cluster_id")),
                    AssignedTeacherName: reader.IsDBNull(reader.GetOrdinal("assigned_teacher_name"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("assigned_teacher_name")),
                    IsAssigned: reader.GetBoolean(reader.GetOrdinal("is_assigned")),
                    ActiveStudentCount: reader.GetInt32(reader.GetOrdinal("active_student_count")),
                    DistanceToSchoolKm: reader.IsDBNull(reader.GetOrdinal("distance_km"))
                        ? null
                        : reader.GetDouble(reader.GetOrdinal("distance_km")),
                    MaxCoordinationHours: reader.GetInt32(reader.GetOrdinal("max_coordination_hours"))
                ));
            }
        }

        return result;
    }
}
