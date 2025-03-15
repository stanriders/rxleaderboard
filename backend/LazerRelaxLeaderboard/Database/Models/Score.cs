using LazerRelaxLeaderboard.OsuApi.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LazerRelaxLeaderboard.Database.Models
{
    public class Score
    {
        [Key] 
        public required long Id { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public required int BeatmapId { get; set; }
        public Beatmap? Beatmap { get; set; } = null!;

        public required Grade Grade { get; set; }

        public required double Accuracy { get; set; }

        public required int Combo { get; set; }

        public required string[] Mods { get; set; } = Array.Empty<string>();

        public required DateTime Date { get; set; }

        public required int TotalScore { get; set; }

        public required int Count50 { get; set; }

        public required int Count100 { get; set; }

        public required int Count300 { get; set; }

        public required int CountMiss { get; set; }

        public required int? SpinnerBonus { get; set; }

        public required int? SpinnerSpins { get; set; }

        public required int? LegacySliderEnds { get; set; }

        public required int? SliderTicks { get; set; }

        public required int? SliderEnds { get; set; }

        public required int? LegacySliderEndMisses { get; set; }

        public required int? SliderTickMisses { get; set; }

        public double? Pp { get; set; }

        public required bool IsBest { get; set; }

        public bool Hidden { get; set; }
    }
}
