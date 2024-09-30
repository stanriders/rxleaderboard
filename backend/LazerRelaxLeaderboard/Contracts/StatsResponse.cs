namespace LazerRelaxLeaderboard.Contracts;

public class StatsResponse
{
    public int ScoresTotal { get; set; }
    public int UsersTotal { get; set; }
    public int BeatmapsTotal { get; set; }
    public double UpdateRunLengthEstimate { get; set; }
}
