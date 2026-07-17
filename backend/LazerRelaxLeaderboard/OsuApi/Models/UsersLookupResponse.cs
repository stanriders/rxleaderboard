using System.Text.Json.Serialization;

namespace LazerRelaxLeaderboard.OsuApi.Models;

public class UsersLookupResponse
{
    [JsonPropertyName("users")]
    public List<User> Users { get; set; }
}
