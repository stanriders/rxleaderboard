using LazerRelaxLeaderboard.Database.Models;

namespace LazerRelaxLeaderboard.Contracts;

public class PlayersDataResponse : User
{
    public required int? Rank { get; set; }
    public required int Playcount { get; set; }
    public required int CountSS { get; set; }
    public required int CountS { get; set; }
    public required int CountA { get; set; }
    public required PlaycountPerMonth[] PlaycountsPerMonth { get; set; }
}

public class PlaycountPerMonth
{
    public DateTime Date { get; set; }
    public int Playcount { get; set; }
}
