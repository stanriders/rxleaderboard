using System.Text.Json.Serialization;

namespace LazerRelaxLeaderboard.OsuApi.Models
{
    public class Beatmap
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("beatmapset")] 
        public BeatmapSet BeatmapSet { get; set; } = null!;

        [JsonPropertyName("version")]
        public string Version { get; set; } = null!;

        [JsonPropertyName("ar")]
        public double ApproachRate { get; set; }

        [JsonPropertyName("accuracy")]
        public double OverallDifficulty { get; set; }

        [JsonPropertyName("cs")]
        public double CircleSize { get; set; }

        [JsonPropertyName("drain")]
        public double HealthDrain { get; set; }

        [JsonPropertyName("bpm")]
        public double BeatsPerMinute { get; set; }

        [JsonPropertyName("count_circles")]
        public int Circles { get; set; }

        [JsonPropertyName("count_sliders")]
        public int Sliders { get; set; }

        [JsonPropertyName("count_spinners")]
        public int Spinners { get; set; }

        [JsonPropertyName("difficulty_rating")]
        public double StarRating { get; set; }
    }
}
