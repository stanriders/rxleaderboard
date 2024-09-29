using System.Text.Json.Serialization;

namespace LazerRelaxLeaderboard.OsuApi.Models
{
    public class Modsss
    {
        [JsonPropertyName("acronym")]
        public string Acronym { get; set; } = null!;

        //public Dictionary<string, string>? Settings { get; set; }
    }
}
