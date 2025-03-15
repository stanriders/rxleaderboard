using LazerRelaxLeaderboard.Database.Models;

namespace LazerRelaxLeaderboard.Contracts;

public class PlayersResult
{
    public List<User> Players { get; set; } = new();
    public int Total { get; set; }
}
