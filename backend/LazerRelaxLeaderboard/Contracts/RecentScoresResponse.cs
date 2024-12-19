using LazerRelaxLeaderboard.Database.Models;

namespace LazerRelaxLeaderboard.Contracts;

public class RecentScoresResponse
{
    public required List<Score> Scores { get; set; } = new();
    public required int ScoresToday { get; set; }
}
