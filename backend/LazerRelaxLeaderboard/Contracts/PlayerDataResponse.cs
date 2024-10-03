using LazerRelaxLeaderboard.Database.Models;

namespace LazerRelaxLeaderboard.Contracts;

    public class PlayersDataResponse : User
    {
        public required int? Rank { get; set; }
    }
