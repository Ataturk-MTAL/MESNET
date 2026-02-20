namespace MESNET.Seeder;

public sealed class SeederOptions
{
    public string ApiBaseUrl { get; set; } = "http://localhost:5226";
    public string KeycloakTokenUrl { get; set; } = "http://localhost:8080/realms/mesnet/protocol/openid-connect/token";
    public string ClientId { get; set; } = "mesnet-api";
    public string ClientSecret { get; set; } = "dev-secret";
    public string? Username { get; set; } = "admin";
    public string? Password { get; set; } = "admin";
}
