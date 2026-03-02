using Marten;
using MESNET.Coordination.Application.Dtos;
using MESNET.Coordination.Application.Queries;

namespace MESNET.Coordination.Application.Handlers;

public static class GetBusinessClustersHandler
{
    private const string ClusterSql = """
        SELECT
            (data->>'Id')::uuid                                        AS business_id,
            data->>'Name'                                              AS business_name,
            (data->'Location'->>'Latitude')::float8                   AS latitude,
            (data->'Location'->>'Longitude')::float8                  AS longitude,
            data->>'District'                                          AS district,
            data->>'BranchCode'                                        AS branch_code,
            data->>'BranchName'                                        AS branch_name,
            data->>'AssignedTeacherName'                               AS assigned_teacher_name,
            CASE WHEN data->>'AssignedTeacherId' IS NOT NULL
                 THEN true ELSE false END                              AS is_assigned,
            COALESCE((data->>'ActiveStudentCount')::int, 0)            AS active_student_count,
            (data->>'DistanceToSchoolKm')::float8                     AS distance_km,
            ST_ClusterDBSCAN(
                ST_SetSRID(
                    ST_MakePoint(
                        (data->'Location'->>'Longitude')::float8,
                        (data->'Location'->>'Latitude')::float8
                    ), 4326
                )::geography::geometry,
                eps := @eps,
                minpoints := @minPoints
            ) OVER ()                                                  AS cluster_id
        FROM coordination.mt_doc_businesscoordinationview
        WHERE (data->>'InstitutionId')::uuid = @institutionId
          AND (data->>'AcademicPeriodId')::uuid = @academicPeriodId
          AND data->'Location' IS NOT NULL
          AND data->'Location'->>'Latitude' IS NOT NULL
          AND data->'Location'->>'Longitude' IS NOT NULL
        ORDER BY cluster_id NULLS LAST, business_name
        """;

    public static async Task<List<BusinessClusterDto>> Handle(
        GetBusinessClusters query,
        IDocumentStore store,
        CancellationToken cancellationToken)
    {
        var result = new List<BusinessClusterDto>();

        var conn = store.Storage.Database.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        await using (conn)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = ClusterSql;
            cmd.Parameters.AddWithValue("institutionId", query.InstitutionId);
            cmd.Parameters.AddWithValue("academicPeriodId", query.AcademicPeriodId);
            cmd.Parameters.AddWithValue("eps", query.EpsMeters);
            cmd.Parameters.AddWithValue("minPoints", query.MinPoints);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(new BusinessClusterDto(
                    BusinessId: reader.GetGuid(reader.GetOrdinal("business_id")),
                    BusinessName: reader.GetString(reader.GetOrdinal("business_name")),
                    Latitude: reader.GetDouble(reader.GetOrdinal("latitude")),
                    Longitude: reader.GetDouble(reader.GetOrdinal("longitude")),
                    District: reader.IsDBNull(reader.GetOrdinal("district"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("district")),
                    BranchCode: reader.GetString(reader.GetOrdinal("branch_code")),
                    BranchName: reader.GetString(reader.GetOrdinal("branch_name")),
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
                        : reader.GetDouble(reader.GetOrdinal("distance_km"))
                ));
            }
        }

        return result;
    }
}
