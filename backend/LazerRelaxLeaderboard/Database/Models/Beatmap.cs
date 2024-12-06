using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using LazerRelaxLeaderboard.OsuApi.Models;

namespace LazerRelaxLeaderboard.Database.Models
{
    public class Beatmap
    {
        [Key]
        public required int Id { get; set; }

        public required string Artist { get; set; } = null!;
        
        public required string Title { get; set; } = null!;
        
        public required int CreatorId { get; set; }

        public required int BeatmapSetId { get; set; }
        
        public required string DifficultyName { get; set; } = null!;
        
        public required double ApproachRate { get; set; }
        
        public required double OverallDifficulty { get; set; }
        
        public required double CircleSize { get; set; }
        
        public required double HealthDrain { get; set; }
        
        public required double BeatsPerMinute { get; set; }
        
        public required int Circles { get; set; }
        
        public required int Sliders { get; set; }
        
        public required int Spinners { get; set; }
        
        public required double StarRatingNormal { get; set; }

        public double? StarRating { get; set; }

        [Obsolete]
        public DateTime ScoresUpdatedOn { get; set; }

        public required BeatmapStatus Status { get; set; }

        public required int MaxCombo { get; set; }

        [InverseProperty(nameof(Score.Beatmap))]
        [JsonIgnore]
        public List<Score> Scores { get; set; } = null!;
    }
}
