using System.Text.Json.Serialization;

namespace LazerRelaxLeaderboard.OsuApi.Models;

public class BeatmapSet
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("artist")]
    public string Artist { get; set; } = null!;

    [JsonPropertyName("title")]
    public string Title { get; set; } = null!;

    [JsonPropertyName("user_id")]
    public int CreatorId { get; set; }
}
