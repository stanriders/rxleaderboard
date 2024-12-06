namespace LazerRelaxLeaderboard.OsuApi.Models
{
    public class ScoresResponse
    {
        public List<Score> Scores { get; set; } = null!;
        public string CursorString { get; set; } = null!;
    }
}
