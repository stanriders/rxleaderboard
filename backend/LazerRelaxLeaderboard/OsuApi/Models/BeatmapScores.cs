using System.Text.Json.Serialization;

namespace LazerRelaxLeaderboard.OsuApi.Models;

public class BeatmapScores
{
    [JsonPropertyName("scores")]
    public Score[] Scores { get; set; } = null!;
}
