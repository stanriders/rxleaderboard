using System.Text.Json.Serialization;
using osu.Game.Online.API;
using osu.Game.Rulesets.Mods;

namespace LazerRelaxLeaderboard.OsuApi.Models
{
    public class Score
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("user")]
        public User User { get; set; } = null!;

        [JsonPropertyName("beatmap_id")]
        public int BeatmapId { get; set; }
        
        [JsonPropertyName("rank")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Grade Grade { get; set; }
        
        [JsonPropertyName("accuracy")]
        public double Accuracy { get; set; }

        [JsonPropertyName("max_combo")]
        public int Combo { get; set; }

        [JsonPropertyName("mods")]
        public APIMod[] Mods { get; set; } = Array.Empty<APIMod>();

        [JsonPropertyName("ended_at")]
        public DateTime Date { get; set; }

        [JsonPropertyName("total_score")]
        public int TotalScore { get; set; }

        [JsonPropertyName("statistics")]
        public ScoreStatistics Statistics { get; set; } = null!;

        public class ScoreStatistics
        {
            [JsonPropertyName("meh")]
            public int Count50 { get; set; }

            [JsonPropertyName("ok")]
            public int Count100 { get; set; }

            [JsonPropertyName("great")]
            public int Count300 { get; set; }

            [JsonPropertyName("miss")]
            public int CountMiss { get; set; }
        }
    }
}
