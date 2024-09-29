using LazerRelaxLeaderboard.Database.Models;

namespace LazerRelaxLeaderboard.Contracts
{
    public class PlayersResult
    {
        public List<User> Players { get; set; } = new List<User>();
        public int Total { get; set; }
    }
}
