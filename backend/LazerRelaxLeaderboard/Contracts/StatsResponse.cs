namespace LazerRelaxLeaderboard.Contracts;

public class StatsResponse
{
    public required int ScoresTotal { get; set; }
    public required int UsersTotal { get; set; }
    public required int BeatmapsTotal { get; set; }
    public required long LatestScoreId { get; set; }
    public required int ScoresInAMonth { get; set; }
    public required List<PlaycountPerMonth> PlaycountPerDay { get; set; }
    public required List<PlaycountPerMonth> PlaycountPerMonth { get; set; }
}
