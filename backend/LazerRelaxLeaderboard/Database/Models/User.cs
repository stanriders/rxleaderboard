using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LazerRelaxLeaderboard.Database.Models
{
    public class User
    {
        [Key]
        public required int Id { get; set; }

        public required string CountryCode { get; set; } = null!;

        public required string Username { get; set; } = null!;

        public double? TotalPp { get; set; }
        public double? TotalAccuracy { get; set; }

        public required DateTime? UpdatedAt { get; set; }

        [InverseProperty(nameof(Score.User))]
        [JsonIgnore]
        public List<Score> Scores { get; set; } = null!;
    }
}
