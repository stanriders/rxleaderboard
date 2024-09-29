using System.Text.Json.Serialization;

namespace LazerRelaxLeaderboard.OsuApi.Models
{
    public class GetUserlessTokenRequest
    {
        [JsonPropertyName("client_id")]
        public required int ClientId { get; set; }

        [JsonPropertyName("client_secret")]
        public required string ClientSecret { get; set; } = null!;

        [JsonPropertyName("grant_type")]
        public string GrantType { get; set; } = "client_credentials";

        [JsonPropertyName("scope")]
        public string Scope { get; set; } = "public";
    }
}
