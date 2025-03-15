namespace LazerRelaxLeaderboard.Services;

public interface IPpService
{
    Task PopulateScores(bool recalculateAll = false);

    Task PopulateStarRatings();
    Task RecalculateStarRatings();

    Task RecalculatePlayersPp();
    Task RecalculatePlayersPp(List<int> playerIds);

    Task RecalculateBestScores();
    Task RecalculateBestScores(List<int> players);

    Task CleanupScores();
}
